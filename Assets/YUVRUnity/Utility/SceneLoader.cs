using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YUVRUnity.Utility
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Scene] private string _sceneName;
        
        public void LoadScene()
        {
            SceneManager.LoadScene(_sceneName);
        }
    }
}