using System.Threading;
using _SchoolFestival_Voxel.Scripts.Player;
using _SchoolFestival_Voxel.Scripts.Timer;
using _SchoolFestival_Voxel.Scripts.UI;
using _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528;
using Cysharp.Threading.Tasks;
using KeyBoard;
using UnityEngine;
using UnityEngine.UI;
using SchoolFestival_Voxel.Scripts.Player;
using Shinkan2024.Ranking.Demo.Scripts.DemoGame.Player;
using Shinkan2025_Cooking.Ranking.Scripts;

namespace _SchoolFestival_Voxel.Scripts.GameManager
{
    /// <summary>
    /// ゲームの進行（シークエンス）を管理するクラス
    /// </summary>
    public class MainSequence : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _startButton;
        [SerializeField] private TimePresenter _timePresenter;
        [SerializeField] private ScorePresenter _scorePresenter;

        [Header("Game Controllers")]
        [SerializeField] private TimeController _timeController;
        [SerializeField] private PlayerTeleport _playerTeleport;
        [SerializeField] private PlayerScoreHolder _playerScoreHolder;
        [SerializeField] private InputKeyCollector _inputKeyCollector;
        [SerializeField] private RankingStorage _rankingStorage;
        [SerializeField] private LogInManager _logInManager;
        [SerializeField] private TutorialClearChecker _tutorialClearChecker;

        [Header("Player References")]
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerhandController _playerhandController;

        [Header("Voxel References")]
        [SerializeField] private MagicaVoxelLoader _voxelLoader;
        [SerializeField] private VoxelWorld _voxelWorld;

        private void Awake()
        {
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }
        }

        private async UniTaskVoid Start() => GameStartAsync().Forget();

        /// <summary>
        /// メインゲームの進行ループ
        /// </summary>
        private async UniTask GameStartAsync()
        {
            // ゲーム全体のループ開始
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                
                // ボクセルワールド全体をクリアし、初期地形モデルをロードし直して再構築
                if (_voxelLoader != null)
                {
                    _voxelLoader.LoadAllModels();
                }

                // 初期地形ロード完了後、すべての VoxelLockItem をワールドに一括登録
                if (_voxelWorld != null)
                {
                    VoxelLockItem.RegisterAllToWorld(_voxelWorld);
                }
                
                // 1. 初期状態（キーボード入力・ロビー待機）のセットアップ
                _playerhandController.OutGame();
                _timePresenter.gameObject.SetActive(false);
                _scorePresenter.gameObject.SetActive(false);
                _playerMovement.OutGame();
                
                // プレイヤーをキーボード（ログインエリア）へテレポートして開始ボタンを有効化
                await _playerTeleport.TeleportToKeyboardAsync(cts.Token);
                _startButton.gameObject.SetActive(true);

                // スタートボタンが押されるのを待機
                await _startButton.OnClickAsync(cts.Token);
                _startButton.gameObject.SetActive(false);

                // 2. ゲーム本番開始
                _playerMovement.InGame();
                _playerhandController.InGame();
                
                // ステージの開始点（旧チュートリアルエリアのスタート地点）へテレポート
                await _playerTeleport.TutorialTeleportAsync(cts.Token);
                await _tutorialClearChecker.OnStartTutorial(cts.Token);
                _timePresenter.gameObject.SetActive(true);
                
                // タイマースタート（カウントダウン演出を含む）
                await _timeController.StartTimerAsync(cts.Token);
                

                // 3. ゲーム終了・リザルトフェーズ
                cts.Cancel(); // 各種非同期処理のキャンセル
                _rankingStorage.Register();
                _playerMovement.OutGame();
                _timePresenter.gameObject.SetActive(false);

                // スコア演出アニメーションの再生
                _scorePresenter.gameObject.SetActive(true);
                await _scorePresenter.ShowScoreAnimationAsync(destroyCancellationToken);
                
                // 4. ステージのリセット処理
                _inputKeyCollector.ResetText();
                _playerScoreHolder.ResetScore();
                _logInManager.LogIn();
                
                // 埋められていたすべてのアイテムの物理状態・初期位置を一括リセット
                VoxelLockItem.ResetAllItems();
            }
        }
    }
}