using TMPro;
using UnityEngine;

namespace Shinkan2024.Ranking.Demo.Scripts.DemoGame.UI
{
    public class PlayerNameUIViewer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textGUI;
        public void UpdateText(string name)
        {
            textGUI.text = name;
        }
    }
}