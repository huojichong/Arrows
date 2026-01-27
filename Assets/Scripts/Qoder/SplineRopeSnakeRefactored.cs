using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 重构后的 Spline 绳子控制器（IArrow 适配器）
/// 使用新的 Manager-Segment 架构
/// </summary>
public class SplineRopeSnakeRefactored : MonoBehaviour, IArrow
{
    [Header("组件引用")]
    public SplineRopePath pathManager;
    public SplineRopeSegment mainSegment;
    public UnitSnake unitSnake;
    
    [Header("配置")]
    public float initialDistanceOffset = 0.1f;
    
    // IArrow 接口实现
    public IArrowData arrowData { get; set; }
    public IArrowData ArrowData { get; }
    public Transform Transform => this.transform;
    
    bool IArrow.IsMoving => pathManager != null && pathManager.isMoving;

    void Awake()
    {
        // 自动获取组件
        if (pathManager == null)
            pathManager = GetComponent<SplineRopePath>();
        
        if (mainSegment == null)
            mainSegment = GetComponent<SplineRopeSegment>();
    }

    public void SetData(IArrowData arrowData)
    {
        var data = arrowData as ArrowData;
        this.arrowData = data;
        
        if (mainSegment != null)
            mainSegment.segmentLength = data.pathLength;
        
        if (pathManager != null)
            pathManager.currentDistance = data.pathLength + initialDistanceOffset;
    }

    public void Hited(Vector3Int hitPoint, Vector3Int arrowDataDirection)
    {
        
    }

    public void Reset()
    {
        StopAllCoroutines();
        if (pathManager != null)
            pathManager.isMoving = false;
    }

    public void MoveOut()
    {
        if (pathManager != null)
            pathManager.isMoving = true;
    }

    public void StartMoving(float distance)
    {
        pathManager.isMoving = true;
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        pathManager.SetWaypoints(points,mainSegment.segmentLength + initialDistanceOffset);
    }

    public void InitArrow()
    {
        if (pathManager == null || mainSegment == null) return;
        
        // 初始化骨骼引用
        if (unitSnake != null && mainSegment.bones == null)
        {
            mainSegment.bones = unitSnake.skinnedMeshRenderer.bones;
        }
        
        pathManager.isMoving = true;
        
        // 强制立即更新所有骨骼（不使用平滑）
        pathManager.ForceUpdateAllSegments();
        
        // 延迟一帧再次确认
        StartCoroutine(DelayedInit());
    }
    
    private IEnumerator DelayedInit()
    {
        yield return null;
        
        // 最终更新
        if (pathManager != null)
            pathManager.ForceUpdateAllSegments();
        
        // 可选：自动开始移动
        pathManager.isMoving = true;
    }

}
