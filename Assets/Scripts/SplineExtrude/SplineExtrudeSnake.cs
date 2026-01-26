using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Splines;

public class SplineExtrudeSnake : MonoBehaviour, IArrow<ArrowData>
{

    [SerializeField]
    private Mesh m_mesh;
    public SplineExtrude SplineExtrude;

    public SnakePath snakePath;

    private Mesh copyMesh;


    [SerializeField]
    private float totalLength;
    // /// <summary>
    // /// 设置线段容器
    // /// </summary>
    // /// <param name="splineContainer"></param>
    // public void SetSplineContainer(SplineContainer splineContainer)
    // {
    //     this.SplineExtrude.Container = splineContainer;
    //     snakePath.splineContainer = splineContainer;
    // }


    #region override


    public ArrowData ArrowData { get; protected set; }

    public void SetData(ArrowData data)
    {
        this.ArrowData = data;
    }

    public Transform Transform { get; }
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
        SplineExtrude.Range = new Vector2(x , y );
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
        // todo 
        StartMoving(0);
    }

    public void StartMoving(float distance)
    {
        // 使用 PrimeTween 进行移动，修改 SplineExtrude 的比例
        // Tween.To(SplineExtrude, distance, new PrimeTweenConfig().SetEase(Ease.Linear).SetOnUpdate((float t) => SplineExtrude.Ratio = t));
        var startValue = SplineExtrude.Range;
        Tween.Custom(0, 1 - startValue.y, onValueChange: (v) =>
        {
            // SplineExtrude.Range = new Vector2(startValue.x + v, startValue.y + v);
            UpdateRange(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 1f).OnComplete(() =>
        {
            Destroy(this.gameObject);
        });
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
        snakePath.waypoints = points;
        snakePath.UpdateSplineFromWaypoints();
    }


    #region IArrow override

    // 显式实现非泛型接口
    IArrowData IArrow.ArrowData => ArrowData;
    void IArrow.SetData(IArrowData data)
    {
        SetData(data as ArrowData);
    }

    #endregion

    #endregion

}

