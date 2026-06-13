using UnityEngine;

namespace Shinkan2024.Ranking.Demo.Scripts.DemoGame.Player
{
    /// <summary>
    /// ランキングのDemoシーンのPlayer(球体)を管理するクラス
    /// </summary>
    public class Player : MonoBehaviour,IPlayer
    {
        private PlayerScoreHolder _holder;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _getPointClip;

        private void Start()
        {
            if(!TryGetComponent<PlayerScoreHolder>(out _holder))
                Debug.LogError("ScoreHolderが取得できません");
        }
        
        
        public void AddScore(int num)
        {
            _holder.AddScore(num);
            _audioSource.PlayOneShot(_getPointClip);
        }
    }
}