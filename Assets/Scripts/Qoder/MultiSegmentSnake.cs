using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 多段蛇控制器：根据蛇的长度自动创建多个 Segment
/// 每个单位长度创建一个 Segment，实现真正的分段式绳子
/// </summary>
public class MultiSegmentSnake : MonoBehaviour, IArrow
{
    [Header("组件引用")]
    [Tooltip("路径管理器")]
    public SplineRopePath pathManager;
    
    // [Tooltip("Segment 预制件模板")]
    // public GameObject segmentPrefab;
    
    [Tooltip("UnitSnake 引用（用于获取骨骼）")]
    public UnitSnake unitSnake { get; set; }
    
    // [Header("配置")]
    [Tooltip("每个 Segment 的长度（单位）")]
    public float segmentUnitLength { get; set; } = 1.0f;
    
    [Tooltip("初始化距离偏移")]
    public float initialDistanceOffset = 0.1f;
    
    [Tooltip("每个 Segment 的骨骼数量")]
    public int bonesPerSegment { get; set; } = 5;
    
    [Tooltip("骨骼分配模式")]
    public SplineRopeSegment.BoneDistributionMode distributionMode { get; set; } = SplineRopeSegment.BoneDistributionMode.Uniform;
    
    // IArrow 接口实现
    public IArrowData arrowData { get; set; }
    public IArrowData ArrowData { get; }
    public Transform Transform => this.transform;
    bool IArrow.IsMoving => pathManager != null && pathManager.isMoving;
    
    private List<SplineRopeSegment> _segments = new List<SplineRopeSegment>();
    // private List<Transform[]> _segmentBones = new List<Transform[]>();
    private float _totalLength = 0f;

    void Awake()
    {
        if (pathManager == null)
            pathManager = GetComponent<SplineRopePath>();
    }

    public void SetData(IArrowData arrowData)
    {
        var data = arrowData as ArrowData;
        this.arrowData = data;
        _totalLength = data.pathLength;
        
        // 根据总长度创建 Segment
        CreateSegments(_totalLength);
        
        if (pathManager != null)
            pathManager.currentDistance = _totalLength + initialDistanceOffset;
    }

    public void Hited(Vector3Int hitPoint, Vector3Int arrowDataDirection)
    {
        
    }

    /// <summary>
    /// 根据总长度动态创建 Segment
    /// </summary>
    public void CreateSegments(float totalLength)
    {
        // 清除现有 Segment
        ClearSegments();
        
        // 计算需要的 Segment 数量（向上取整）
        int segmentCount = Mathf.CeilToInt(totalLength / segmentUnitLength);
        if (segmentCount == 0) segmentCount = 1;
        
        Debug.Log($"[MultiSegmentSnake] 创建 {segmentCount} 个 Segment，总长度: {totalLength}");
        
        // 准备骨骼池
        Transform[] allBones = null;
        if (unitSnake != null && unitSnake.skinnedMeshRenderer != null)
        {
            allBones = unitSnake.skinnedMeshRenderer.bones;
        }
        
        // int totalBonesNeeded = segmentCount * bonesPerSegment;
        if (allBones != null && allBones.Length < bonesPerSegment)
        {
            Debug.LogWarning($"[MultiSegmentSnake] 骨骼数量不足！需要 {bonesPerSegment}，但只有 {allBones.Length}");
        }
        
        // 创建每个 Segment
        for (int i = 0; i < segmentCount; i++)
        {
            SplineRopeSegment segment;
            
            // 使用预制件或直接添加组件
            if (unitSnake != null)
            {
                GameObject segmentObj = Instantiate(unitSnake.gameObject, transform);
                segmentObj.name = $"Segment_{i}";
                segment = segmentObj.GetComponent<SplineRopeSegment>();
                if (segment == null)
                    segment = segmentObj.AddComponent<SplineRopeSegment>();
            }
            else
            {
                GameObject segmentObj = new GameObject($"Segment_{i}");
                segmentObj.transform.SetParent(transform);
                segment = segmentObj.AddComponent<SplineRopeSegment>();
            }
            
            // 配置 Segment 参数
            segment.segmentLength = segmentUnitLength;
            segment.segmentOffset = i * segmentUnitLength; // 每个 Segment 依次偏移
            Debug.Log("创建 Segment Offset : " +  segment.segmentOffset);
            segment.distributionMode = distributionMode;
            segment.pathManager = pathManager;
            
            // 分配骨骼
            // if (allBones != null)
            {
                // Transform[] segmentBones = AssignBonesToSegment(allBones, i, bonesPerSegment);
                segment.bones = allBones;
                // _segmentBones.Add(segmentBones);
            }
            
            // 注册到路径管理器
            if (pathManager != null)
                pathManager.RegisterSegment(segment);
            
            _segments.Add(segment);
        }
        
        InitArrow();
        Debug.Log($"[MultiSegmentSnake] 成功创建 {_segments.Count} 个 Segment");
    }

    /// <summary>
    /// 从骨骼池中分配骨骼给指定 Segment
    /// </summary>
    private Transform[] AssignBonesToSegment(Transform[] allBones, int segmentIndex, int bonesCount)
    {
        int startIndex = segmentIndex * bonesCount;
        int actualCount = Mathf.Min(bonesCount, allBones.Length - startIndex);
        
        if (actualCount <= 0)
        {
            Debug.LogWarning($"[MultiSegmentSnake] Segment {segmentIndex} 没有可用骨骼！");
            return new Transform[0];
        }
        
        Transform[] bones = new Transform[actualCount];
        for (int i = 0; i < actualCount; i++)
        {
            bones[i] = allBones[startIndex + i];
        }
        
        return bones;
    }

    /// <summary>
    /// 清除所有 Segment
    /// </summary>
    private void ClearSegments()
    {
        foreach (var segment in _segments)
        {
            if (pathManager != null)
                pathManager.UnregisterSegment(segment);
            
            if (segment != null)
                Destroy(segment.gameObject);
        }
        
        _segments.Clear();
        // _segmentBones.Clear();
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        if (pathManager != null)
        {
            pathManager.SetWaypoints(points, _totalLength + initialDistanceOffset);
        }
    }

    public void InitArrow()
    {
        if (pathManager == null) return;
        
        pathManager.isMoving = true;
        
        // 强制立即更新所有骨骼
        pathManager.ForceUpdateAllSegments();
        pathManager.ForceUpdateAllSegments(); // 双重确认
        
        // 延迟一帧最终确认
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return null;
        
        if (pathManager != null)
        {
            pathManager.ForceUpdateAllSegments();
            pathManager.isMoving = false; // 初始化完成后不自动开始移动
        }
    }

    public void MoveOut()
    {
        if (pathManager != null)
            pathManager.isMoving = true;
    }

    public void StartMoving(float distance)
    {
        if (pathManager != null)
            pathManager.isMoving = true;
    }

    public void Reset()
    {
        StopAllCoroutines();
        if (pathManager != null)
            pathManager.isMoving = false;
    }

    void OnDestroy()
    {
        ClearSegments();
    }

    /// <summary>
    /// 运行时动态调整 Segment 数量（用于蛇的增长）
    /// </summary>
    public void AdjustSegmentCount(float newTotalLength)
    {
        _totalLength = newTotalLength;
        CreateSegments(newTotalLength);
        
        if (pathManager != null)
            pathManager.currentDistance = newTotalLength + initialDistanceOffset;
    }

    /// <summary>
    /// 获取当前 Segment 数量
    /// </summary>
    public int GetSegmentCount()
    {
        return _segments.Count;
    }

    /// <summary>
    /// 设置所有 Segment 的分配模式
    /// </summary>
    public void SetDistributionMode(SplineRopeSegment.BoneDistributionMode mode)
    {
        distributionMode = mode;
        foreach (var segment in _segments)
        {
            if (segment != null)
                segment.distributionMode = mode;
        }
    }
}
