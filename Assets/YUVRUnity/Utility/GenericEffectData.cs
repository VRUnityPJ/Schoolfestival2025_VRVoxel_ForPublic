using System;
using UnityEngine;
using UnityEngine.VFX;

namespace YUVRUnity.Utility
{
    /// <summary>
    /// 任意のキーと値を登録でき、取り出せるクラス
    /// </summary>
    /// <typeparam name="TKey">キーにしたい任意のEnum型</typeparam>
    [Serializable]
    public class GenericEffectData<TKey>
        where TKey : Enum
    {
        // VFX のデータ
        [SerializeField] private GenericData<TKey, VisualEffect> _vfxData;

        // ParticleSystem のデータ
        [SerializeField] private GenericData<TKey, ParticleSystem> _particleData;

        // AudioClip のデータ
        [SerializeField] private GenericData<TKey, AudioClip> _audioData;

        /// <summary>
        /// VFXDataに登録されている値を取得する
        /// </summary>
        /// <param name="key">値に関連付けられているキー</param>
        public VisualEffect GetVisualEffect(TKey key)
        {
            return _vfxData.GetValue(key);
        }

        /// <summary>
        /// ParticleDataに登録されている値を取得する
        /// </summary>
        /// <param name="key">値に関連付けられているキー</param>
        public ParticleSystem GetParticleEffect(TKey key)
        {
            return _particleData.GetValue(key);
        }

        /// <summary>
        /// AudioDataに登録されている値を取得する
        /// </summary>
        /// <param name="key">値に関連付けられているキー</param>
        public AudioClip GetAudioClip(TKey key)
        {
            return _audioData.GetValue(key);
        }
    }
}