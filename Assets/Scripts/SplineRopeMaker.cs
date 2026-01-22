using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 负责生成绳子模型并初始化 SplineRopeSnake
/// 基于Unity的SkinnedMeshRenderer系统创建可变形的绳子网格
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
[RequireComponent(typeof(SplineRopeSnake))]
public class SplineRopeMaker : MonoBehaviour
{
    [Header("Rope Mesh Settings")]
    public float length = 5f;           // 绳子总长度
    public float radius = 0.15f;        // 绳子半径
    public int radialSegments = 12;     // 径向分段数（圆形的边数）
    public int lengthSegments = 60;    // 长度分段数（影响网格密度）
    public int boneCount = 30;          // 骨骼数量（影响变形精度）
    public Material material;           // 绳子材质

    private List<Transform> bones = new List<Transform>();  // 骨骼变换列表
    private SplineRopeSnake m_Snake;                 // 绳子控制器引用

    void Awake()
    {
        GenerateRope();
    }

    [ContextMenu("Generate Rope")]
    public void GenerateRope()
    {
        // 清理旧骨骼，避免重复创建
        Transform oldBones = transform.Find("RopeBones");
        if (oldBones != null) DestroyImmediate(oldBones.gameObject);

        // 1. 创建骨骼系统
        // 骨骼用于控制绳子的变形，每个骨骼影响一段绳子
        GameObject bonesRoot = new GameObject("RopeBones");
        bonesRoot.transform.SetParent(transform, false);
        
        bones.Clear();
        Matrix4x4[] bindPoses = new Matrix4x4[boneCount];  // 绑定姿态矩阵

        // 沿Z轴均匀分布骨骼
        for (int i = 0; i < boneCount; i++)
        {
            GameObject b = new GameObject($"Bone_{i}");
            b.transform.SetParent(bonesRoot.transform, false);
            
            // 初始沿 Z 轴排列，确保骨骼均匀分布
            float z = (length / (boneCount - 1)) * i;
            b.transform.localPosition = new Vector3(0, 0, z);
            
            bones.Add(b.transform);
            // 计算绑定姿态：骨骼到模型空间的变换矩阵
            bindPoses[i] = b.transform.worldToLocalMatrix * transform.localToWorldMatrix;
        }

        // 2. 生成圆柱形网格
        // 创建沿长度方向的圆柱体网格，支持骨骼蒙皮变形
        Mesh mesh = new Mesh();
        mesh.name = "SplineRopeMesh";

        List<Vector3> vertices = new List<Vector3>();      // 顶点位置
        List<int> triangles = new List<int>();            // 三角形索引
        List<BoneWeight> weights = new List<BoneWeight>(); // 骨骼权重
        List<Vector2> uvs = new List<Vector2>();           // UV坐标

        float segmentLength = length / lengthSegments;    // 每段的长度

        // 生成顶点：沿长度方向分段，每段生成圆形截面
        for (int i = 0; i <= lengthSegments; i++)
        {
            float z = i * segmentLength;  // 当前段的Z坐标
            float t = z / length;          // 归一化位置 [0,1]
            
            // 计算当前顶点受哪两个骨骼影响以及权重
            float boneFloat = t * (boneCount - 1);
            int boneA = Mathf.FloorToInt(boneFloat);                    // 前一个骨骼索引
            int boneB = Mathf.Clamp(boneA + 1, 0, boneCount - 1);      // 后一个骨骼索引
            float weightB = boneFloat - boneA;                          // 后骨骼权重
            float weightA = 1f - weightB;                               // 前骨骼权重

            // 生成当前圆形截面的所有顶点
            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;  // 圆周角度
                float x = Mathf.Cos(angle) * radius;                         // X坐标
                float y = Mathf.Sin(angle) * radius;                         // Y坐标

                vertices.Add(new Vector3(x, y, z));
                // UV映射：U方向对应圆周，V方向对应长度
                uvs.Add(new Vector2(j / (float)radialSegments, t));

                // 设置骨骼权重：每个顶点受两个相邻骨骼影响
                BoneWeight bw = new BoneWeight
                {
                    boneIndex0 = boneA, weight0 = weightA,
                    boneIndex1 = boneB, weight1 = weightB
                };
                weights.Add(bw);
            }
        }

        // 生成三角形索引：连接相邻的两个圆形截面
        // 每个四边形由两个三角形组成
        for (int i = 0; i < lengthSegments; i++)
        {
            int start = i * radialSegments;      // 当前截面的起始顶点索引
            int next = (i + 1) * radialSegments;  // 下一个截面的起始顶点索引

            // 为当前段的所有四边形生成三角形
            for (int j = 0; j < radialSegments; j++)
            {
                // 计算四边形的四个顶点索引
                int a = start + j;                           // 当前截面当前顶点
                int b = start + (j + 1) % radialSegments;    // 当前截面下一个顶点（循环）
                int c = next + j;                            // 下一截面当前顶点
                int d = next + (j + 1) % radialSegments;     // 下一截面下一个顶点（循环）

                // 生成两个三角形：(a,c,b) 和 (b,c,d)
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        // 设置网格数据
        mesh.SetVertices(vertices);                    // 设置顶点位置
        mesh.SetTriangles(triangles, 0);               // 设置三角形索引
        mesh.SetUVs(0, uvs);                          // 设置UV坐标
        mesh.boneWeights = weights.ToArray();         // 设置骨骼权重
        mesh.bindposes = bindPoses;                   // 设置绑定姿态
        mesh.RecalculateNormals();                    // 重新计算法线（用于光照）
        mesh.RecalculateBounds();                     // 重新计算边界框

        AssetDatabase.CreateAsset(mesh, "Assets/mesh.asset");
        
        // 3. 配置蒙皮网格渲染器
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;                        // 设置共享网格
        smr.bones = bones.ToArray();                  // 设置骨骼变换数组
        smr.rootBone = bones[0];                      // 设置根骨骼
        if (material != null) smr.material = material; // 设置材质

        // 4. 初始化绳子控制器
        m_Snake = GetComponent<SplineRopeSnake>();
        m_Snake.bones = bones;                     // 传递骨骼引用
        m_Snake.baseLength = length;                // 设置基础长度
    }
}
