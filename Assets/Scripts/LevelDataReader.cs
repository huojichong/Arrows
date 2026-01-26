using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConfigBean;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public static class LevelDataReader
    {
        public static LevelData LoadLevelData(int level)
        {
            var data = new  LevelData();

            var json = File.ReadAllText(Application.streamingAssetsPath + "/Current_Level_1768961611274.json");
            Debug.Log(json);
            var dataConfig = JsonConvert.DeserializeObject<LevelDataConfig>(json);
            
            data.levelName = dataConfig.levelName;
            data.levelNumber = dataConfig.levelNumber;
            data.arrowBlocks = new List<ArrowData>();
            
            foreach (var snakeDataConfig in dataConfig.snakes)
            {
                var arrowBlockData = new ArrowData();
                arrowBlockData.id = snakeDataConfig.id;
                arrowBlockData.direction = new Vector3Int(snakeDataConfig.direction.dr,0, snakeDataConfig.direction.dc);

                // ColorUtility.TryParseHtmlString("#" + snakeDataConfig.blockColor, out arrowBlockData.blockColor);
                arrowBlockData.blockColor = Color.red;
                arrowBlockData.customPath = new List<Vector3Int>();
                arrowBlockData.pathLength = CalcLength(snakeDataConfig);
                foreach (var segment in snakeDataConfig.segments)
                {
                    arrowBlockData.customPath.Add(new Vector3Int(segment.r, 0, segment.c));
                }
                
                data.arrowBlocks.Add(arrowBlockData);
            }
            
            return data;
        }

        private static int CalcLength(SnakeDataConfig snake)
        {
            int totalLength = 0;
            for (int i = 1; i < snake.segments.Count; i++)
            {
                var prev = snake.segments[i - 1];
                var curr = snake.segments[i];

                int dr = curr.r - prev.r;
                int dc = curr.c - prev.c;

                // 欧几里得距离
                totalLength += Mathf.Abs(dr) + Mathf.Abs(dc);
            }

            return totalLength;
        }
        
        
    }
}