using System.Collections.Generic;

namespace Gesture.Handlers
{
    /// <summary>
    /// 同一套输入事件，需要同时驱动多种手势逻辑，
    /// 但又不希望在 GestureRecognizer 里写 if / switch。
    /// </summary>
    public class CompositeGestureHandler : IGestureHandler
    {
        readonly List<IGestureHandler> _handlers;

        public CompositeGestureHandler(params IGestureHandler[] handlers)
        {
            _handlers = new List<IGestureHandler>(handlers);
        }

        public void Add(IGestureHandler handler)
        {
            if (handler != null && !_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public void Remove(IGestureHandler handler)
        {
            _handlers.Remove(handler);
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        public void OnPointerDown(SingleFingerGestureContext ctx)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].OnPointerDown(ctx);
        }

        public void OnClick(SingleFingerGestureContext ctx)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].OnClick(ctx);
        }

        public void OnDragBegin(SingleFingerGestureContext ctx)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].OnDragBegin(ctx);
        }

        public void OnDrag(SingleFingerGestureContext ctx)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].OnDrag(ctx);
        }

        public void OnDragEnd(SingleFingerGestureContext ctx)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].OnDragEnd(ctx);
        }
    }

}