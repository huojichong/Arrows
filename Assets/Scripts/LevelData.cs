using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 关卡数据配置
/// </summary>
[System.Serializable]
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
[System.Serializable]
public class ArrowBlockData
{
    public string id;
    public Vector3 startPosition;
    public ArrowBlock.ArrowType arrowType;
    public ArrowBlock.Direction initialDirection;
    
    [Header("模型详细配置")]
    public float startLength = 1.0f;  // 起点段长度
    public float turnLength = 1.0f;   // 转弯段长度
    public float tailLength = 0.5f;   // 尾部长度
    public float thickness = 0.3f;    // 模型粗细
    
    [Header("视觉配置")]
    public Color blockColor = Color.gray;
    public List<Vector3> customPath;  // 自定义路径点
}

