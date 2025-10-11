using NaughtyAttributes;
using Shinkan2024.Scripts.Game.Utility;
using UnityEngine;
using UnityEngine.VFX;

public class TestVFX : MonoBehaviour
{
    [SerializeField] private VisualEffect[] effects;

    private int i = 0;
    [Button]
    private void Go()
    {
        effects[i].Play();
        i++;
        if (i >= effects.Length)
            i = 0;
    }

    private void Update()
    {
        EffectPlayer.PlayOneShot(effects[0],transform.position);
    }
}
