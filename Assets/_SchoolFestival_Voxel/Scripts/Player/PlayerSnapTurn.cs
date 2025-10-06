using NaughtyAttributes;
using SchoolFestival_Voxel.Scripts.Player.Interfaces;
using Unity.XR.CoreUtils;
using UnityEngine;
using VContainer;
using R3;
using System;

namespace SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerSnapTurn : MonoBehaviour, IPlayerSnapTurn
    {
        [SerializeField, Required] private XROrigin _xrOrigin;
        [SerializeField] private float _turnAngle = 30f;

        private IPlayerInputManager _playerInputManager;

        [Inject]
        public void Constructor(IPlayerInputManager playerInputManager)
        {
            _playerInputManager = playerInputManager;
        }

        private void Start()
        {
            // イベントのサブスクライブ
            _playerInputManager?.OnTurn
                .Subscribe(OnTurn)
                .AddTo(this);
        }

        private void OnTurn(Vector2 inputValue)
        {
            // determine turning left or right
            var rotationDirection = inputValue.x > 0 ? 1f : -1f;
            
            // Turn Origin
            _xrOrigin.RotateAroundCameraUsingOriginUp(rotationDirection * _turnAngle);
        }
    }
}