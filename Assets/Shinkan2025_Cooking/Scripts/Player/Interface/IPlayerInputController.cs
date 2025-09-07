using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;

public interface IPlayerInputController
{
    public ReadOnlyReactiveProperty<bool> CanStab { get; }
    public Vector3 Velocity { get; }
    public Vector3 AngularVelocity { get; }
}
