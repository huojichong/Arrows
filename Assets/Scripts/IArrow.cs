

using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IArrowData
{
    List<Vector3> customPath { get; set; }
    string id { get; set; }
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
