using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// 貫通バレット：銃口から直線上にボクセルを破壊して通り道を切り開きます。
    /// チャージ時間に応じて、貫通穴の太さ（半径）が太くなります。
    /// </summary>
    public class PierceBullet : VoxelBullet
    {
        [Header("Pierce Settings")]
        [SerializeField] private float _minRadius = 0.5f;
        [SerializeField] private float _maxRadius = 2.0f;
        [SerializeField] private float _penetrationDistance = 20.0f;
        private void Awake()
        {
            _isChargeable = true;
            _hitLayers = 0; // コライダーと衝突させず、最大距離まで貫通して飛び続けるようにする
        }

        public override void Fire(Vector3 origin, Vector3 direction, float chargeTime)
        {
            _maxDistance = _penetrationDistance;
            base.Fire(origin, direction, chargeTime);
        }

        protected override void OnHit(RaycastHit hit)
        {
            // _hitLayers = 0 に設定しているため基本的には呼ばれませんが、
            // 抽象メソッド定義のためにオーバーライドが必要です。
        }

        protected override void OnMaxDistanceReached()
        {
            if (_voxelWorld != null && _voxelDestoyer != null)
            {
                float chargeRatio = Mathf.Clamp01(_chargeTime / _maxChargeTime);
                float radius = Mathf.Lerp(_minRadius, _maxRadius, chargeRatio);
                Vector3 endPoint = _startPosition + _direction * _penetrationDistance;
                _voxelDestoyer.DestroyLine(_startPosition, endPoint, radius);
            }
        }
    }
}
