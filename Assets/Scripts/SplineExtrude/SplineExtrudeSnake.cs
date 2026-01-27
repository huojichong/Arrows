using System.Collections.Generic;
using PrimeTween;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
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

    public bool IsMoving { get; protected set; }

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
        IsMoving = true;
        Tween.Custom(0, 1 - startValue.y, onValueChange: (v) =>
        {
            // SplineExtrude.Range = new Vector2(startValue.x + v, startValue.y + v);
            UpdateRange(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 1f).OnComplete(() =>
        {
            IsMoving = false;
            Destroy(this.gameObject);
        });
    }

    /// <summary>
    /// 单位格子数 需要有反弹
    /// </summary>
    /// <param name="gridCnt"></param>
    public void StartMoving(float gridCnt)
    {
        IsMoving = true;
        //todo 时间最好根据距离来计算，移动距离不同，移动数据不一样，看起来太生硬了
        var startValue = SplineExtrude.Range;
        var move = gridCnt / totalLength + startValue.y;
        Tween.Custom(0, move - startValue.y, onValueChange: (v) =>
        {
            UpdateRange(startValue.x + v, startValue.y + v);
            SplineExtrude.Rebuild();
        }, ease: Ease.Linear, duration: 0.2f, cycleMode: CycleMode.Yoyo, cycles: 2).OnComplete(() =>
        {
            IsMoving = false;
        });
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

    /// <summary>
    /// 当前箭头被撞击，撞击点，撞击来的方向
    /// </summary>
    /// <param name="hitPoint"></param>
    /// <param name="arrowDataDirection"></param>
    public void Hited(Vector3Int hitPoint, Vector3Int arrowDataDirection)
    {
        var spline = snakePath.splineContainer.Spline;
        // 1. 获取最近点的索引
        // 注意：GetNearestPoint 默认返回 float3，我们需要找到对应的 Knot 索引
        // 建议使用 SplineUtility 找到最近的 t，然后推算出 Knot 索引，或者遍历 Knots
        int knotIndex = GetNearestKnotIndex(hitPoint);
        if (knotIndex == -1) return;

        // 获取原始坐标
        float3 originalPos = spline[knotIndex].Position;
        // 计算撞击目标点（往撞击方向偏移）
        float3 targetPos = originalPos + (float3)((Vector3)arrowDataDirection * 0.5f); // 偏移距离可调

        // 2. 使用 PrimeTween 制作动画
        // 我们创建一个从 0 到 1 的数值动画，在更新回调里插值位置并重新赋值给 Spline
        Tween.Custom(0f, 1f, duration: 0.4f, onValueChange: newVal =>
            {
                // 计算当前帧的位置：使用 Punch 曲线效果更好，或者手动做回弹
                // 这里使用 Shake/Punch 逻辑的变体：
                // 也可以简单地：CurrentPos = Lerp(originalPos, targetPos, someCurveValue);
        
                var currentKnot = spline[knotIndex];
                // 模拟反弹：这里使用简化的插值，配合 Ease.Punch 效果最佳
                float multiplier = Mathf.Sin(newVal * Mathf.PI); // 简单的 0 -> 1 -> 0 变化
                currentKnot.Position = math.lerp(originalPos, targetPos, multiplier);
        
                // 必须重新赋值，Spline 才会更新
                spline[knotIndex] = currentKnot;
            },ease:Ease.OutQuad,cycles:2,cycleMode:CycleMode.Yoyo); // 设置缓动
    }

    // 辅助方法：找到最近的 Knot 索引
    private int GetNearestKnotIndex(Vector3 worldPos)
    {
        var splineContainer = snakePath.splineContainer;
        float3 localPos = splineContainer.transform.InverseTransformPoint(worldPos);
        float minDst = float.MaxValue;
        int index = -1;

        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            float dst = math.distance(localPos, splineContainer.Spline[i].Position);
            if (dst < minDst)
            {
                minDst = dst;
                index = i;
            }
        }
        return index;
    }
    #endregion

}

