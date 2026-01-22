using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 曲线路径跟随器，用于记录和回放路径，实现贪吃蛇式的跟随效果
/// </summary>
public class CurvedPathFollower : MonoBehaviour
{
    [Header("路径配置")]
    public float recordInterval = 0.05f; // 记录路径点的时间间隔
    public int maxPathPoints = 200; // 最大路径点数量
    
    [Header("路径数据")]
    public List<PathPoint> recordedPath = new List<PathPoint>();
    
    private float recordTimer = 0f;
    
    /// <summary>
    /// 路径点数据结构
    /// </summary>
    [System.Serializable]
    public struct PathPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        
        public PathPoint(Vector3 pos, Quaternion rot, float time)
        {
            position = pos;
            rotation = rot;
            timestamp = time;
        }
    }
    
    /// <summary>
    /// 开始记录路径
    /// </summary>
    public void StartRecording()
    {
        recordedPath.Clear();
        recordTimer = 0f;
        // 记录初始位置
        RecordCurrentPosition();
    }
    
    /// <summary>
    /// 记录当前位置
    /// </summary>
    void RecordCurrentPosition()
    {
        PathPoint point = new PathPoint(
            transform.position,
            transform.rotation,
            Time.time
        );
        
        recordedPath.Add(point);
        
        // 限制路径点数量，移除最老的点
        if (recordedPath.Count > maxPathPoints)
        {
            recordedPath.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 更新路径记录
    /// </summary>
    public void UpdateRecording()
    {
        recordTimer += Time.deltaTime;
        
        if (recordTimer >= recordInterval)
        {
            RecordCurrentPosition();
            recordTimer = 0f;
        }
    }
    
    /// <summary>
    /// 根据距离获取路径上的位置和旋转
    /// </summary>
    public bool GetPositionAtDistance(float distance, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        
        if (recordedPath.Count < 2)
        {
            return false;
        }
        
        float accumulatedDistance = 0f;
        
        // 从头部开始向后查找
        for (int i = recordedPath.Count - 1; i > 0; i--)
        {
            Vector3 currentPos = recordedPath[i].position;
            Vector3 previousPos = recordedPath[i - 1].position;
            
            float segmentDistance = Vector3.Distance(currentPos, previousPos);
            
            if (accumulatedDistance + segmentDistance >= distance)
            {
                // 找到目标距离所在的线段
                float remainingDistance = distance - accumulatedDistance;
                float t = remainingDistance / segmentDistance;
                
                // 在线段上插值
                position = Vector3.Lerp(currentPos, previousPos, t);
                rotation = Quaternion.Slerp(recordedPath[i].rotation, recordedPath[i - 1].rotation, t);
                
                return true;
            }
            
            accumulatedDistance += segmentDistance;
        }
        
        // 如果距离超出路径长度，返回路径起点
        if (recordedPath.Count > 0)
        {
            position = recordedPath[0].position;
            rotation = recordedPath[0].rotation;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取路径总长度
    /// </summary>
    public float GetTotalPathLength()
    {
        if (recordedPath.Count < 2)
        {
            return 0f;
        }
        
        float totalLength = 0f;
        
        for (int i = 1; i < recordedPath.Count; i++)
        {
            totalLength += Vector3.Distance(
                recordedPath[i].position,
                recordedPath[i - 1].position
            );
        }
        
        return totalLength;
    }
    
    /// <summary>
    /// 清空路径记录
    /// </summary>
    public void ClearPath()
    {
        recordedPath.Clear();
        recordTimer = 0f;
    }
    
    /// <summary>
    /// 可视化路径
    /// </summary>
    void OnDrawGizmos()
    {
        if (recordedPath == null || recordedPath.Count < 2)
        {
            return;
        }
        
        Gizmos.color = Color.cyan;
        
        for (int i = 1; i < recordedPath.Count; i++)
        {
            Gizmos.DrawLine(recordedPath[i - 1].position, recordedPath[i].position);
        }
        
        // 绘制路径点
        Gizmos.color = Color.yellow;
        foreach (var point in recordedPath)
        {
            Gizmos.DrawSphere(point.position, 0.05f);
        }
    }
}
