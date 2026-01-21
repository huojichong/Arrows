using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

/// <summary>
/// 关卡管理器，用于加载和保存关卡
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("关卡配置")]
    // public List<LevelData> levels = new List<LevelData>();
    public GameObject arrowBlockPrefab;
    
    private int currentLevelIndex = 0;

    private void Start()
    {
        CreateExampleLevel();
    }

    /// <summary>
    /// 加载指定关卡
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        // if (levelIndex < 0 || levelIndex >= levels.Count)
        // {
        //     Debug.LogError($"关卡索引越界: {levelIndex}");
        //     return;
        // }
        //
        // 清空当前关卡
        ClearCurrentLevel();
        
        currentLevelIndex = levelIndex;
        StartCoroutine(CreateSnakeCor());
    }

    private IEnumerator CreateSnakeCor()
    {
        var data = LevelDataReader.LoadLevelData(0);
        foreach (var blockData in data.arrowBlocks)
        {
            yield return new WaitForEndOfFrame();
            CreateArrowBlock(blockData);
            // yield break;
        }
    }

    /// <summary>
    /// 创建箭头块
    /// </summary>
    GameObject CreateArrowBlock(ArrowBlockData data)
    {
        var splineRopeController = Instantiate(arrowBlockPrefab).GetComponent<SplineRopeController>();
        var path = new List<Vector3>();
        var endPos = data.customPath.Last() + new Vector3(data.direction.x, 0, data.direction.y) * 10;
        path.AddRange(data.customPath);
        path.Add(endPos);
        // 延长起点坐标
        splineRopeController.SetWaypoints(path);
        
        splineRopeController.baseLength = data.pathLength;
        splineRopeController.currentDistance = data.pathLength;
        splineRopeController.isMoving = true;
        return splineRopeController.gameObject;
    }
    
    /// <summary>
    /// 清空当前关卡
    /// </summary>
    void ClearCurrentLevel()
    {
        SplineRopeController[] arrows = FindObjectsOfType<SplineRopeController>();
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
        
        Debug.Log("创建示例关卡完成！");
        LoadLevel(0);
    }
}