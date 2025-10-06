using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Player.Interfaces
{
    public interface IPlayerHandData
    {
        /// <summary>
        /// 利き手を変更する
        /// </summary>
        /// <param name="hand"></param>
        public void ChangeDominantHand(HandType hand);
        
        /// <summary>
        /// 利き手の手のタイプを取得する
        /// </summary>
        /// <returns></returns>
        public HandType GetDominantHand();
        
        /// <summary>
        /// 利き手とは逆の手のタイプを取得する
        /// </summary>
        /// <returns></returns>
        public HandType GetNonDominantHand();
        
        /// <summary>
        /// 指定した手のTransformを取得する
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public Transform GetHandTransform(HandType hand);
    }
}