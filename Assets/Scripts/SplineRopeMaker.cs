using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 负责生成绳子模型并初始化 SplineRopeController
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
[RequireComponent(typeof(SplineRopeController))]
public class SplineRopeMaker : MonoBehaviour
{
    [Header("Rope Mesh Settings")]
    public float length = 5f;
    public float radius = 0.15f;
    public int radialSegments = 12;
    public int lengthSegments = 60;
    public int boneCount = 30;
    public Material material;

    private List<Transform> bones = new List<Transform>();
    private SplineRopeController controller;

    void Awake()
    {
        GenerateRope();
    }

    [ContextMenu("Generate Rope")]
    public void GenerateRope()
    {
        // 清理旧骨骼
        Transform oldBones = transform.Find("RopeBones");
        if (oldBones != null) DestroyImmediate(oldBones.gameObject);

        // 1. 创建骨骼
        GameObject bonesRoot = new GameObject("RopeBones");
        bonesRoot.transform.SetParent(transform, false);
        
        bones.Clear();
        Matrix4x4[] bindPoses = new Matrix4x4[boneCount];

        for (int i = 0; i < boneCount; i++)
        {
            GameObject b = new GameObject($"Bone_{i}");
            b.transform.SetParent(bonesRoot.transform, false);
            
            // 初始沿 Z 轴排列
            float z = (length / (boneCount - 1)) * i;
            b.transform.localPosition = new Vector3(0, 0, z);
            
            bones.Add(b.transform);
            bindPoses[i] = b.transform.worldToLocalMatrix * transform.localToWorldMatrix;
        }

        // 2. 生成 Mesh
        Mesh mesh = new Mesh();
        mesh.name = "SplineRopeMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<BoneWeight> weights = new List<BoneWeight>();
        List<Vector2> uvs = new List<Vector2>();

        float segmentLength = length / lengthSegments;

        for (int i = 0; i <= lengthSegments; i++)
        {
            float z = i * segmentLength;
            float t = z / length;
            
            // 计算骨骼权重
            float boneFloat = t * (boneCount - 1);
            int boneA = Mathf.FloorToInt(boneFloat);
            int boneB = Mathf.Clamp(boneA + 1, 0, boneCount - 1);
            float weightB = boneFloat - boneA;
            float weightA = 1f - weightB;

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(j / (float)radialSegments, t));

                BoneWeight bw = new BoneWeight
                {
                    boneIndex0 = boneA, weight0 = weightA,
                    boneIndex1 = boneB, weight1 = weightB
                };
                weights.Add(bw);
            }
        }

        for (int i = 0; i < lengthSegments; i++)
        {
            int start = i * radialSegments;
            int next = (i + 1) * radialSegments;

            for (int j = 0; j < radialSegments; j++)
            {
                int a = start + j;
                int b = start + (j + 1) % radialSegments;
                int c = next + j;
                int d = next + (j + 1) % radialSegments;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.boneWeights = weights.ToArray();
        mesh.bindposes = bindPoses;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 3. 配置组件
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        smr.bones = bones.ToArray();
        smr.rootBone = bones[0];
        if (material != null) smr.material = material;

        controller = GetComponent<SplineRopeController>();
        controller.bones = bones;
        controller.baseLength = length;
    }
}
