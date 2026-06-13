using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    [RequireComponent(typeof(Rigidbody))]
    public class VoxelLockItem : MonoBehaviour
    {
        // 全てのアクティブなVoxelLockItemを一元管理するための静的リスト（シーン走査を排除）
        private static readonly List<VoxelLockItem> _allInstances = new List<VoxelLockItem>();
        public static IReadOnlyList<VoxelLockItem> AllInstances => _allInstances;

        [Inject] private VoxelWorld _voxelWorld;

        private Rigidbody _rigidBody;
        private bool _isUnearthed = false;
        
        // 登録されているチャンクの座標と、現在登録中かどうかのフラグ
        private Vector3Int _registeredChunkCoord;
        private bool _isRegistered = false;

        [Header("Excavation Settings")]
        [SerializeField] private Vector3Int _checkBounds = new Vector3Int(3, 3, 3);

        // リセット用の初期値保存
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        private void Awake()
        {
            _rigidBody = GetComponent<Rigidbody>();
            _rigidBody.isKinematic = true;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            
            // Awake時に全インスタンス管理用リストに追加する（非アクティブになっても参照を維持するため）
            if (!_allInstances.Contains(this))
            {
                _allInstances.Add(this);
            }
        }

        private void OnEnable()
        {
            RegisterToWorld();
            _rigidBody.isKinematic = true;
        }

        private void OnDisable()
        {
            UnregisterFromWorld();
            
            // 非アクティブ化された時、物理ターゲットリストから除外する
            if (_voxelWorld != null)
            {
                _voxelWorld.UnregisterPhysicsTarget(transform);
            }
        }

        private void Start()
        {
            // DIが機能しなかった場合のセーフティフォールバック
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }

            RegisterToWorld();
        }

        private void OnDestroy()
        {
            _allInstances.Remove(this);
            UnregisterFromWorld();
            
            // 破棄された時、物理ターゲットリストから除外する
            if (_voxelWorld != null)
            {
                _voxelWorld.UnregisterPhysicsTarget(transform);
            }
        }

        /// <summary>
        /// 所属するチャンクに対して、自身を検出走査対象として登録する
        /// </summary>
        [Button]
        private void RegisterToWorld()
        {
            if (_voxelWorld == null || _isRegistered) return;

            float voxelSize = _voxelWorld.VoxelSize;
            Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / voxelSize);
            _registeredChunkCoord = _voxelWorld.GetChunkCoordinate(gridPos);
            
            _voxelWorld.RegisterLockItem(_registeredChunkCoord, this);
            _isRegistered = true;
        }

        /// <summary>
        /// 所属チャンクの登録リストから自身を除外する
        /// </summary>
        private void UnregisterFromWorld()
        {
            if (_voxelWorld != null && _isRegistered)
            {
                _voxelWorld.UnregisterLockItem(_registeredChunkCoord, this);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// 地形破壊時に、更新されたチャンクの内部に属するもののみがピンポイントで実行される判定関数
        /// </summary>
        public void CheckIfUnearthed()
        {
            if (_isUnearthed) return; // すでに掘り起こされている場合はスルー

            float voxelSize = _voxelWorld.VoxelSize;
            Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / voxelSize);

            // アイテムを中心に据えるため、判定範囲の半分だけオフセット
            Vector3Int startPoint = gridPos - _checkBounds / 2;

            if (_voxelWorld.IsAllEmpty(startPoint, _checkBounds))
            {
                _isUnearthed = true;
                _rigidBody.isKinematic = false;

                // 掘り起こされたため、以降の地形破壊時の検索リストから完全に登録解除する (O(1)最適化)
                UnregisterFromWorld();

                // 発掘された瞬間に自身の周囲のコライダーを有効化するため、物理ターゲットとして登録
                if (_voxelWorld != null)
                {
                    _voxelWorld.RegisterPhysicsTarget(transform, 16f);
                }
                _rigidBody.isKinematic = false;
            }
        }

        /// <summary>
        /// 単体のアイテム状態を初期状態にリセットする
        /// </summary>
        public void ResetItem()
        {
            // リセット時、物理ターゲットリストから自身を確実に登録解除する
            if (_voxelWorld != null)
            {
                _voxelWorld.UnregisterPhysicsTarget(transform);
            }

            _rigidBody.isKinematic = true;
            _rigidBody.linearVelocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            _rigidBody.isKinematic = true;

            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _isUnearthed = false;

            // チャンクへ再登録し、再び掘り起こし判定を受ける状態に戻す
            RegisterToWorld();
            this.gameObject.SetActive(true);
        }

        /// <summary>
        /// 静的リストに登録されたすべてのVoxelLockItemを一括リセットする（シーンのFindObjectを回避し、高速に動作）
        /// </summary>
        public static void ResetAllItems()
        {
            for (int i = _allInstances.Count - 1; i >= 0; i--)
            {
                if (_allInstances[i] != null)
                {
                    _allInstances[i].ResetItem();
                }
            }
        }

        /// <summary>
        /// すべてのVoxelLockItemを対象のVoxelWorldに対して一括再登録する（ステージ再生成直後に実行）
        /// </summary>
        public static void RegisterAllToWorld(VoxelWorld voxelWorld)
        {
            if (voxelWorld == null) return;
            
            foreach (var item in _allInstances)
            {
                if (item != null)
                {
                    item._voxelWorld = voxelWorld;
                    item._isRegistered = false; // 強制的に未登録状態にして再登録する
                    item.RegisterToWorld();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 旧コードのプレイヤー接触時スコア加算の移植
            if (_isUnearthed && other.gameObject.TryGetComponent(out Shinkan2024.Ranking.Demo.Scripts.DemoGame.Player.Player player))
            {
                player.AddScore(100);
                
                // // プレイヤーに獲得されたため、物理ターゲットリストから登録解除する
                // if (_voxelWorld != null)
                // {
                //     _voxelWorld.UnregisterPhysicsTarget(transform);
                // }

                gameObject.SetActive(false);
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            // 旧コードのプレイヤー接触時スコア加算の移植
            if (_isUnearthed && collision.gameObject.TryGetComponent(out Shinkan2024.Ranking.Demo.Scripts.DemoGame.Player.Player player))
            {
                player.AddScore(100);
                
                // // プレイヤーに獲得されたため、物理ターゲットリストから登録解除する
                // if (_voxelWorld != null)
                // {
                //     _voxelWorld.UnregisterPhysicsTarget(transform);
                // }

                gameObject.SetActive(false);
            }
        }
    }
}
