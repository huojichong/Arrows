using Gesture;
using Gesture.Handlers;
using UnityEngine;


public class GridDragPathHandler : IGestureHandler
{
    public System.Action<Vector2, Vector2> OnDragPath;

    public void OnDrag(SingleFingerGestureContext ctx)
    {
        Vector2 from = new Vector2(
            ctx.screenLast.x,
            ctx.screenLast.y
        );

        Vector2 to = new Vector2(
            ctx.screenCurrent.x,
            ctx.screenCurrent.y
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

