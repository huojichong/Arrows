using System.Collections.Generic;
using UnityEngine;

public static class RightAngleFilletPath
{
    public static List<Vector3> Build(
        List<Vector3> raw,
        float radius,
        int arcResolution = 8
    )
    {
        List<Vector3> result = new();

        result.Add(raw[0]);

        for (int i = 1; i < raw.Count - 1; i++)
        {
            Vector3 prev = raw[i - 1];
            Vector3 curr = raw[i];
            Vector3 next = raw[i + 1];

            Vector3 dirIn = (curr - prev).normalized;
            Vector3 dirOut = (next - curr).normalized;

            // 是否是直角拐弯
            if (Mathf.Abs(Vector3.Dot(dirIn, dirOut)) < 0.01f)
            {
                Vector3 p1 = curr - dirIn * radius;
                Vector3 p2 = curr + dirOut * radius;

                Vector3 center = curr - dirIn * radius + dirOut * radius;

                float startAngle = Mathf.Atan2(
                    p1.z - center.z,
                    p1.x - center.x
                );
                float endAngle = Mathf.Atan2(
                    p2.z - center.z,
                    p2.x - center.x
                );

                // 确保顺时针/逆时针正确
                if (endAngle < startAngle)
                    endAngle += Mathf.PI * 2f;

                for (int j = 0; j <= arcResolution; j++)
                {
                    float t = j / (float)arcResolution;
                    float angle = Mathf.Lerp(startAngle, endAngle, t);

                    Vector3 arcPoint = new Vector3(
                        center.x + Mathf.Cos(angle) * radius,
                        curr.y,
                        center.z + Mathf.Sin(angle) * radius
                    );

                    result.Add(arcPoint);
                }
            }
            else
            {
                result.Add(curr);
            }
        }

        result.Add(raw[^1]);
        return result;
    }
}