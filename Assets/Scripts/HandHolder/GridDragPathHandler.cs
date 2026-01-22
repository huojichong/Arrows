using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class GridDragPathHandler : IGestureHandler
{
    public System.Action<Vector2Int, Vector2Int> OnDragPath;

    public void OnDrag(SingleFingerGestureContext ctx)
    {
        Vector2Int from = new Vector2Int(
            Mathf.RoundToInt(ctx.worldDown.x),
            Mathf.RoundToInt(ctx.worldDown.z)
        );

        Vector2Int to = new Vector2Int(
            Mathf.RoundToInt(ctx.worldCurrent.x),
            Mathf.RoundToInt(ctx.worldCurrent.z)
        );

        OnDragPath?.Invoke(from, to);
    }

    public void OnPointerDown(SingleFingerGestureContext ctx)
    {
    }

    public void OnClick(SingleFingerGestureContext ctx)
    {
    }

    public void OnDragBegin(SingleFingerGestureContext ctx)
    {
    }

    public void OnDragEnd(SingleFingerGestureContext ctx)
    {
    }
}

