using System.Threading;
using _SchoolFestival_Voxel.Scripts.Timer;
using Cysharp.Threading.Tasks;
using KeyBoard;
using Ranking.Demo.Scripts.DemoGame;
using UnityEngine;
using UnityEngine.UI;
using Ranking.Scripts;
using SchoolFestival_Voxel.Scripts.Voxel.Remake;

namespace _SchoolFestival_Voxel.Scripts.GameManager
{
    /// <summary>
    /// ゲームの進行を管理するクラス
    /// </summary>
    public class MainSequence : MonoBehaviour
    {
        [SerializeField]
        private Button _startButton;
        [SerializeField] private TimeController _timeController;
        [SerializeField] private PlayerTeleport _playerTeleport;
        [SerializeField] private VoxelPhysicsController _voxelPhysicsController;
        [SerializeField] private PlayerScoreHolder  _playerScoreHolder;
        [SerializeField] private InputKeyCollector  _inputKeyCollector;
        [SerializeField] private RankingStorage _rankingStorage;
        [SerializeField] private LogInManager  _logInManager;
        [SerializeField] private ChunkManager  _chunkManager;

        private async UniTaskVoid Start() => GameStartAsync().Forget();

        /// <summary>
        /// メインゲームを進行する
        /// </summary>
        private async UniTask GameStartAsync()
        {
            //ループ開始
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                _voxelPhysicsController.OutVoxelMode();
                await _playerTeleport.TeleportToKeyboardAsync(cts.Token);

                _startButton.gameObject.SetActive(true);
                
                //ここでステージ生成終わるまで待つ(まだない)
                //ゲームスタートボタン押すまで待つ
                await _startButton.OnClickAsync(cts.Token);
                _voxelPhysicsController.InVoxelMode();
                //プレイヤーの移動完了まで待つ
                await _playerTeleport.StageTeleportAsync(cts.Token);
                //ゲームスタートカウントダウン(まだない)

                //タイマースタート
                await _timeController.StartTimerAsync(cts.Token);

                Debug.Log("ゲーム終了");
                
                cts.Cancel();
                _rankingStorage.Register();
                _inputKeyCollector.ResetText();
                _playerScoreHolder.ResetScore();
                _logInManager.LogIn();
                _chunkManager.ResetStage();

                // await _scorePresenter.OnShowScoreAnimationAsync(destroyCancellationToken);
                
            }
        }

    }
}