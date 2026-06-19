using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using _SchoolFestival_Voxel.Scripts.Voxel;
using R3;
using SchoolFestival_Voxel.Scripts.Player;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerMovement : MonoBehaviour, IStepClimbable
    {
        public float CurrentMoveSpeed => _rigidbody.linearVelocity.magnitude;
        public float MaxSpeed => _maxSpeed;

        [Header("Required Components")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Camera _camera;
        
        private VoxelWorld _voxelWorld;
        private CapsuleCollider _capsuleCollider;
        private CharacterController _characterController;
        private VoxelPhysicsController _physicsController;
        
        /// <summary>
        /// 最大速度
        /// </summary>
        [Header("Parameters")]
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _floatPower = 100f;
        [SerializeField] private float _maxAcceleration = 50f;
        [SerializeField] private float _airControlMultiplier = 0.5f;
        
        [Header("Step Climb Settings")]
        [SerializeField] private bool _enableStepClimb = true;
        [SerializeField] private int _maxStepBlocks = 2;
        public int MaxStepBlocks => _maxStepBlocks;
        

        [Header("Ground Cast Setting")] 
        [SerializeField] private Vector3 _groundCastBoxSize = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] private Vector3 _groundCastBoxOffset = Vector3.zero;
        /// <summary>
        /// 地面となるレイヤー
        /// </summary>
        [SerializeField] private LayerMask _groundLayer;

        private readonly ReactiveProperty<bool> _isGround = new(false);
        private IPlayerInputManager _playerInputManager;
        private MainInput _mainInput;
        private bool _isMovable = true;
        private bool _isFloatingLeft = false;
        private bool _isFloatingRight = false;
        private float _lastMoveLogTime = 0f;

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _characterController = GetComponent<CharacterController>();
            _physicsController = GetComponent<VoxelPhysicsController>();
        }
        
        /// <summary>
        /// これがVContainer？よくわからない…
        /// </summary>
        /// <param name="playerInputManager"></param>
        [Inject]
        public void Constructor(IPlayerInputManager playerInputManager, VoxelWorld voxelWorld)
        {
            _playerInputManager = playerInputManager;
            _voxelWorld = voxelWorld;
        }
        public void InGame() =>_isMovable = true;
        public void OutGame() =>_isMovable = false;

        private void Start()
        {
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }

            if (_voxelWorld != null)
            {
                _voxelWorld.RegisterPhysicsTarget(transform, 48f);
            }

            _playerInputManager.OnMove
                .Subscribe(input => Move(input))
                .AddTo(this);
            _playerInputManager.OnFloatLeft
                .Subscribe(_ => _isFloatingLeft = true)
                .AddTo(this);
            _playerInputManager.OnFloatCanceledLeft
                .Subscribe(_ => _isFloatingLeft = false)
                .AddTo(this);
            _playerInputManager.OnFloatRight
                .Subscribe(_ => _isFloatingRight = true)
                .AddTo(this);
            _playerInputManager.OnFloatCanceledRight
                .Subscribe(_ => _isFloatingRight = false)
                .AddTo(this);
        }

        private void OnDestroy()
        {
            if (_voxelWorld != null)
            {
                _voxelWorld.UnregisterPhysicsTarget(transform);
            }
        }

        private void Update()
        {
            _isGround.Value = CheckIsGround();
        }

        /// <summary>
        /// プレイヤーを移動させるメソッド
        /// </summary>
        /// <param name="inputValue">移動方向を計算するための</param>
        private void Move(Vector2 inputValue)
        {
            if (Time.time - _lastMoveLogTime > 0.5f)
            {
                _lastMoveLogTime = Time.time;
            }

            if (!_isMovable) return;

            Transform cameraTransform = _camera.transform;
            
            // カメラの方向をXZ軸のみ取得
            Vector3 cameraForward = cameraTransform.forward * inputValue.y;
            Vector3 cameraRight   = cameraTransform.right   * inputValue.x;
            Vector3 moveDirection = (cameraForward + cameraRight).normalized;
            moveDirection.y = 0f;

            // すでに壁にめり込んでいる場合、壁の方向への移動入力を制限する（壁抜け防止）
            if (_physicsController != null)
            {
                foreach (var contact in _physicsController.ActiveContacts)
                {
                    // 壁（法線のY成分が低く、めり込みがある場合）
                    if (contact.normal.y < 0.7f && contact.penetration > 0.001f)
                    {
                        float dot = Vector3.Dot(moveDirection, contact.normal);
                        if (dot < 0f)
                        {
                            // 壁の法線方向の入力を相殺し、平行移動のみ残す
                            moveDirection -= contact.normal * dot;
                        }
                    }
                }
                
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    moveDirection.Normalize();
                }
                else
                {
                    moveDirection = Vector3.zero;
                }
            }

            Vector3 targetVelocity = moveDirection * _maxSpeed;
            
            // 速度差分（必要な加速度）を計算して適用する (VelocityChange モード)
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 velocityChange = targetVelocity - horizontalVelocity;

            bool isGrounded = CheckIsGround();

            // 接地／非接地に応じた最大加速度を制限
            float maxAccel = isGrounded ? _maxAcceleration : _maxAcceleration * _airControlMultiplier;
            float maxVelChange = maxAccel * Time.fixedDeltaTime;
            velocityChange = Vector3.ClampMagnitude(velocityChange, maxVelChange);
            
            _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
            Float();

            // --- 段差自動乗り越え（Step-Climb）の実行 ---
            if (_enableStepClimb && isGrounded && moveDirection.sqrMagnitude > 0.01f)
            {
                TryStepClimb(moveDirection);
            }
        }

        private void TryStepClimb(Vector3 moveDir)
        {
            if (_voxelWorld == null) return;

            float radius = 0.3f;
            float height = 2.0f;
            Vector3 colliderCenter = Vector3.zero;

            if (_capsuleCollider != null)
            {
                radius = _capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                height = _capsuleCollider.height * transform.lossyScale.y;
                colliderCenter = _capsuleCollider.center;
            }
            else if (_characterController != null)
            {
                radius = _characterController.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                height = _characterController.height * transform.lossyScale.y;
                colliderCenter = _characterController.center;
            }

            float voxelSize = _voxelWorld.VoxelSize;
            Vector3 worldBottom = transform.TransformPoint(colliderCenter) - transform.up * (height * 0.5f);
            float checkDist = radius + 0.2f;

            int targetStepY = -1;
            Vector3Int targetStepVoxel = Vector3Int.zero;

            // 低い段差から順にレイを撃って、登れる最大の高さを探す
            for (int dy = 0; dy < _maxStepBlocks; dy++)
            {
                // 各段の高さから水平にレイを撃つ（わずかに上から撃つことで、その高さのブロックを確実に捉える）
                Vector3 rayOrigin = worldBottom + Vector3.up * (dy * voxelSize + 0.1f);

                // もしこの高さがプレイヤーの頭部を超えている場合はチェックしない
                if (dy * voxelSize + 0.1f >= height) break;

                if (_voxelWorld.Raycast(rayOrigin, moveDir, checkDist, out VoxelRaycastHit hit))
                {
                    Vector3Int stepVoxel = hit.voxelPosition;
                    Vector3Int stepTopVoxel = stepVoxel + Vector3Int.up;

                    // その段差の直上が空気であるか
                    if (_voxelWorld.GetVoxel(stepTopVoxel).density < 0.5f)
                    {
                        // プレイヤーの身長分の頭上空間が空いているかチェック
                        int heightInVoxels = Mathf.CeilToInt(height / voxelSize);
                        bool headroomClear = true;

                        for (int yOffset = 1; yOffset < heightInVoxels; yOffset++)
                        {
                            if (_voxelWorld.GetVoxel(stepTopVoxel + Vector3Int.up * yOffset).density >= 0.5f)
                            {
                                headroomClear = false;
                                break;
                            }
                        }

                        if (headroomClear)
                        {
                            // 登る段差の候補を更新（より高い段差が検出された場合、そちらが優先される）
                            targetStepVoxel = stepVoxel;
                            targetStepY = stepVoxel.y;
                        }
                    }
                }
            }

            if (targetStepY != -1)
            {
                // 段差の天面のY座標を算出
                float targetWorldY = (targetStepVoxel.y + 1) * voxelSize + _voxelWorld.transform.position.y;

                // 物理位置を段差の上に補正
                _rigidbody.position = new Vector3(_rigidbody.position.x, targetWorldY + 0.02f, _rigidbody.position.z);
                // わずかに前方に進めることで、段差の角とのコライダー干渉を抜ける
                _rigidbody.position += moveDir * 0.05f;

                // 落下速度がある場合は打ち消す
                Vector3 velocity = _rigidbody.linearVelocity;
                if (velocity.y < 0)
                {
                    velocity.y = 0;
                    _rigidbody.linearVelocity = velocity;
                }
            }
        }

        private void Float()
        {
            if (_isFloatingLeft|| _isFloatingRight)
                _rigidbody.AddForce(Vector3.up * _floatPower, ForceMode.Force);
        }
        

        private bool CheckIsGround()
        {
            if (_physicsController != null)
            {
                return _physicsController.IsGrounded;
            }
            var origin = transform.position + _groundCastBoxOffset;
            return Physics.CheckBox(origin, _groundCastBoxSize / 2f, transform.rotation, _groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + _groundCastBoxOffset, _groundCastBoxSize);
        }
    }
}
