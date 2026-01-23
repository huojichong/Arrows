using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 多蛇管理器：管理多条蛇，每条蛇有独立的路径和长度
/// 演示如何为不同长度的蛇动态创建 Segment
/// </summary>
public class MultiSnakeManager : MonoBehaviour
{
    [System.Serializable]
    public class SnakeConfig
    {
        [Tooltip("蛇的名称")]
        public string snakeName = "Snake";
        
        [Tooltip("蛇的总长度")]
        public float totalLength = 5.0f;
        
        [Tooltip("路径控制点（世界坐标）")]
        public List<Vector3> waypoints = new List<Vector3>();
        
        [Tooltip("每个 Segment 的单位长度")]
        public float segmentUnitLength = 1.0f;
        
        [Tooltip("每个 Segment 的骨骼数")]
        public int bonesPerSegment = 5;
        
        [Tooltip("骨骼分配模式")]
        public SplineRopeSegment.BoneDistributionMode distributionMode = SplineRopeSegment.BoneDistributionMode.Curvature;
    }
    
    [Header("蛇配置列表")]
    [Tooltip("每条蛇的配置")]
    public List<SnakeConfig> snakeConfigs = new List<SnakeConfig>();
    
    [Header("预制件引用")]
    [Tooltip("蛇预制件 路径配置")]
    public GameObject snakePrefab;
    
    
    [Tooltip("Segment 预制件模板")]
    public GameObject unitSnakePrefab;
    
    [Header("全局配置")]
    [Tooltip("拐弯半径")]
    public float filletRadius = 0.5f;
    
    [Tooltip("移动速度")]
    public float moveSpeed = 2.0f;
    
    
    [Header("自适应分布")]
    [Range(0, 30f)]
    [Tooltip("曲率敏感度：值越大，拐弯处的骨骼越密集，直线处越稀疏")]
    public float curvatureSensitivity = 15.0f;
    
    private List<MultiSegmentSnake> _activeSnakes = new List<MultiSegmentSnake>();
    
    /// <summary>
    /// 创建单条蛇
    /// </summary>
    private void CreateSnake(SnakeConfig config, int index,IArrowData data)
    {
        // 1. 创建蛇对象
        GameObject snakeObj;
        if (snakePrefab != null)
        {
            snakeObj = Instantiate(snakePrefab, transform);
        }
        else
        {
            snakeObj = new GameObject($"Snake_{index}_{config.snakeName}");
            snakeObj.transform.SetParent(transform);
        }
        
        // 2. 添加路径管理器
        SplineRopePath ropePath = snakeObj.GetComponent<SplineRopePath>();
        if (ropePath == null)
            ropePath = snakeObj.AddComponent<SplineRopePath>();
        
        // 添加 SplineContainer
        SplineContainer splineContainer = snakeObj.GetComponent<SplineContainer>();
        if (splineContainer == null)
            splineContainer = snakeObj.AddComponent<SplineContainer>();
        
        ropePath.splineContainer = splineContainer;
        ropePath.filletRadius = filletRadius;
        ropePath.moveSpeed = moveSpeed;
        ropePath.curvatureSensitivity = curvatureSensitivity;
        
        // 3. 添加多段蛇控制器
        MultiSegmentSnake multiSnake = snakeObj.GetComponent<MultiSegmentSnake>();
        if (multiSnake == null)
            multiSnake = snakeObj.AddComponent<MultiSegmentSnake>();
        
        multiSnake.pathManager = ropePath;
        
        multiSnake.segmentUnitLength = config.segmentUnitLength;
        multiSnake.bonesPerSegment = config.bonesPerSegment;
        multiSnake.distributionMode = config.distributionMode;
        
        // 4. 获取或创建 UnitSnake
        multiSnake.unitSnake = unitSnakePrefab.GetComponent<UnitSnake>();
        
        // 5. 设置路径点
        if (config.waypoints.Count >= 2)
        {
            ropePath.waypoints = new List<Vector3>(config.waypoints);
            ropePath.UpdateSplineFromWaypoints();
        }
        else
        {
            Debug.LogWarning($"[MultiSnakeManager] Snake {index} 的路径点少于 2 个！");
        }
        
        // 6. 创建 Segment
        multiSnake.CreateSegments(config.totalLength);
        
        // 7. 初始化
        ropePath.currentDistance = config.totalLength + multiSnake.initialDistanceOffset;
        
        multiSnake.SetData(data);
        multiSnake.InitArrow();
        _activeSnakes.Add(multiSnake);
        
        Debug.Log($"[MultiSnakeManager] 创建蛇 {index}: {config.snakeName}, 长度: {config.totalLength}, Segment数: {multiSnake.GetSegmentCount()}");
    }

    /// <summary>
    /// 清除所有蛇
    /// </summary>
    [ContextMenu("Clear All Snakes")]
    public void ClearAllSnakes()
    {
        foreach (var snake in _activeSnakes)
        {
            if (snake != null)
                Destroy(snake.gameObject);
        }
        
        _activeSnakes.Clear();
    }

    /// <summary>
    /// 启动所有蛇的移动
    /// </summary>
    [ContextMenu("Start All Snakes")]
    public void StartAllSnakes()
    {
        foreach (var snake in _activeSnakes)
        {
            if (snake != null)
                snake.MoveOut();
        }
    }

    /// <summary>
    /// 停止所有蛇的移动
    /// </summary>
    [ContextMenu("Stop All Snakes")]
    public void StopAllSnakes()
    {
        foreach (var snake in _activeSnakes)
        {
            if (snake != null)
                snake.Reset();
        }
    }

    /// <summary>
    /// 动态添加一条新蛇
    /// </summary>
    public MultiSegmentSnake AddSnake(string name, float length, List<Vector3> wayPoints,IArrowData arrowData, float unitLength = 1.0f, int bonesPerSegment = 5)
    {
        SnakeConfig config = new SnakeConfig
        {
            snakeName = name,
            totalLength = length,
            waypoints = wayPoints,
            segmentUnitLength = unitLength,
            bonesPerSegment = bonesPerSegment,
            
        };
        
        CreateSnake(config, _activeSnakes.Count,arrowData);
        return _activeSnakes[_activeSnakes.Count - 1];
    }

    /// <summary>
    /// 获取活跃的蛇数量
    /// </summary>
    public int GetSnakeCount()
    {
        return _activeSnakes.Count;
    }

    /// <summary>
    /// 获取指定索引的蛇
    /// </summary>
    public MultiSegmentSnake GetSnake(int index)
    {
        if (index >= 0 && index < _activeSnakes.Count)
            return _activeSnakes[index];
        return null;
    }

    /// <summary>
    /// 设置所有蛇的分配模式
    /// </summary>
    public void SetAllDistributionMode(SplineRopeSegment.BoneDistributionMode mode)
    {
        foreach (var snake in _activeSnakes)
        {
            if (snake != null)
                snake.SetDistributionMode(mode);
        }
    }

    void OnDrawGizmos()
    {
        // 可视化每条蛇的路径点
        if (snakeConfigs == null) return;
        
        for (int i = 0; i < snakeConfigs.Count; i++)
        {
            var config = snakeConfigs[i];
            if (config.waypoints.Count < 2) continue;
            
            // 不同的蛇用不同的颜色
            Gizmos.color = Color.HSVToRGB((i * 0.3f) % 1.0f, 0.8f, 1.0f);
            
            for (int j = 0; j < config.waypoints.Count - 1; j++)
            {
                Gizmos.DrawLine(config.waypoints[j], config.waypoints[j + 1]);
                Gizmos.DrawWireSphere(config.waypoints[j], 0.1f);
            }
            Gizmos.DrawWireSphere(config.waypoints[config.waypoints.Count - 1], 0.1f);
        }
    }
}
