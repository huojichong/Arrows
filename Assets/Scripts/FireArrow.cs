using System.Collections.Generic;
using UnityEngine;

// test 
public class FireArrow : MonoBehaviour, IArrow<ArrowData>
{
    public ArrowData ArrowData { get; private set; }

    // 显式实现非泛型接口
    IArrowData IArrow.ArrowData => ArrowData;
    void IArrow.SetData(IArrowData data)
    {
        SetData(data as ArrowData);
    }

    public Transform Transform => transform;

    public bool IsMoving { get; private set; }

    public void SetData(ArrowData data)
    {
        ArrowData = data;
    }

    public void InitArrow()
    {
    }

    public void Reset()
    {
    }

    public void MoveOut()
    {
    }

    public void StartMoving(float distance)
    {
    }

    public void SetWaypoints(List<Vector3> points, bool resetDistance = true)
    {
    }

    
}