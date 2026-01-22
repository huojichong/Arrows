using System.Collections.Generic;
using UnityEngine;

namespace Gesture
{
    class TwoFingerState
    {
        public bool Active { get; private set; }

        readonly Dictionary<int, Vector2> _start = new();

        public void Begin(List<PointerState> pointers)
        {
            _start.Clear();
            _start[pointers[0].id] = pointers[0].position;
            _start[pointers[1].id] = pointers[1].position;
            Active = true;
        }

        public void Reset()
        {
            Active = false;
            _start.Clear();
        }

        public TwoFingerGestureContext BuildContext(List<PointerState> pointers)
        {
            var p0 = pointers[0];
            var p1 = pointers[1];

            Vector2 s0 = _start[p0.id];
            Vector2 s1 = _start[p1.id];

            float startDist = Vector2.Distance(s0, s1);
            float currDist = Vector2.Distance(p0.position, p1.position);

            float startAngle = Mathf.Atan2(s1.y - s0.y, s1.x - s0.x);
            float currAngle = Mathf.Atan2(
                p1.position.y - p0.position.y,
                p1.position.x - p0.position.x
            );

            return new TwoFingerGestureContext
            {
                p0Start = s0,
                p1Start = s1,
                p0Current = p0.position,
                p1Current = p1.position,
                startDistance = startDist,
                currentDistance = currDist,
                deltaDistance = currDist - startDist,
                deltaAngle = Mathf.DeltaAngle(
                    startAngle * Mathf.Rad2Deg,
                    currAngle * Mathf.Rad2Deg
                ),
                center = (p0.position + p1.position) * 0.5f
            };
        }
    }

}