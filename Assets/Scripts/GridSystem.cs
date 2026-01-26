using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 网格系统，用于管理3D空间中的箭头块位置和碰撞检测
/// </summary>
public class GridSystem : MonoBehaviour
{
    [Header("网格配置")]
    public float gridSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    public Vector2Int gridDimensions = new Vector2Int(10, 10);
    
    [Header("可视化")]
    public bool showGrid = true;
    public Color gridColor = Color.white;
    
    // 网格数据存储：位置 -> 箭头块
    private Dictionary<Vector3Int, IArrow> gridOccupancy = new Dictionary<Vector3Int, IArrow>();
    
    /// <summary>
    /// 初始化网格系统
    /// </summary>
    public void InitializeGrid()
    {
        gridOccupancy.Clear();
        
    }
    
    /// <summary>
    /// 将世界坐标转换为网格坐标
    /// </summary>
    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - gridOrigin;
        return new Vector3Int(
            Mathf.RoundToInt(offset.x / gridSize),
            Mathf.RoundToInt(offset.y / gridSize),
            Mathf.RoundToInt(offset.z / gridSize)
        );
    }
    
    /// <summary>
    /// 将网格坐标转换为世界坐标
    /// </summary>
    public Vector3 GridToWorld(Vector3Int gridPosition)
    {
        return gridOrigin + new Vector3(
            gridPosition.x * gridSize,
            gridPosition.y * gridSize,
            gridPosition.z * gridSize
        );
    }

    /// <summary>
    /// 注册箭头块到网格
    /// </summary>
    public void RegisterArrowBlock(IArrow arrow)
    {
        foreach (var data in arrow.ArrowData.customPath)
        {
            var dataInt = GetGridPosition(data);
            if (gridOccupancy.ContainsKey(dataInt))
            {
                Debug.LogWarning($"网格位置 {dataInt} 已被占用！");
            }

            gridOccupancy[dataInt] = arrow;
        }
    }
    
    private Vector3Int GetGridPosition(Vector3 data)
    {
        var dataInt = new Vector3Int(
            Mathf.RoundToInt(data.x),
            Mathf.RoundToInt(data.y),
            Mathf.RoundToInt(data.z)
        );
        return dataInt;
    }

    /// <summary>
    /// 从网格注销箭头块
    /// </summary>
    public void UnregisterArrowBlock(IArrow arrow)
    {
        foreach (var data in arrow.ArrowData.customPath)
        {
            var dataInt = GetGridPosition(data);
            if (gridOccupancy.ContainsKey(dataInt) && gridOccupancy[dataInt] == arrow)
            {
                Debug.LogWarning($"网格位置 {dataInt} 已被占用！");
                gridOccupancy.Remove(dataInt);
            }
        }
    }
    
    /// <summary>
    /// 更新箭头块在网格中的位置
    /// </summary>
    public void UpdateArrowBlockPosition(IArrow arrow, Vector3 oldPosition, Vector3 newPosition)
    {
        Vector3Int oldGridPos = WorldToGrid(oldPosition);
        Vector3Int newGridPos = WorldToGrid(newPosition);
        
        if (oldGridPos != newGridPos)
        {
            // 从旧位置移除
            if (gridOccupancy.ContainsKey(oldGridPos) && gridOccupancy[oldGridPos] == arrow)
            {
                gridOccupancy.Remove(oldGridPos);
            }
            
            // 添加到新位置
            if (gridOccupancy.ContainsKey(newGridPos))
            {
                Debug.LogWarning($"目标网格位置 {newGridPos} 已被占用！");
            }
            gridOccupancy[newGridPos] = arrow;
        }
    }
    
    /// <summary>
    /// 检查网格位置是否被占用
    /// </summary>
    public bool IsGridOccupied(Vector3Int gridPosition)
    {
        return gridOccupancy.ContainsKey(gridPosition);
    }
    
    /// <summary>
    /// 检查世界坐标位置是否被占用
    /// </summary>
    public bool IsWorldPositionOccupied(Vector3 worldPosition)
    {
        Vector3Int gridPos = WorldToGrid(worldPosition);
        return IsGridOccupied(gridPos);
    }
    
    /// <summary>
    /// 获取指定网格位置的箭头块
    /// </summary>
    public IArrow GetArrowBlockAt(Vector3Int gridPosition)
    {
        return gridOccupancy.GetValueOrDefault(gridPosition,null);
    }
    
    /// <summary>
    /// 获取指定世界坐标的箭头块
    /// </summary>
    public IArrow GetArrowBlockAt(Vector3 worldPosition)
    {
        Vector3Int gridPos = WorldToGrid(worldPosition);
        return GetArrowBlockAt(gridPos);
    }
    
    /// <summary>
    /// 检查路径是否可通行
    /// </summary>
    public bool IsPathClear(List<Vector3> path, ArrowBlock movingArrow)
    {
        foreach (var point in path)
        {
            Vector3Int gridPos = WorldToGrid(point);
            if (gridOccupancy.TryGetValue(gridPos, out IArrow occupyingArrow))
            {
                if (occupyingArrow != movingArrow)
                {
                    return false; // 路径被其他箭头占用
                }
            }
        }
        return true;
    }
    
    /// <summary>
    /// 对齐位置到网格
    /// </summary>
    public Vector3 SnapToGrid(Vector3 position)
    {
        Vector3Int gridPos = WorldToGrid(position);
        return GridToWorld(gridPos);
    }
    
    /// <summary>
    /// 绘制网格（用于调试）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGrid) return;
        
        Gizmos.color = gridColor;
        
        // 绘制XZ平面的网格
        for (int x = 0; x <= gridDimensions.x; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * gridSize, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x * gridSize, 0, gridDimensions.y * gridSize);
            Gizmos.DrawLine(start, end);
        }
        
        for (int z = 0; z <= gridDimensions.y; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0, z * gridSize);
            Vector3 end = gridOrigin + new Vector3(gridDimensions.x * gridSize, 0, z * gridSize);
            Gizmos.DrawLine(start, end);
        }
        
        // 绘制被占用的网格位置
        Gizmos.color = Color.yellow;
        foreach (var kvp in gridOccupancy)
        {
            Vector3 worldPos = GridToWorld(kvp.Key);
            Gizmos.DrawWireCube(worldPos, Vector3.one * gridSize * 0.9f);
        }
    }
}
