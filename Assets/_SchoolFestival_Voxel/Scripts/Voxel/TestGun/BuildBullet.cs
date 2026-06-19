using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// ビルドバレット：着弾点の表面（法線方向へオフセットした位置）に球体状にボクセルを配置します。
    /// </summary>
    public class BuildBullet : VoxelBullet
    {
        [Header("Build Settings")]
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private int _materialID = 2;

        protected override void OnHit(RaycastHit hit)
        {
            if (_voxelWorld != null && _voxelBuilder != null)
            {
                // 積み上がるように法線方向に球の中心をオフセット
                Vector3 buildCenter = hit.point + hit.normal * (_radius * 0.5f);
                _voxelBuilder.BuildSphere(buildCenter, _radius, _materialID);
            }
        }
    }
}
