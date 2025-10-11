using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
// using Shinkan2024.Scripts.Game.Stage;
using NaughtyAttributes;
using Ranking.Scripts;
using Ranking.Scripts.Interface;
// using UniRx;
using TMPro;
namespace Shinkan2024.Scripts.Game.UI
{
        public class GameOverScreen : MonoBehaviour,IRankingViewer
    {
        // [SerializeField, Required] private GameJudge gameJudge;
        [SerializeField, Required] private TextMeshProUGUI _scoreText;
        [SerializeField, Required] private TextMeshProUGUI _nameText;
        private AudioSource _audioSource;
        private AudioClip _openAudio;
        //TODO ここScene属性使ってScene名を取得したほうがいいと思います
        [SerializeField, Scene] private int _sceneBuildIndex;
        private float _BeforeDisplayMilliSeconds = 3000;
        private float _displayingMilliSeconds = 8000;
        
        //Ranking関連のコンポーネント
        private RankingStorage _storage;
        
        
        // private void Awake()
        // {
        //     gameJudge.OnGameOver
        //         .Subscribe(_ => DisplayScreen())
        //         .AddTo(this);
        //     Debug.Log("GameOverScreenを_gameOverSubjectにサブスクライブした");
        // }

        // Start is called before the first frame update
        void Start()
        {
            gameObject.SetActive(false);
            
            //ランキングストレージの取得
            SetStorage();
        }

        private async UniTask DisplayScreen()
        {
            var point = _storage.GetData<Point.Point>();
            var name = _storage.GetData<PlayerName>();
            
            _scoreText.text = "SCORE : " + point.IntValue.ToString();
            _nameText.text = "NAME : " + name.StringValue;
            
            //debugModeのときはランキング登録しない
            if(!DebugMode.IsDebugModeOneTime)
                Register();
            
            await UniTask.Delay(TimeSpan.FromMilliseconds(_BeforeDisplayMilliSeconds));
            if(_openAudio)
                _audioSource.PlayOneShot(_openAudio);
            gameObject.SetActive(true);
            await UniTask.Delay(TimeSpan.FromMilliseconds(_displayingMilliSeconds));
            await LoadTitleScene(_sceneBuildIndex);
        }
        
        //後でintにする
        private async UniTask LoadTitleScene(int buildIndex)
        {
            await SceneManager.LoadSceneAsync(buildIndex);
        }

        public void SetStorage()
        {
            _storage = RankingStorage.instance;
            if(!_storage)
                Debug.LogError("rankingストレージが取得できていません");
        }

        private void Register()
        {
            _storage.Register();
        }
    }

}
