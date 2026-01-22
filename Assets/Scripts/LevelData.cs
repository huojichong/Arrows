using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡数据配置
/// </summary>
[Serializable]
public class LevelData
{
    public int levelNumber;
    public string levelName;
    public List<ArrowData> arrowBlocks = new List<ArrowData>();
    public Vector3 cameraPosition = new Vector3(0, 10, -10);
    public Vector3 cameraRotation = new Vector3(45, 0, 0);
}


public interface IArrowData
{
    List<Vector3> customPath { get; set; }
    string id { get; set; }
}

/// <summary>
/// 单个箭头块的数据
/// </summary>
[Serializable]
public class ArrowData : IArrowData
{
    public string id { get; set; }
    public Vector3 startPosition;
    public Vector2Int direction;
    
    [Header("视觉配置")]
    public Color blockColor = Color.gray;
    public List<Vector3> customPath { get; set; }  // 自定义路径点
    public int pathLength;
}

