using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace Shinkan2024.Scripts.Game.Utility
{
    public class EffectPlayer
    {
        public static void PlayOneShot(VisualEffect effect,Vector3 position,Quaternion rotation = new())
        {
            GameObject obj = new("Effect");
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.Play();
        }
    }
}