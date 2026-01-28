using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// 将源网格沿 Spline 变形，避免挤出方式在拐角处的折叠问题
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SplineMeshDeformer : MonoBehaviour
{
    [Tooltip("Spline 容器")]
    public SplineContainer splineContainer;
    
    [Tooltip("沿曲线的起始位置 (0-1)")]
    [Range(0f, 1f)]
    public float startT = 0f;
    
    [Tooltip("沿曲线的结束位置 (0-1)")]
    [Range(0f, 1f)]
    public float endT = 1f;
    
    [Header("管道参数")]
    [Tooltip("管道半径")]
    public float radius = 0.25f;
    
    [Tooltip("圆周细分数")]
    [Range(3, 32)]
    public int sides = 8;
    
    [Tooltip("长度方向细分数")]
    [Range(2, 200)]
    public int segments = 50;
    
    [Header("沿路径缩放")]
    [Tooltip("起点处的缩放")]
    public float scaleAtStart = 1f;
    
    [Tooltip("终点处的缩放")]
    public float scaleAtEnd = 1f;
    
    [Tooltip("缩放曲线（横轴为路径位置0-1，纵轴为缩放倍数）")]
    public AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    
    [Tooltip("使用缩放曲线而非线性插值")]
    public bool useScaleCurve = false;
    
    private Mesh deformedMesh;
    private MeshFilter meshFilter;
    
    void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        deformedMesh = new Mesh();
        deformedMesh.name = "Deformed Spline Mesh";
    }
    
    void Update()
    {
        if (splineContainer == null)
            return;
            
        DeformMesh();
    }
    
    void DeformMesh()
    {
        var spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2)
            return;
        
        int vertCount = (sides + 1) * (segments + 1);
        int triCount = sides * segments * 6;
        
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[triCount];
        
        // 生成沿 Spline 变形的管道顶点
        for (int seg = 0; seg <= segments; seg++)
        {
            float normalizedZ = (float)seg / segments;
            
            // 计算 t 值（支持闭合曲线跨越 0 点）
            float t;
            if (endT >= startT)
            {
                t = Mathf.Lerp(startT, endT, normalizedZ);
            }
            else
            {
                float range = (1f - startT) + endT;
                float offset = range * normalizedZ;
                t = startT + offset;
                if (t > 1f) t -= 1f;
            }
            
            // 计算该位置的缩放
            float localScale;
            if (useScaleCurve)
            {
                localScale = scaleCurve.Evaluate(normalizedZ);
            }
            else
            {
                localScale = Mathf.Lerp(scaleAtStart, scaleAtEnd, normalizedZ);
            }
            float localRadius = radius * localScale;
            
            // 获取 spline 上的位置和方向
            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);
            
            // 转换到世界坐标
            Vector3 worldPos = splineContainer.transform.TransformPoint(pos);
            Vector3 forward = splineContainer.transform.TransformDirection(tangent).normalized;
            Vector3 upDir = splineContainer.transform.TransformDirection(up).normalized;
            
            // 构建局部坐标系
            Vector3 right = Vector3.Cross(upDir, forward).normalized;
            if (right == Vector3.zero)
            {
                right = Vector3.Cross(Vector3.up, forward).normalized;
                if (right == Vector3.zero)
                    right = Vector3.Cross(Vector3.forward, forward).normalized;
            }
            upDir = Vector3.Cross(forward, right).normalized;
            
            // 生成圆周上的顶点
            for (int side = 0; side <= sides; side++)
            {
                float angle = (float)side / sides * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * localRadius;
                float y = Mathf.Sin(angle) * localRadius;
                
                int idx = seg * (sides + 1) + side;
                
                Vector3 localOffset = right * x + upDir * y;
                vertices[idx] = worldPos + localOffset - transform.position;
                normals[idx] = (right * Mathf.Cos(angle) + upDir * Mathf.Sin(angle)).normalized;
                uvs[idx] = new Vector2((float)side / sides, normalizedZ);
            }
        }
        
        // 生成三角形
        int triIdx = 0;
        for (int seg = 0; seg < segments; seg++)
        {
            for (int side = 0; side < sides; side++)
            {
                int current = seg * (sides + 1) + side;
                int next = current + sides + 1;
                
                triangles[triIdx++] = current;
                triangles[triIdx++] = next;
                triangles[triIdx++] = current + 1;
                
                triangles[triIdx++] = current + 1;
                triangles[triIdx++] = next;
                triangles[triIdx++] = next + 1;
            }
        }
        
        // 更新网格
        deformedMesh.Clear();
        deformedMesh.vertices = vertices;
        deformedMesh.normals = normals;
        deformedMesh.uv = uvs;
        deformedMesh.triangles = triangles;
        deformedMesh.RecalculateBounds();
        
        meshFilter.sharedMesh = deformedMesh;
    }
    
    void OnDisable()
    {
        if (deformedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(deformedMesh);
            else
                DestroyImmediate(deformedMesh);
        }
    }
}
