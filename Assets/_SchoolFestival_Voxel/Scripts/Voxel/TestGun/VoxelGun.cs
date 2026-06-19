using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// 各種ボクセル弾丸を打ち分ける銃（Gun）コンポーネント。
    /// </summary>
    public class VoxelGun : MonoBehaviour
    {
        [Inject] private VoxelObjectSpawner _voxelObjectSpawner;

        [Header("Gun Settings")]
        [SerializeField] private Transform _muzzle;

        [Header("Input Bindings (Unity Input System)")]
        [SerializeField] private InputActionProperty _fireAction;
        [SerializeField] private InputActionProperty _switchAction;

        [Header("Bullet List")]
        [SerializeField] private List<VoxelBullet> _bulletPrefabs = new List<VoxelBullet>();

        private int _currentBulletIndex = 0;
        private float _chargeTimer = 0f;
        private bool _isCharging = false;

        private void Start()
        {
            // DIが解決されなかった場合のフォールバック
            if (_voxelObjectSpawner == null)
            {
                _voxelObjectSpawner = FindObjectOfType<VoxelObjectSpawner>();
            }
            if (_muzzle == null)
            {
                _muzzle = transform;
            }
        }

        private void OnEnable()
        {
            if (_fireAction.action != null)
            {
                _fireAction.action.Enable();
                _fireAction.action.started += OnFireStarted;
                _fireAction.action.canceled += OnFireCanceled;
            }

            if (_switchAction.action != null)
            {
                _switchAction.action.Enable();
                _switchAction.action.started += OnSwitchPressed;
            }
        }

        private void OnDisable()
        {
            if (_fireAction.action != null)
            {
                _fireAction.action.started -= OnFireStarted;
                _fireAction.action.canceled -= OnFireCanceled;
            }

            if (_switchAction.action != null)
            {
                _switchAction.action.started -= OnSwitchPressed;
            }

            _isCharging = false;
            _chargeTimer = 0f;
        }

        private void Update()
        {
            if (_isCharging && _bulletPrefabs.Count > 0)
            {
                var currentBullet = _bulletPrefabs[_currentBulletIndex];
                if (currentBullet != null && currentBullet.IsChargeable)
                {
                    _chargeTimer += Time.deltaTime;
                    // 最大チャージ時間でクランプ
                    _chargeTimer = Mathf.Min(_chargeTimer, currentBullet.MaxChargeTime);
                }
            }
        }

        private void OnFireStarted(InputAction.CallbackContext context)
        {
            if (_bulletPrefabs.Count == 0) return;
            var currentBullet = _bulletPrefabs[_currentBulletIndex];
            if (currentBullet == null) return;

            if (currentBullet.IsChargeable)
            {
                _isCharging = true;
                _chargeTimer = 0f;
                Debug.Log($"[VoxelGun] Charging started for: {currentBullet.name}");
            }
            else
            {
                // 即時発射
                Shoot(0f);
            }
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            if (_isCharging)
            {
                Shoot(_chargeTimer);
                _isCharging = false;
                _chargeTimer = 0f;
            }
        }

        private void OnSwitchPressed(InputAction.CallbackContext context)
        {
            if (_bulletPrefabs.Count <= 1) return;

            _currentBulletIndex = (_currentBulletIndex + 1) % _bulletPrefabs.Count;
            var nextBullet = _bulletPrefabs[_currentBulletIndex];
            Debug.Log($"[VoxelGun] Bullet switched to slot {_currentBulletIndex}: {(nextBullet != null ? nextBullet.name : "None")}");
        }

        private void Shoot(float chargeTime)
        {
            if (_bulletPrefabs.Count == 0) return;
            var bulletPrefab = _bulletPrefabs[_currentBulletIndex];
            if (bulletPrefab == null) return;

            if (_voxelObjectSpawner != null)
            {
                // VoxelObjectSpawner.Spawn を用いて弾丸オブジェクトを生成（依存注入が自動で実行されます）
                GameObject bulletObj = _voxelObjectSpawner.Spawn(bulletPrefab.gameObject, _muzzle.position, _muzzle.rotation);
                if (bulletObj != null && bulletObj.TryGetComponent<VoxelBullet>(out var bulletInstance))
                {
                    Debug.Log($"[VoxelGun] Shot fired: {bulletPrefab.name} (Charge: {chargeTime:F2}s)");
                    bulletInstance.Fire(_muzzle.position, _muzzle.forward, chargeTime);
                }
            }
            else
            {
                // フォールバック（通常生成）
                GameObject bulletObj = Instantiate(bulletPrefab.gameObject, _muzzle.position, _muzzle.rotation);
                if (bulletObj != null && bulletObj.TryGetComponent<VoxelBullet>(out var bulletInstance))
                {
                    Debug.LogWarning("[VoxelGun] VoxelObjectSpawner is null. Falling back to normal Instantiate (DI will not be applied).");
                    bulletInstance.Fire(_muzzle.position, _muzzle.forward, chargeTime);
                }
            }
        }
    }
}
