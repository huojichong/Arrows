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
        var totalLength = PathTool.CalcLength(snakePath.waypoints);
        SplineExtrude.Range = new Vector2(0, ArrowData.pathLength / totalLength);
        
        // 拷贝 mesh
        copyMesh = Instantiate(m_mesh);
        // 触发重建
        SplineExtrude.targetMesh = copyMesh;
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
            SplineExtrude.Range = new Vector2(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 0.1f).OnComplete(() =>
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

