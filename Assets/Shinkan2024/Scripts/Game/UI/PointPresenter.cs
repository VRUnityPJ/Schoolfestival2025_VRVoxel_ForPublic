using System;
using R3;
using Replace;
using UnityEngine;


namespace Shinkan2024.Scripts.Game.UI
{
    public class PointPresenter : MonoBehaviour
    {
        private PointHolder _holder;
        private PointViewer _viewer;

        private void Start()
        {
            if(!TryGetComponent(out _holder))
                UnityEngine.Debug.LogError("PointHolderが取得できません");
            if(!TryGetComponent(out _viewer))
                UnityEngine.Debug.LogError("PointViewerが取得できません");

            _holder.Point
                .Subscribe(ConvertAndSendPoint)
                .AddTo(this);
        }
        /// <summary>
        /// Pointをint型に返還しviewerに送る
        /// </summary>
        private void ConvertAndSendPoint(Point _point)
        {
            int value = _point.IntValue;
            
            _viewer.UpdatePointText(value);
        }
    }
}