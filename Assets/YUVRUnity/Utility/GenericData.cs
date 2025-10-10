using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace YUVRUnity.Utility
{
    [Serializable]
    public class GenericData<TKey, TType>
        where TKey : Enum
    {
        /// <summary>
        /// インスペクターで指定されたパラメータ
        /// </summary>
        [SerializeField] private List<DataParameter<TKey, TType>> _parameters;

        /// <summary>
        /// _parametersを元に構築されるDictionary
        /// </summary>
        private Dictionary<TKey, TType> _dictionary = new();

        /// <summary>
        /// 初期化が終わってDictionaryが構築されているか
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// 重複したキーでDictionaryに登録しようとしたときのエラーメッセージ
        /// </summary>
        private const string ERROR_MESSAGE_FOR_UNIQUE_KEY = "キーは各Parametersの中で一意でなければなりません。重複したキーを指定していないか確認してください。";

        /// <summary>
        /// 登録されていないキーを使ってデータにアクセスしようとしたときのエラーメッセージ
        /// </summary>
        private const string ERROR_MESSAGE_FOR_CANT_ACCESS_DATA = "キーを含む値が登録されていません。そのキーがデータに存在しているか確認してください。";

        /// <summary>
        /// _parametersに登録されている値を取得する
        /// </summary>
        /// <param name="key">値に紐づけられているキー</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public TType GetValue(TKey key)
        {
            // 初期化が終わってなければ初期化を実行する
            if (!_isInitialized)
                Initialize();

            // Dictionaryにキーが登録されているかどうかを確認する
            if (!_dictionary.ContainsKey(key))
            {
                throw new NullReferenceException(ERROR_MESSAGE_FOR_CANT_ACCESS_DATA + $" key : {key}");
            }

            var value = _dictionary.GetValueOrDefault(key);
            return value;
        }

        /// <summary>
        /// データを取り出せるようにする準備を行う
        /// </summary>
        private void Initialize()
        {
            // データは取り出せないのでFalse
            _isInitialized = false;

            // Dictionaryを構築
            CreateDictionary();

            // データを取り出す準備ができたのでTrue
            _isInitialized = true;
        }

        /// <summary>
        /// _parametersのデータを元にDictionaryを構築する。
        /// </summary>
        /// <exception cref="InvalidEnumArgumentException">重複したキーを登録しようとしたときにだすエラー</exception>
        private void CreateDictionary()
        {
            foreach (var parameter in _parameters)
            {
                // キーが重複していたら例外をスローする
                if (_dictionary.ContainsKey(parameter.Key))
                    throw new InvalidEnumArgumentException(ERROR_MESSAGE_FOR_UNIQUE_KEY + $" key : {parameter.Key}");

                // パラメータをDictionaryに登録する
                _dictionary.Add(parameter.Key, parameter.Value);
            }
        }
    }
}