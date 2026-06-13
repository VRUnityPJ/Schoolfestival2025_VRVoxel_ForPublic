using R3;
using UnityEngine;

namespace Shinkan2024.Ranking.Demo.Scripts.DemoGame.UI
{
    public class PlayerNameUIPresenter : MonoBehaviour
    {
        [SerializeField] private Player.PlayerNameHolder _model;
        [SerializeField] private PlayerNameUIViewer _viewer;

        private void Start()
        {
            _model.PlayerName.Subscribe(val => 
            {
                _viewer.UpdateText(val.StringValue);
            }).AddTo(this);
        }
    }
}