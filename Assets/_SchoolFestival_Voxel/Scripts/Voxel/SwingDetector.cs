
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel
{
    public class SwingDetector : MonoBehaviour
    {
        [Inject] private VoxelWorld _voxelWorld;
        [Inject] private VoxelDestoyer _voxelDestoyer;

        [Header("Swing Settings")]
        [SerializeField] private float _swingThreshold = -0.7f;
        [SerializeField] private float _swingSpeed = 4f;
        [SerializeField] private float _destroyRadius = 0.5f;

        [Header("References")]
        [SerializeField] private GameObject _playerTransform;
        [SerializeField] private AudioSource _crashAudio;
        [SerializeField] private AudioClip _crashAudioClip;

        private Vector3 _prevPos;
        private Vector3 _prevPlayerPos;
        private bool _wasSwinging = false;

        private void Start()
        {
            // DIのフォールバック
            if (_voxelWorld == null) _voxelWorld = FindObjectOfType<VoxelWorld>();
            if (_voxelDestoyer == null) _voxelDestoyer = FindObjectOfType<VoxelDestoyer>();

            _prevPos = transform.position;
            if (_playerTransform != null)
            {
                _prevPlayerPos = _playerTransform.transform.position;
            }
        }

        private void Update()
        {
            if (_voxelWorld == null || _voxelDestoyer == null || _playerTransform == null) return;

            Vector3 currentPos = transform.position;
            Vector3 currentPlayerPos = _playerTransform.transform.position;

            // プレイヤーの移動速度を差し引いた、手の純粋な移動速度を計算
            Vector3 relativeVelocity = ((currentPos - _prevPos) - (currentPlayerPos - _prevPlayerPos)) / Time.deltaTime;
            Vector3 moveDirection = relativeVelocity.normalized;
            
            // 振る方向（手の真上方向と動いた方向の内積）
            float dot = Vector3.Dot(transform.up, moveDirection);
            float currentSpeed = relativeVelocity.magnitude;

            // 振る速度がしきい値を超え、かつ直前まで振っていなかった場合
            if (dot < _swingThreshold && currentSpeed > _swingSpeed)
            {
                // 前回のフレーム位置から今回のフレーム位置までの軌道をレイキャスト
                Vector3 swingDir = (currentPos - _prevPos).normalized;
                float swingDist = Vector3.Distance(_prevPos, currentPos);
                
                // 確実にヒットを拾うためにレイをわずかに延長（例: 0.1m）
                float maxRayDist = swingDist + 0.1f;

                if (_voxelWorld.Raycast(_prevPos, swingDir, maxRayDist, out VoxelRaycastHit hit))
                {
                    // 衝突した面の少し内側（法線と逆方向）を破壊の中心とする
                    Vector3 destroyCenter = hit.hitPoint - hit.normal * (_destroyRadius * 0.2f);
                    
                    _voxelDestoyer.DestroySphere(destroyCenter, _destroyRadius);
                    if (_crashAudio != null && _crashAudioClip != null)
                    {
                        _crashAudio.PlayOneShot(_crashAudioClip);
                    }
                    _wasSwinging = true;
                }
            }
            else if (currentSpeed < _swingSpeed && _wasSwinging)
            {
                _wasSwinging = false;
            }

            _prevPos = currentPos;
            _prevPlayerPos = currentPlayerPos;
        }
    }
}
