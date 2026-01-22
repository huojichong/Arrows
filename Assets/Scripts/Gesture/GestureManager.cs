using Gesture.Handlers;

namespace Gesture
{
    using UnityEngine;
using System.Collections.Generic;

public class GestureManager : MonoBehaviour
{
    [Header("Single Finger")]
    public float clickTimeThreshold = 0.2f;
    public float dragDistanceThreshold = 10f;
    public float planeY = 0f;

    public IGestureHandler SingleFingerHandler;

    public System.Action<TwoFingerGestureContext> OnTwoFingerBegin;
    public System.Action<TwoFingerGestureContext> OnTwoFingerUpdate;
    public System.Action<TwoFingerGestureContext> OnTwoFingerEnd;

    readonly List<PointerState> _pointers = new();
    Plane _plane;

    SingleFingerState _single;
    TwoFingerState _double;

    void Awake()
    {
        _plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
    }

    void Update()
    {
        InputAdapter.GetPointers(_pointers);

        if (_pointers.Count >= 2)
        {
            UpdateTwoFinger();
            _single.Reset(); // 双指时强制打断单指
        }
        else if (_pointers.Count == 1)
        {
            UpdateSingleFinger(_pointers[0]);
            _double.Reset();
        }
        else
        {
            _single.Reset();
            _double.Reset();
        }
    }

    #region Single Finger

    void UpdateSingleFinger(PointerState p)
    {
        if (p.down)
            _single.Begin(p, this);

        if (_single.Active)
            _single.Update(p, this);

        if (p.up)
            _single.End(this);
    }

    #endregion

    #region Two Finger

    void UpdateTwoFinger()
    {
        if (!_double.Active)
        {
            _double.Begin(_pointers);
            OnTwoFingerBegin?.Invoke(_double.BuildContext(_pointers));
        }
        else
        {
            var ctx = _double.BuildContext(_pointers);
            OnTwoFingerUpdate?.Invoke(ctx);
        }
    }

    #endregion

    #region Helpers

    public bool RaycastPlane(Vector2 screen, out Vector3 world)
    {
        Ray ray = Camera.main.ScreenPointToRay(screen);
        if (_plane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }

        world = default;
        return false;
    }

    #endregion
}

}