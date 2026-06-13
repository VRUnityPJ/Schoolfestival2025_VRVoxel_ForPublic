using DG.Tweening;
using NaughtyAttributes;
using Replace;
using Shinkan2025_Cooking.Ranking.Scripts;
using Shinkan2025_Cooking.Ranking.Scripts.@interface;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Shinkan2024.Scripts.Game.UI
{
    public class FinishUI : MonoBehaviour,IRankingViewer
    {
        [SerializeField] private Image _outlineImage;
        [SerializeField] private TextMeshProUGUI _text;

        public UnityEvent onCompleteUIAnimation;
        
        private RankingStorage _storage;
        
        private Sequence finishSequence;

        [Button]
        public void Show()
        {
            Initialize();
            gameObject.SetActive(true);
            finishSequence.Restart();
        }
        
        public void SetStorage()
        {
            _storage = RankingStorage.instance;
        }
        
        private void SetSequence()
        {
            finishSequence = DOTween.Sequence();

            finishSequence
                // .Append(_text.DOFontSize(110, 2).SetEase(Ease.InQuad))
                // .Append(_text.DOFontSize(100, 0.2f).SetEase(Ease.OutQuad))
                .AppendInterval(1.5f)
                .Append(_text.DOFade(0, 0.5f))
                .AppendCallback(ChangeText)
                .Append(_text.DOFade(1, 0.5f))
                .Append(_outlineImage.DOFillAmount(0, 8).SetEase(Ease.Linear))
                .OnComplete(() => onCompleteUIAnimation?.Invoke())
                .Pause()
                .SetAutoKill(false)
                .SetLink(gameObject);;
        }

        private void ChangeText()
        {
            var point = _storage.GetData<Point>();
            int value = point.IntValue;
            _text.text = $"Result\n<color=\"red\"> {value.ToString()} </color>pt";
            // _text.text = $"Result\n<color=\"red\"> 10000 </color>pt";
        }

        private void Initialize()
        {
            SetStorage();
            SetSequence();
            
            _text.text = "Finish !";
            _text.fontSize = 0;
            _outlineImage.fillAmount = 1;
        }
    }
}