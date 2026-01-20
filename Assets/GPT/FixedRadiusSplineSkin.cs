using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;

public class FixedRadiusSplineSkin : MonoBehaviour
{
    public SplineContainer spline;
    public List<Transform> bones;

    void LateUpdate()
    {
        var sp = spline.Spline;
        Quaternion rot = Quaternion.identity;

        for (int i = 0; i < bones.Count; i++)
        {
            float t = i / (float)(bones.Count - 1);

            SplineUtility.Evaluate(
                sp,
                t,
                out float3 pos,
                out float3 tangent,
                out float3 up
            );

            bones[i].position = pos;

            Quaternion target = Quaternion.LookRotation(tangent, up);

            if (i == 0)
                rot = target;
            else
                rot = Quaternion.Slerp(rot, target, 1f);

            bones[i].rotation = rot;
        }
    }
}