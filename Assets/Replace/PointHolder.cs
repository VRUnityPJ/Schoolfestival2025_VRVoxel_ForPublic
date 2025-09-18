using Ranking.Scripts;
using Ranking.Scripts.Interface;
//using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinkan2024.Scripts.Game.Point
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
        //private ReactiveProperty<Point> _point = new ReactiveProperty<Point>();
        //public IReadOnlyReactiveProperty<Point> Point => _point;
        public static PointHolder Instance;

        void Start()
        {
            //SetStorage().
            //_point.Value = new Point(0);

            //_point
               // .Subscribe(value => SendData(value))
                //.AddTo(this);

            if (Instance == null)
                Instance = this;
            else
                Destroy(this);

            SceneManager.sceneUnloaded += DestroyInstance;
        }
        public void SetStorage()
        {
            _storage = RankingStorage.instance;
            if(!_storage)
                Debug.LogError("Storageが取得できません");
        }
        public void SendData(Point point)
        {
            _storage?.UpdateData(point);
        }

        /// <summary>
        /// Pointを加算する関数
        /// </summary>
        /// <param name="val"></param>
        public void UpPoint(int val)
        {
            //if (_point is null) return;
            //_point.Value = _point.Value.Add(new Point(val));
        }

        private void DestroyInstance(Scene _)
        {
            Destroy(Instance);
        }
    }
}
