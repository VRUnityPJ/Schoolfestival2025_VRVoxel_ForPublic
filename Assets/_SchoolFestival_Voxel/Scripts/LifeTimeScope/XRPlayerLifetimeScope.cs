using _SchoolFestival_Voxel.Scripts.Player;
using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528;
using NaughtyAttributes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _SchoolFestival_Voxel.Scripts.LifeTimeScope
{
    public class XRPlayerLifetimeScope : LifetimeScope
    {
        [SerializeField, Required] private PlayerInputManager _playerInputManager;
        [SerializeField, Required] private PlayerMovement _playerMovement;
        [SerializeField, Required] private VoxelWorld _voxelWorld;
        [SerializeField] private VoxelMaterialDatabase _materialDatabase;
        [SerializeField] private VoxelWorldRenderer _voxelWorldRenderer; 
        [SerializeField] private VoxelDestoyer _voxelDestoyer;
        
            
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
                
            // インスペクター未設定時のフォールバック処理
            if (_materialDatabase == null)
            {
                var dbs = Resources.FindObjectsOfTypeAll<VoxelMaterialDatabase>();
                if (dbs != null && dbs.Length > 0) _materialDatabase = dbs[0];
            }
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }
            if (_voxelWorldRenderer == null)
            {
                _voxelWorldRenderer = FindObjectOfType<VoxelWorldRenderer>();
            }
            if (_voxelDestoyer == null)
            {
                _voxelDestoyer = FindObjectOfType<VoxelDestoyer>();
            }

            // Register Component
            builder.RegisterComponent(_playerInputManager).As<IPlayerInputManager>();
            builder.RegisterComponent(_playerMovement).As<PlayerMovement>();
            
            if (_materialDatabase != null)
            {
                builder.RegisterInstance(_materialDatabase);
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelMaterialDatabase is null and could not be resolved.");
            }

            if (_voxelWorld != null)
            {
                builder.RegisterInstance(_voxelWorld).As<VoxelWorld>();
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelWorld is null and could not be resolved.");
            }

            if (_voxelWorldRenderer != null)
            {
                builder.RegisterInstance(_voxelWorldRenderer).As<VoxelWorldRenderer>();
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelWorldRenderer is null and could not be resolved.");
            }

            if (_voxelDestoyer != null)
            {
                builder.RegisterComponent(_voxelDestoyer);
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelDestoyer is null and could not be resolved.");
            }
        }
    }
}

