using UnityEngine;
using R3;
using UnityEngine.Serialization;

public class VoxelLockItem : MonoBehaviour
{
    
    [SerializeField] private ChunkManager _chunkManager;
    private Rigidbody _rigidBody;
    private bool _isUnearthed = false;

    // アイテムを囲むチェック範囲（3x3x3のグリッドをチェック）
    [SerializeField]private Vector3Int _checkBounds = new (3, 3, 3);

    void Start()
    {
        TryGetComponent<Rigidbody>(out _rigidBody);
        // chunkManager.OnVoxelRecreated += CheckIfUnearthed;
    }
    private void OnDestroy()
    {
        // シーン終了時にイベントから解除
        // chunkManager.OnVoxelRecreated -= CheckIfUnearthed;
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
}