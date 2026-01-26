using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public bool showCoordinates = true;
    public Color coordinateTextColor = Color.white;
    
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
    /// 检查网格位置是否被占用
    /// </summary>
    public bool IsGridOccupied(Vector3Int gridPosition)
    {
        return gridOccupancy.ContainsKey(gridPosition);
    }
    
    
    /// <summary>
    /// 获取指定网格位置的箭头块
    /// </summary>
    public IArrow GetArrowBlockAt(Vector3Int gridPosition)
    {
        return gridOccupancy.GetValueOrDefault(gridPosition,null);
    }
    
    /// <summary>
    /// 绘制网格（用于调试）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGrid) return;
        
        Gizmos.color = gridColor;
        
        // 绘制XZ平面的网格
        // for (int x = 0; x <= gridDimensions.x; x++)
        // {
        //     Vector3 start = gridOrigin + new Vector3(x * gridSize, 0, 0);
        //     Vector3 end = gridOrigin + new Vector3(x * gridSize, 0, gridDimensions.y * gridSize);
        //     Gizmos.DrawLine(start, end);
        // }
        //
        // for (int z = 0; z <= gridDimensions.y; z++)
        // {
        //     Vector3 start = gridOrigin + new Vector3(0, 0, z * gridSize);
        //     Vector3 end = gridOrigin + new Vector3(gridDimensions.x * gridSize, 0, z * gridSize);
        //     Gizmos.DrawLine(start, end);
        // }
        
        // 绘制被占用的网格位置
        
        
        
        Gizmos.color = Color.yellow;
        foreach (var kvp in gridOccupancy)
        {
            Vector3 worldPos = (kvp.Key);
            Gizmos.DrawWireCube(worldPos, Vector3.one * gridSize * 0.9f);
        }
        
        // 绘制坐标信息
#if UNITY_EDITOR
        if (showCoordinates)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = coordinateTextColor;
            style.fontSize = 10;
            style.alignment = TextAnchor.MiddleCenter;

            Gizmos.color = Color.yellow;
            foreach (var kvp in gridOccupancy)
            {
                Vector3 worldPos = (kvp.Key);

                var label = $"{worldPos.x},{worldPos.z}";
                Handles.Label(worldPos + Vector3.up * 0.1f, label, style);
            }

        }
#endif
    }
    
}
