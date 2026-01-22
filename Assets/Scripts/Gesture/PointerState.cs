using UnityEngine;

namespace Gesture
{
    public struct PointerState
    {
        public int id;               // fingerId / -1 for mouse
        public bool down;
        public bool held;
        public bool up;
        public Vector2 position;
    }

}