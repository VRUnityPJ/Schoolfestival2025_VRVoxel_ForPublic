using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Player
{
    [AddComponentMenu("Shinkan2024/Player/PlayerGrappleGunHandler")]
    public class PlayerGrappleGunHandler : MonoBehaviour//, IPlayerGrappleGunHandler
    {
        /*// GrappleGunがPlayerGrappleGunHandlerを参照して自分自身を登録するという方法もあるがとりあえずこれで様子見
        [SerializeField] private bool _isEnableGrapple = true;
        [SerializeField, Required] private Rigidbody _rigidbody;
        [SerializeField, Required] private GrappleGun _leftGrappleGun;
        [SerializeField, Required] private GrappleGun _rightGrappleGun;
        private readonly Dictionary<HandType, IGrappleGun> _grappleGuns = new();

        private void Start()
        {
            // Dictionaryを構築する
            _grappleGuns.TryAdd(HandType.Left, _leftGrappleGun);
            _grappleGuns.TryAdd(HandType.Right, _rightGrappleGun);

            // Setup grapple gun
            _leftGrappleGun.SetUp(_rigidbody);
            _rightGrappleGun.SetUp(_rigidbody);
        }

        public void ShootGrappleGun(HandType handType)
        {
            if (!_isEnableGrapple) return; 
            _grappleGuns.GetValueOrDefault(handType)?.Shoot();
        }

        public void ReleaseGrappleGun(HandType handType)
        {
            _grappleGuns.GetValueOrDefault(handType)?.Release();
        }

        public void GrappleBoost(HandType handType)
        {
            _grappleGuns.GetValueOrDefault(handType)?.Boost();
        }

        public void SetEnableGrapple(bool isEnable)
        {
            _isEnableGrapple = isEnable;
        }
        */
    }
}