using NaughtyAttributes;
using SchoolFestival_Voxel.Scripts.Player;
using SchoolFestival_Voxel.Scripts.Player.Interfaces;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SchoolFestival_Voxel.LifeTimeScope
{
    public class XRPlayerLifetimeScope : LifetimeScope
    {
        [SerializeField, Required] private PlayerInputManager _playerInputManager;
        [SerializeField, Required] private PlayerMovement _playerMovement;
        
            
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
                
            // Register Component
            builder.RegisterComponent(_playerInputManager).As<IPlayerInputManager>();
            builder.RegisterComponent(_playerMovement).As<PlayerMovement>();
        }
    }
}

