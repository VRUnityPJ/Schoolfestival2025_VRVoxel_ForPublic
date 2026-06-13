using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shinkan2024.Scripts.Game.Utility
{
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(string t, LogType logType = LogType.Log)
        {
            switch (logType)
            {
                case LogType.Log:
                    UnityEngine.Debug.Log(t);       
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(t);
                    break;
                case LogType.Error:
                    UnityEngine.Debug.LogError(t);
                    break;
                case LogType.Assert:
                case LogType.Exception:
                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void Assert(bool f, string t = "")
        {
            UnityEngine.Debug.Assert(f, t);
        }
    }
}