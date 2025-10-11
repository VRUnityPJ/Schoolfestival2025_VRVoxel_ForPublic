using System;
using UnityEngine;

namespace Shinkan2024.Scripts.Game.Utility
{
    public class AudioPlayer : MonoBehaviour
    {
        public static AudioSource PlayOneShotAudioAtPoint(
            AudioClip clip, Vector3 position, float volume = 1f ,float spatialBlend = 0.5f)
        {
            var audioObj = new GameObject($"[OneShotAudio] {clip.name}");
            audioObj.transform.position = position;
            var audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
            audioSource.Play();
            Destroy(audioObj, clip.length);
            return audioSource;
        }
    }
}
