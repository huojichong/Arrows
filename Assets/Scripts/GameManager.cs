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
        
        Application.targetFrameRate = 60;
        
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

        click.OnGridClicked += gameController.OnGridClicked;
        var drag = new GridDragPathHandler();
        drag.OnDragPath += OnDragPath;
        var composite = new CompositeGestureHandler(click, drag);

        gestureManager.SingleFingerHandler = composite;

        gestureManager.OnTwoFingerUpdate += OnTwoFingerUpdate;
    }

    private void OnTwoFingerUpdate(TwoFingerGestureContext obj)
    {
        
    }

    private void OnDragPath(Vector2 from, Vector2 to)
    {
        // 处理拖动路径，控制相机移动， 需要有缓动
        Vector2 center = (to - from) * -0.05f;
        mainCamera.transform.position += new Vector3(center.x, 0, center.y);
        
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
}
