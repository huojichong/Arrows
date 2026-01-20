using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏管理器，负责点击检测、游戏流程控制
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("游戏配置")]
    public Camera mainCamera;
    public LayerMask arrowLayer;
    
    [Header("关卡配置")]
    public int currentLevel = 1;
    public int totalMoves = 0;
    
    [Header("引用")]
    public List<ArrowBlock> arrowBlocks = new List<ArrowBlock>();
    public List<SegmentedArrow> segmentedArrows = new List<SegmentedArrow>();
    public List<MegaBendArrow> megaBendArrows = new List<MegaBendArrow>();
    
    private ArrowBlock selectedArrow = null;
    private SegmentedArrow selectedSegmentedArrow = null;
    private MegaBendArrow selectedMegaBendArrow = null;
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // 查找场景中所有箭头块
        FindAllArrowBlocks();
        FindAllSegmentedArrows();
        FindAllMegaBendArrows();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 鼠标点击检测
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, arrowLayer))
            {
                // 先检查是否是 MegaBend 箭头
                MegaBendArrow megaBendArrow = hit.collider.GetComponent<MegaBendArrow>();
                if (megaBendArrow != null)
                {
                    OnMegaBendArrowClicked(megaBendArrow);
                    return;
                }
                
                // 再检查是否是分段箭头
                SegmentedArrow segArrow = hit.collider.GetComponentInParent<SegmentedArrow>();
                if (segArrow != null)
                {
                    OnSegmentedArrowClicked(segArrow);
                    return;
                }
                
                // 最后检查是否是普通箭头
                ArrowBlock arrow = hit.collider.GetComponent<ArrowBlock>();
                if (arrow != null)
                {
                    OnArrowClicked(arrow);
                }
            }
        }
        
        // 触摸屏支持
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, arrowLayer))
            {
                // 先检查是否是 MegaBend 箭头
                MegaBendArrow megaBendArrow = hit.collider.GetComponent<MegaBendArrow>();
                if (megaBendArrow != null)
                {
                    OnMegaBendArrowClicked(megaBendArrow);
                    return;
                }
                
                // 再检查是否是分段箭头
                SegmentedArrow segArrow = hit.collider.GetComponentInParent<SegmentedArrow>();
                if (segArrow != null)
                {
                    OnSegmentedArrowClicked(segArrow);
                    return;
                }
                
                // 最后检查是否是普通箭头
                ArrowBlock arrow = hit.collider.GetComponent<ArrowBlock>();
                if (arrow != null)
                {
                    OnArrowClicked(arrow);
                }
            }
        }
    }
    
    /// <summary>
    /// 箭头被点击时的处理
    /// </summary>
    void OnArrowClicked(ArrowBlock arrow)
    {
        if (arrow.isMoving)
        {
            Debug.Log("箭头正在移动中，无法点击");
            return;
        }
        
        Debug.Log($"点击了箭头: {arrow.name}");
        selectedArrow = arrow;
        arrow.OnArrowClicked();
        totalMoves++;
        
        // 检查是否有其他箭头在移动
        CheckGameState();
    }
    
    /// <summary>
    /// 查找场景中所有箭头块
    /// </summary>
    void FindAllArrowBlocks()
    {
        arrowBlocks.Clear();
        ArrowBlock[] foundArrows = FindObjectsOfType<ArrowBlock>();
        arrowBlocks.AddRange(foundArrows);
        Debug.Log($"找到 {arrowBlocks.Count} 个箭头块");
    }
    
    /// <summary>
    /// 查找场景中所有分段箭头
    /// </summary>
    void FindAllSegmentedArrows()
    {
        segmentedArrows.Clear();
        SegmentedArrow[] foundSegmented = FindObjectsOfType<SegmentedArrow>();
        segmentedArrows.AddRange(foundSegmented);
        Debug.Log($"找到 {segmentedArrows.Count} 个分段箭头");
    }
    
    /// <summary>
    /// 查找场景中所有 MegaBend 箭头
    /// </summary>
    void FindAllMegaBendArrows()
    {
        megaBendArrows.Clear();
        MegaBendArrow[] foundMegaBend = FindObjectsOfType<MegaBendArrow>();
        megaBendArrows.AddRange(foundMegaBend);
        Debug.Log($"找到 {megaBendArrows.Count} 个 MegaBend 箭头");
    }
    
    /// <summary>
    /// MegaBend 箭头被点击时的处理
    /// </summary>
    void OnMegaBendArrowClicked(MegaBendArrow arrow)
    {
        if (arrow.isMoving)
        {
            Debug.Log("箭头正在移动中，无法点击");
            return;
        }
        
        Debug.Log($"点击了 MegaBend 箭头: {arrow.name}");
        selectedMegaBendArrow = arrow;
        arrow.StartMoving();
        totalMoves++;
        
        CheckGameState();
    }
    void OnSegmentedArrowClicked(SegmentedArrow arrow)
    {
        if (arrow.isMoving)
        {
            Debug.Log("箭头正在移动中，无法点击");
            return;
        }
        
        Debug.Log($"点击了分段箭头: {arrow.name}");
        selectedSegmentedArrow = arrow;
        arrow.StartMoving();
        totalMoves++;
        
        CheckGameState();
    }
    
    /// <summary>
    /// 检查游戏状态
    /// </summary>
    void CheckGameState()
    {
        // 检查是否所有箭头都完成移动
        bool allCompleted = true;
        
        // 检查普通箭头
        foreach (var arrow in arrowBlocks)
        {
            if (arrow.isMoving)
            {
                allCompleted = false;
                break;
            }
        }
        
        // 检查分段箭头
        if (allCompleted)
        {
            foreach (var arrow in segmentedArrows)
            {
                if (arrow.isMoving)
                {
                    allCompleted = false;
                    break;
                }
            }
        }
        
        // 检查 MegaBend 箭头
        if (allCompleted)
        {
            foreach (var arrow in megaBendArrows)
            {
                if (arrow.isMoving)
                {
                    allCompleted = false;
                    break;
                }
            }
        }
        
        if (allCompleted)
        {
            CheckLevelComplete();
        }
    }
    
    /// <summary>
    /// 检查关卡是否完成
    /// </summary>
    void CheckLevelComplete()
    {
        // 这里可以添加关卡完成的判断逻辑
        // 例如：检查所有箭头是否到达目标位置
        Debug.Log("所有箭头移动完成！");
    }
    
    /// <summary>
    /// 重置当前关卡
    /// </summary>
    public void ResetLevel()
    {
        totalMoves = 0;
        
        // 重置普通箭头
        foreach (var arrow in arrowBlocks)
        {
            arrow.StopAllCoroutines();
            arrow.isMoving = false;
        }
        
        // 重置分段箭头
        foreach (var arrow in segmentedArrows)
        {
            arrow.StopAllCoroutines();
            arrow.isMoving = false;
        }
        
        // 重置 MegaBend 箭头
        foreach (var arrow in megaBendArrows)
        {
            arrow.StopAllCoroutines();
            arrow.isMoving = false;
        }
    }
    
    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LoadNextLevel()
    {
        currentLevel++;
        // 实现关卡加载逻辑
        Debug.Log($"加载第 {currentLevel} 关");
    }
    
    /// <summary>
    /// UI显示 - 可以通过GUI显示基本信息
    /// </summary>
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), $"关卡: {currentLevel}");
        GUI.Label(new Rect(10, 40, 200, 30), $"移动次数: {totalMoves}");
        
        if (GUI.Button(new Rect(10, 80, 100, 30), "重置关卡"))
        {
            ResetLevel();
        }
        
        // 提示信息
        GUI.Label(new Rect(10, 120, 400, 100), 
            "点击箭头块开始移动\n" +
            "普通箭头: 简单移动\n" +
            "分段箭头: 类似贪吃蛇，支持弯曲效果\n" +
            "MegaBend 箭头: 使用 MegaFiers 插件，模型弯曲效果");
    }
}
