using KeyBoard;
using UnityEngine;

namespace Shinkan2024.Scripts.Game.Debug
{
    public class DebugModeController : MonoBehaviour
    {
        /// <summary>
        /// デバッグモードに移行するテキスト
        /// </summary>
        private IKeyBoardEventTrigger _keyBoardEventTrigger;
        void Start()
        {
            //KeyBoardSettingを取得
            if(!TryGetComponent(out _keyBoardEventTrigger))
                UnityEngine.Debug.LogError("KeyBoardEventTriggerが取得できません");
        
            //イベントが存在するときAdd
            _keyBoardEventTrigger.onTypedDebugText?.AddListener(OnEnterDebugMode);
        }
        private void OnEnterDebugMode()
        {
            UnityEngine.Debug.Log("DEBUGイベント発火");
            //DebugMode有効化
            DebugMode.IsDebugModeOneTime = true;
        }
    }
}
