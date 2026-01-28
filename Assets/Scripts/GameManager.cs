using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using GameController;
using Gesture;
using Gesture.Handlers;
using UnityEngine;

/// <summary>
/// 游戏管理器，负责点击检测、游戏流程控制
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("游戏配置")]
    public Camera mainCamera;
    
    
    [Header("引用")]
    public List<IArrow> arrowBlocks = new List<IArrow>();
    
    [Header("关卡配置")]
    public GameObject arrowBlockPrefab;

    [SerializeField]
    private GestureManager gestureManager;
    
    [SerializeField]
    private GridSystem gridSystem;

    private int level = 0;
    
    IGameController gameController;
    
    private void Awake()
    {
        if (gestureManager == null)
        {
            gestureManager = GetComponent<GestureManager>();
        }
        if (gridSystem == null)
        {
            gridSystem = GetComponent<GridSystem>();
        }
    }

    void Start()
    {
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // 查找场景中所有箭头块
        gridSystem.InitializeGrid();

        gameController = new ClassicsGameController();

        if (gameController is ClassicsGameController classicsGameController)
        {
            classicsGameController.InitConfig(gridSystem,arrowBlockPrefab);
        }
        
        InitGestureManager();

        gameController.InitAsync(level);
    }
    

    private void InitGestureManager()
    {
        var click = new GridClickHandler();
        
        click.OnGridClicked +=gameController.OnGridClicked;
        var drag  = new GridDragPathHandler();

        var composite = new CompositeGestureHandler(click, drag);

        gestureManager.SingleFingerHandler = composite;
  
    }
    



    /// <summary>
    /// 重置当前关卡
    /// </summary>
    public void ResetLevel()
    {
        
        // 重置普通箭头
        foreach (var arrow in arrowBlocks)
        {
            arrow.Reset();
        }
        
    }
    
    
    /// <summary>
    /// UI显示 - 可以通过GUI显示基本信息
    /// </summary>
    void OnGUI()
    {
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
