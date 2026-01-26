using System.Collections.Generic;
using UnityEngine;

public interface IArrowData
{
    List<Vector3Int> customPath { get; set; }
    string id { get; set; }
    
    Vector3Int header { get; }
    
    Vector3Int direction { get; set; }
}

public interface IArrow 
{
    IArrowData ArrowData { get; }
    Transform Transform { get; }
    bool IsMoving { get; }

    void InitArrow();
    void Reset();
    void MoveOut();
    void StartMoving(float distance);
    void SetWaypoints(List<Vector3> points, bool resetDistance = true);

    void SetData(IArrowData data);
}


public interface IArrow<TD> : IArrow where TD : IArrowData
{
    new TD ArrowData { get; }
    void SetData(TD data);
}
