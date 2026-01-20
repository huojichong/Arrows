using System.Collections.Generic;
using UnityEngine;

public class SkinnedSplineFollower : MonoBehaviour
{
    [Header("Path")]
    public Transform pathRoot;
    public int splineResolution = 10;
    public float moveSpeed = 3f;

    [Header("Bones")]
    public Transform[] bones;

    [Header("Length")]
    public float baseLength = 5f;
    public float lengthMultiplier = 1f;

    CatmullRomSpline spline;
    float moveDistance;

    float BoneSpacing => (baseLength * lengthMultiplier) / (bones.Length - 1);

    void Start()
    {
        List<Vector3> pts = new();
        foreach (Transform t in pathRoot)
            pts.Add(t.position);

        List<Vector3> smooth =
            RightAngleFilletPath.Build(pts, 0.6f, 10);

        spline = new CatmullRomSpline(smooth, splineResolution);
    }

    void Update()
    {
        moveDistance += moveSpeed * Time.deltaTime;

        for (int i = 0; i < bones.Length; i++)
        {
            float d = moveDistance - i * BoneSpacing;

            Vector3 pos = spline.GetPointAtDistance(d);
            Vector3 dir = spline.GetDirectionAtDistance(d);

            bones[i].position = pos;
            if (dir != Vector3.zero)
                bones[i].rotation = Quaternion.LookRotation(dir);
        }
    }
}