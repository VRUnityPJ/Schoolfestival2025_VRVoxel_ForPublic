using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using NaughtyAttributes;
using R3;
using Unity.XR.CoreUtils;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerSnapTurn : MonoBehaviour, IPlayerSnapTurn
    {
        [SerializeField, Required] private XROrigin _xrOrigin;
        [SerializeField] private float _turnAngle = 30f;

        private IPlayerInputManager _playerInputManager;

        [Inject]
        public void Constructor(IPlayerInputManager playerInputManager)
        {
            _playerInputManager = playerInputManager;
        }

        private void Start()
        {
            // イベントのサブスクライブ
            _playerInputManager?.OnTurn
                .Subscribe(OnTurn)
                .AddTo(this);
        }

        private void OnTurn(Vector2 inputValue)
        {
            if (_xrOrigin == null || _xrOrigin.Camera == null) return;

            // determine turning left or right
            var rotationDirection = inputValue.x > 0 ? 1f : -1f;
            float angle = rotationDirection * _turnAngle;

            var cameraPosition = _xrOrigin.Camera.transform.position;
            var originUp = _xrOrigin.transform.up;
            var cameraRotation = Quaternion.AngleAxis(angle, originUp);
            
            var targetPosition = cameraPosition + cameraRotation * (_xrOrigin.transform.position - cameraPosition);
            var targetRotation = cameraRotation * _xrOrigin.transform.rotation;

            // XROriginまたはその親からRigidbodyを検索
            var rb = _xrOrigin.GetComponent<Rigidbody>();
            if (rb == null) rb = _xrOrigin.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                // Rigidbodyを通じて位置と回転を適用し、物理エンジンへテレポートと衝突判定の再計算を正しく通知する
                rb.position = targetPosition;
                rb.rotation = targetRotation;

                // 物理的な速度ベクトル（慣性）も回転させ、回転後の進行方向と同期させる
                rb.linearVelocity = cameraRotation * rb.linearVelocity;
            }
            else
            {
                // Rigidbodyが取得できない場合の安全なフォールバック
                _xrOrigin.RotateAroundCameraUsingOriginUp(angle);
            }
        }
    }
}