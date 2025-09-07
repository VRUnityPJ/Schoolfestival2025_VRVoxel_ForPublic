using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public interface IPlayerInputController
{
    public IReadOnlyReactiveProperty<bool> CanStab { get; }
    public Vector3 Velocity { get; }
    public Vector3 AngularVelocity { get; }
}
