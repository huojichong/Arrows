
        using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineRopeSnakeManager : MonoBehaviour
{
    [Header("配置")] public GameObject segmentPrefab; // 带有 SplineRopeSnake 的预制体
    public float segmentLength = 1.0f; // 单个分段的物理长度

    [Header("动态参数")] public float curvatureBoostPerCorner = 2.0f; // 每个拐角额外增加的曲率敏感度

    private List<SplineRopeSnake> activeSegments = new List<SplineRopeSnake>();
    private SplineContainer splineContainer;

    public void SetupSnake(List<Vector3> waypoints)
    {
        if (splineContainer == null) 
            splineContainer = GetComponent<SplineContainer>();

        // 1. 清理旧分段
        foreach (var seg in activeSegments) Destroy(seg.gameObject);
        activeSegments.Clear();

        // 2. 计算路径信息
        int cornerCount = CalculateCorners(waypoints);
        float totalPathLength = CalculatePathLength(waypoints);

        // 3. 动态决定需要多少分段
        // 你可以根据 totalPathLength 来决定生成多少个 segmentPrefab
        int segmentCount = Mathf.CeilToInt(totalPathLength / segmentLength);

        // 4. 动态调整曲率敏感度
        float dynamicCurvature = 10f + (cornerCount * curvatureBoostPerCorner);

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject go = Instantiate(segmentPrefab, transform);
            SplineRopeSnake snake = go.GetComponent<SplineRopeSnake>();

            snake.splineContainer = splineContainer;
            snake.baseLength = segmentLength;
            snake.segmentOffset = i * segmentLength; // 每个分段排在后一个后面
            snake.curvatureSensitivity = dynamicCurvature;
            snake.waypoints = waypoints;

            snake.UpdateSplineFromWaypoints();
            snake.InitArrow();

            activeSegments.Add(snake);
        }
    }

    private int CalculateCorners(List<Vector3> pts)
    {
        int count = 0;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            if (Vector3.Angle(pts[i] - pts[i - 1], pts[i + 1] - pts[i]) > 10f) count++;
        }

        return count;
    }

    private float CalculatePathLength(List<Vector3> pts)
    {
        float len = 0;
        for (int i = 0; i < pts.Count - 1; i++) len += Vector3.Distance(pts[i], pts[i + 1]);
        return len;
    }

    // 统一同步移动进度
    public void SetMoveDistance(float dist)
    {
        foreach (var seg in activeSegments)
        {
            seg.currentDistance = dist;
            seg.UpdateBones();
        }
    }
}