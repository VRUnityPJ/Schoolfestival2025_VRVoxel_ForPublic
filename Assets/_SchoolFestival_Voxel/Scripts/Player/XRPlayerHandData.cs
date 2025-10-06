using NaughtyAttributes;
using SchoolFestival_Voxel.Scripts.Player.Interfaces;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SchoolFestival_Voxel.Scripts.Player
{
    public class XRPlayerHandData : MonoBehaviour, IPlayerHandData
    {
        [SerializeField, Required] private ActionBasedController _leftHandController;
        [SerializeField, Required] private ActionBasedController _rightHandController;
        private HandType _dominantHand = HandType.Right;
        
        public void ChangeDominantHand(HandType hand)
        {
            _dominantHand = hand;
        }

        public HandType GetDominantHand()
        {
            return _dominantHand;
        }

        public HandType GetNonDominantHand()
        {
            return _dominantHand == HandType.Left ? HandType.Right : HandType.Left;
        }

        public Transform GetHandTransform(HandType hand)
        {
            return _dominantHand == HandType.Left ? _leftHandController.transform : _rightHandController.transform;
        }
    }
}