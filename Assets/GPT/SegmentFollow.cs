using UnityEngine;

public class SegmentFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}