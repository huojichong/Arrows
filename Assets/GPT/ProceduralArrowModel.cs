using UnityEngine;

public class ProceduralArrowModel : MonoBehaviour
{
    [Header("Model")]
    public int segmentCount = 20;
    public float segmentLength = 0.4f;
    public Vector2 segmentSize = new Vector2(0.3f, 0.3f);

    [Header("Generated")]
    public Transform visualRoot;
    public Transform[] bones;

    void Awake()
    {
        Generate();
    }

    public void Generate()
    {
        // 清理
        foreach (Transform c in transform)
            DestroyImmediate(c.gameObject);

        visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(transform);

        bones = new Transform[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            // Bone
            GameObject bone = new GameObject($"Bone_{i}");
            bone.transform.SetParent(transform);
            bones[i] = bone.transform;

            // Segment Mesh
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Segment_{i}";
            seg.transform.SetParent(visualRoot);
            seg.transform.localScale = new Vector3(
                segmentSize.x,
                segmentSize.y,
                segmentLength
            );

            // 初始对齐
            seg.transform.position = bone.transform.position;
            seg.transform.rotation = bone.transform.rotation;

            // 绑定关系
            seg.AddComponent<SegmentFollow>().target = bone.transform;
        }

        var follower = GetComponent<StretchableBendingFollower>();
        follower.bones = bones;
    }
}