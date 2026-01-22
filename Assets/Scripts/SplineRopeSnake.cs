using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 基于 Unity Spline 的可伸缩弯曲绳子控制器。
/// 特点：支持固定半径 90 度圆角、骨骼自适应分布（弯道密集、直线稀疏）、防抖动处理。
/// </summary>
public class SplineRopeSnake : MonoBehaviour, IArrow
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
    [Tooltip("位置平滑系数：用于消除出弯时的微小抖动，0为禁用")]
    public float positionSmoothing = 0.1f;
    
    [Range(0f, 1f)]
    [Tooltip("旋转平滑系数：用于消除急转弯处的旋转扭曲，0为禁用")]
    public float rotationSmoothing = 0.3f;

    [Header("路径点 (世界空间)")]
    public List<Vector3> waypoints = new List<Vector3>();

    // 静态密度分布图数据，用于固定骨骼分布逻辑，防止移动时抖动
    private List<float> splineDistances = new List<float>();
    private List<float> splineWeights = new List<float>();
    private float totalPathWeight = 0;
    private const int DENSITY_SAMPLES_PER_UNIT = 20; // 每单位长度的采样点密度（提高精度）

    void Awake()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
            
        // 如果初始有路点，生成路径
        if (waypoints.Count >= 2) 
            UpdateSplineFromWaypoints();
    }

    void Update()
    {
        // 更新移动进度
        if (isMoving)
        {
            currentDistance += moveSpeed * Time.deltaTime;
        
            // 每一帧更新骨骼位置和旋转
            UpdateBones();

            // isMoving = false;
        }
        
    }

    public void StartMoving(float distance)
    {
        if (isMoving)
            return;
        isMoving = true;
    }

    /// <summary>
    /// 设置新的路点并刷新路径
    /// </summary>
    /// <param name="points">世界空间的路点列表</param>
    /// <param name="resetDistance">是否将绳子重置到路径起点</param>
    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        waypoints = points;
        UpdateSplineFromWaypoints();
        if (resetDistance)
            currentDistance = baseLength + initialDistanceOffset;
    }

    /// <summary>
    /// 根据 waypoints 生成带有固定半径圆角的平滑 Spline 路径
    /// </summary>
    [ContextMenu("Update Spline")]
    public void UpdateSplineFromWaypoints()
    {
        if (waypoints.Count < 2 || splineContainer == null) return;

        Spline spline = splineContainer.Spline;
        spline.Clear();

        // 1. 将世界坐标路点转换到 SplineContainer 的本地空间，保证缩放和位移一致性
        List<Vector3> localPoints = new List<Vector3>();
        foreach (var wp in waypoints)
            localPoints.Add(splineContainer.transform.InverseTransformPoint(wp));

        // 2. 添加起始点
        spline.Add(new BezierKnot(localPoints[0]));

        // 3. 处理中间的转弯点
        for (int i = 1; i < localPoints.Count - 1; i++)
        {
            Vector3 prev = localPoints[i - 1];
            Vector3 curr = localPoints[i];
            Vector3 next = localPoints[i + 1];

            Vector3 dirIn = (curr - prev).normalized;
            Vector3 dirOut = (next - curr).normalized;
            
            // 计算两条线段的夹角
            float angle = Vector3.Angle(dirIn, dirOut);

            // 如果角度变化大于阈值，则生成圆弧
            if (angle > 0.01f)
            {
                float alphaRad = angle * Mathf.Deg2Rad;
                
                // 计算从转角顶点到切点的距离 D = R * tan(theta/2)
                float tangentDist = filletRadius * Mathf.Tan(alphaRad * 0.5f);
                
                // 限制：圆角切点不能超过线段长度的 45%，防止连续转弯导致的路径重叠
                float distPrev = Vector3.Distance(prev, curr);
                float distNext = Vector3.Distance(curr, next);
                float maxAllowedDist = Mathf.Min(distPrev, distNext) * 0.45f;
                
                float actualDist = Mathf.Min(tangentDist, maxAllowedDist);
                // 如果实际距离被压缩了，重新推算对应的实际半径
                float actualRadius = actualDist / Mathf.Tan(alphaRad * 0.5f);

                // 计算圆弧的两个端点 (切点)
                Vector3 p1 = curr - dirIn * actualDist;
                Vector3 p2 = curr + dirOut * actualDist;
                
                // 计算 Bezier 句柄长度：h = (4/3) * tan(theta/4) * R
                float handleLen = (4f / 3f) * Mathf.Tan(alphaRad * 0.25f) * actualRadius;

                // 添加圆弧起点：TangentOut 指向转角顶点方向
                BezierKnot knot1 = new BezierKnot(p1);
                knot1.TangentOut = (float3)(dirIn * handleLen);
                spline.Add(knot1);

                // 添加圆弧终点：TangentIn 从转角顶点方向指回
                BezierKnot knot2 = new BezierKnot(p2);
                knot2.TangentIn = (float3)(-dirOut * handleLen);
                spline.Add(knot2);
            }
            else
            {
                // 直线点，直接添加
                spline.Add(new BezierKnot(curr));
            }
        }
        
        // 4. 添加终点
        spline.Add(new BezierKnot(localPoints[localPoints.Count - 1]));

        // 5. 路径改变后，立即重建密度图
        BuildStaticDensityMap();
    }

    /// <summary>
    /// 预计算整条路径的"疏密权重图"。
    /// 核心逻辑：在转弯半径处采样更高权重，使骨骼自动聚集在弯道。
    /// 使用静态图代替动态采样，彻底消除移动时的数值抖动。
    /// </summary>
    void BuildStaticDensityMap()
    {
        splineDistances.Clear();
        splineWeights.Clear();
        totalPathWeight = 0;
    
        float fullLength = splineContainer.CalculateLength();
        // 根据总长度决定采样精度（提高采样密度以更准确捕捉曲率）
        int sampleCount = Mathf.Max(30, Mathf.CeilToInt(fullLength * DENSITY_SAMPLES_PER_UNIT));
    
        for (int i = 0; i <= sampleCount; i++)
        {
            float d = (i / (float)sampleCount) * fullLength;
            float weight = 1.0f; // 基础权重
    
            // 评估当前位置的曲率
            float normT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, d, PathIndexUnit.Distance);
            splineContainer.Evaluate(normT, out _, out float3 tangent, out _);
                
            // 缩短采样步长以更精确捕捉急转弯（从0.1减小到0.05）
            float nextD = Mathf.Min(d + 0.05f, fullLength);
            float nextNormT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, nextD, PathIndexUnit.Distance);
            splineContainer.Evaluate(nextNormT, out _, out float3 nextTangent, out _);
                
            // 夹角越大，权重越高（使用指数增强曲率敏感度）
            float angleDiff = Vector3.Angle(tangent, nextTangent);
            weight += Mathf.Pow(angleDiff, 1.5f) * curvatureSensitivity;
    
            splineDistances.Add(d);
            splineWeights.Add(weight);
            totalPathWeight += weight;
        }
    }
    
    /// <summary>
    /// 计算并应用骨骼的位置和旋转
    /// </summary>
    public void UpdateBones()
    {
        if (bones == null || bones.Count == 0 || splineWeights.Count == 0) return;

        float fullLength = splineContainer.CalculateLength();
        float currentRopeLength = baseLength * stretchMultiplier;

        // 1. 获取绳子头部和尾部在权重坐标系中的累积权重位置
        float headWeightPos = GetAccumulatedWeightAtPos(currentDistance);
        float tailWeightPos = GetAccumulatedWeightAtPos(currentDistance - currentRopeLength);
        
        // 绳子当前占据的总权重跨度
        float weightSpan = headWeightPos - tailWeightPos;

        // 2. 遍历骨骼，根据权重比例非线性地分配到 Spline 路径上
        for (int i = 0; i < bones.Count; i++)
        {
            // 计算骨骼在权重维度上的目标位置
            float t_bone = (bones.Count > 1) ? i / (float)(bones.Count - 1) : 0;
            float targetWeight = headWeightPos - (t_bone * weightSpan);
            
            // 将权重位置反推回实际的物理距离 (Distance)
            float actualDist = GetDistFromAccumulatedWeight(targetWeight);

            // 3. 将物理距离转换为 Spline 的 0-1 参数 t，并更新 Transform
            if (actualDist < 0) 
                EvaluateAndApply(i, 0);
            else if (actualDist > fullLength) 
                EvaluateAndApply(i, 1.0f);
            else 
                EvaluateAndApply(i, SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, actualDist, PathIndexUnit.Distance));
        }
    }

    /// <summary>
    /// 给定一个物理距离，计算其在路径上的累积权重
    /// </summary>
    float GetAccumulatedWeightAtPos(float dist)
    {
        if (dist <= 0) return 0;
        float currentAcc = 0;
        for (int i = 0; i < splineDistances.Count - 1; i++)
        {
            if (dist <= splineDistances[i + 1])
            {
                // 在两个采样点之间做线性插值，保证移动平滑
                float t = (dist - splineDistances[i]) / (splineDistances[i + 1] - splineDistances[i]);
                return currentAcc + splineWeights[i] * t;
            }
            currentAcc += splineWeights[i];
        }
        return totalPathWeight;
    }

    /// <summary>
    /// 给定一个累积权重值，反推其在路径上的物理距离
    /// </summary>
    float GetDistFromAccumulatedWeight(float targetWeight)
    {
        if (targetWeight <= 0) return 0;
        float currentAcc = 0;
        for (int i = 0; i < splineWeights.Count - 1; i++)
        {
            if (targetWeight <= currentAcc + splineWeights[i])
            {
                // 在权重区间内做插值反推物理距离
                float t = (targetWeight - currentAcc) / splineWeights[i];
                return Mathf.Lerp(splineDistances[i], splineDistances[i + 1], t);
            }
            currentAcc += splineWeights[i];
        }
        return splineDistances[splineDistances.Count - 1];
    }

    /// <summary>
    /// 评估 Spline 上参数 t 的位置和切线，并应用到指定骨骼。
    /// 包含位置平滑逻辑和切线对齐修正。
    /// </summary>
    void EvaluateAndApply(int boneIndex, float t)
    {
        splineContainer.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
        
        // 转换到世界坐标空间
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);
        
        // 应用位置平滑 (positionSmoothing)，消除由于数值计算导致的微小高频抖动
        // 仅在移动时启用平滑，初始化时立即定位
        if (positionSmoothing > 0 && Application.isPlaying && isMoving)
            bones[boneIndex].position = Vector3.Lerp(bones[boneIndex].position, worldPos, 1f - positionSmoothing);
        else
            bones[boneIndex].position = worldPos;

        // 计算骨骼朝向
        if (math.lengthsq(tan) > 0.001f)
        {
            Vector3 forward = splineContainer.transform.TransformDirection(tan);
            
            // 计算目标旋转（锁定 Up 轴为世界坐标向上）
            Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
            
            // 应用旋转平滑，减少急转弯处的扭曲
            // 初始化时立即应用，移动时启用平滑过渡
            if (rotationSmoothing > 0 && Application.isPlaying && isMoving)
            {
                bones[boneIndex].rotation = Quaternion.Slerp(bones[boneIndex].rotation, targetRotation, 1f - rotationSmoothing);
            }
            else
            {
                bones[boneIndex].rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// 设置新的移动目的地
    /// </summary>
    public void SetPath(List<Vector3> newWaypoints)
    {
        waypoints = new List<Vector3>(newWaypoints);
        UpdateSplineFromWaypoints();
        currentDistance = 0f;
    }

    /// <summary>
    /// 模拟贪吃蛇式增长
    /// </summary>
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
        // 添加偏移量，确保绳子大部分显示在路径上（避免尾部在起点堆积）
        this.currentDistance = data.pathLength + initialDistanceOffset;
    }
    
    public Transform Transform => this.transform;
    
    bool IArrow.IsMoving
    {
        get => isMoving;
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
        // 禁用位置平滑，确保初始化时骨骼立即到位
        float originalSmoothing = positionSmoothing;
        positionSmoothing = 0;
        
        // 初始阶段更新骨骼，立即定位（位置和旋转）
        UpdateBones();
        
        // 强制再次更新，确保旋转完全应用
        // 某些情况下 SkinnedMeshRenderer 会在第一帧覆盖骨骼旋转
        UpdateBones();
        
        // 延迟一帧再次确认，彻底解决 SkinnedMeshRenderer 的初始化问题
        StartCoroutine(DelayedBoneUpdate(originalSmoothing));
    }
    
    private IEnumerator DelayedBoneUpdate(float originalSmoothing)
    {
        // 等待一帧，让 SkinnedMeshRenderer 完成内部初始化
        yield return null;
        
        // 最终更新，确保所有骨骼位置和旋转都正确
        UpdateBones();
        
        // 恢复平滑设置
        positionSmoothing = originalSmoothing;
    }
}
