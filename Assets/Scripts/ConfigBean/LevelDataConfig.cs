using System;
using System.Collections.Generic;

namespace ConfigBean
{
    /// <summary>
    /// 关卡数据配置
    /// </summary>
    [Serializable]
    public class LevelDataConfig
    {
        public int levelNumber;
        public string levelName;
        public List<SnakeDataConfig> snakes = new List<SnakeDataConfig>();
    }

    /// <summary>
    /// 单个箭头块的数据
    /// </summary>
    [Serializable]
    public class SnakeDataConfig
    {
        public string id;
        public SnakeDirection direction;
        
        public string blockColor = "";
        public List<SnakePos> segments = new List<SnakePos>();  // 自定义路径点
    }
    
    [Serializable]
    public struct SnakePos
    {
        public int r;
        public int c;
    }

    [Serializable]
    public struct SnakeDirection
    {
        public int dr;
        public int dc;
    }
}