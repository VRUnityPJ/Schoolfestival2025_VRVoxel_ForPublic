using UnityEngine;
using UnityEngine.Events;

namespace Shinkan2024.Scripts.Game.Utility
{
    public class PlayerHitEvent : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onHitPlayer = new();
        [SerializeField] private bool _isOneShotEvent = false;

        private void OnTriggerEnter(Collider other)
        {
            _onHitPlayer?.Invoke();
            if (_isOneShotEvent) gameObject.SetActive(false);
        }
    }
}