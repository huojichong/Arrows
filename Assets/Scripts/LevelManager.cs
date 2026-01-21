
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡管理器，用于加载和保存关卡
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("关卡配置")]
    public List<LevelData> levels = new List<LevelData>();
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
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError($"关卡索引越界: {levelIndex}");
            return;
        }
        
        // 清空当前关卡
        ClearCurrentLevel();
        
        currentLevelIndex = levelIndex;
        LevelData level = levels[levelIndex];
        
        // // 设置相机
        // Camera.main.transform.position = level.cameraPosition;
        // Camera.main.transform.eulerAngles = level.cameraRotation;
        
        // 生成箭头块
        // foreach (var arrowData in level.arrowBlocks)
        // {
        //     CreateArrowBlock(arrowData);
        // }
        
        // Debug.Log($"加载关卡: {level.levelName}");
        
        
        
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
            // arrow.arrowType = data.arrowType;
            arrow.currentDirection = data.direction;
            
            // 如果有自定义路径，使用自定义路径
            if (data.customPath != null && data.customPath.Count > 0)
            {
                foreach (var path in data.customPath)
                {
                    arrow.pathNodes.Add(new Vector3(path.x, 0,path.y));   
                }
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
        
        Debug.Log("创建示例关卡完成！");
        LoadLevel(0);
    }
}