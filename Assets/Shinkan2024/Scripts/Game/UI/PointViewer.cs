using TMPro;
using UnityEngine;

namespace Shinkan2024.Scripts.Game.UI
{
    public class PointViewer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointTextMesh;

        public void UpdatePointText(int pointValue)
        {
            _pointTextMesh.text = pointValue.ToString();
        }
    }
}