using UnityEngine;

namespace Gesture
{
    public struct TwoFingerGestureContext
    {
        public Vector2 p0Start;
        public Vector2 p1Start;

        public Vector2 p0Current;
        public Vector2 p1Current;

        public float startDistance;
        public float currentDistance;

        public float deltaDistance;
        public float deltaAngle;
        public Vector2 center;
    }

}