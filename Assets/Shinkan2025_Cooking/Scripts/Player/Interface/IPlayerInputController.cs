using R3;
using UnityEngine;

namespace Shinkan2025_Cooking.Scripts.Player.Interface
{
    public interface IPlayerInputController
    {
        public ReadOnlyReactiveProperty<bool> CanStab { get; }
        public Vector3 Velocity { get; }
        public Vector3 AngularVelocity { get; }
    }
}
