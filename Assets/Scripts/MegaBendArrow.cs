using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MegaFiers;

/// <summary>
/// 使用 MegaFiers 插件实现箭头的动态弯曲效果
/// 模型会根据移动路径自动弯曲，类似贪吃蛇
/// </summary>
[RequireComponent(typeof(MegaModifyObject))]
public class MegaBendArrow : MonoBehaviour
{
    [Header("箭头配置")]
    public ArrowBlock.ArrowType arrowType = ArrowBlock.ArrowType.Straight;
    public ArrowBlock.Direction currentDirection = ArrowBlock.Direction.Forward;
    
    [Header("移动参数")]
    public float moveSpeed = 3f;
    public float gridSize = 1f;
    public bool isMoving = false;
    
    [Header("弯曲配置")]
    public float maxBendAngle = 90f; // 最大弯曲角度
    public float bendSpeed = 5f; // 弯曲速度
    public MegaAxis bendAxis = MegaAxis.Y; // 弯曲轴
    
    [Header("组件引用")]
    public MegaModifyObject megaModifier;
    public MegaBend megaBend;
    
    private List<Vector3> targetPath = new List<Vector3>();
    private List<ArrowBlock.Direction> pathDirections = new List<ArrowBlock.Direction>(); // 每个路径点的方向
    private int currentPathIndex = 0;
    private Quaternion initialRotation;
    private ArrowBlock.Direction previousDirection;
    
    void Awake()
    {
        // 获取 MegaModifyObject 组件
        megaModifier = GetComponent<MegaModifyObject>();
        if (megaModifier == null)
        {
            megaModifier = gameObject.AddComponent<MegaModifyObject>();
        }
        
        // 获取或创建 MegaBend 组件
        megaBend = GetComponent<MegaBend>();
        if (megaBend == null)
        {
            megaBend = gameObject.AddComponent<MegaBend>();
        }
        
        // 配置 MegaBend
        megaBend.axis = bendAxis;
        megaBend.angle = 0f;
        megaBend.dir = 0f;
    }
    
    void Start()
    {
        initialRotation = transform.rotation;
        previousDirection = currentDirection;
        GenerateArrowPath();
    }
    
    /// <summary>
    /// 生成箭头路径
    /// </summary>
    void GenerateArrowPath()
    {
        targetPath.Clear();
        pathDirections.Clear();
        
        targetPath.Add(transform.position);
        pathDirections.Add(currentDirection);
        
        Vector3 currentPos = transform.position;
        ArrowBlock.Direction currentDir = currentDirection;
        
        switch (arrowType)
        {
            case ArrowBlock.ArrowType.Straight:
                // 直线前进
                for (int i = 1; i <= 5; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                break;
                
            case ArrowBlock.ArrowType.TurnLeft:
                // 前进后左转
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                currentDir = TurnLeft(currentDir);
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                break;
                
            case ArrowBlock.ArrowType.TurnRight:
                // 前进后右转
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                currentDir = TurnRight(currentDir);
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                break;
                
            case ArrowBlock.ArrowType.UTurn:
                // U型转弯
                currentPos += GetDirectionVector(currentDir) * gridSize;
                targetPath.Add(currentPos);
                pathDirections.Add(currentDir);
                
                currentDir = TurnLeft(currentDir);
                currentPos += GetDirectionVector(currentDir) * gridSize;
                targetPath.Add(currentPos);
                pathDirections.Add(currentDir);
                
                currentDir = TurnLeft(currentDir);
                for (int i = 1; i <= 2; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * gridSize;
                    targetPath.Add(currentPos);
                    pathDirections.Add(currentDir);
                }
                break;
        }
    }
    
    /// <summary>
    /// 点击箭头，开始移动
    /// </summary>
    public void StartMoving()
    {
        if (!isMoving)
        {
            StartCoroutine(MoveAlongPath());
        }
    }
    
    /// <summary>
    /// 沿路径移动的协程
    /// </summary>
    IEnumerator MoveAlongPath()
    {
        isMoving = true;
        currentPathIndex = 0;
        
        while (currentPathIndex < targetPath.Count - 1)
        {
            currentPathIndex++;
            Vector3 targetPos = targetPath[currentPathIndex];
            ArrowBlock.Direction targetDir = pathDirections[currentPathIndex];
            
            // 检测是否需要弯曲（方向改变）
            bool needsBend = (targetDir != previousDirection);
            
            if (needsBend)
            {
                // 计算弯曲角度和方向
                float bendAngle = CalculateBendAngle(previousDirection, targetDir);
                float bendDir = CalculateBendDirection(previousDirection, targetDir);
                
                // 开始弯曲动画
                yield return StartCoroutine(AnimateBend(bendAngle, bendDir, targetPos));
                
                previousDirection = targetDir;
            }
            else
            {
                // 直线移动，逐渐恢复弯曲
                yield return StartCoroutine(MoveStraight(targetPos));
            }
        }
        
        // 移动完成，重置弯曲
        yield return StartCoroutine(AnimateBend(0f, 0f, transform.position));
        
        isMoving = false;
        Debug.Log("箭头移动完成！");
    }
    
    /// <summary>
    /// 直线移动
    /// </summary>
    IEnumerator MoveStraight(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            
            // 逐渐减小弯曲
            if (Mathf.Abs(megaBend.angle) > 0.1f)
            {
                megaBend.angle = Mathf.Lerp(megaBend.angle, 0f, bendSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
        
        transform.position = targetPos;
    }
    
    /// <summary>
    /// 弯曲动画
    /// </summary>
    IEnumerator AnimateBend(float targetAngle, float targetDir, Vector3 targetPos)
    {
        float startAngle = megaBend.angle;
        float startDir = megaBend.dir;
        float progress = 0f;
        
        Vector3 startPos = transform.position;
        
        while (progress < 1f)
        {
            progress += Time.deltaTime * bendSpeed * 0.5f;
            progress = Mathf.Clamp01(progress);
            
            // 插值弯曲参数
            megaBend.angle = Mathf.Lerp(startAngle, targetAngle, progress);
            megaBend.dir = Mathf.Lerp(startDir, targetDir, progress);
            
            // 同时移动位置
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            
            yield return null;
        }
        
        megaBend.angle = targetAngle;
        megaBend.dir = targetDir;
        transform.position = targetPos;
    }
    
    /// <summary>
    /// 计算弯曲角度
    /// </summary>
    float CalculateBendAngle(ArrowBlock.Direction from, ArrowBlock.Direction to)
    {
        int diff = Mathf.Abs((int)to - (int)from);
        if (diff == 3) diff = 1; // 处理环形差值
        
        return maxBendAngle * diff / 4f;
    }
    
    /// <summary>
    /// 计算弯曲方向
    /// </summary>
    float CalculateBendDirection(ArrowBlock.Direction from, ArrowBlock.Direction to)
    {
        int diff = (int)to - (int)from;
        
        // 标准化差值到 -1, 0, 1
        if (diff > 2) diff -= 4;
        if (diff < -2) diff += 4;
        
        // 左转为正，右转为负
        return diff > 0 ? 0f : 180f;
    }
    
    /// <summary>
    /// 获取方向向量
    /// </summary>
    Vector3 GetDirectionVector(ArrowBlock.Direction dir)
    {
        switch (dir)
        {
            case ArrowBlock.Direction.Forward: return Vector3.forward;
            case ArrowBlock.Direction.Right: return Vector3.right;
            case ArrowBlock.Direction.Back: return Vector3.back;
            case ArrowBlock.Direction.Left: return Vector3.left;
            default: return Vector3.forward;
        }
    }
    
    /// <summary>
    /// 向左转
    /// </summary>
    ArrowBlock.Direction TurnLeft(ArrowBlock.Direction dir)
    {
        return (ArrowBlock.Direction)(((int)dir + 3) % 4);
    }
    
    /// <summary>
    /// 向右转
    /// </summary>
    ArrowBlock.Direction TurnRight(ArrowBlock.Direction dir)
    {
        return (ArrowBlock.Direction)(((int)dir + 1) % 4);
    }
    
    /// <summary>
    /// 可视化路径
    /// </summary>
    void OnDrawGizmos()
    {
        if (targetPath == null || targetPath.Count < 2)
        {
            return;
        }
        
        Gizmos.color = Color.magenta;
        for (int i = 0; i < targetPath.Count - 1; i++)
        {
            Gizmos.DrawLine(targetPath[i], targetPath[i + 1]);
            Gizmos.DrawWireSphere(targetPath[i], 0.15f);
            
            // 绘制方向改变点
            if (i > 0 && pathDirections[i] != pathDirections[i - 1])
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPath[i], 0.2f);
                Gizmos.color = Color.magenta;
            }
        }
        Gizmos.DrawWireSphere(targetPath[targetPath.Count - 1], 0.2f);
    }
}
