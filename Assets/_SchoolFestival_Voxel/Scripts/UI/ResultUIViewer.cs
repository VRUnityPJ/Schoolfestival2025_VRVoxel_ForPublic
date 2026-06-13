using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.UI
{
    public class ResultUIViewer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        
        [SerializeField] private TextMeshProUGUI _endText;

        private void Start() => Init();

        /// <summary>
        /// 初期化処理（View的役割）
        /// </summary>
        public void Init()
        {
            _finalScoreText.gameObject.SetActive(false);
            
            _endText.gameObject.SetActive(false);
        }

        /// <summary>
        /// ゲーム終了時にスコアとランクを表示（Presenter的役割）
        /// </summary>
        public async UniTask ShowResultAsync(int score, CancellationToken token)
        {
            // 「やめ」表示
            _endText.gameObject.SetActive(true);
            _endText.text = "Finish";
            _endText.rectTransform.localScale = Vector3.one * 0.5f;

            await _endText.rectTransform
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutBack)
                .ToUniTask(cancellationToken:  token);

            await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: token);

            // 「やめ」非表示
            _endText.gameObject.SetActive(false);

            await ShowScoreAsync(score, token); // View更新

            await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: token);
        }

        /// <summary>
        /// スコア表示（View的役割）
        /// </summary>
        private async UniTask ShowScoreAsync(int score,  CancellationToken token)
        {
            _finalScoreText.gameObject.SetActive(true);
            _finalScoreText.text = $"Score : {score}";
            _finalScoreText.rectTransform.localScale = Vector3.one * 0.5f;

            await _finalScoreText.rectTransform
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutBack)
                .ToUniTask(cancellationToken: token);
            
            
            await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: token);
            _finalScoreText.gameObject.SetActive(false);
        }
    }
}
