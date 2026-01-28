using UnityEngine;

namespace Gesture
{
    public struct SingleFingerGestureContext
    {
        public Vector3 worldDown;
        public Vector3 worldCurrent;

        public Vector2 screenDown;
        public Vector2 screenLast;      // 上一帧点
        public Vector2 screenCurrent;

        public float elapsed;
        public Vector2 span  => screenCurrent - screenDown;
        public Vector2 delta => screenCurrent - screenLast;
    }

}