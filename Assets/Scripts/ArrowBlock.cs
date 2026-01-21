using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ConfigBean;

/// <summary>
/// 3D箭头块，支持直角转弯和沿路径移动
/// </summary>
public class ArrowBlock : MonoBehaviour
{
    [Header("箭头配置")]
    // public ArrowType arrowType = ArrowType.Straight;
    public Vector2Int currentDirection = Vector2Int.up;
    
    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float gridSize = 1f;
    public bool keepInitialSnakeDirection = true; // 是否保持初始方向
    
    [Header("状态")]
    public bool isMoving = false;
    public List<Vector3> pathNodes = new List<Vector3>(); // 箭头路径节点
    
    private int currentPathIndex = 0;
    private Vector3 targetPosition;
    
    
    void Start()
    {
        // 保存初始旋转
        InitializeArrowPath();
    }
    
    /// <summary>
    /// 初始化箭头路径
    /// </summary>
    void InitializeArrowPath()
    {
        pathNodes.Clear();
        pathNodes.Add(transform.position);
        
    }
    
    
    /// <summary>
    /// 点击箭头时触发移动
    /// </summary>
    public void OnArrowClicked()
    {
        if (!isMoving && pathNodes.Count > 1)
        {
            StartCoroutine(MoveAlongPath());
        }
    }
    
    /// <summary>
    /// 沿路径移动协程
    /// </summary>
    IEnumerator MoveAlongPath()
    {
        isMoving = true;
        currentPathIndex = 0;
        
        while (currentPathIndex < pathNodes.Count - 1)
        {
            currentPathIndex++;
            targetPosition = pathNodes[currentPathIndex];
            
            // 如果不保持初始方向，才计算目标旋转
            // if (!keepInitialDirection)
            // {
            //     // 计算移动方向和目标旋转
            //     Vector3 moveSnakeDirection = (targetPosition - transform.position).normalized;
            //     if (moveSnakeDirection != Vector3.zero)
            //     {
            //         targetRotation = Quaternion.LookRotation(moveDirection);
            //     }
            //     
            //     // 先旋转到目标方向
            //     yield return StartCoroutine(RotateToTarget());
            // }
            // else
            // {
            //     // 保持初始旋转
            //     targetRotation = initialRotation;
            //     transform.rotation = initialRotation;
            // }
            
            // 移动到目标位置
            yield return StartCoroutine(MoveToTarget());
        }
        
        isMoving = false;
        OnPathCompleted();
    }
    
    /// <summary>
    /// 移动到目标位置
    /// </summary>
    IEnumerator MoveToTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPosition, 
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.position = targetPosition;
    }
    
    /// <summary>
    /// 路径完成回调
    /// </summary>
    void OnPathCompleted()
    {
        Debug.Log("箭头移动完成！");
        // 可以在这里触发游戏结束检测、音效、特效等
    }
    
    
    /// <summary>
    /// 调试绘制路径
    /// </summary>
    void OnDrawGizmos()
    {
        if (pathNodes == null || pathNodes.Count < 2) return;
        
        Gizmos.color = Color.green;
        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            Gizmos.DrawLine(pathNodes[i], pathNodes[i + 1]);
            Gizmos.DrawSphere(pathNodes[i], 0.1f);
        }
        Gizmos.DrawSphere(pathNodes[pathNodes.Count - 1], 0.1f);
    }
}
