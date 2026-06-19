using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.TestGun
{
    /// <summary>
    /// ビルドバレットEx：着弾点に指定された MagicaVoxel モデル (.vox) を読み込んで配置します。
    /// </summary>
    public class BuildBulletEx : VoxelBullet
    {
        [Header("BuildEx Settings")]
        [SerializeField] private string _modelName = "TestColorCube128.vox";

        protected override void OnHit(RaycastHit hit)
        {
            if (_voxelWorld != null && _voxelBuilder != null)
            {
                _voxelBuilder.BuildModel(hit.point, hit.normal, _modelName);
            }
        }
    }
}
