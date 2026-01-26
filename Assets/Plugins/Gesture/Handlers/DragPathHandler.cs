using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class DragPathHandler : IGestureHandler
{
    public System.Action<Vector2, Vector2> OnDragPath;

    public void OnDrag(SingleFingerGestureContext ctx)
    {
        Vector2 from = new Vector2(
            ctx.worldDown.x,
            ctx.worldDown.z
        );

        Vector2 to = new Vector2(
            ctx.worldCurrent.x,
            ctx.worldCurrent.z
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

