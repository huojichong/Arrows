namespace Gesture.Handlers
{
    public interface IGestureHandler
    {
        void OnPointerDown(SingleFingerGestureContext ctx);
        void OnClick(SingleFingerGestureContext ctx);
        void OnDragBegin(SingleFingerGestureContext ctx);
        void OnDrag(SingleFingerGestureContext ctx);
        void OnDragEnd(SingleFingerGestureContext ctx);
    }

}