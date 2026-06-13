using System.Collections.Generic;
using _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528;
using UnityEngine;
using VContainer;

public class VoxelWorldRenderer : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] 
    private ChunkRenderer _chunkPrefab; // チャンクのプレハブ
    
    [Inject] [SerializeField] private VoxelWorld _voxelWorld;
    [Inject] [SerializeField] private VoxelMaterialDatabase _materialDatabase;
    
    [Header("Collider Optimization Settings")]
    [SerializeField] private float _updateInterval = 0.2f; // チェック間隔（秒）
    
    private float _lastUpdateTime = 0f;

    // レンダラーの管理
    private Dictionary<Vector3Int, ChunkRenderer> _activeRenderers = new Dictionary<Vector3Int, ChunkRenderer>();
    // VoxelWorldのデータをもとに、メッシュを描画・更新する
    public void RebuildDirtyChunks()
    {
        var dirtyChunks = _voxelWorld.DirtyChunks;
        if (dirtyChunks.Count == 0) return;
        foreach (var chunkCoord in dirtyChunks)
        {
            // このチャンクに登録されているVoxelLockItemをチェックする
            var items = _voxelWorld.GetLockItemsInChunk(chunkCoord);
            if (items != null)
            {
                // リストから登録解除される可能性を考慮して、逆順でループする
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    items[i].CheckIfUnearthed();
                }
            }

            // 描画が必要なデータ（固体）が存在するか？
            bool hasData = _voxelWorld.HasChunkData(chunkCoord);
            if (hasData)
            {
                // --- 描画・更新処理 ---
                if (!_activeRenderers.TryGetValue(chunkCoord, out var rendererInstance))
                {
                    rendererInstance = Instantiate(_chunkPrefab, this.transform);
                    rendererInstance.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
                    
                    Vector3 worldPos = new Vector3(
                        chunkCoord.x * VoxelChunkData.ChunkSize,
                        chunkCoord.y * VoxelChunkData.ChunkSize,
                        chunkCoord.z * VoxelChunkData.ChunkSize
                    ) * _voxelWorld.VoxelSize;
                    rendererInstance.transform.localPosition = worldPos;
                    _activeRenderers[chunkCoord] = rendererInstance;
                }
                
                // メッシュ生成を実行し、有効な面（頂点）があるか確認
                bool hasVisibleMesh = rendererInstance.GenerateMesh(_voxelWorld, chunkCoord, _voxelWorld.VoxelSize, _materialDatabase);
                
                // 有効な面がない（完全に空気、または地中に完全に埋まっている）場合は、GameObject自体を非表示にして描画・物理演算の負荷を削減
                rendererInstance.gameObject.SetActive(hasVisibleMesh);
            }
            else
            {
                // --- 破棄処理 ---
                // データが空になったが、画面上にレンダラーが残っている場合は破棄して辞書から消す
                if (_activeRenderers.TryGetValue(chunkCoord, out var rendererInstance))
                {
                    if (rendererInstance != null)
                    {
                        Destroy(rendererInstance.gameObject);
                    }
                    _activeRenderers.Remove(chunkCoord);
                }
            }
        }
        _voxelWorld.ClearDirtyChunks();
    }

    private void Update()
    {
        // 負荷を抑えるため一定の間隔で実行
        if (Time.time - _lastUpdateTime < _updateInterval) return;
        _lastUpdateTime = Time.time;

        OptimizeMeshColliders();
    }

    private void OptimizeMeshColliders()
    {
        var targets = _voxelWorld.ActivePhysicsTargets;
        if (targets.Count == 0) return;

        float voxelSize = _voxelWorld.VoxelSize;
        float chunkSizeWorld = VoxelChunkData.ChunkSize * voxelSize;
        Vector3 chunkHalfOffset = Vector3.one * (chunkSizeWorld * 0.5f);

        foreach (var pair in _activeRenderers)
        {
            Vector3Int chunkCoord = pair.Key;
            ChunkRenderer rendererInstance = pair.Value;

            // チャンク自体が非アクティブな場合はコライダー判定を行わない（空気または完全地中はスキップ）
            if (rendererInstance == null || !rendererInstance.gameObject.activeSelf) continue;

            Vector3 chunkCenter = new Vector3(
                chunkCoord.x * VoxelChunkData.ChunkSize,
                chunkCoord.y * VoxelChunkData.ChunkSize,
                chunkCoord.z * VoxelChunkData.ChunkSize
            ) * voxelSize + chunkHalfOffset;

            bool inRange = false;
            
            // 登録ターゲットのいずれか1つの有効半径内に入っているか
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.transform == null) continue;

                float distance = Vector3.Distance(target.transform.position, chunkCenter);
                if (distance <= target.radius)
                {
                    inRange = true;
                    break;
                }
            }

            if (rendererInstance.TryGetComponent<MeshCollider>(out var meshCollider))
            {
                meshCollider.enabled = inRange;
            }
        }
    }

    /// <summary>
    /// すべての生成済みチャンクレンダラーを破棄し、レンダラー辞書を初期化する
    /// </summary>
    public void ResetRenderer()
    {
        foreach (var rendererInstance in _activeRenderers.Values)
        {
            if (rendererInstance != null)
            {
                Destroy(rendererInstance.gameObject);
            }
        }
        _activeRenderers.Clear();
    }
}
