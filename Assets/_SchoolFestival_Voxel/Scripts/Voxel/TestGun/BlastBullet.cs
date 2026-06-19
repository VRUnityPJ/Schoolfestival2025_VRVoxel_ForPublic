
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// 爆破バレット：着弾点で球体状にボクセルを破壊します。
    /// </summary>
    public class BlastBullet : VoxelBullet
    {
        [Header("Blast Settings")]
        [SerializeField] private float _radius = 2.0f;

        protected override void OnHit(RaycastHit hit)
        {
            if (_voxelWorld != null && _voxelDestoyer != null)
            {
                // 衝突した面の少し内側（弾の進行方向）を破壊の中心とする
                Vector3 destroyCenter = hit.point + _direction * 0.1f;
                _voxelDestoyer.DestroySphere(destroyCenter, _radius);
            }
        }
    }
}
