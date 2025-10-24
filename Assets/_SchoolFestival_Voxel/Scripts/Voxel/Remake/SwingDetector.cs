using UnityEngine;
using SchoolFestival_Voxel.Scripts.Voxel.Remake;
namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    public class SwingDetector : MonoBehaviour
    {
        // Inspectorから調整できる速度のしきい値
        [SerializeField]
        private float swingThreshold = -0.7f;
        [SerializeField]
        private float swingSpeed = 4f;
        [SerializeField] private RemakeMeshDestroyer _meshDestroyer;
        [SerializeField] private ChunkManager _chunkManager;
        [SerializeField] private float _destroyRadius = 0.5f; // 破壊する球の半径
        [SerializeField]private GameObject _playerP;
        [SerializeField]private AudioSource _crashAudio;
        [SerializeField] private AudioClip _crashAudioClip;
        private Vector3 _prevPos;
        private Vector3 _prevPosPlayer;

        // オブジェクトのRigidbodyコンポーネントを格納する変数
        private Rigidbody rb;

        // 連続で関数が呼ばれるのを防ぐためのフラグ
        private bool wasSwinging = false;

        void Start()
        {
            // オブジェクトにアタッチされているRigidbodyコンポーネントを取得
            
            _prevPos = transform.position;
            _prevPosPlayer = _playerP.transform.position;
            
            
        }

        void Update()
        {
            
            Vector3 currentPos = transform.position;
            Vector3 currentPosPlayer = _playerP.transform.position;
            Vector3 velocity = ((currentPos - _prevPos) -(currentPosPlayer-_prevPosPlayer))/ Time.deltaTime;
            Vector3 moveDicrection = velocity.normalized;
            float dot = Vector3.Dot(transform.up, moveDicrection);

            

            // 速度の「大きさ」を取得
            float currentSpeed = velocity.magnitude;
            
            

            // 速度がしきい値を超え、かつ直前まで振っていなかった場合
            if (dot < swingThreshold&&currentSpeed>swingSpeed)
            {
                Debug.Log("振った");
                Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / _chunkManager.voxelSize);
                if (_chunkManager.IsSolid(gridPos))
                {
                    _meshDestroyer.DestroySphere(this.gameObject.transform.position, _destroyRadius);
                    _crashAudio.PlayOneShot(_crashAudioClip);
                    // 振っている状態にフラグを更新
                    wasSwinging = true;
                }
                
            }
            // 速度がしきい値を下回ったら、フラグをリセット
            // これにより、次にしきい値を超えたときに再度関数が呼ばれるようになる
            else if (currentSpeed < swingSpeed && wasSwinging)
            {
                wasSwinging = false;
            }
            _prevPos = currentPos;
            _prevPosPlayer = currentPosPlayer;
        }
    }
}