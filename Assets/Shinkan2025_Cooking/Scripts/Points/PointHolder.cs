using System.Collections.Generic;
using Ranking.Scripts;
using Ranking.Scripts.Interface;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Shinkan2025_Cooking.Scripts.Points
{
    /// <summary>
    /// Pointを保持するクラス
    /// 特定のSceneに一つだけ存在する
    /// </summary>
    public class PointHolder : MonoBehaviour,IRankingDataHolder<Point>
    {
        /// <summary>
        /// このHolderを使用するScene
        /// </summary>
        private RankingStorage _storage;
        private ReactiveProperty<Point> _point = new ReactiveProperty<Point>();
        public IReadOnlyReactiveProperty<Point> Point => _point;
        public IReadOnlyReactiveProperty<int> UPPoint => _upPoint;
        private ReactiveProperty<int> _upPoint = new();
        private static PointHolder _instance;
        [Inject] private IGameProgressIndicatable _gameProgress;
        public static PointHolder Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject singleton = new GameObject();
                    _instance = singleton.AddComponent<PointHolder>();
                    DontDestroyOnLoad(singleton);
                }
                return _instance;
            }
        }
        void Start()
        {
            SetInstance();
            SetStorage();
            _point.Value = new Point(0);

            _point
                .Subscribe(value => SendData(value))
                .AddTo(this);
            
            _gameProgress.OnEndGame.Subscribe(_ => _storage.Register()).AddTo(this);

            /*
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);

            SceneManager.sceneUnloaded += DestroyInstance;
        */
            
            //シングルトンパターン
            //現在のsceneがinitialSceneに設定されていなかったらスルー
            
        }

        public void SetInstance()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        public void SetStorage()
        {
            _storage = RankingStorage.instance;
            Debug.Log(_storage);
            if(!_storage)
                Debug.LogError("Storageが取得できません");
        }
        public void SendData(Point point)
        {
            Debug.Log("Ranking Updating...");
            _storage?.UpdateData(point);
            Debug.Log("Ranking Updated");
        }

        /// <summary>
        /// Pointを加算する関数
        /// </summary>
        /// <param name="val"></param>
        public void UpPoint(int val)
        {
            if (_point is null) return;
            _point.Value = _point.Value.Add(new Point(val));
            _upPoint.Value = val;
        }

        private void DestroyInstance(Scene _)
        {
            Destroy(Instance);
        }
    }
}