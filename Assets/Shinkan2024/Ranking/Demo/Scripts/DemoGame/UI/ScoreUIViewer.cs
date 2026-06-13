using TMPro;
using UnityEngine;

namespace Shinkan2024.Ranking.Demo.Scripts.DemoGame.UI
{
    public class ScoreUIViewer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textGUI;
        public void UpdateText(int num)
        {
            _textGUI.text = $"Score:{num}";
        }
    }
}