using R3;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Player.Interfaces
{
    public interface IPlayerInputManager
    {
        public Observable<Unit> OnShootLeftWire { get; }
        public Observable<Unit> OnShootRightWire { get; }
        public Observable<Unit> OnReleaseLeftWire { get; }
        public Observable<Unit> OnReleaseRightWire { get; }
        public Observable<Unit> OnBoostLeft { get; }
        public Observable<Unit> OnBoostRight { get; }
        public Observable<Unit> OnInputLeftTrigger { get; }
        public Observable<Unit> OnInputRightTrigger { get; }
        public Observable<Vector2> OnTurn { get; }
        public Observable<Vector2> OnMove { get; }
        public Observable<Unit> OnFloatLeft { get; }
        public Observable<Unit> OnFloatCanceledLeft { get; }
        public Observable<Unit> OnFloatRight { get; }
        public Observable<Unit> OnFloatCanceledRight { get; }
    }
}
