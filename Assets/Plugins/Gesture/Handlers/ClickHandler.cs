using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class ClickHandler : IGestureHandler
{
    public System.Action<Vector2> OnGridClicked;

    public void OnClick(SingleFingerGestureContext ctx)
    {
        Vector2 grid = new Vector2(
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

