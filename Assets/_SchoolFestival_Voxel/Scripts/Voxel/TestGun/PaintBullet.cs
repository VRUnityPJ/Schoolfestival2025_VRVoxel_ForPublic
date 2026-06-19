using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// ペイントバレット：着弾点で球体状にボクセルの色（マテリアルID）を変更します。
    /// </summary>
    public class PaintBullet : VoxelBullet
    {
        [Header("Paint Settings")]
        [SerializeField] private float _radius = 2.0f;
        [SerializeField] private int _materialID = 1;

        protected override void OnHit(RaycastHit hit)
        {
            if (_voxelWorld != null && _voxelPainter != null)
            {
                // ペイントの中心を着弾点の少し内側（弾の進行方向）にする
                Vector3 paintCenter = hit.point + _direction * 0.1f;
                _voxelPainter.PaintSphere(paintCenter, _radius, _materialID);
            }
        }
    }
}
