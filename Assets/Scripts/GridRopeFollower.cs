using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 示例：控制绳子在网格上移动，并演示拉伸效果
/// </summary>
public class GridRopeFollower : MonoBehaviour
{
    // public SplineRopeSnakeManager ropeSnakeMgr;
    // public float gridSize = 1f;
    
    [Header("Demo Settings")]
    // public float stretchSpeed = 0.5f;
    // public bool autoMove = true;

    private List<Vector3> demoWaypoints = new List<Vector3>();

    void Start()
    {
        // if (ropeSnakeMgr == null)
        //     ropeSnakeMgr = GetComponent<SplineRopeSnakeManager>();

        // 创建一个演示路径：直走 -> 左转 -> 右转 -> 直走
        Vector3 p = transform.position;
        demoWaypoints.Add(p);
        p += Vector3.forward * 3;
        demoWaypoints.Add(p);
        p += Vector3.left * 3;
        demoWaypoints.Add(p);
        p += Vector3.forward * 3;
        demoWaypoints.Add(p);
        p += Vector3.right * 3;
        demoWaypoints.Add(p);

        p += Vector3.forward * 4;
        demoWaypoints.Add(p);

        p += Vector3.left * 5;
        demoWaypoints.Add(p);


        // ropeSnakeMgr.SetupSnake(demoWaypoints);
        // ropeSnake.isMoving = autoMove;
    }

    void Update()
    {
        // 按键演示方向控制 (可选)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }
}
