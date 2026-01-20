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
    public Vector3 startPosition;
    public ArrowBlock.ArrowType arrowType;
    public ArrowBlock.Direction initialDirection;
    public List<Vector3> customPath; // 自定义路径点
    public Color blockColor = Color.gray;
}

/// <summary>
/// 关卡管理器，用于加载和保存关卡
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("关卡配置")]
    public List<LevelData> levels = new List<LevelData>();
    public GameObject arrowBlockPrefab;
    
    private int currentLevelIndex = 0;
    
    /// <summary>
    /// 加载指定关卡
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError($"关卡索引越界: {levelIndex}");
            return;
        }
        
        // 清空当前关卡
        ClearCurrentLevel();
        
        currentLevelIndex = levelIndex;
        LevelData level = levels[levelIndex];
        
        // 设置相机
        Camera.main.transform.position = level.cameraPosition;
        Camera.main.transform.eulerAngles = level.cameraRotation;
        
        // 生成箭头块
        foreach (var arrowData in level.arrowBlocks)
        {
            CreateArrowBlock(arrowData);
        }
        
        Debug.Log($"加载关卡: {level.levelName}");
    }
    
    /// <summary>
    /// 创建箭头块
    /// </summary>
    GameObject CreateArrowBlock(ArrowBlockData data)
    {
        if (arrowBlockPrefab == null)
        {
            Debug.LogError("箭头块预制体未设置！");
            return null;
        }
        
        GameObject arrowObj = Instantiate(arrowBlockPrefab, data.startPosition, Quaternion.identity);
        ArrowBlock arrow = arrowObj.GetComponent<ArrowBlock>();
        
        if (arrow != null)
        {
            arrow.arrowType = data.arrowType;
            arrow.currentDirection = data.initialDirection;
            
            // 如果有自定义路径，使用自定义路径
            if (data.customPath != null && data.customPath.Count > 0)
            {
                arrow.pathNodes = new List<Vector3>(data.customPath);
            }
            
            // 设置颜色
            Renderer renderer = arrowObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = data.blockColor;
            }
        }
        
        return arrowObj;
    }
    
    /// <summary>
    /// 清空当前关卡
    /// </summary>
    void ClearCurrentLevel()
    {
        ArrowBlock[] arrows = FindObjectsOfType<ArrowBlock>();
        foreach (var arrow in arrows)
        {
            Destroy(arrow.gameObject);
        }
    }
    
    /// <summary>
    /// 创建示例关卡
    /// </summary>
    public void CreateExampleLevel()
    {
        LevelData level1 = new LevelData
        {
            levelNumber = 1,
            levelName = "第一关 - 基础",
            cameraPosition = new Vector3(0, 10, -10),
            cameraRotation = new Vector3(45, 0, 0)
        };
        
        // 添加一个直线箭头
        level1.arrowBlocks.Add(new ArrowBlockData
        {
            startPosition = new Vector3(0, 0, 0),
            arrowType = ArrowBlock.ArrowType.Straight,
            initialDirection = ArrowBlock.Direction.Forward,
            blockColor = Color.blue
        });
        
        // 添加一个左转箭头
        level1.arrowBlocks.Add(new ArrowBlockData
        {
            startPosition = new Vector3(2, 0, 0),
            arrowType = ArrowBlock.ArrowType.TurnLeft,
            initialDirection = ArrowBlock.Direction.Forward,
            blockColor = Color.red
        });
        
        // 添加一个右转箭头
        level1.arrowBlocks.Add(new ArrowBlockData
        {
            startPosition = new Vector3(-2, 0, 0),
            arrowType = ArrowBlock.ArrowType.TurnRight,
            initialDirection = ArrowBlock.Direction.Forward,
            blockColor = Color.green
        });
        
        levels.Add(level1);
        Debug.Log("创建示例关卡完成！");
    }
}
