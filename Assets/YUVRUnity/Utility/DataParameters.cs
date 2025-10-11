using System;
using UnityEngine;

namespace YUVRUnity.Utility
{
    
    /// <summary>
    /// GenericDataクラスで使用されるパラメータクラス
    /// </summary>
    /// <typeparam name="TEnum">キーとなるEnum型</typeparam>
    /// <typeparam name="TValueType">値の型</typeparam>
    [Serializable]
    public class DataParameter<TEnum, TValueType>
        where TEnum : Enum
    {
        /// <summary>
        /// キーとなっている変数
        /// </summary>
        public TEnum Key => _key;

        /// <summary>
        /// キーと関連付ける値
        /// </summary>
        public TValueType Value => _value;

        [SerializeField] private TEnum _key;
        [SerializeField] private TValueType _value;
    }
}