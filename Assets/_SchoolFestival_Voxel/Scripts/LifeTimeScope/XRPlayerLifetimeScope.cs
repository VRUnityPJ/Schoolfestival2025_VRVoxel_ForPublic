using _SchoolFestival_Voxel.Scripts.Player;
using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using _SchoolFestival_Voxel.Scripts.Voxel;
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
        [SerializeField] private VoxelPainter _voxelPainter;
        [SerializeField] private VoxelBuilder _voxelBuilder;
        [SerializeField] private VoxelObjectSpawner _voxelObjectSpawner;
        
            
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
            if (_voxelPainter == null)
            {
                _voxelPainter = FindObjectOfType<VoxelPainter>();
            }
            if (_voxelBuilder == null)
            {
                _voxelBuilder = FindObjectOfType<VoxelBuilder>();
            }
            if (_voxelObjectSpawner == null)
            {
                _voxelObjectSpawner = FindObjectOfType<VoxelObjectSpawner>();
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

            if (_voxelPainter != null)
            {
                builder.RegisterComponent(_voxelPainter);
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelPainter is null and could not be resolved.");
            }

            if (_voxelBuilder != null)
            {
                builder.RegisterComponent(_voxelBuilder);
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelBuilder is null and could not be resolved.");
            }

            if (_voxelObjectSpawner != null)
            {
                builder.RegisterComponent(_voxelObjectSpawner);
            }
            else
            {
                Debug.LogWarning("[XRPlayerLifetimeScope] VoxelObjectSpawner is null and could not be resolved.");
            }
        }
    }
}

