using System.Collections;
using System.Collections.Generic;
using Shinkan2024.Scripts.Game.Point;
using UnityEngine;
using UnityEngine.UI;

public class DemoResultPanel : MonoBehaviour
{
    [SerializeField] private PointHolder _pointHolder;
    [SerializeField] private DemoStageInformation stageInfo;
    
    public void Show()
    {
        GetValues();
        gameObject.SetActive(true);
    }

    private void GetValues()
    {
        Debug.Log($"length : {(int)stageInfo.RoadLength}");
        Debug.Log( $"width : {(int)stageInfo.RoadWidth}");
        // Debug.Log( $"point : {(_pointHolder.Point.Value.IntValue)}");
        Debug.Log($"Time : {(DemoTimer.StopAndGetTime())}");
    }
}
