using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 分段箭头系统，实现类似贪吃蛇的长条形箭头，支持弯曲效果
/// </summary>
public class SegmentedArrow : MonoBehaviour
{
    [Header("分段配置")]
    public int segmentCount = 8; // 箭头分段数量（类似蛇的身体长度）
    public float segmentSpacing = 0.3f; // 分段间距
    public GameObject segmentPrefab; // 分段预制体
    
    [Header("移动配置")]
    public float moveSpeed = 3f;
    public bool isMoving = false;
    public bool keepInitialRotation = true; // 是否保持初始方向
    
    [Header("箭头类型")]
    public ArrowBlock.ArrowType arrowType = ArrowBlock.ArrowType.Straight;
    public ArrowBlock.Direction currentDirection = ArrowBlock.Direction.Forward;
    
    [Header("组件引用")]
    public CurvedPathFollower pathFollower;
    
    private List<ArrowSegment> segments = new List<ArrowSegment>();
    private List<Vector3> targetPath = new List<Vector3>(); // 目标路径
    private int currentPathIndex = 0;
    private GameObject headObject; // 头部对象，用于移动
    private Quaternion initialRotation; // 初始旋转
    
    void Awake()
    {
        if (pathFollower == null)
        {
            pathFollower = gameObject.AddComponent<CurvedPathFollower>();
        }
        
        InitializeSegments();
    }
    
    void Start()
    {
        // 保存初始旋转
        initialRotation = transform.rotation;
        GenerateArrowPath();
    }
    
    /// <summary>
    /// 初始化箭头分段
    /// </summary>
    void InitializeSegments()
    {
        // 创建头部对象（不可见，用于记录路径）
        headObject = new GameObject("ArrowHead");
        headObject.transform.parent = transform;
        headObject.transform.position = transform.position;
        headObject.transform.rotation = transform.rotation;
        
        // 保存初始旋转
        initialRotation = transform.rotation;
        
        // 将路径跟随器附加到头部
        if (pathFollower.transform.parent != headObject.transform)
        {
            pathFollower.transform.parent = headObject.transform;
            pathFollower.transform.localPosition = Vector3.zero;
            pathFollower.transform.localRotation = Quaternion.identity;
        }
        
        // 创建分段
        for (int i = 0; i < segmentCount; i++)
        {
            CreateSegment(i);
        }
    }
    
    /// <summary>
    /// 创建单个分段
    /// </summary>
    void CreateSegment(int index)
    {
        GameObject segmentObj;
        
        if (segmentPrefab != null)
        {
            segmentObj = Instantiate(segmentPrefab, transform);
        }
        else
        {
            // 如果没有预制体，创建默认立方体
            segmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObj.transform.parent = transform;
            segmentObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }
        
        segmentObj.name = $"Segment_{index}";
        
        // 添加分段组件
        ArrowSegment segment = segmentObj.GetComponent<ArrowSegment>();
        if (segment == null)
        {
            segment = segmentObj.AddComponent<ArrowSegment>();
        }
        
        segment.segmentIndex = index;
        
        // 设置初始位置（向后排列）
        Vector3 initialPos = transform.position - GetDirectionVector() * segmentSpacing * index;
        segment.SetPosition(initialPos, transform.rotation);
        
        // 设置视觉样式
        bool isHead = (index == 0);
        bool isTail = (index == segmentCount - 1);
        segment.SetVisualStyle(isHead, isTail);
        
        segments.Add(segment);
    }
    
    /// <summary>
    /// 生成箭头路径
    /// </summary>
    void GenerateArrowPath()
    {
        targetPath.Clear();
        targetPath.Add(transform.position);
        
        Vector3 currentPos = transform.position;
        ArrowBlock.Direction currentDir = currentDirection;
        
        switch (arrowType)
        {
            case ArrowBlock.ArrowType.Straight:
                // 直线前进
                for (int i = 1; i <= 5; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
                }
                break;
                
            case ArrowBlock.ArrowType.TurnLeft:
                // 前进后左转
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
                }
                currentDir = TurnLeft(currentDir);
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
                }
                break;
                
            case ArrowBlock.ArrowType.TurnRight:
                // 前进后右转
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
                }
                currentDir = TurnRight(currentDir);
                for (int i = 1; i <= 3; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
                }
                break;
                
            case ArrowBlock.ArrowType.UTurn:
                // U型转弯
                currentPos += GetDirectionVector(currentDir) * 1f;
                targetPath.Add(currentPos);
                currentDir = TurnLeft(currentDir);
                currentPos += GetDirectionVector(currentDir) * 1f;
                targetPath.Add(currentPos);
                currentDir = TurnLeft(currentDir);
                for (int i = 1; i <= 2; i++)
                {
                    currentPos += GetDirectionVector(currentDir) * 1f;
                    targetPath.Add(currentPos);
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
        
        // 开始记录路径
        pathFollower.StartRecording();
        
        // 移动头部沿着目标路径
        while (currentPathIndex < targetPath.Count - 1)
        {
            currentPathIndex++;
            Vector3 targetPos = targetPath[currentPathIndex];
            
            // 如果保持初始方向，不旋转头部
            if (!keepInitialRotation)
            {
                // 计算移动方向并旋转头部
                Vector3 moveDir = (targetPos - headObject.transform.position).normalized;
                if (moveDir != Vector3.zero)
                {
                    headObject.transform.rotation = Quaternion.LookRotation(moveDir);
                }
            }
            else
            {
                // 保持初始旋转
                headObject.transform.rotation = initialRotation;
            }
            
            // 移动头部到目标位置
            while (Vector3.Distance(headObject.transform.position, targetPos) > 0.01f)
            {
                headObject.transform.position = Vector3.MoveTowards(
                    headObject.transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
                
                // 更新路径记录
                pathFollower.UpdateRecording();
                
                // 更新所有分段位置
                UpdateSegments();
                
                yield return null;
            }
            
            headObject.transform.position = targetPos;
        }
        
        isMoving = false;
        Debug.Log("箭头移动完成！");
    }
    
    /// <summary>
    /// 更新所有分段的位置（贪吃蛇效果）
    /// </summary>
    void UpdateSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            // 计算该分段应该在的距离
            float distance = (i + 1) * segmentSpacing;
            
            Vector3 segmentPos;
            Quaternion segmentRot;
            
            if (pathFollower.GetPositionAtDistance(distance, out segmentPos, out segmentRot))
            {
                segments[i].targetPosition = segmentPos;
                
                // 如果保持初始方向，使用初始旋转，否则跟随路径旋转
                if (keepInitialRotation)
                {
                    segments[i].targetRotation = initialRotation;
                }
                else
                {
                    segments[i].targetRotation = segmentRot;
                }
                
                segments[i].SmoothMove(0.1f);
            }
        }
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
    
    Vector3 GetDirectionVector()
    {
        return GetDirectionVector(currentDirection);
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
    /// 可视化目标路径
    /// </summary>
    void OnDrawGizmos()
    {
        if (targetPath == null || targetPath.Count < 2)
        {
            return;
        }
        
        Gizmos.color = Color.green;
        for (int i = 0; i < targetPath.Count - 1; i++)
        {
            Gizmos.DrawLine(targetPath[i], targetPath[i + 1]);
            Gizmos.DrawWireSphere(targetPath[i], 0.1f);
        }
        Gizmos.DrawWireSphere(targetPath[targetPath.Count - 1], 0.15f);
    }
}
