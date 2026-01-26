using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
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
    
    [Header("关卡配置")]
    public int currentLevel = 1;
    public int totalMoves = 0;
    
    [Header("引用")]
    public List<IArrow> arrowBlocks = new List<IArrow>();
    
    [Header("关卡配置")]
    public GameObject arrowBlockPrefab;

    [SerializeField]
    private GestureManager gestureManager;
    
    [SerializeField]
    private GridSystem gridSystem;

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
        else if(IsCanMoveOut(arrow,gridSystem))
        {
            gridSystem.UnregisterArrowBlock(arrow);
            arrow.MoveOut();
        }
        else
        {
            Debug.Log("无法移动出去。。");
            return;
        }
        
        totalMoves++;

        // 检查是否有其他箭头在移动
        CheckGameState();
    }

    private bool IsCanMoveOut(IArrow arrow, GridSystem gridSystem1)
    {
        return true;
        // 检查前方是否有别的方块阻挡
        var header = arrow.ArrowData.header;
        var dir = arrow.ArrowData.direction;
        for (int i = 1; i < 30; i++)
        {
            if (gridSystem1.IsGridOccupied(header + dir * i))
            {
                return false;
            }
        }
        
        return true;
    }


    private IEnumerator CreateSnakeCor()
    {
        var data = LevelDataReader.LoadLevelData(0);
        foreach (var blockData in data.arrowBlocks)
        {
            // yield return new WaitForEndOfFrame();
            var arrow = CreateArrowBlock(blockData);
            gridSystem.RegisterArrowBlock(arrow);
            // yield break;
        }
        yield return new WaitForEndOfFrame();
    }

    /// <summary>
    /// 创建箭头块
    /// </summary>
    IArrow CreateArrowBlock(ArrowData data)
    {
        var ropeSnake = Instantiate(arrowBlockPrefab).GetComponent<IArrow>();
        var path = new List<Vector3>();
        var arrVect = new Vector3(data.direction.x, data.direction.y, data.direction.z);
        
        var endPos = data.customPath.Last() + arrVect * 30;

        bool isRemoveSameLinePoint = false;

        if (isRemoveSameLinePoint)
        {
            var firstPos = data.customPath.First();
            path.Add(new Vector3(firstPos.x, firstPos.y, firstPos.z));
            
            for(int i = 1;i<data.customPath.Count - 1;i++)
            {
                var pre = data.customPath[i - 1];
                var current = data.customPath[i];
                var next = data.customPath[i + 1];
            
                // 判断点 pre, current,next 是否在同一条线上，
                if (Vector3.Dot(current - pre, next - current) < 0.01f)
                {
                    // 不在同一条线上
                    Debug.Log("点 pre, current,next 不在同一条线上");
                
                
                    path.Add(new Vector3(current.x, current.y, current.z));
                }
            
            }
            var lastPos = data.customPath.Last();
            path.Add(new Vector3(lastPos.x, lastPos.y, lastPos.z));
        }
        else
        {
            foreach (var pos in data.customPath)
            {
                path.Add(new Vector3(pos.x,pos.y,pos.z));
            }
        }
        // 还有头的显示, 最后一个是头
        
        path.Add(endPos);
        // 延长起点坐标
        ropeSnake.SetWaypoints(path);
        
        ropeSnake.SetData(data);
        ropeSnake.InitArrow();
        return ropeSnake;
    }
    
    /// <summary>
    /// 检查游戏状态
    /// </summary>
    void CheckGameState()
    {
        // 检查是否所有箭头都完成移动
        bool allCompleted = false;
        
        // 检查普通箭头
        // foreach (var arrow in arrowBlocks)
        // {
        //     if (arrow.IsMoving)
        //     {
        //         allCompleted = false;
        //         break;
        //     }
        // }
        
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
