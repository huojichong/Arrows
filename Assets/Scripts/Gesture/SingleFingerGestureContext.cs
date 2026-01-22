using UnityEngine;

namespace Gesture
{
    public struct SingleFingerGestureContext
    {
        public Vector3 worldDown;
        public Vector3 worldCurrent;

        public Vector2 screenDown;
        public Vector2 screenCurrent;

        public float elapsed;
    }

}