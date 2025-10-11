using System.Threading;
using Cysharp.Threading.Tasks;
using Ranking.Demo.Scripts.DemoGame;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.UI
{
    public class ScorePresenter : MonoBehaviour
    {
        [SerializeField]
        private PlayerScoreHolder _scoreHolder;

        [SerializeField]
        private ResultUIViewer _resultUIViewer; //追加

        /// <summary>
        /// ゲーム終了時にスコアをアニメーションさせて表示する
        /// </summary>
        public async UniTask ShowScoreAnimationAsync(CancellationToken token)
        {
            await _resultUIViewer.ShowResultAsync(
                _scoreHolder.GetScore().IntValue,
                token);
        }
    }
}
