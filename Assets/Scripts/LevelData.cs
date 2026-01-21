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
    public List<ArrowBlockData> arrowBlocks = new List<ArrowBlockData>();
    public Vector3 cameraPosition = new Vector3(0, 10, -10);
    public Vector3 cameraRotation = new Vector3(45, 0, 0);
}

/// <summary>
/// 单个箭头块的数据
/// </summary>
[Serializable]
public class ArrowBlockData
{
    public string id;
    public Vector3 startPosition;
    public Vector2Int direction;
    
    [Header("模型详细配置")]
    public float startLength = 1.0f;  // 起点段长度
    
    [Header("视觉配置")]
    public Color blockColor = Color.gray;
    public List<Vector2Int> customPath;  // 自定义路径点
}

