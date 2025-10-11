using System;
using System.Collections;
using System.Collections.Generic;
using Shinkan2024.Scripts.Game.Utility;
using UnityEngine;

public class DemoStart : MonoBehaviour
{
    public AudioClip _startClip;
    private void Update()
    {
        if (Camera.main.transform.position.z > transform.position.z)
        {
            DemoTimer.StartMeasure();
            Debug.Log("Start");
            this.enabled = false;
            AudioPlayer.PlayOneShotAudioAtPoint(_startClip,transform.position);
            return;
        }
    }
}
