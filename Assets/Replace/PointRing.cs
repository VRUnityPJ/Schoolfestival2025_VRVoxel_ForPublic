using DG.Tweening;
using Shinkan2024.Scripts.Game.Point;
//using Shinkan2024.Scripts.Game.Utility;
using UnityEngine;
using UnityEngine.VFX;

namespace Shinkan2024.Scripts.Game.PointRing
{
    public class PointRing : MonoBehaviour
    {
        /// <summary>
        /// リングを取ったときに加算されるスコア
        /// </summary>
        [Header("Point")]
        [SerializeField] private int _score = 100;
        /// <summary>
        /// リングの見た目となるオブジェクト
        /// </summary>
        [Header("Body")]
        [SerializeField] private Renderer _objectBody;
        
        [Header("Move Animation")]
        [SerializeField] private Vector3 _moveValue;
        /// <summary>
        /// 移動アニメーションの再生時間
        /// </summary>
        [SerializeField] private float _moveDuration;
        /// <summary>
        /// 移動アニメーションのイージングタイプ
        /// </summary>
        [SerializeField] private Ease _moveEaseType;
        /// <summary>
        /// 移動アニメーションのループタイプ
        /// </summary>
        [SerializeField] private LoopType _moveLoopType;
        /// <summary>
        /// アニメーションの回転量
        /// </summary>
        [Header("Rotate Animation")]
        [SerializeField] private Vector3 _rotateValue;
        /// <summary>
        /// 回転アニメーションの再生時間
        /// </summary>
        [SerializeField] private float _rotateDuration;
        /// <summary>
        /// 回転アニメーションのイージングタイプ
        /// </summary>
        [SerializeField] private Ease _rotateEaseType;
        /// <summary>
        /// 回転アニメーションのループタイプ
        /// </summary>
        [SerializeField] private LoopType _rotateLoopType;

        private void Start()
        {
            // アニメーションを設定
            // SetLinkで自身が破壊されたときに、Tweenも削除するようにする
            _objectBody.transform
                .DOLocalMove(_moveValue, _moveDuration)
                .SetEase(_moveEaseType)
                .SetLoops(-1, _moveLoopType)
                .SetLink(gameObject);
            _objectBody.transform
                .DOLocalRotate(_rotateValue, _rotateDuration, RotateMode.FastBeyond360)
                .SetEase(_rotateEaseType)
                .SetLoops(-1, _rotateLoopType)
                .SetLink(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Get PointRing : {gameObject.name}");
            
            // ポイントを加算
            PointHolder.Instance?.UpPoint(_score);
            
            if (TryGetComponent(out PointRingEffectData effectData))
            {
                //var audioClip = effectData.GetAudioClip(PointRingEffectDataType.Captured);
                //var vfx = effectData.GetVisualEffect(PointRingEffectDataType.Captured);
                //PlayCapturedAudio(audioClip);
                //PlayCapturedEffect(vfx, other.gameObject.transform);
            }
            else
            {
                Debug.LogWarning("Effect Dataが読み込めなかったため、エフェクトは再生されません。");
            }
            
            Destroy(gameObject);
        }

        private void PlayCapturedAudio(AudioClip clip)
        {
            if (clip is null) return;
            //AudioPlayer.PlayOneShotAudioAtPoint(clip, transform.position);
        }

        private void PlayCapturedEffect(VisualEffect vfx, Transform playerTransform)
        {
            if (vfx is null) return;
            var effectObject = Instantiate(vfx, transform.position, transform.rotation);
            var playerDir = playerTransform.position - transform.position;
            var cross = Vector3.Cross(transform.right, playerDir);
            if (cross.y >= 0)
            {
                effectObject.transform.right = -effectObject.transform.right;
            }
        }
    }
}