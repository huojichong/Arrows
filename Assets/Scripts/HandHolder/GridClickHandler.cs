using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class GridClickHandler : IGestureHandler
{
    public System.Action<Vector3Int> OnGridClicked;

    public void OnClick(SingleFingerGestureContext ctx)
    {
        Vector3Int grid = new Vector3Int(
            Mathf.RoundToInt(ctx.worldDown.x),
            0,
            Mathf.RoundToInt(ctx.worldDown.z)
        );

        OnGridClicked?.Invoke(grid);
    }

    public void OnPointerDown(SingleFingerGestureContext ctx)
    {
    }

    public void OnDragBegin(SingleFingerGestureContext ctx)
    {
    }

    public void OnDrag(SingleFingerGestureContext ctx)
    {
    }

    public void OnDragEnd(SingleFingerGestureContext ctx)
    {
    }
}

