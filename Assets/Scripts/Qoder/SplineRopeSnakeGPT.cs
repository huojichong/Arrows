using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 基于 Unity Spline 的可伸缩弯曲绳子控制器（工业级稳定版本）。
/// 特点：
// — 支持固定半径圆角（90° 可调）
// — 骨骼自适应分布（弯道密集、直线稀疏）
// — 静态权重积分，消除移动抖动
// — 平行传输帧保证旋转连续
// — 支持动态伸缩与移动
/// </summary>
public class SplineRopeSnakeGPT : MonoBehaviour, IArrow
{
    [Header("Spline 核心配置")]
    [Tooltip("用于渲染和路径计算的 Spline 容器")]
    public SplineContainer splineContainer;

    [Tooltip("拐弯处的固定圆角半径")]
    public float filletRadius = 0.5f;

    [Header("绳子与骨骼")]
    [Tooltip("绳子的骨骼列表，头部为 index 0")]
    public List<Transform> bones;

    [Tooltip("绳子在路径上的固定长度")]
    public float baseLength = 2.0f;

    [Tooltip("初始距离偏移量，用于确保绳子完整显示")]
    public float initialDistanceOffset = 0.1f;

    [Range(0.1f, 10f)]
    [Tooltip("拉伸倍率，用于动态改变绳子长度")]
    public float stretchMultiplier = 1.0f;

    [Header("移动控制")]
    [Tooltip("绳子沿路径移动的速度")]
    public float moveSpeed = 2.0f;

    [Tooltip("是否正在移动")]
    public bool isMoving = false;

    [Tooltip("当前移动的距离（头部相对于路径起点）")]
    public float currentDistance = 0f;

    [Header("自适应分布与稳定性")]
    [Range(0, 30f)]
    [Tooltip("曲率敏感度：值越大，拐弯处的骨骼越密集，直线处越稀疏")]
    public float curvatureSensitivity = 15.0f;

    [Range(0f, 1f)]
    [Tooltip("位置平滑系数：用于消除微小抖动，0 为禁用")]
    public float positionSmoothing = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("旋转平滑系数：减少急转弯处的旋转扭曲，0 为禁用")]
    public float rotationSmoothing = 0.3f;

    [Header("路径点 (世界空间)")]
    public List<Vector3> waypoints = new List<Vector3>();

    // -------------------------
    // 内部数据
    // -------------------------
    private List<float> splineDistances = new List<float>();
    private List<float> splineWeights = new List<float>();
    private List<float> segmentWeights = new List<float>();
    private List<float> paramTable = new List<float>();
    private List<float> weightCDF = new List<float>();
    private float totalPathWeight = 0f;
    private const int DENSITY_SAMPLES_PER_UNIT = 20; // 每单位长度采样点
    private float cachedSplineLength = 0f;
    private Vector3 lastUp = Vector3.up; // 平行传输帧 Up

    void Awake()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (waypoints.Count >= 2)
            UpdateSplineFromWaypoints();
    }

    void Update()
    {
        if (isMoving)
        {
            currentDistance += moveSpeed * Time.deltaTime;
            currentDistance = Mathf.Min(currentDistance, cachedSplineLength + baseLength * stretchMultiplier);
            UpdateBones();
        }
    }

    // -------------------------
    // 公共接口
    // -------------------------
    public void StartMoving(float distance)
    {
        if (isMoving) return;
        isMoving = true;
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        waypoints = points;
        UpdateSplineFromWaypoints();
        if (resetDistance)
            currentDistance = baseLength + initialDistanceOffset;
    }

    public void SetPath(List<Vector3> newWaypoints)
    {
        SetWaypoints(newWaypoints);
        currentDistance = 0f;
    }

    public void AddWaypoint(Vector3 point)
    {
        waypoints.Add(point);
        UpdateSplineFromWaypoints();
    }

    public IArrowData arrowData { get; set; }

    public void SetData(IArrowData arrowData)
    {
        var data = arrowData as ArrowData;
        this.arrowData = data;
        this.baseLength = data.pathLength;
        this.currentDistance = data.pathLength + initialDistanceOffset;
    }

    public IArrowData ArrowData { get; }
    public Transform Transform => this.transform;

    public bool IsMoving
    {
        get
        {
            return isMoving;
        }
        set
        {
            isMoving = value;
        }
    }


    public void Reset()
    {
        StopAllCoroutines();
        isMoving = false;
    }

    public void MoveOut()
    {
        isMoving = true;
    }

    public void InitArrow()
    {
        isMoving = false;
        float originalSmoothing = positionSmoothing;
        positionSmoothing = 0;
        lastUp = Vector3.up;

        UpdateBones();
        UpdateBones();
        StartCoroutine(DelayedBoneUpdate(originalSmoothing));
    }

    private IEnumerator DelayedBoneUpdate(float originalSmoothing)
    {
        yield return null;
        UpdateBones();
        positionSmoothing = originalSmoothing;
    }

    // -------------------------
    // 核心 Spline 构建
    // -------------------------
    [ContextMenu("Update Spline")]
    public void UpdateSplineFromWaypoints()
    {
        if (waypoints.Count < 2 || splineContainer == null) return;

        Spline spline = splineContainer.Spline;
        spline.Clear();

        // 转换为本地坐标
        List<Vector3> localPoints = new List<Vector3>();
        foreach (var wp in waypoints)
            localPoints.Add(splineContainer.transform.InverseTransformPoint(wp));

        // 添加起点
        spline.Add(new BezierKnot(localPoints[0]));

        // 中间拐点处理
        for (int i = 1; i < localPoints.Count - 1; i++)
        {
            Vector3 prev = localPoints[i - 1];
            Vector3 curr = localPoints[i];
            Vector3 next = localPoints[i + 1];

            Vector3 dirIn = (curr - prev).normalized;
            Vector3 dirOut = (next - curr).normalized;

            float angle = Vector3.Angle(dirIn, dirOut);

            if (angle > 0.01f)
            {
                float alphaRad = angle * Mathf.Deg2Rad;
                float tangentDist = filletRadius * Mathf.Tan(alphaRad * 0.5f);
                float distPrev = Vector3.Distance(prev, curr);
                float distNext = Vector3.Distance(curr, next);
                float maxAllowedDist = Mathf.Min(distPrev, distNext) * 0.45f;
                float actualDist = Mathf.Min(tangentDist, maxAllowedDist);
                float actualRadius = actualDist / Mathf.Tan(alphaRad * 0.5f);

                Vector3 p1 = curr - dirIn * actualDist;
                Vector3 p2 = curr + dirOut * actualDist;
                float handleLen = (4f / 3f) * Mathf.Tan(alphaRad * 0.25f) * actualRadius;

                BezierKnot knot1 = new BezierKnot(p1);
                knot1.TangentOut = (float3)(-dirIn * handleLen);
                spline.Add(knot1);

                BezierKnot knot2 = new BezierKnot(p2);
                knot2.TangentIn = (float3)(dirOut * handleLen);
                spline.Add(knot2);
            }
            else
            {
                spline.Add(new BezierKnot(curr));
            }
        }

        spline.Add(new BezierKnot(localPoints[localPoints.Count - 1]));

        cachedSplineLength = splineContainer.CalculateLength();
        BuildStaticDensityMap();
    }

    // -------------------------
    // 静态密度图与权重积分
    // -------------------------
    void BuildStaticDensityMap()
    {
        splineDistances.Clear();
        splineWeights.Clear();
        segmentWeights.Clear();
        paramTable.Clear();
        weightCDF.Clear();
        totalPathWeight = 0f;

        int sampleCount = Mathf.Max(30, Mathf.CeilToInt(cachedSplineLength * DENSITY_SAMPLES_PER_UNIT));

        for (int i = 0; i <= sampleCount; i++)
        {
            float d = (i / (float)sampleCount) * cachedSplineLength;
            splineDistances.Add(d);

            float normT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, d, PathIndexUnit.Distance);
            splineContainer.Evaluate(normT, out _, out float3 tangent, out _);

            float nextD = Mathf.Min(d + 0.05f, cachedSplineLength);
            float nextNormT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, nextD, PathIndexUnit.Distance);
            splineContainer.Evaluate(nextNormT, out _, out float3 nextTangent, out _);

            float angleDiff = Vector3.Angle(tangent, nextTangent);
            float weight = 1f + Mathf.Pow(angleDiff, 1.5f) * curvatureSensitivity;
            splineWeights.Add(weight);
        }

        // 区间权重
        for (int i = 0; i < splineWeights.Count - 1; i++)
        {
            float segW = (splineWeights[i] + splineWeights[i + 1]) * 0.5f;
            segmentWeights.Add(segW);
            totalPathWeight += segW;

            float t = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, splineDistances[i], PathIndexUnit.Distance);
            paramTable.Add(t);
            weightCDF.Add(totalPathWeight);
        }
    }

    float GetSplineParamFromWeight(float targetWeight)
    {
        if (targetWeight <= 0) return paramTable[0];
        if (targetWeight >= totalPathWeight) return paramTable[paramTable.Count - 1];

        for (int i = 0; i < weightCDF.Count; i++)
        {
            if (targetWeight <= weightCDF[i])
            {
                float prev = i == 0 ? 0f : weightCDF[i - 1];
                float t = Mathf.InverseLerp(prev, weightCDF[i], targetWeight);
                return Mathf.Lerp(paramTable[i], paramTable[i + 1], t);
            }
        }

        return paramTable[paramTable.Count - 1];
    }

    // -------------------------
    // 骨骼位置与旋转
    // -------------------------
    public void UpdateBones()
    {
        if (bones == null || bones.Count == 0 || splineWeights.Count == 0) return;

        float currentRopeLength = baseLength * stretchMultiplier;
        float headWeightPos = GetAccumulatedWeightAtPos(currentDistance);
        float tailWeightPos = GetAccumulatedWeightAtPos(currentDistance - currentRopeLength);
        float weightSpan = headWeightPos - tailWeightPos;

        Vector3 lastUpBackup = lastUp;

        for (int i = 0; i < bones.Count; i++)
        {
            float t_bone = (bones.Count > 1) ? i / (float)(bones.Count - 1) : 0f;
            float targetWeight = headWeightPos - t_bone * weightSpan;

            float param = GetSplineParamFromWeight(targetWeight);
            EvaluateAndApply(i, param);
        }

        lastUp = lastUpBackup; // 保持平行传输连续性
    }

    void EvaluateAndApply(int boneIndex, float t)
    {
        splineContainer.Evaluate(t, out float3 pos, out float3 tan, out float3 up);

        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);

        // 位置平滑
        if (positionSmoothing > 0 && Application.isPlaying && isMoving)
            bones[boneIndex].position = Vector3.Lerp(bones[boneIndex].position, worldPos, 1f - positionSmoothing);
        else
            bones[boneIndex].position = worldPos;

        if (math.lengthsq(tan) > 0.001f)
        {
            Vector3 forward = splineContainer.transform.TransformDirection(tan).normalized;

            // 平行传输帧计算 Up
            Vector3 right = Vector3.Cross(lastUp, forward).normalized;
            Vector3 newUp = Vector3.Cross(forward, right).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forward, newUp);

            if (rotationSmoothing > 0 && Application.isPlaying && isMoving)
                bones[boneIndex].rotation = Quaternion.Slerp(bones[boneIndex].rotation, targetRotation, 1f - rotationSmoothing);
            else
                bones[boneIndex].rotation = targetRotation;

            lastUp = newUp;
        }
    }

    float GetAccumulatedWeightAtPos(float dist)
    {
        if (dist <= 0f) return 0f;
        if (dist >= cachedSplineLength) return totalPathWeight;

        float acc = 0f;

        for (int i = 0; i < splineDistances.Count - 1; i++)
        {
            float d0 = splineDistances[i];
            float d1 = splineDistances[i + 1];

            if (dist <= d1)
            {
                float t = Mathf.InverseLerp(d0, d1, dist);
                return acc + segmentWeights[i] * t;
            }

            acc += segmentWeights[i];
        }

        return totalPathWeight;
    }
}
