using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Splines;

// [ExecuteInEditMode]
public class SplineExtrudeSnake : MonoBehaviour, IArrow<ArrowData>
{

    [SerializeField]
    private Mesh m_mesh;
    public SplineExtrude SplineExtrude;

    public SplineEndCap head;
    public SplineEndCap tail;

    public SnakePath snakePath;

    private Mesh copyMesh;

    [SerializeField]
    private float totalLength;

    private void Awake()
    {
        Debug.Log("xxx");
    }

    #region override

    public ArrowData ArrowData { get; protected set; }

    public void SetData(ArrowData data)
    {
        this.ArrowData = data;
    }

    public bool IsMoving { get; }

    public void InitArrow()
    {
        // 先设置数据，最后设置 mesh
        // 设置范围，比例
        totalLength = PathTool.CalcLength(snakePath.waypoints);
        
        float endDistance = snakePath.GetDistanceOnSpline(ArrowData.customPath[^1]);

        CalcRange(0, endDistance);
        SplineExtrude.RebuildOnSplineChange = true;

        // 拷贝 mesh
        copyMesh = Instantiate(m_mesh);
        // 触发重建
        SplineExtrude.targetMesh = copyMesh;
    }


    /// <summary>
    /// ❌ 不要再用 waypoint 的直线距离
    /// ❌ 不要用 index 比例
    /// ❌ 不要用 PathTool.CalcLength(waypoints)
    /// ✅ 一定要用 spline 本身的弧长参数空间
    /// </summary>
    private void CalcRange(float startDistance, float endDistance)
    {
        float tStart = SplineUtility.GetNormalizedInterpolation(
            snakePath.splineContainer.Spline, startDistance, PathIndexUnit.Distance);

        float tEnd = SplineUtility.GetNormalizedInterpolation(
            snakePath.splineContainer.Spline, endDistance, PathIndexUnit.Distance);

        UpdateRange(tStart, tEnd);
    }
    
    /// <summary>
    /// SplineExtrude 在 Range 边界 + Broken Bezier + 极小角度时，Frame 构建不稳定导致的可视化伪弯曲。
    /// SplineExtrude 在 Range 端点采样到一个几何/切线不连续点时，发生法线和截面旋转异常，表现为模型突然弯曲；微调 Range 避开该点即可消失。
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// </summary>
    private void UpdateRange(float x, float y)
    {
        SplineExtrude.Range = new Vector2(x, y);
        UpdateFollowers(x, y);
    }
    
#if UNITY_EDITOR
    void Update()
    {
        UpdateFollowers(SplineExtrude.Range.x, SplineExtrude.Range.y);
    }
#endif

    private void UpdateFollowers(float x, float y)
    {
        if (snakePath == null || snakePath.splineContainer == null) return;

        var container = snakePath.splineContainer;
        var containerTransform = container.transform;


        void UpdatePos(SplineEndCap tranns,float t)
        {
            tranns.UpdateCapPos();
        }
        
        UpdatePos(head,y);
        UpdatePos(tail,x);
        
        // 头部跟随 Range.y (End of range)
        // if (head != null)
        // {
        //     if (container.Evaluate(y, out var pos, out var tangent, out var up))
        //     {
        //         // 计算世界空间的位置和旋转
        //         // Vector3 worldPos = containerTransform.TransformPoint((Vector3)pos);
        //         Quaternion worldRot = containerTransform.rotation * Quaternion.LookRotation((Vector3)tangent, (Vector3)up);
        //
        //         // 转换为相对于当前节点的本地空间 (因为头尾现在是子节点)
        //         // head.localPosition = transform.InverseTransformPoint(worldPos);
        //         head.localPosition = pos;
        //         head.localRotation = Quaternion.Inverse(transform.rotation) * worldRot;
        //     }
        // }
        //
        // // 尾部跟随 Range.x (Start of range)
        // if (tail != null)
        // {
        //     if (container.Evaluate(x, out var pos, out var tangent, out var up))
        //     {
        //         // 计算世界空间的位置和旋转
        //         // Vector3 worldPos = containerTransform.TransformPoint((Vector3)pos);
        //         Quaternion worldRot = containerTransform.rotation * Quaternion.LookRotation((Vector3)tangent, (Vector3)up);
        //
        //         // 转换为相对于当前节点的本地空间
        //         tail.localPosition = pos;
        //         tail.localRotation = Quaternion.Inverse(transform.rotation) * worldRot;
        //     }
        // }
    }

    public void Reset()
    {
        // 删除 mesh
        if (copyMesh != null)
        {
            Destroy(copyMesh);
        }
    }

    public void MoveOut()
    {
        var startValue = SplineExtrude.Range;
        // todo 移动速率，起点不同，移动的长度也不同。
        // 待定现在在每个头的位置，额外增加30个长度，
        Tween.Custom(0, 1 - startValue.y, onValueChange: (v) =>
        {
            // SplineExtrude.Range = new Vector2(startValue.x + v, startValue.y + v);
            UpdateRange(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 1f).OnComplete(() => { Destroy(this.gameObject); });
    }

    /// <summary>
    /// 单位格子数 需要有反弹
    /// </summary>
    /// <param name="gridCnt"></param>
    public void StartMoving(float gridCnt)
    {
        //todo 时间最好根据距离来计算，移动距离不同，移动数据不一样，看起来太生硬了
        var startValue = SplineExtrude.Range;
        var move = gridCnt / totalLength + startValue.y;
        Tween.Custom(0, move - startValue.y, onValueChange: (v) =>
        {
            UpdateRange(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 0.2f, cycleMode: CycleMode.Yoyo, cycles: 2);
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        snakePath.waypoints = points;
        snakePath.UpdateSplineFromWaypoints();
    }
    #endregion
 
    
    #region IArrow override
    // 显式实现非泛型接口
    IArrowData IArrow.ArrowData => ArrowData;
    void IArrow.SetData(IArrowData data)
    {
        SetData(data as ArrowData);
    }
    #endregion

}

