using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class GridClickHandler : IGestureHandler
{
    public System.Action<Vector2Int> OnGridClicked;

    public void OnClick(SingleFingerGestureContext ctx)
    {
        Vector2Int grid = new Vector2Int(
            Mathf.RoundToInt(ctx.worldDown.x),
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

