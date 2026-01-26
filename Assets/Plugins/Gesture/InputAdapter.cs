namespace Gesture
{
    using UnityEngine;
    using System.Collections.Generic;

    public static class InputAdapter
    {
        public static void GetPointers(List<PointerState> output)
        {
            output.Clear();

#if UNITY_EDITOR || UNITY_STANDALONE
            output.Add(new PointerState
            {
                id = -1,
                down = Input.GetMouseButtonDown(0),
                held = Input.GetMouseButton(0),
                up   = Input.GetMouseButtonUp(0),
                position = Input.mousePosition
            });
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            output.Add(new PointerState
            {
                id = t.fingerId,
                down = t.phase == TouchPhase.Began,
                held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary,
                up   = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled,
                position = t.position
            });
        }
#endif
        }
    }

}