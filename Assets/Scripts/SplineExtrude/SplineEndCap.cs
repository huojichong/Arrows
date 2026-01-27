using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 将物体放置在 Spline 的指定位置，用于封住 SplineExtrude 的端点
/// </summary>
[ExecuteInEditMode]
public class SplineEndCap : MonoBehaviour
{
    [Tooltip("目标 SplineContainer")]
    public SplineContainer splineContainer;
    
    [Tooltip("目标 SplineExtrude，用于自动获取范围端点")]
    public SplineExtrude splineExtrude;
    
    [Tooltip("使用挤出范围的哪一端：true=起点(Range.x)，false=终点(Range.y)")]
    public bool useStartEnd = true;
    
    [Tooltip("手动指定 t 值（0-1），当 splineExtrude 为空时使用")]
    [Range(0f, 1f)]
    public float manualT = 0f;
    
    [Tooltip("位置偏移（沿切线方向）")]
    public float offsetAlongTangent = 0f;

    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;
        
        // 获取 t 值
        float t = manualT;
        if (splineExtrude != null)
        {
            var range = splineExtrude.Range;
            t = useStartEnd ? range.x : range.y;
        }
        
        // 计算 Spline 上的位置和方向
        var spline = splineContainer.Spline;
        spline.Evaluate(t, out var localPosition, out var tangent, out var upVector);
        
        // 转换到世界坐标
        Vector3 worldPos = splineContainer.transform.TransformPoint(localPosition);
        Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent).normalized;
        Vector3 worldUp = splineContainer.transform.TransformDirection(upVector).normalized;
        
        // 应用偏移
        worldPos += worldTangent * offsetAlongTangent;
        
        // 设置位置
        transform.position = worldPos;
        
        // 设置旋转，让物体朝向切线方向
        if (worldTangent != Vector3.zero)
        {
            // 起点端盖朝向反方向，终点端盖朝向正方向
            Vector3 forward = useStartEnd ? -worldTangent : worldTangent;
            transform.rotation = Quaternion.LookRotation(forward, worldUp);
        }
    }
}