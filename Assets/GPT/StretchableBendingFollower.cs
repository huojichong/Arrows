using System.Collections.Generic;
using UnityEngine;

public class StretchableBendingFollower : MonoBehaviour
{
    [Header("Path")]
    public Transform pathRoot;
    public float moveSpeed = 2f;

    [Header("Bones")]
    public Transform[] bones;

    [Header("Length")]
    public float baseLength = 5f;
    [Range(0.3f, 3f)]
    public float lengthMultiplier = 1f;

    [Header("Visual")]
    public Transform visualRoot;

    private List<Vector3> pathPoints = new();
    private float moveDistance;

    float CurrentLength => baseLength * lengthMultiplier;
    float BoneSpacing => CurrentLength / (bones.Length - 1);

    void Start()
    {
        foreach (Transform t in pathRoot)
            pathPoints.Add(t.position);
    }

    void Update()
    {
        moveDistance += moveSpeed * Time.deltaTime;

        UpdateVisualStretch();
        UpdateBones();
    }

    void UpdateVisualStretch()
    {
        if (!visualRoot) return;

        Vector3 scale = visualRoot.localScale;
        scale.z = lengthMultiplier;   // 假设模型朝 Z
        visualRoot.localScale = scale;
    }

    void UpdateBones()
    {
        for (int i = 0; i < bones.Length; i++)
        {
            float d = moveDistance - i * BoneSpacing;

            Vector3 pos = GetPointAtDistance(d);
            Vector3 dir = GetDirectionAtDistance(d);

            bones[i].position = pos;

            if (dir != Vector3.zero)
                bones[i].rotation = Quaternion.LookRotation(dir);
        }
    }

    Vector3 GetPointAtDistance(float distance)
    {
        if (distance <= 0)
            return pathPoints[0];

        float remain = distance;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            float segLen = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            if (remain <= segLen)
                return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], remain / segLen);
            remain -= segLen;
        }

        return pathPoints[^1];
    }

    Vector3 GetDirectionAtDistance(float distance)
    {
        Vector3 p1 = GetPointAtDistance(distance);
        Vector3 p2 = GetPointAtDistance(distance + 0.05f);
        return (p2 - p1).normalized;
    }
}
