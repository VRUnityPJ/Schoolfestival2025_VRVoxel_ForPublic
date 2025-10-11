using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.UI
{
    public class TrackingUI : MonoBehaviour
    {
        /// <summary>
        /// カメラとの距離
        /// </summary>
        [SerializeField]
        private float _distance = 5f;

        [SerializeField]
        private float _deltaYPosition = 0.2f;

        [SerializeField, MinValue(0f),MaxValue(1f)]
        private float _moveSpeed = 0.1f;

        [SerializeField, MinValue(0f), MaxValue(1f)]
        private float _rotateSpeed = 0.1f;

        [SerializeField] private bool isLockX = false;
        [SerializeField] private bool isLockY = false;
        [SerializeField] private bool isLockZ = false;
        private Transform _startTransform;
        
        /// <summary>
        /// プレイヤーのカメラ
        /// </summary>
        private Camera _camera;
        
        // Start is called before the first frame update
        public void Reset()
        {
            transform.position = _startTransform.position;
        }

        void Start()
        {
            // プレイヤーのカメラを取得
            _camera = Camera.main;
            _startTransform = transform;

            // *WorldSpaceにしたCanvasにはEventCameraを設定しないと正しく動かない
            // CanvasにEventCameraをセット
            var canvasList = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvasList)
            {
                canvas.worldCamera = _camera;
            }
            
            // すぐにカメラの前に持ってくる
            MoveImmediate();
            
        }

        private void Update()
        {
            // 移動
            // 目的の座標を計算
            var targetPosition = _distance * _camera.transform.forward + _camera.transform.position;
            if (targetPosition.y > _camera.transform.position.y + _deltaYPosition)
                targetPosition.y = _camera.transform.position.y + _deltaYPosition;
            if (targetPosition.y < _camera.transform.position.y - _deltaYPosition)
                targetPosition.y = _camera.transform.position.y - _deltaYPosition;
            
            // 移動先を計算して代入
            // transform.position = Vector3.Lerp(transform.position, targetPosition, _moveSpeed);
            transform.position = targetPosition;
            
            // 回転
            // 目標のQuaternionを取得
            var targetRotation = _camera.transform.rotation;
            // 各回転軸の回転量を0にする
            if (isLockX) targetRotation.x = 0;
            if (isLockY) targetRotation.y = 0;
            if (isLockZ) targetRotation.z = 0;
            // 回転量を計算して代入
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotateSpeed);
        }

        /// <summary>
        /// 今すぐに目的の座標に移動させたいときに使う
        /// </summary>
        public void MoveImmediate()
        {
            // 移動
            transform.position = _distance * _camera.transform.forward + _camera.transform.position;
            // 回転
            // 回転
            // 目標のQuaternionを取得
            var targetRotation = _camera.transform.rotation;
            // 各回転軸の回転量を0にする
            if (isLockX) targetRotation.x = 0;
            if (isLockY) targetRotation.y = 0;
            if (isLockZ) targetRotation.z = 0;
            // 回転量を計算して代入
            transform.rotation = targetRotation;
        }
    }
}
