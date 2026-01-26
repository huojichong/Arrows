using System.Collections.Generic;
using UnityEngine;


public static class PathTool
{

    public static float CalcLength(List<Vector3> paths)
    {
        float totalLength = 0;
        for (int i = 1; i < paths.Count; i++)
        {
            var prev = paths[i - 1];
            var curr = paths[i];

            float dr = curr.x - prev.x;
            float dc = curr.z - prev.z;

            // 欧几里得距离
            totalLength += Mathf.Abs(dr) + Mathf.Abs(dc);
        }

        return totalLength;
    }

}
