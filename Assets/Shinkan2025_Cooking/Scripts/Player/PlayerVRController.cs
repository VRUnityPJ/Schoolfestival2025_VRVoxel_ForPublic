using System.Collections.Generic;
using R3;
using R3.Triggers;
using Shinkan2025_Cooking.Scripts.Player.Interface;
using UnityEngine;
using UnityEngine.XR;

namespace Shinkan2025_Cooking.Scripts.Player
{
    public class PlayerVRController : MonoBehaviour, IPlayerInputController
    {
        [SerializeField]
        private XRNode _xrNode;
        private readonly ReactiveProperty<bool> _canStab;
        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        public ReadOnlyReactiveProperty<bool> CanStab => _canStab;
        public Vector3 Velocity => _velocity;
        public Vector3 AngularVelocity => _angularVelocity;

        public void Start()
        {
            SubscribeXRVelocitys();
        }

        private void SubscribeXRVelocitys()
        {
            List<XRNodeState> states = new();
            InputTracking.GetNodeStates(states);

            this.FixedUpdateAsObservable()
                .Subscribe(x =>
                {
                
                    foreach(XRNodeState xrNode in states)
                    {
                        if (xrNode.nodeType != _xrNode) return;
                 
                        xrNode.TryGetVelocity(out _velocity);
                        xrNode.TryGetAngularVelocity(out _angularVelocity);

                        break;
                    }

                }).AddTo(this);
        }
    }
}
