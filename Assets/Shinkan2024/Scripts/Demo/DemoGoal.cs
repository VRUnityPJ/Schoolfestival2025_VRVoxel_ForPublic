using System;
using Shinkan2024.Scripts.Game.Utility;
using UnityEngine;

namespace KeyBoard.Shinkan2024.Scripts.Demo
{
    public class DemoGoal: MonoBehaviour
    {
        [SerializeField] private DemoResultPanel result;
        public AudioClip clip;
        private void Update()
        {
            if (Camera.main.transform.position.z > transform.position.z)
            {
                Debug.Log("Goal");
                result.Show();
                AudioPlayer.PlayOneShotAudioAtPoint(clip,transform.position);
                this.enabled = false;
            }
        }
    }
}