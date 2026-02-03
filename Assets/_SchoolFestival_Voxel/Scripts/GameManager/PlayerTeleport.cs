using UnityEngine;
using UnityEngine.Serialization;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace _SchoolFestival_Voxel.Scripts.GameManager
{
    public class PlayerTeleport : MonoBehaviour
    {
        [SerializeField]private Transform _keyboardPoint;
        [SerializeField]private Transform _stagePoint;
        [SerializeField]private Transform _tutorialPoint;
        [SerializeField]private GameObject _player;
        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Init()
        {

        }
        public async UniTask TutorialTeleportAsync(CancellationToken ct)
        {
            await Teleport(_tutorialPoint);
        }
        public async UniTask StageTeleportAsync(CancellationToken ct)
        {
            await Teleport(_stagePoint);
        }
        public async UniTask TeleportToKeyboardAsync(CancellationToken ct)
        {
            await Teleport(_keyboardPoint);
        }
        UniTask Teleport(Transform point)
        {
            _player.transform.position = point.position;
            return UniTask.CompletedTask;
        }
    }
}