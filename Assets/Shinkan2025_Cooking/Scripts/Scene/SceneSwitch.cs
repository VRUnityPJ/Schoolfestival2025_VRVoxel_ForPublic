using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinkan2025_Cooking.Scripts.Scene
{
    public class SceneSwitch : MonoBehaviour
    {
        [Scene, Required] public string _sceneName;

        public void SwitchScene()
        {
            SceneManager.LoadScene(_sceneName);
        }
    }
}
