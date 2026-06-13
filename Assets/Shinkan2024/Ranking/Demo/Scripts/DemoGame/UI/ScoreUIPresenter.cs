using R3;
using Shinkan2024.Ranking.Demo.Scripts.DemoGame.Player;
using UnityEngine;

namespace Shinkan2024.Ranking.Demo.Scripts.DemoGame.UI
{
    public class ScoreUIPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerScoreHolder _model;
        [SerializeField] private ScoreUIViewer _viewer;
        private void Start()
        {
            _model.Score.Subscribe(val =>
            {
                _viewer.UpdateText(val.IntValue);
            }).AddTo(this);
        }
    }
}