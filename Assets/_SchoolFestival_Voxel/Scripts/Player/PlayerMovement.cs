using NaughtyAttributes;
using R3;
using SchoolFestival_Voxel.Scripts.Player.Interfaces;
using UnityEngine;
using VContainer;
using System;
using NUnit.Framework.Constraints;

namespace SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        public float CurrentMoveSpeed => _rigidbody.linearVelocity.magnitude;
        public float MaxSpeed => _maxSpeed;
        [Header("Required Components")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Camera _camera;
        
        /// <summary>
        /// 最大速度
        /// </summary>
        [Header("Parameters")]
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _floatPower = 100f;
        

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
        
        /// <summary>
        /// これがVContainer？よくわからない…
        /// </summary>
        /// <param name="playerInputManager"></param>
        [Inject]
        public void Constructor(IPlayerInputManager playerInputManager)
        {
            _playerInputManager = playerInputManager;
        }
        public void InGame() =>_isMovable = true;
        public void OutGame() =>_isMovable = false;

        private void Start()
        {
            _playerInputManager.OnMove
                .Subscribe(input => Move(input))
                .AddTo(this);
            _isGround
                .Subscribe(OnChangeIsGround)
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
            /*
            GameJudge.instance?
                .OnGameOver
                .Subscribe(_ => _isMovable = false)
                .AddTo(this);
                */
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
            if (!_isMovable) return;
            Transform cameraTransform = _camera.transform;
            
            // カメラの方向をXZ軸のみ取得
            Vector3 cameraForward = cameraTransform.forward * inputValue.y;
            Vector3 cameraRight   = cameraTransform.right   * inputValue.x;
            Vector3 moveDirection = (cameraForward + cameraRight).normalized;
            Vector3 targetVelocity = moveDirection * _maxSpeed;
            
            // XZ平面のみを考慮する
            targetVelocity.y = 0f;

            // 滞空時は移動量を調整する
            if (!_isGround.Value)
                // targetVelocity *= _airMoveMultiplier;
            
            _rigidbody.AddForce(targetVelocity, ForceMode.Force);
            Float();
        }
        private void Float()
        {
            if (_isFloatingLeft|| _isFloatingRight)
                _rigidbody.AddForce(Vector3.up * _floatPower, ForceMode.Force);
        }

        private void OnChangeIsGround(bool isGround)
        {
            // _rigidbody.linearDamping = isGround ? _groundDrag : _airDrag;
        }

        private bool CheckIsGround()
        {
            var origin = transform.position + _groundCastBoxOffset;
            return Physics.CheckBox(origin, _groundCastBoxSize / 2f, transform.rotation, _groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + _groundCastBoxOffset, _groundCastBoxSize);
        }
    }
}
