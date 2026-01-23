using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Spline 绳子段，仅负责根据 Manager 提供的数据更新自身骨骼
/// </summary>
public class SplineRopeSegment : MonoBehaviour
{
    /// <summary>
    /// 骨骼分配模式
    /// </summary>
    public enum BoneDistributionMode
    {
        Uniform,        // 平均分布：骨骼均匀分布在路径上
        Curvature      // 曲率自适应：骨骼在弯道密集，直线稀疏
    }

    [Header("绳子段配置")]
    [Tooltip("绳子的骨骼列表，头部为 index 0")]
    public Transform[] bones;
    
    [Tooltip("该段在路径上的固定长度")]
    public float segmentLength = 2.0f;
    
    [Tooltip("相对于全局头部的偏移距离")]
    public float segmentOffset = 0f;
    
    [Header("骨骼分配模式")]
    [Tooltip("骨骼分配模式：平均分布或曲率自适应")]
    public BoneDistributionMode distributionMode = BoneDistributionMode.Curvature;
    
    [Header("平滑参数")]
    [Range(0f, 1f)]
    [Tooltip("位置平滑系数：用于消除出弯时的微小抖动，0为禁用")]
    public float positionSmoothing = 0.1f;
    
    [Range(0f, 1f)]
    [Tooltip("旋转平滑系数：用于消除急转弯处的旋转扭曲，0为禁用")]
    public float rotationSmoothing = 0.3f;

    [Header("引用")]
    public SplineRopePath pathManager;

    private bool _isInitialized = false;

    /// <summary>
    /// 更新骨骼位置和旋转（由 Manager 调用）
    /// </summary>
    public void UpdateBones(float globalDistance, PathData pathData, SplineContainer splineContainer)
    {
        if (bones == null || bones.Length == 0)
        {
            return;
        }

        // 计算该段的有效头部和尾部距离
        float effectiveHeadDist = globalDistance - segmentOffset;
        float effectiveTailDist = effectiveHeadDist - segmentLength;

        Debug.Log(this.transform.name +" UpdateBones: "  + "globalDistance:" + globalDistance + " segmentOffset:" + segmentOffset + " segmentLength:" + segmentLength +" effectiveHeadDist:" + effectiveHeadDist + " effectiveTailDist:" + effectiveTailDist);
        // 根据分配模式选择不同的骨骼位置计算方法
        bool useSmoothing = pathManager != null && pathManager.isMoving;
        if (distributionMode == BoneDistributionMode.Uniform)
        {
            UpdateBonesUniform(effectiveHeadDist, effectiveTailDist, pathData, splineContainer, useSmoothing);
        }
        else // Curvature
        {
            UpdateBonesCurvature(effectiveHeadDist, effectiveTailDist, pathData, splineContainer, useSmoothing);
        }
    }

    /// <summary>
    /// 平均分布模式：骨骼均匀分布在路径上
    /// </summary>
    private void UpdateBonesUniform(float headDist, float tailDist, PathData pathData, SplineContainer splineContainer, bool useSmoothing)
    {
        // 简单线性插值，不考虑权重
        for (int i = 0; i < bones.Length; i++)
        {
            float t_bone = (bones.Length > 1) ? i / (float)(bones.Length - 1) : 0;
            float actualDist = Mathf.Lerp(headDist, tailDist, t_bone);

            if (actualDist < 0)
                EvaluateAndApply(i, 0, splineContainer, useSmoothing);
            else if (actualDist > pathData.fullLength)
                EvaluateAndApply(i, 1.0f, splineContainer, useSmoothing);
            else
                EvaluateAndApply(i, SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, actualDist, PathIndexUnit.Distance), splineContainer, useSmoothing);
        }
    }

    /// <summary>
    /// 曲率自适应模式：骨骼在弯道密集，直线稀疏
    /// </summary>
    private void UpdateBonesCurvature(float headDist, float tailDist, PathData pathData, SplineContainer splineContainer, bool useSmoothing)
    {
        if (pathData.weights.Count == 0) return;

        // 获取权重位置
        float headWeightPos = GetAccumulatedWeightAtPos(headDist, pathData);
        float tailWeightPos = GetAccumulatedWeightAtPos(tailDist, pathData);
        float weightSpan = headWeightPos - tailWeightPos;

        // 遍历骨骼，根据权重比例非线性地分配
        for (int i = 0; i < bones.Length; i++)
        {
            float t_bone = (bones.Length > 1) ? i / (float)(bones.Length - 1) : 0;
            float targetWeight = headWeightPos - (t_bone * weightSpan);
            float actualDist = GetDistFromAccumulatedWeight(targetWeight, pathData);

            if (actualDist < 0)
                EvaluateAndApply(i, 0, splineContainer, useSmoothing);
            else if (actualDist > pathData.fullLength)
                EvaluateAndApply(i, 1.0f, splineContainer, useSmoothing);
            else
                EvaluateAndApply(i, SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, actualDist, PathIndexUnit.Distance), splineContainer, useSmoothing);
        }
    }

    /// <summary>
    /// 强制立即更新（用于初始化，不使用平滑）
    /// </summary>
    public void ForceUpdate(float globalDistance, PathData pathData, SplineContainer splineContainer)
    {
        if (bones == null || bones.Length == 0) return;

        float effectiveHeadDist = globalDistance - segmentOffset;
        float effectiveTailDist = effectiveHeadDist - segmentLength;

        // 根据分配模式选择不同的更新方法
        if (distributionMode == BoneDistributionMode.Uniform)
        {
            UpdateBonesUniform(effectiveHeadDist, effectiveTailDist, pathData, splineContainer, false);
        }
        else // Curvature
        {
            UpdateBonesCurvature(effectiveHeadDist, effectiveTailDist, pathData, splineContainer, false);
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 给定一个物理距离，计算其在路径上的累积权重
    /// </summary>
    float GetAccumulatedWeightAtPos(float dist, PathData pathData)
    {
        if (dist <= 0) return 0;
        float currentAcc = 0;
        for (int i = 0; i < pathData.distances.Count - 1; i++)
        {
            if (dist <= pathData.distances[i + 1])
            {
                float t = (dist - pathData.distances[i]) / (pathData.distances[i + 1] - pathData.distances[i]);
                return currentAcc + pathData.weights[i] * t;
            }
            currentAcc += pathData.weights[i];
        }
        return pathData.totalWeight;
    }

    /// <summary>
    /// 给定一个累积权重值，反推其在路径上的物理距离
    /// </summary>
    float GetDistFromAccumulatedWeight(float targetWeight, PathData pathData)
    {
        if (targetWeight <= 0) return 0;
        float currentAcc = 0;
        for (int i = 0; i < pathData.weights.Count - 1; i++)
        {
            if (targetWeight <= currentAcc + pathData.weights[i])
            {
                float t = (targetWeight - currentAcc) / pathData.weights[i];
                return Mathf.Lerp(pathData.distances[i], pathData.distances[i + 1], t);
            }
            currentAcc += pathData.weights[i];
        }
        return pathData.distances[pathData.distances.Count - 1];
    }

    /// <summary>
    /// 评估 Spline 上参数 t 的位置和切线，并应用到指定骨骼
    /// </summary>
    void EvaluateAndApply(int boneIndex, float t, SplineContainer splineContainer, bool useSmoothing)
    {
        splineContainer.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
        
        Vector3 worldPos = splineContainer.transform.TransformPoint(pos);
        
        // 应用位置平滑
        if (positionSmoothing > 0 && Application.isPlaying && useSmoothing)
            bones[boneIndex].position = Vector3.Lerp(bones[boneIndex].position, worldPos, 1f - positionSmoothing);
        else
            bones[boneIndex].position = worldPos;

        // 计算骨骼朝向
        if (math.lengthsq(tan) > 0.001f)
        {
            Vector3 forward = splineContainer.transform.TransformDirection(tan);
            Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
            
            // 应用旋转平滑
            if (rotationSmoothing > 0 && Application.isPlaying && useSmoothing)
            {
                bones[boneIndex].rotation = Quaternion.Slerp(bones[boneIndex].rotation, targetRotation, 1f - rotationSmoothing);
            }
            else
            {
                bones[boneIndex].rotation = targetRotation;
            }
        }
    }
}
