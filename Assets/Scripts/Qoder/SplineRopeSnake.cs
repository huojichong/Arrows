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
       
    [Header("绳子与骨骼")]
    [Tooltip("绳子的骨骼列表，头部为 index 0")]
    public Transform[] bones;
    
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

    [Tooltip("单元弯曲")]
    public UnitSnake unitSnake;
    
    // 新增：相对于头部的距离偏移（用于多段拼接）
    [HideInInspector]
    public float segmentOffset = 0f;

    
    [Range(0f, 1f)]
    [Tooltip("位置平滑系数：用于消除出弯时的微小抖动，0为禁用")]
    public float positionSmoothing = 0.1f;
    
    [Range(0f, 1f)]
    [Tooltip("旋转平滑系数：用于消除急转弯处的旋转扭曲，0为禁用")]
    public float rotationSmoothing = 0.3f;

    private List<SplineRopeSnake> activeSegments = new List<SplineRopeSnake>();
    
    [SerializeField]
    public SnakePath snakePath;
    

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
        // waypoints = points;
        // UpdateSplineFromWaypoints();
        snakePath.waypoints = points;
        snakePath.UpdateSplineFromWaypoints();
        if (resetDistance)
            currentDistance = baseLength + initialDistanceOffset;
    }

    
    /// <summary>
    /// 计算并应用骨骼的位置和旋转
    /// </summary>
    public void UpdateBones()
    {
        if (bones == null || bones.Length == 0 || snakePath.splineWeights.Count == 0) return;

        float fullLength = snakePath.fullLength;
        float currentRopeLength = baseLength; // 假设长度固定

        // float currentRopeLength = baseLength * stretchMultiplier;

        // 修改：头部位置现在考虑了段偏移
        float effectiveHeadDist = currentDistance - segmentOffset;
        float effectiveTailDist = effectiveHeadDist - currentRopeLength;

        // 1. 获取绳子分段在权重坐标系中的累积权重位置
        float headWeightPos = snakePath.GetAccumulatedWeightAtPos(effectiveHeadDist);
        float tailWeightPos = snakePath.GetAccumulatedWeightAtPos(effectiveTailDist);
        
        // 绳子当前占据的总权重跨度
        float weightSpan = headWeightPos - tailWeightPos;

        // 2. 遍历骨骼，根据权重比例非线性地分配到 Spline 路径上
        for (int i = 0; i < bones.Length; i++)
        {
            // 计算骨骼在权重维度上的目标位置
            float t_bone = (bones.Length > 1) ? i / (float)(bones.Length - 1) : 0;
            float targetWeight = headWeightPos - (t_bone * weightSpan);
            
            // 将权重位置反推回实际的物理距离 (Distance)
            float actualDist = snakePath.GetDistFromAccumulatedWeight(targetWeight);

            // 3. 将物理距离转换为 Spline 的 0-1 参数 t，并更新 Transform
            if (actualDist < 0) 
                EvaluateAndApply(i, 0);
            else if (actualDist > fullLength) 
                EvaluateAndApply(i, 1.0f);
            else 
                EvaluateAndApply(i, SplineUtility.GetNormalizedInterpolation(snakePath.splineContainer.Spline, actualDist, PathIndexUnit.Distance));
        }
    }
    
    // /// <summary>
    // /// 给定一个物理距离，计算其在路径上的累积权重
    // /// </summary>
    // float GetAccumulatedWeightAtPos(float dist)
    // {
    //     if (dist <= 0) return 0;
    //     float currentAcc = 0;
    //     for (int i = 0; i < splineDistances.Count - 1; i++)
    //     {
    //         if (dist <= splineDistances[i + 1])
    //         {
    //             // 在两个采样点之间做线性插值，保证移动平滑
    //             float t = (dist - splineDistances[i]) / (splineDistances[i + 1] - splineDistances[i]);
    //             return currentAcc + splineWeights[i] * t;
    //         }
    //         currentAcc += splineWeights[i];
    //     }
    //     return currentAcc;
    // }
    
    // /// <summary>
    // /// 给定一个累积权重值，反推其在路径上的物理距离
    // /// </summary>
    // float GetDistFromAccumulatedWeight(float targetWeight)
    // {
    //     if (targetWeight <= 0) return 0;
    //     float currentAcc = 0;
    //     for (int i = 0; i < snakePath.splineWeights.Count - 1; i++)
    //     {
    //         if (targetWeight <= currentAcc + snakePath.splineWeights[i])
    //         {
    //             // 在权重区间内做插值反推物理距离
    //             float t = (targetWeight - currentAcc) / splineWeights[i];
    //             return Mathf.Lerp(splineDistances[i], splineDistances[i + 1], t);
    //         }
    //         currentAcc += splineWeights[i];
    //     }
    //     return splineDistances[splineDistances.Count - 1];
    // }

    /// <summary>
    /// 评估 Spline 上参数 t 的位置和切线，并应用到指定骨骼。
    /// 包含位置平滑逻辑和切线对齐修正。
    /// </summary>
    void EvaluateAndApply(int boneIndex, float t)
    {
        snakePath.splineContainer.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
        
        // 转换到世界坐标空间
        Vector3 worldPos = snakePath.splineContainer.transform.TransformPoint(pos);
        
        // 应用位置平滑 (positionSmoothing)，消除由于数值计算导致的微小高频抖动
        // 仅在移动时启用平滑，初始化时立即定位
        if (positionSmoothing > 0 && Application.isPlaying && isMoving)
            bones[boneIndex].position = Vector3.Lerp(bones[boneIndex].position, worldPos, 1f - positionSmoothing);
        else
            bones[boneIndex].position = worldPos;

        // 计算骨骼朝向
        if (math.lengthsq(tan) > 0.001f)
        {
            Vector3 forward = snakePath.splineContainer.transform.TransformDirection(tan);
            
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
        snakePath.waypoints = new List<Vector3>(newWaypoints);
        snakePath.UpdateSplineFromWaypoints();
        currentDistance = 0f;
    }

    /// <summary>
    /// 模拟贪吃蛇式增长
    /// </summary>
    public void AddWaypoint(Vector3 point)
    {
        snakePath.waypoints.Add(point);
        snakePath.UpdateSplineFromWaypoints();
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

    public IArrowData ArrowData { get; }
    public Transform Transform => this.transform;
    public bool IsMoving { get; set; }
    
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
        
        
        // 1. 清理旧分段
        // foreach (var seg in activeSegments) Destroy(seg.gameObject);
        // activeSegments.Clear();
        //
        // // 2. 计算路径信息
        // int cornerCount = CalculateCorners(waypoints);
        // float totalPathLength = CalculatePathLength(waypoints);
        //
        // // 3. 动态决定需要多少分段
        // // 你可以根据 totalPathLength 来决定生成多少个 segmentPrefab  // toto 具体逻辑待确定
        // int segmentCount = Mathf.CeilToInt((totalPathLength + cornerCount) / segmentLength);
        //
        // // 4. 动态调整曲率敏感度
        // // float dynamicCurvature = 10f + (cornerCount * curvatureBoostPerCorner);
        //
        // var totalBones = new List<Transform>();
        // for (int i = 0; i < segmentCount; i++)
        // {
        //     GameObject go = Instantiate(this.unitSnake.gameObject, transform);
        //     var snake = go.GetComponent<UnitSnake>();
        //     snake.head.gameObject.SetActive(i == 0);
        //     totalBones.AddRange(snake.skinnedMeshRenderer.bones);
        // }

        bones = unitSnake.skinnedMeshRenderer.bones;
        
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

        isMoving = true;

        Time.timeScale = 0.1f;
    }
}
