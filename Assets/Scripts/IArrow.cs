

using System.Collections.Generic;
using UnityEngine;

public interface IArrow : IArrow<IArrowData>
{
    Transform Transform { get; }
    bool IsMoving { get; }
    void Reset();

    void MoveOut();
    
    void StartMoving(float distance);
    
    public void SetWaypoints(List<Vector3> points, bool resetDistance = true);
}

public interface IArrow<TD> where TD : IArrowData
{
    TD arrowData { get; set; }
    
    void SetData(TD arrowData);
}
