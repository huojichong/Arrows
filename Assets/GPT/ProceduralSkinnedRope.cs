using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ProceduralSkinnedRope : MonoBehaviour
{
    [Header("Mesh Shape")]
    public float length = 5f;
    public float radius = 0.15f;
    public int radialSegments = 12;      // 圆周细分
    public int lengthSegments = 24;      // 沿长度方向切段（越多越平滑）

    [Header("Bones")]
    public int boneCount = 12;

    void Awake()
    {
        Generate();
    }

    void Generate()
    {
        // 1. 创建骨骼
        Transform bonesRoot = new GameObject("Bones").transform;
        bonesRoot.SetParent(transform, false);

        Transform[] bones = new Transform[boneCount];
        Matrix4x4[] bindPoses = new Matrix4x4[boneCount];

        for (int i = 0; i < boneCount; i++)
        {
            GameObject b = new GameObject($"Bone_{i}");
            b.transform.SetParent(bonesRoot, false);

            float z = (length / (boneCount - 1)) * i;
            b.transform.localPosition = new Vector3(0, 0, z);

            bones[i] = b.transform;
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }

        // 2. 生成 Mesh
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralSkinnedRope";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<BoneWeight> weights = new();
        List<Vector3> normals = new();

        float segmentLength = length / lengthSegments;

        for (int i = 0; i <= lengthSegments; i++)
        {
            float z = i * segmentLength;
            float t = z / length;
            float boneFloat = t * (boneCount - 1);

            int boneA = Mathf.FloorToInt(boneFloat);
            int boneB = Mathf.Clamp(boneA + 1, 0, boneCount - 1);
            float boneWeightB = boneFloat - boneA;
            float boneWeightA = 1f - boneWeightB;

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                vertices.Add(new Vector3(x, y, z));
                normals.Add(new Vector3(x, y, 0).normalized);

                BoneWeight bw = new BoneWeight
                {
                    boneIndex0 = boneA,
                    weight0 = boneWeightA,
                    boneIndex1 = boneB,
                    weight1 = boneWeightB
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

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.boneWeights = weights.ToArray();
        mesh.bindposes = bindPoses;
        mesh.RecalculateBounds();

        // 3. 绑定 SkinnedMeshRenderer
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        smr.bones = bones;
        smr.rootBone = bones[0];
        
        
        GetComponent<SkinnedSplineFollower>().bones = bones;
        GetComponent<FixedRadiusSplineSkin>().bones = bones.ToList();
    }
}
