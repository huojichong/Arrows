using UnityEngine;

/// <summary>
/// 箭头可视化组件，用于在场景中显示箭头方向
/// </summary>
[RequireComponent(typeof(ArrowBlock))]
public class ArrowVisualizer : MonoBehaviour
{
    [Header("可视化配置")]
    public bool showArrowInScene = true;
    public Color arrowColor = Color.cyan;
    public float arrowSize = 0.5f;
    
    private ArrowBlock arrowBlock;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Awake()
    {
        arrowBlock = GetComponent<ArrowBlock>();
        
        // 如果没有MeshFilter，创建一个
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        
        // 如果没有MeshRenderer，创建一个
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        CreateArrowMesh();
    }
    
    /// <summary>
    /// 创建箭头3D模型
    /// </summary>
    void CreateArrowMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Arrow Mesh";
        
        // 创建一个简单的箭头形状（由立方体和三角锥组成）
        Vector3[] vertices = new Vector3[]
        {
            // 箭头主体（长方体）
            new Vector3(-0.15f, 0, 0), new Vector3(0.15f, 0, 0),
            new Vector3(-0.15f, 0.3f, 0), new Vector3(0.15f, 0.3f, 0),
            new Vector3(-0.15f, 0, 0.8f), new Vector3(0.15f, 0, 0.8f),
            new Vector3(-0.15f, 0.3f, 0.8f), new Vector3(0.15f, 0.3f, 0.8f),
            
            // 箭头头部（三角锥）
            new Vector3(-0.3f, 0, 0.8f), new Vector3(0.3f, 0, 0.8f),
            new Vector3(-0.3f, 0.3f, 0.8f), new Vector3(0.3f, 0.3f, 0.8f),
            new Vector3(0, 0, 1.2f), new Vector3(0, 0.3f, 1.2f)
        };
        
        // 定义三角形面
        int[] triangles = new int[]
        {
            // 主体前面
            0, 2, 1, 1, 2, 3,
            // 主体后面
            5, 7, 4, 4, 7, 6,
            // 主体左面
            4, 6, 0, 0, 6, 2,
            // 主体右面
            1, 3, 5, 5, 3, 7,
            // 主体顶面
            2, 6, 3, 3, 6, 7,
            // 主体底面
            4, 0, 5, 5, 0, 1,
            
            // 箭头头部前面
            8, 10, 12, 9, 12, 11,
            // 箭头头部左侧
            8, 12, 4, 4, 12, 6,
            // 箭头头部右侧
            9, 5, 12, 12, 5, 7,
            // 箭头头部顶面
            10, 13, 12, 11, 12, 13
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        // 创建材质
        if (meshRenderer.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = arrowColor;
            meshRenderer.material = mat;
        }
    }
    
    /// <summary>
    /// 更新箭头方向显示
    /// </summary>
    public void UpdateArrowDirection()
    {
        if (arrowBlock == null) return;
        
        // 根据箭头方向旋转模型
        float angle = 0;
        switch (arrowBlock.currentDirection)
        {
            case ArrowBlock.Direction.Up:
                angle = 0;
                break;
            case ArrowBlock.Direction.Right:
                angle = 90;
                break;
            case ArrowBlock.Direction.Down:
                angle = 180;
                break;
            case ArrowBlock.Direction.Left:
                angle = 270;
                break;
        }
        
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }
    
    void Update()
    {
        UpdateArrowDirection();
    }
    
    /// <summary>
    /// 在Scene视图中绘制箭头和路径
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showArrowInScene || arrowBlock == null) return;
        
        // 绘制箭头路径
        if (arrowBlock.pathNodes != null && arrowBlock.pathNodes.Count > 1)
        {
            Gizmos.color = arrowColor;
            
            for (int i = 0; i < arrowBlock.pathNodes.Count - 1; i++)
            {
                Vector3 start = arrowBlock.pathNodes[i];
                Vector3 end = arrowBlock.pathNodes[i + 1];
                
                // 绘制路径线
                Gizmos.DrawLine(start + Vector3.up * 0.15f, end + Vector3.up * 0.15f);
                
                // 绘制路径点
                Gizmos.DrawWireSphere(start + Vector3.up * 0.15f, 0.1f);
            }
            
            // 绘制终点
            Gizmos.DrawWireSphere(arrowBlock.pathNodes[arrowBlock.pathNodes.Count - 1] + Vector3.up * 0.15f, 0.15f);
        }
        
        // 绘制当前方向指示
        Vector3 dirVector = Vector3.zero;
        switch (arrowBlock.currentDirection)
        {
            case ArrowBlock.Direction.Up:
                dirVector = Vector3.forward;
                break;
            case ArrowBlock.Direction.Right:
                dirVector = Vector3.right;
                break;
            case ArrowBlock.Direction.Down:
                dirVector = Vector3.back;
                break;
            case ArrowBlock.Direction.Left:
                dirVector = Vector3.left;
                break;
        }
        
        Gizmos.color = Color.yellow;
        Vector3 arrowStart = transform.position + Vector3.up * 0.5f;
        Vector3 arrowEnd = arrowStart + dirVector * 0.5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Gizmos.DrawSphere(arrowEnd, 0.08f);
    }
}
