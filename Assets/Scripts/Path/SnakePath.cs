
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Splines;

    public class SnakePath : MonoBehaviour
    {
        [Header("Spline 核心配置")] [Tooltip("用于渲染和路径计算的 Spline 容器")]
        public SplineContainer splineContainer;

        [Header("路径点 (世界空间)")] public List<Vector3> waypoints = new List<Vector3>();
        [Tooltip("拐弯处的固定圆角半径")]
        public float filletRadius = 0.5f;

        [Header("自适应分布与稳定性")]
        [Range(0, 30f)]
        [Tooltip("曲率敏感度：值越大，拐弯处的骨骼越密集，直线处越稀疏")]
        public float curvatureSensitivity = 15.0f;

        // 静态密度分布图数据，用于固定骨骼分布逻辑，防止移动时抖动
        public List<float> splineDistances = new List<float>();
        public List<float> splineWeights = new List<float>();
        private float totalPathWeight = 0;
        
        public float fullLength { get; protected set; }
        private const int DENSITY_SAMPLES_PER_UNIT = 10; // 每单位长度的采样点密度（提高精度）


        /// <summary>
        /// 根据 waypoints 生成带有固定半径圆角的平滑 Spline 路径
        /// </summary>
        [ContextMenu("Update Spline")]
        public void UpdateSplineFromWaypoints()
        {
            if (waypoints.Count < 2 || splineContainer == null) return;

            Spline spline = splineContainer.Spline;
            spline.Clear();

            // 1. 将世界坐标路点转换到 SplineContainer 的本地空间，保证缩放和位移一致性
            List<Vector3> localPoints = new List<Vector3>();
            foreach (var wp in waypoints)
                localPoints.Add(splineContainer.transform.InverseTransformPoint(wp));

            // 2. 预计算：为了解决连续转弯冲突，我们需要知道每条边能分配给圆角的“最大配额”
            // 逻辑：每条边被两端的圆角平分，确保 p1 和 p2 不会交错
            float[] sideQuotas = new float[localPoints.Count - 1];
            for (int i = 0; i < localPoints.Count - 1; i++)
            {
                sideQuotas[i] = math.distance(localPoints[i], localPoints[i + 1]) * 0.5f;
            }
            
            // === 工具函数：添加 Linear Knot ===
            void AddLinearKnot(Vector3 pos)
            {
                BezierKnot k = new BezierKnot(pos);
                k.TangentIn = float3.zero;
                k.TangentOut = float3.zero;
                spline.Add(k, TangentMode.Linear);
            }

            // === 工具函数：添加 Bezier Knot（Broken）===
            void AddBezierKnot(Vector3 pos, float3 tanIn, float3 tanOut)
            {
                BezierKnot k = new BezierKnot(pos);
                k.TangentIn = tanIn;
                k.TangentOut = tanOut;
                spline.Add(k, TangentMode.Broken);
            }
            
            // 3. 添加起始点
            // spline.Add(new BezierKnot(localPoints[0]));
            AddLinearKnot(localPoints[0]);
            // 4. 处理中间的转弯点
            for (int i = 1; i < localPoints.Count - 1; i++)
            {
                Vector3 prev = localPoints[i - 1];
                Vector3 curr = localPoints[i];
                Vector3 next = localPoints[i + 1];

                Vector3 dirIn = (curr - prev).normalized;
                Vector3 dirOut = (next - curr).normalized;

                // 使用点积计算夹角，防止 math.acos 范围溢出导致 NaN
                float dot = math.dot(dirIn, dirOut);
                dot = math.clamp(dot, -1f, 1f);
                float angleRad = math.acos(dot); // 这是方向矢量之间的夹角 (外角)

                // 如果角度太小（接近直线）或角度太大（接近回头弯 180°）
                if (angleRad < 0.001f || angleRad > math.PI - 0.01f)
                {
                    // spline.Add(new BezierKnot(curr));
                    AddLinearKnot(localPoints[i]);
                    continue;
                }
                
                // 计算切点距离：D = R * tan(θ/2)
                float tangentDist = filletRadius * math.tan(angleRad * 0.5f);

                // 限制距离：不能超过进入边和出去边的配额
                float actualDist = math.min(tangentDist, math.min(sideQuotas[i - 1], sideQuotas[i]));
            
                // 重新计算受限后的实际半径（用于计算 Bezier 句柄长度）
                float actualRadius = actualDist / math.tan(angleRad * 0.5f);

                // 计算切点
                float3 p1 = curr - dirIn * actualDist;
                float3 p2 = curr + dirOut * actualDist;

                // 计算 Bezier 句柄长度系数：h = (4/3) * tan(θ/4) * R
                float handleLen = (4f / 3f) * math.tan(angleRad * 0.25f) * actualRadius;

                // 添加圆弧起点 (p1)
                // 关键：必须设置 TangentMode 为 Broken，否则前后的直线段会被这个点的入方向句柄带歪
                // BezierKnot knot1 = new BezierKnot(p1);
                // knot1.TangentIn = float3.zero;
                // knot1.TangentOut = dirIn * handleLen;
                // spline.Add(knot1, TangentMode.Broken);
                AddBezierKnot(p1, float3.zero, dirIn * handleLen);
                // 添加圆弧终点 (p2)
                // BezierKnot knot2 = new BezierKnot(p2);
                // knot2.TangentIn = -dirOut * handleLen;
                // knot2.TangentOut = float3.zero;
                // spline.Add(knot2, TangentMode.Broken);
                //
                AddBezierKnot(p2,  -dirOut * handleLen, float3.zero);
            }

            // 4. 添加终点
            // spline.Add(new BezierKnot(localPoints[localPoints.Count - 1]));
            AddLinearKnot(localPoints[localPoints.Count - 1]);

            // 5. 路径改变后，立即重建密度图
            BuildStaticDensityMap();
        }

        /// <summary>
        /// 预计算整条路径的"疏密权重图"。
        /// 核心逻辑：在转弯半径处采样更高权重，使骨骼自动聚集在弯道。
        /// 使用静态图代替动态采样，彻底消除移动时的数值抖动。
        /// </summary>
        void BuildStaticDensityMap()
        {
            splineDistances.Clear();
            splineWeights.Clear();
            totalPathWeight = 0;

            this.fullLength = splineContainer.CalculateLength();
            // 根据总长度决定采样精度（提高采样密度以更准确捕捉曲率）
            int sampleCount = Mathf.Max(30, Mathf.CeilToInt(fullLength * DENSITY_SAMPLES_PER_UNIT));

            for (int i = 0; i <= sampleCount; i++)
            {
                float d = (i / (float)sampleCount) * fullLength;
                float weight = 1.0f; // 基础权重

                // 评估当前位置的曲率
                float normT =
                    SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, d, PathIndexUnit.Distance);
                splineContainer.Evaluate(normT, out _, out float3 tangent, out _);

                // 缩短采样步长以更精确捕捉急转弯（从0.1减小到0.05）
                float nextD = Mathf.Min(d + 0.05f, fullLength);
                float nextNormT =
                    SplineUtility.GetNormalizedInterpolation(splineContainer.Spline, nextD, PathIndexUnit.Distance);
                splineContainer.Evaluate(nextNormT, out _, out float3 nextTangent, out _);

                // 夹角越大，权重越高（使用指数增强曲率敏感度）
                float angleDiff = Vector3.Angle(tangent, nextTangent);
                weight += Mathf.Pow(angleDiff, 1.5f) * curvatureSensitivity;

                splineDistances.Add(d);
                splineWeights.Add(weight);
                totalPathWeight += weight;
            }
        }


        /// <summary>
        /// 给定一个物理距离，计算其在路径上的累积权重
        /// </summary>
        public float GetAccumulatedWeightAtPos(float dist)
        {
            if (dist <= 0)
            {
                return 0;
            }
            float currentAcc = 0;
            for (int i = 0; i < splineDistances.Count - 1; i++)
            {
                if (dist <= splineDistances[i + 1])
                {
                    // 在两个采样点之间做线性插值，保证移动平滑
                    float t = (dist - splineDistances[i]) /
                              (splineDistances[i + 1] - splineDistances[i]);
                    return currentAcc + splineWeights[i] * t;
                }

                currentAcc += splineWeights[i];
            }

            return totalPathWeight;
        }

        /// <summary>
        /// 给定一个累积权重值，反推其在路径上的物理距离
        /// </summary>
        public float GetDistFromAccumulatedWeight(float targetWeight)
        {
            if (targetWeight <= 0) return 0;
            float currentAcc = 0;
            for (int i = 0; i < splineWeights.Count - 1; i++)
            {
                if (targetWeight <= currentAcc + splineWeights[i])
                {
                    // 在权重区间内做插值反推物理距离
                    float t = (targetWeight - currentAcc) / splineWeights[i];
                    return Mathf.Lerp(splineDistances[i], splineDistances[i + 1], t);
                }

                currentAcc += splineWeights[i];
            }

            return splineDistances[splineDistances.Count - 1];
        }

    }
