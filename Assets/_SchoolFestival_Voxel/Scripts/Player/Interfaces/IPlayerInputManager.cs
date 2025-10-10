using System;
using R3;
using UnityEngine;
namespace SchoolFestival_Voxel.Scripts.Player.Interfaces
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
        public Observable<Unit> OnFloat { get; }
        public Observable<Unit> OnFloatCanceled { get; }
        public Observable<Vector2> OnTurn { get; }
        public Observable<Vector2> OnMove { get; }
    }
}
