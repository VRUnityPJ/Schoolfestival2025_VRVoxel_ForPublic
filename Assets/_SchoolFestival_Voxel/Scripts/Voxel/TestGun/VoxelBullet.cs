using _SchoolFestival_Voxel.Scripts.Player;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// すべてのボクセル弾丸（Bullet）の基底となる抽象クラス。
    /// VoxelObjectSpawner 経由で生成されることで、各種依存関係が自動で注入されます。
    /// </summary>
    public abstract class VoxelBullet : MonoBehaviour
    {        [Header("Bullet Settings")]
        [SerializeField] protected float _maxDistance = 100f;
        [SerializeField] protected bool _isChargeable = false;
        [SerializeField] protected float _maxChargeTime = 2.0f;
        [SerializeField] protected float _speed = 30f; // 弾速 (m/s)
        [SerializeField] protected float _selfCollisionIgnoreDistance = 1.0f; // 自己衝突を防ぐために無視する開始距離 (m)
        [SerializeField] protected LayerMask _hitLayers = ~0; // 衝突レイヤー
        
        [Header("Physics Target Settings")]
        [SerializeField] protected bool _registerAsPhysicsTarget = true;
        [SerializeField] protected float _physicsActivationRadius = 16f; // 通常1チャンク分(16m)のコライダーを有効化

        public bool IsChargeable => _isChargeable;
        public float MaxChargeTime => _maxChargeTime;

        // DI注入される依存関係群
        [Inject] protected VoxelWorld _voxelWorld;
        [Inject] protected VoxelWorldRenderer _voxelWorldRenderer;
        [Inject] protected VoxelDestoyer _voxelDestoyer;
        [Inject] protected VoxelPainter _voxelPainter;
        [Inject] protected VoxelBuilder _voxelBuilder;

        protected bool _isFired = false;
        protected Vector3 _direction;
        protected float _chargeTime;
        protected Vector3 _startPosition;
        protected Vector3 _lastPosition;
        protected float _traveledDistance = 0f;

        protected virtual void Start()
        {
            // DIが機能しなかった場合のフォールバック検索
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }

            // 弾丸の周囲のコライダーを有効化するため、自身を物理ターゲットに登録
            if (_registerAsPhysicsTarget && _voxelWorld != null)
            {
                _voxelWorld.RegisterPhysicsTarget(transform, _physicsActivationRadius);
            }
        }

        protected virtual void OnDestroy()
        {
            // 弾丸消滅時に登録解除
            if (_registerAsPhysicsTarget && _voxelWorld != null)
            {
                _voxelWorld.UnregisterPhysicsTarget(transform);
            }
        }

        protected virtual void Update()
        {
            if (!_isFired) return;

            float moveStep = _speed * Time.deltaTime;
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + _direction * moveStep;

            float nextTraveledDistance = _traveledDistance + moveStep;

            // 線分を表現するため、前フレーム位置から現フレーム位置までのベクトルに対して RaycastAll を実行
            Vector3 diff = nextPos - _lastPosition;
            float distance = diff.magnitude;

            if (distance > 0.0001f)
            {
                Vector3 direction = diff / distance;
                RaycastHit[] hits = Physics.RaycastAll(_lastPosition, direction, distance, _hitLayers);
                if (hits.Length > 0)
                {
                    // 起点に近い順（distanceの昇順）にソート
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (var hit in hits)
                    {
                        // 1. 自分自身のコライダーは無視
                        if (hit.collider.gameObject == gameObject) continue;

                        float distFromStart = Vector3.Distance(_startPosition, hit.point);

                        // 詳細な衝突ログ
                        Debug.Log($"[VoxelBullet Debug] Hit: {hit.collider.gameObject.name} (Tag: {hit.collider.tag}) at {hit.point}, Distance: {distFromStart:F2}m (Ignore: {_selfCollisionIgnoreDistance:F2}m)");

                        // 2. プレイヤーや手元、武器、銃モデルのコライダーは無視
                        if (hit.collider.CompareTag("Player") ||
                            hit.collider.GetComponentInParent<PlayerhandController>() != null ||
                            hit.collider.name.Contains("Hand") ||
                            hit.collider.name.Contains("Controller") ||
                            hit.collider.name.Contains("Gun") ||
                            hit.collider.name.Contains("Hummer"))
                        {
                            Debug.Log($"[VoxelBullet Debug] -> Ignored (Player/Hand/Gun): {hit.collider.gameObject.name}");
                            continue;
                        }

                        // 3. 自己衝突防止距離以内の衝突は無視
                        if (distFromStart <= _selfCollisionIgnoreDistance)
                        {
                            Debug.Log($"[VoxelBullet Debug] -> Ignored (Within ignore distance: {distFromStart:F2}m <= {_selfCollisionIgnoreDistance:F2}m): {hit.collider.gameObject.name}");
                            continue;
                        }

                        // 有効な衝突を検出！
                        Debug.Log($"[VoxelBullet Debug] -> VALID HIT! Target: {hit.collider.gameObject.name} at {hit.point}");
                        transform.position = hit.point;
                        OnHit(hit);
                        _isFired = false;
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            _lastPosition = currentPos;
            transform.position = nextPos;
            _traveledDistance = nextTraveledDistance;

            if (_traveledDistance >= _maxDistance)
            {
                OnMaxDistanceReached();
                _isFired = false;
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 銃から発射された瞬間に呼び出される実行ロジック。
        /// </summary>
        public virtual void Fire(Vector3 origin, Vector3 direction, float chargeTime)
        {
            _direction = direction.normalized;
            _chargeTime = chargeTime;
            _startPosition = origin;
            _lastPosition = origin;
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(_direction);
            _traveledDistance = 0f;
            _isFired = true;
        }



        /// <summary>
        /// 飛行中に何かに衝突した際の処理（各弾丸でオーバーライドする）。
        /// </summary>
        protected abstract void OnHit(RaycastHit hit);

        /// <summary>
        /// 衝突せずに最大飛距離に達した際の処理（デフォルトは消滅、必要に応じてオーバーライド）。
        /// </summary>
        protected virtual void OnMaxDistanceReached()
        {
            // デフォルトは何もしない
        }
    }
}
