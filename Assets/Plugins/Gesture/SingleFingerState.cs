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
        Vector2 _lastScreenPos;
        Vector3 _worldDown;

        public void Begin(PointerState p, GestureManager gm)
        {
            if (!gm.RaycastPlane(p.position, out _worldDown))
                return;

            _pressed = true;
            _dragging = false;
            _time = 0f;
            _screenDown = p.position;
            _lastScreenPos = p.position;

            gm.SingleFingerHandler?.OnPointerDown(BuildContext(p, gm));
        }

        public void Update(PointerState p, GestureManager gm)
        {
            _time += Time.deltaTime;

            float totalDist = Vector2.Distance(_screenDown, p.position);

            if (!_dragging && totalDist > gm.dragDistanceThreshold)
            {
                _dragging = true;
                _lastScreenPos = p.position;
                gm.SingleFingerHandler?.OnDragBegin(BuildContext(p, gm));
                return;
            }

            if (_dragging)
            {
                Vector2 delta = p.position - _lastScreenPos;
                float deltaDist = delta.magnitude;
                float speed = deltaDist / Time.deltaTime;

                if (deltaDist >= gm.dragMinDelta || speed >= gm.dragMinSpeed)
                {
                    gm.SingleFingerHandler?.OnDrag(BuildContext(p, gm));
                    _lastScreenPos = p.position;
                }
            }
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
                screenLast = _lastScreenPos,
                screenCurrent = p.position,
                elapsed = _time
            };
        }
    }

}