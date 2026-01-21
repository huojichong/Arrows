using System.Collections.Generic;
using System.IO;
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

            var json = File.ReadAllText(Application.persistentDataPath + "/Current_Level_1768961611274.json");
            Debug.Log(json);
            var dataConfig = JsonConvert.DeserializeObject<LevelDataConfig>(json);
            
            data.levelName = dataConfig.levelName;
            data.levelNumber = dataConfig.levelNumber;
            data.arrowBlocks = new List<ArrowBlockData>();

            foreach (var snakeDataConfig in dataConfig.snakes)
            {
                var arrowBlockData = new ArrowBlockData();
                arrowBlockData.id = snakeDataConfig.id;
                arrowBlockData.direction = new Vector2Int(snakeDataConfig.direction.dr, snakeDataConfig.direction.dc);
                
                arrowBlockData.blockColor = snakeDataConfig.blockColor;
                arrowBlockData.customPath = new List<Vector2Int>();
                foreach (var segment in snakeDataConfig.segments)
                {
                    arrowBlockData.customPath.Add(new Vector2Int(segment.r, segment.c));
                }
            }
            
            return data;
        }
        
    }
}