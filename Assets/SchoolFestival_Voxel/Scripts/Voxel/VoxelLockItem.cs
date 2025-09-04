using UnityEngine;
using R3;
public class VoxelLockItem : MonoBehaviour
{
    
    [SerializeField] private SeamlessChunkManager chunkManager;
    public  Rigidbody rigidBody;
    private bool isUnearthed = false;

    // アイテムを囲むチェック範囲（3x3x3のグリッドをチェック）
    public Vector3Int checkBounds = new Vector3Int(3, 3, 3);

    void Start()
    {
        chunkManager.OnVoxelRecreated += CheckIfUnearthed;
    }
    private void OnDestroy()
    {
        // シーン終了時にイベントから解除
        chunkManager.OnVoxelRecreated -= CheckIfUnearthed;
    }

    // 地形が破壊された時などに呼び出す関数
    public void CheckIfUnearthed()
    {
        if (isUnearthed) return; // すでに掘り出されていたら何もしない

        // アイテムのワールド座標をグリッド座標に変換
        Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / chunkManager.voxelSize);

        // チェック範囲の開始点を計算（アイテムを中心とするため、範囲の半分だけオフセット）
        Vector3Int startPoint = gridPos - checkBounds / 2;

        // ChunkManagerの関数を呼び出して判定！
        if (chunkManager.IsAllEmpty(startPoint, checkBounds))
        {
            Debug.Log("掘り出された！");
            isUnearthed = true;
            rigidBody.isKinematic = false;

            // レンダラーやコライダーを有効化する処理
            // GetComponent<MeshRenderer>().enabled = true;
            // GetComponent<Collider>().enabled = true;
        }
    }
}