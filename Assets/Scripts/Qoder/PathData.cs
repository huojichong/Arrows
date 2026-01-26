using System.Collections.Generic;


/// <summary>
/// 路径数据结构，存储预计算的密度图
/// </summary>
[System.Serializable]
public class PathData
{
    public List<float> distances = new List<float>();
    public List<float> weights = new List<float>();
    public float totalWeight = 0;
    public float fullLength = 0;
}
