using Ranking.Scripts.Interface;

namespace Shinkan2024.Scripts.Game.Point
{
    public class Point : IRankingDataElement<Point>
    {
        private int _intValue;
        public int IntValue => _intValue;

        /// <summary>
        /// デフォルトコンストラクタ
        /// ランキングデータの生成に必要
        /// </summary>
        public Point()
        {
            _intValue = 0;
        }
        public Point(int initialINTValue)
        {
            _intValue = initialINTValue;
        }
        /// <summary>
        /// 渡されたPointを加算して返す
        /// </summary>
        public Point Add(Point addPoint)
        {
            var newValue = _intValue + addPoint.IntValue;
            return new Point(newValue);
        }
        /// <summary>
        /// 渡されたPointを減算して返す
        /// </summary>
        public Point Delete(Point delPoint)
        {
            var newValue = _intValue - delPoint.IntValue;
            return new Point(newValue);
        }
    }
}