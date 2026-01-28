using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace GameController
{
    public class ClassicsGameController : IGameController
    {

        // [Header("关卡配置")] 
        public int currentLevel = 1;
        public int totalMoves = 0;

        protected Dictionary<int,IArrow> arrowSnakes = new Dictionary<int,IArrow>();

        private GridSystem gridSystem { get; set; }
        
        protected GameObject arrowBlockPrefab { get; set; }

        public void InitConfig(GridSystem gridSystem, GameObject arrowBlockPrefab)
        {
            this.gridSystem = gridSystem;
            this.arrowBlockPrefab = arrowBlockPrefab;
        }

        public void OnGridClicked(Vector3Int obj)
        {
            Debug.Log("onGridClicked:" + obj);
            var arrow = gridSystem.GetArrowBlockAt(obj) ?? gridSystem.GetArrowBlocksInRadius(obj);

            
            if (arrow == null)
            {
                return;
            }

            Debug.Log($"点击了箭头: {arrow}");
            if (arrow.IsMoving)
            {
                Debug.Log("箭头正在移动中，无法点击");
                return;
            }
            else if (IsCanMoveOut(arrow, gridSystem, out int distance))
            {
                gridSystem.UnregisterArrowBlock(arrow);
                arrowSnakes.Remove(arrow.ArrowData.id);
                arrow.MoveOut();
            }
            else
            {
                // 移动不出去，前移 distance 距离，然后反弹回来。
                arrow.StartMoving(distance);

                // 查找到撞击点
                // var hitPoint = arrow.ArrowData.header + arrow.ArrowData.direction * distance;
                // // 拿到被撞的箭头，
                // var hitArrow = gridSystem.GetArrowBlockAt(hitPoint);
                // // 
                // hitArrow.Hited(hitPoint, arrow.ArrowData.direction);


                Debug.Log("无法移动出去。。");
                return;
            }

            totalMoves++;

            // 检查是否有其他箭头在移动
            CheckGameState();
        }

        public UniTask InitAsync(int level)
        {
            gridSystem.InitializeGrid();
            arrowSnakes.Clear();
            
            return CreateSnakeCorAsync();
        }

        private async UniTask CreateSnakeCorAsync()
        {
            var data = LevelDataReader.LoadLevelData(0);
            foreach (var blockData in data.arrowBlocks)
            {
                // yield return new WaitForEndOfFrame();
                var arrow = CreateArrowBlock(blockData);
                gridSystem.RegisterArrowBlock(arrow);
                arrowSnakes.TryAdd(arrow.ArrowData.id,arrow);
                // yield break;
            }
        
            await UniTask.NextFrame();
        }

        /// <summary>
        /// 创建箭头块
        /// </summary>
        IArrow CreateArrowBlock(ArrowData data)
        {
            var ropeSnake = GameObject.Instantiate(arrowBlockPrefab).GetComponent<IArrow>();
            var path = new List<Vector3>();
            var arrVect = new Vector3(data.direction.x, data.direction.y, data.direction.z);

            var endPos = data.customPath.Last() + arrVect * 30;

            bool isRemoveSameLinePoint = false;

            if (isRemoveSameLinePoint)
            {
                var firstPos = data.customPath.First();
                path.Add(new Vector3(firstPos.x, firstPos.y, firstPos.z));

                for (int i = 1; i < data.customPath.Count - 1; i++)
                {
                    var pre = data.customPath[i - 1];
                    var current = data.customPath[i];
                    var next = data.customPath[i + 1];

                    // 判断点 pre, current,next 是否在同一条线上，
                    if (Vector3.Dot(current - pre, next - current) < 0.01f)
                    {
                        // 不在同一条线上
                        Debug.Log("点 pre, current,next 不在同一条线上");


                        path.Add(new Vector3(current.x, current.y, current.z));
                    }

                }

                var lastPos = data.customPath.Last();
                path.Add(new Vector3(lastPos.x, lastPos.y, lastPos.z));
            }
            else
            {
                foreach (var pos in data.customPath)
                {
                    path.Add(new Vector3(pos.x, pos.y, pos.z));
                }
            }
            // 还有头的显示, 最后一个是头

            path.Add(endPos);
            // 延长起点坐标
            ropeSnake.SetWaypoints(path);

            ropeSnake.SetData(data);
            ropeSnake.InitArrow();
            return ropeSnake;
        }

        /// <summary>
        /// 检查游戏状态
        /// </summary>
        void CheckGameState()
        {
            // 检查是否所有箭头都完成移动
            bool allCompleted = false;

            // 检查普通箭头
            // foreach (var arrow in arrowBlocks)
            // {
            //     if (arrow.IsMoving)
            //     {
            //         allCompleted = false;
            //         break;
            //     }
            // }

            if (arrowSnakes.Count == 0)
            {
                CheckLevelCompleteAsync();
            }
        }

        /// <summary>
        /// 检查关卡是否完成
        /// </summary>
        async UniTask CheckLevelCompleteAsync()
        {
            // 这里可以添加关卡完成的判断逻辑
            // 例如：检查所有箭头是否到达目标位置
            Debug.Log("所有箭头移动完成！");

            await UniTask.DelayFrame(30);
            
            currentLevel++;
            await InitAsync(currentLevel);
        }



        private bool IsCanMoveOut(IArrow arrow, GridSystem gridSystem1, out int distance)
        {
            // 检查前方是否有别的方块阻挡
            distance = 0;
            var header = arrow.ArrowData.header;
            var dir = arrow.ArrowData.direction;
            for (int i = 1; i < 30; i++)
            {
                if (gridSystem1.IsGridOccupied(header + dir * i))
                {
                    distance = i;
                    return false;
                }
            }

            return true;
        }
    }
}