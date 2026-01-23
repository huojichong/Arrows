using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Gesture;
using Gesture.Handlers;

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
    public List<IArrow> arrowBlocks = new List<IArrow>();
    
    [Header("关卡配置")]
    public GameObject arrowBlockPrefab;

    public GameObject arrowBlockGPTPrefab;
    
    [SerializeField]
    private GestureManager gestureManager;
    
    [SerializeField]
    private GridSystem gridSystem;

    private void Awake()
    {

// 创建蛇 1：长度 6，6 个 Segment
        
// 创建蛇 2：长度 3，3 个 Segment
        // List<Vector3> path2 = new List<Vector3> { 
        //     new Vector3(10, 0, 0), 
        //     new Vector3(15, 0, 0) 
        // };
        // var snake2 = manager.AddSnake("Snake_Short", 3.0f, path2, unitLength: 1.0f, bonesPerSegment: 5);

    }

    void Start()
    {
        

        Time.timeScale = 0.1f;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // 查找场景中所有箭头块
        gridSystem.InitializeGrid();
        InitGestureManager();

        InitArrowData();
    }

    private void InitArrowData()
    {
        StartCoroutine(CreateSnakeCor());
    }

    private void InitGestureManager()
    {
        var click = new GridClickHandler();
        
        click.OnGridClicked += OnGridClicked;
        var drag  = new GridDragPathHandler();

        var composite = new CompositeGestureHandler(click, drag);

        gestureManager.SingleFingerHandler = composite;
        
        gestureManager.OnTwoFingerUpdate += ctx =>
        {
        };
    }
    

    private void OnGridClicked(Vector3Int obj)
    {
        Debug.Log("onGridClicked:" + obj);
        var arrow = gridSystem.GetArrowBlockAt(obj);

        if (arrow == null)
        {
            return;
        }
        Debug.Log($"点击了箭头: {arrow}");
        if (arrow.IsMoving)
        {
            Debug.Log("箭头正在移动中，无法点击");
            return;
        }
        else
        {
            // todo 检查前方是否有别的方块阻挡，
            // 现在直接移动出去
            gridSystem.UnregisterArrowBlock(arrow);
            arrow.MoveOut();
        }
        
        totalMoves++;

        // 检查是否有其他箭头在移动
        CheckGameState();
    }
    
    
    private IEnumerator CreateSnakeCor()
    {
        
        MultiSnakeManager manager = gameObject.GetComponent<MultiSnakeManager>();


        var data = LevelDataReader.LoadLevelData(0);
        foreach (var blockData in data.arrowBlocks)
        {
            yield return new WaitForEndOfFrame();
            
            var arrow = CreateArrowBlock(blockData,manager);

            gridSystem.RegisterArrowBlock(arrow);
            // yield break;
        }
    }

    /// <summary>
    /// 创建箭头块
    /// </summary>
    IArrow CreateArrowBlock(ArrowData data,MultiSnakeManager manager)
    {
        // var ropeSnake = Instantiate(arrowBlockPrefab).GetComponent<SplineRopeSnakeRefactored>();
        var path = new List<Vector3>();
        var arrVect = new Vector3(data.direction.x, 0, data.direction.y);
        var endPos = data.customPath.Last() + arrVect * 10;
        path.AddRange(data.customPath);
        // 还有头的显示, 最后一个是头
        path[^1] -= arrVect;
        
        path.Add(endPos);
        
        // 延长起点坐标
        // ropeSnake.SetWaypoints(path);
        // ropeSnake.SetData(data);
        // ropeSnake.InitArrow();
        var snake1 = manager.AddSnake("Snake_Long", data.pathLength, path, unitLength: 1.0f, bonesPerSegment: 5);

        return snake1;
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
            if (arrow.IsMoving)
            {
                allCompleted = false;
                break;
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
            arrow.Reset();
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
