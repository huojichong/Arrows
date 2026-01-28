using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameController
{
    public interface IGameController
    {
        void OnGridClicked(Vector3Int obj);

        UniTask InitAsync(int level);
    }
}