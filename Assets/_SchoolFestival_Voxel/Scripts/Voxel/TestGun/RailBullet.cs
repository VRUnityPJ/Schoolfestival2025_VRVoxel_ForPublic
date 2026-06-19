using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// レールバレット：銃口から着弾点に向けて線状にボクセルを配置します。
    /// チャージ時間に応じて、生成されるレールの太さ（半径）が太くなります。
    /// </summary>
    public class RailBullet : VoxelBullet
    {
        [Header("Rail Settings")]
        [SerializeField] private float _minRadius = 0.2f;
        [SerializeField] private float _maxRadius = 1.5f;
        [SerializeField] private int _materialID = 3;

        private void Awake()
        {
            // インスペクター設定に関わらずチャージ対応にする
            _isChargeable = true;
        }

        protected override void OnHit(RaycastHit hit)
        {
            BuildRail(hit.point);
        }

        protected override void OnMaxDistanceReached()
        {
            Vector3 endPoint = _startPosition + _direction * _maxDistance;
            BuildRail(endPoint);
        }

        private void BuildRail(Vector3 endPoint)
        {
            Debug.Log($"[RailBullet] BuildRail called. Start: {_startPosition}, End: {endPoint}, Distance: {Vector3.Distance(_startPosition, endPoint):F2}m, ChargeTime: {_chargeTime:F2}s");
            if (_voxelWorld != null && _voxelBuilder != null)
            {
                float chargeRatio = Mathf.Clamp01(_chargeTime / _maxChargeTime);
                float radius = Mathf.Lerp(_minRadius, _maxRadius, chargeRatio);
                _voxelBuilder.BuildLine(_startPosition, endPoint, radius, _materialID);
            }
        }
    }
}
