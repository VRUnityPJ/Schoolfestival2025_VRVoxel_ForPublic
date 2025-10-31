using System.Threading;
using _SchoolFestival_Voxel.Scripts.Player;
using _SchoolFestival_Voxel.Scripts.Timer;
using _SchoolFestival_Voxel.Scripts.UI;
using _SchoolFestival_Voxel.Scripts.Voxel.Remake;
using Cysharp.Threading.Tasks;
using KeyBoard;
using Ranking.Demo.Scripts.DemoGame;
using UnityEngine;
using UnityEngine.UI;
using Ranking.Scripts;
using SchoolFestival_Voxel.Scripts.Player;
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
        [SerializeField] private ChunkManager  _stageChunkManager;
        [SerializeField] private RemakeMeshDestroyer _stageMeshDestroyer;
        [SerializeField] private ChunkManager  _tutorialChunkManager;
        [SerializeField] private RemakeMeshDestroyer _tutorialMeshDestroyer;
        [SerializeField] private TimePresenter _timePresenter;
        [SerializeField] private ScorePresenter _scorePresenter;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerhandController _playerhandController;
        [SerializeField] private TutorialClearChecker _tutorialClearChecker;
        [SerializeField] private SwingDetector _detectorLeft;
        [SerializeField] private SwingDetector _detectorRight;

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
                _playerhandController.OutGame();
                _timePresenter.gameObject.SetActive(false);
                _scorePresenter.gameObject.SetActive(false);
                _voxelPhysicsController.OutVoxelMode();
                _playerMovement.OutGame();
                await _playerTeleport.TeleportToKeyboardAsync(cts.Token);

                _startButton.gameObject.SetActive(true);

                //ここでステージ生成終わるまで待つ(まだない)
                //ゲームスタートボタン押すまで待つ
                await _startButton.OnClickAsync(cts.Token);
                //チュートリアルステージに移動
                _voxelPhysicsController.InVoxelMode();
                _playerMovement.InGame();
                _playerhandController.InGame();
                _detectorLeft._chunkManager =  _tutorialChunkManager;
                _detectorRight._chunkManager =  _tutorialChunkManager;
                _detectorLeft._meshDestroyer  = _tutorialMeshDestroyer;
                _detectorRight._meshDestroyer  = _tutorialMeshDestroyer;
                await _playerTeleport.TutorialTeleportAsync(cts.Token);
                _voxelPhysicsController._chunkManager=_tutorialChunkManager;
                
                
                await _tutorialClearChecker.OnStartTutorial(cts.Token);
                _detectorLeft._chunkManager =  _stageChunkManager;
                _detectorRight._chunkManager =  _stageChunkManager;
                _detectorLeft._meshDestroyer  = _stageMeshDestroyer;
                _detectorRight._meshDestroyer  = _stageMeshDestroyer;
                
                
                //ゲームスタート
                //プレイヤーの移動完了まで待つ
                _voxelPhysicsController._chunkManager=_stageChunkManager;
                await _playerTeleport.StageTeleportAsync(cts.Token);
                _timePresenter.gameObject.SetActive(true);
                
              

                //タイマースタート
                await _timeController.StartTimerAsync(cts.Token);
                _scorePresenter.gameObject.SetActive(true);
                

                cts.Cancel();
                _rankingStorage.Register();
                _playerMovement.OutGame();
                _timePresenter.gameObject.SetActive(false);
                

                await _scorePresenter.ShowScoreAnimationAsync(destroyCancellationToken);
                _inputKeyCollector.ResetText();
                _playerScoreHolder.ResetScore();
                _logInManager.LogIn();
                _stageChunkManager.ResetStage();
                _tutorialChunkManager.ResetStage();
                

            }
        }

    }
}