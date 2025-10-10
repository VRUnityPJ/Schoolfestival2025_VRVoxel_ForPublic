using System;
using System.Collections;
using System.Collections.Generic;
using Shinkan2024.Scripts.Game.Utility;
using UnityEngine;

public class DemoTimer : MonoBehaviour
{
    private static float time;
    private static bool isMeasuring = false;
    private void Start()
    {
        time = 0f;
    }

    private void Update()
    {
        if(!isMeasuring)
            return;

        time += Time.deltaTime;
    }

    public static void StartMeasure()
    {
        time = 0;
        isMeasuring = true;
    }

    public static string StopAndGetTime()
    {
        isMeasuring = false;
        return ConvertTimeToString();
    }

    private static string ConvertTimeToString()
    {
        int min = 0;
        int val = (int)time;
        while (val >= 60)
        {
            val -= 60;
            min++;
        }

        int sec = val;

        return $"{min}min:{sec}sec";
    }
}
