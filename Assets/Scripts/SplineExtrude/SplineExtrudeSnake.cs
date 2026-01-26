using System.Collections.Generic;
using Unity.VisualScripting;
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

    }

    public void StartMoving(float distance)
    {

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

