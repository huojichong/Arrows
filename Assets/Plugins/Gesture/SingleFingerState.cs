using UnityEngine;

namespace Gesture
{
    class SingleFingerState
    {
        public bool Active => _pressed;

        bool _pressed;
        bool _dragging;
        float _time;

        Vector2 _screenDown;
        Vector3 _worldDown;

        public void Begin(PointerState p, GestureManager gm)
        {
            if (!gm.RaycastPlane(p.position, out _worldDown))
                return;

            _pressed = true;
            _dragging = false;
            _time = 0f;
            _screenDown = p.position;

            gm.SingleFingerHandler?.OnPointerDown(BuildContext(p, gm));
        }

        public void Update(PointerState p, GestureManager gm)
        {
            _time += Time.deltaTime;

            float dist = Vector2.Distance(_screenDown, p.position);
            if (!_dragging && dist > gm.dragDistanceThreshold)
            {
                _dragging = true;
                gm.SingleFingerHandler?.OnDragBegin(BuildContext(p, gm));
            }

            if (_dragging)
                gm.SingleFingerHandler?.OnDrag(BuildContext(p, gm));
        }

        public void End(GestureManager gm)
        {
            if (!_dragging && _time <= gm.clickTimeThreshold)
                gm.SingleFingerHandler?.OnClick(BuildContext(default, gm));
            else if (_dragging)
                gm.SingleFingerHandler?.OnDragEnd(BuildContext(default, gm));

            Reset();
        }

        public void Reset()
        {
            _pressed = false;
            _dragging = false;
            _time = 0f;
        }

        SingleFingerGestureContext BuildContext(PointerState p, GestureManager gm)
        {
            gm.RaycastPlane(p.position, out Vector3 worldCurrent);

            return new SingleFingerGestureContext
            {
                worldDown = _worldDown,
                worldCurrent = worldCurrent,
                screenDown = _screenDown,
                screenCurrent = p.position,
                elapsed = _time
            };
        }
    }

}