using System.Collections.Generic;
using UnityEngine;

public class CatmullRomSpline
{
    List<Vector3> points;
    int resolution;

    List<Vector3> sampledPoints = new();
    List<float> cumulativeLength = new();
    float totalLength;

    public CatmullRomSpline(List<Vector3> pts, int resolutionPerSegment = 10)
    {
        points = pts;
        resolution = resolutionPerSegment;
        Build();
    }

    void Build()
    {
        sampledPoints.Clear();
        cumulativeLength.Clear();
        totalLength = 0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[Mathf.Max(i - 1, 0)];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;
                Vector3 pos = GetCatmullRom(p0, p1, p2, p3, t);

                if (sampledPoints.Count > 0)
                    totalLength += Vector3.Distance(sampledPoints[^1], pos);

                sampledPoints.Add(pos);
                cumulativeLength.Add(totalLength);
            }
        }

        // 最后一个点
        sampledPoints.Add(points[^1]);
        cumulativeLength.Add(totalLength);
    }

    Vector3 GetCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    public Vector3 GetPointAtDistance(float distance)
    {
        if (distance <= 0) return sampledPoints[0];
        if (distance >= totalLength) return sampledPoints[^1];

        for (int i = 1; i < cumulativeLength.Count; i++)
        {
            if (cumulativeLength[i] >= distance)
            {
                float t = Mathf.InverseLerp(
                    cumulativeLength[i - 1],
                    cumulativeLength[i],
                    distance
                );
                return Vector3.Lerp(sampledPoints[i - 1], sampledPoints[i], t);
            }
        }
        return sampledPoints[^1];
    }

    public Vector3 GetDirectionAtDistance(float distance)
    {
        Vector3 p1 = GetPointAtDistance(distance);
        Vector3 p2 = GetPointAtDistance(distance + 0.05f);
        return (p2 - p1).normalized;
    }
}
