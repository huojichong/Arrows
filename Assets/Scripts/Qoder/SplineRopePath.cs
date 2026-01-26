using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;


/// <summary>
/// Spline 路径管理器，负责路径生成、密度计算和全局状态管理
/// </summary>
public class SplineRopePath : MonoBehaviour
{
    [Header("Spline 核心配置")]
    [Tooltip("用于渲染和路径计算的 Spline 容器")]
    public SplineContainer splineContainer;
    
    [Tooltip("拐弯处的固定圆角半径")]
    public float filletRadius = 0.5f;
    
    [Header("移动控制")]
    [Tooltip("绳子沿路径移动的速度")]
    public float moveSpeed = 2.0f;
    
    [Tooltip("是否正在移动")]
    public bool isMoving = false;
    
    [Tooltip("当前移动的距离（头部相对于路径起点）")]
    public float currentDistance = 0f;

    [Header("自适应分布")]
    [Range(0, 30f)]
    [Tooltip("曲率敏感度：值越大，拐弯处的骨骼越密集，直线处越稀疏")]
    public float curvatureSensitivity = 15.0f;

    [Header("路径点 (世界空间)")]
    public List<Vector3> waypoints = new List<Vector3>();
    

    private PathData _pathData = new PathData();
    private const int DENSITY_SAMPLES_PER_UNIT = 20;
    
    // 管理的所有 Segment
    private List<SplineRopeSegment> _segments = new List<SplineRopeSegment>();

    public PathData PathDataRef => _pathData;

    void Awake()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
    }

    void Start()
    {
        if (waypoints.Count >= 2)
            UpdateSplineFromWaypoints();
    }

    void Update()
    {
        if (isMoving)
        {
            currentDistance += moveSpeed * Time.deltaTime;
            
            // 通知所有 Segment 更新
            foreach (var segment in _segments)
            {
                segment.UpdateBones(currentDistance, _pathData, splineContainer);
            }
        }
    }

    /// <summary>
    /// 注册一个 Segment 到管理器
    /// </summary>
    public void RegisterSegment(SplineRopeSegment segment)
    {
        if (!_segments.Contains(segment))
            _segments.Add(segment);
    }

    /// <summary>
    /// 注销一个 Segment
    /// </summary>
    public void UnregisterSegment(SplineRopeSegment segment)
    {
        _segments.Remove(segment);
    }

    /// <summary>
    /// 设置新的路点并刷新路径
    /// </summary>
    public void SetWaypoints(List<Vector3> points, float initialDistance = 0f)
    {
        waypoints = points;
        UpdateSplineFromWaypoints();
        currentDistance = initialDistance;
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

        // 1. 将世界坐标路点转换到 SplineContainer 的本地空间
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
                knot1.TangentOut = (float3)(dirIn * handleLen);
                spline.Add(knot1);

                BezierKnot knot2 = new BezierKnot(p2);
                knot2.TangentIn = (float3)(-dirOut * handleLen);
                spline.Add(knot2);
            }
            else
            {
                spline.Add(new BezierKnot(curr));
            }
        }
        
        // 4. 添加终点
        spline.Add(new BezierKnot(localPoints[localPoints.Count - 1]));

        // 5. 重建密度图
        BuildStaticDensityMap();
    }

    /// <summary>
    /// 预计算整条路径的疏密权重图
    /// </summary>
    void BuildStaticDensityMap()
    {
        _pathData.distances.Clear();
        _pathData.weights.Clear();
        _pathData.totalWeight = 0;

        _pathData.fullLength = splineContainer.CalculateLength();
        int sampleCount = Mathf.Max(30, Mathf.CeilToInt(_pathData.fullLength * DENSITY_SAMPLES_PER_UNIT));
    
        for (int i = 0; i <= sampleCount; i++)
        {
            float d = (i / (float)sampleCount) * _pathData.fullLength;
            float weight = CalculateWeightAtDistance(d);

            _pathData.distances.Add(d);
            _pathData.weights.Add(weight);
            _pathData.totalWeight += weight;
        }
    }

    /// <summary>
    /// 计算指定距离处的权重
    /// </summary>
    float CalculateWeightAtDistance(float d)
    {
        float normT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, d, PathIndexUnit.Distance);
        splineContainer.Evaluate(normT, out _, out float3 tangent, out _);
        
        float nextD = Mathf.Min(d + 0.05f, _pathData.fullLength);
        float nextNormT = SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, nextD, PathIndexUnit.Distance);
        splineContainer.Evaluate(nextNormT, out _, out float3 nextTangent, out _);
        
        float angleDiff = Vector3.Angle(tangent, nextTangent);
        return 1.0f + Mathf.Pow(angleDiff, 1.5f) * curvatureSensitivity;
    }

    /// <summary>
    /// 强制所有 Segment 立即更新（用于初始化）
    /// </summary>
    public void ForceUpdateAllSegments()
    {
        foreach (var segment in _segments)
        {
            segment.ForceUpdate(currentDistance, _pathData, splineContainer);
        }
    }
}
