using UnityEngine;

/// <summary>
/// 箭头的单个分段，类似贪吃蛇的身体节点
/// </summary>
public class ArrowSegment : MonoBehaviour
{
    [Header("分段配置")]
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public int segmentIndex; // 在整个箭头中的索引
    
    [Header("视觉")]
    public MeshRenderer segmentRenderer;
    public bool isHead = false; // 是否是箭头头部
    public bool isTail = false; // 是否是箭头尾部
    
    private Vector3 currentVelocity;
    
    void Awake()
    {
        segmentRenderer = GetComponent<MeshRenderer>();
        if (segmentRenderer == null)
        {
            segmentRenderer = gameObject.AddComponent<MeshRenderer>();
        }
    }
    
    /// <summary>
    /// 平滑移动到目标位置
    /// </summary>
    public void SmoothMove(float smoothTime)
    {
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            smoothTime
        );
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            Time.deltaTime * 10f
        );
    }
    
    /// <summary>
    /// 直接设置位置（用于初始化）
    /// </summary>
    public void SetPosition(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        targetPosition = position;
        transform.rotation = rotation;
        targetRotation = rotation;
    }
    
    /// <summary>
    /// 设置分段的视觉样式
    /// </summary>
    public void SetVisualStyle(bool head, bool tail)
    {
        isHead = head;
        isTail = tail;
        
        // 根据类型调整缩放
        if (isHead)
        {
            // 箭头头部可以稍大
            transform.localScale = new Vector3(1f, 1f, 1.2f);
        }
        else if (isTail)
        {
            // 箭头尾部可以稍小
            transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }
        else
        {
            // 中间部分标准大小
            transform.localScale = Vector3.one;
        }
    }
}
