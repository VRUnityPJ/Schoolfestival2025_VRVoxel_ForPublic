using UnityEngine;
using R3;
using Ranking.Demo.Scripts.DemoGame;
using UnityEngine.Serialization;

public class VoxelLockItem : MonoBehaviour
{
    
    [SerializeField] private ChunkManager _chunkManager;
    private Rigidbody _rigidBody;
    private bool _isUnearthed = false;

    // アイテムを囲むチェック範囲（3x3x3のグリッドをチェック）
    [SerializeField]private Vector3Int _checkBounds = new (3, 3, 3);
    
    
    private Transform _startTransform;

    void Start()
    {
        _startTransform = transform;
        TryGetComponent<Rigidbody>(out _rigidBody);
        // chunkManager.OnVoxelRecreated += CheckIfUnearthed;
        _chunkManager.OnVoxelRecreated
            .Subscribe(input => CheckIfUnearthed())
            .AddTo(this);
        _chunkManager.OnVoxelReset
            .Subscribe(input => Reset())
            .AddTo(this);
    }
  

    // 地形が破壊された時などに呼び出す関数
    public void CheckIfUnearthed()
    {
        if (_isUnearthed) return; // すでに掘り出されていたら何もしない

        // アイテムのワールド座標をグリッド座標に変換
        Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / _chunkManager.voxelSize);

        // チェック範囲の開始点を計算（アイテムを中心とするため、範囲の半分だけオフセット）
        Vector3Int startPoint = gridPos - _checkBounds / 2;

        // ChunkManagerの関数を呼び出して判定！
        if (_chunkManager.IsAllEmpty(startPoint, _checkBounds))
        {
            _isUnearthed = true;
            _rigidBody.isKinematic = false;

            // レンダラーやコライダーを有効化する処理
            // GetComponent<MeshRenderer>().enabled = true;
            // GetComponent<Collider>().enabled = true;
        }
    }

    public void Reset()
    {
        _rigidBody.isKinematic = true;
        this.gameObject.SetActive(true);
        transform.position = _startTransform.position;
        _isUnearthed = false;
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Player player)&& _isUnearthed)
        {
            player.AddScore(100);
            
            gameObject.SetActive(false);
        }
    }
}