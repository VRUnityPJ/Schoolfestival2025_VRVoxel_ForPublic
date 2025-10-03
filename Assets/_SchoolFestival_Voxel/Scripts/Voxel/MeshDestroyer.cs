// Assets/Scripts/MeshDestroyer.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MeshDestroyer:
/// - worldPos (Unity units) と radius (Unity units) を与えると、
///   グローバル密度を空洞化し、影響チャンクを再メッシュ化する
/// - 再メッシュはコルーチンでフレーム分散可能
/// </summary>
public class MeshDestroyer : MonoBehaviour
{
    // public ChunkManager chunkManager;
    public SeamlessChunkManager chunkManager;
    public float clearValue = -10f; // 空洞化時にセットする密度値（iso より十分小さく）
    public bool rebuildAsync = true;

    void Awake()
    {
        // if (chunkManager == null) chunkManager = GetComponent<ChunkManager>();
        if (chunkManager == null) chunkManager = GetComponent<SeamlessChunkManager>();
        if (chunkManager == null) Debug.LogWarning("MeshDestroyer: chunkManager not assigned.");
    }

    /// <summary>
    /// worldPos: Unity world space position (chunks were positioned so that chunk origin = gridStart * voxelSize)
    /// radius: world units
    /// </summary>
    public void DestroySphere(Vector3 worldPos, float radius)
    {
        if (chunkManager == null) return;

        float voxelSize = chunkManager.voxelSize;
        float rGrid = radius / voxelSize;
        Vector3 gridPosF = worldPos / voxelSize;

        float[,,] global = chunkManager.GetGlobalDensity();
        int sx = global.GetLength(0), sy = global.GetLength(1), sz = global.GetLength(2);

        int minX = Mathf.Max(0, Mathf.FloorToInt(gridPosF.x - rGrid));
        int maxX = Mathf.Min(sx - 1, Mathf.CeilToInt(gridPosF.x + rGrid));
        int minY = Mathf.Max(0, Mathf.FloorToInt(gridPosF.y - rGrid));
        int maxY = Mathf.Min(sy - 1, Mathf.CeilToInt(gridPosF.y + rGrid));
        int minZ = Mathf.Max(0, Mathf.FloorToInt(gridPosF.z - rGrid));
        int maxZ = Mathf.Min(sz - 1, Mathf.CeilToInt(gridPosF.z + rGrid));

        HashSet<Tuple<int,int,int>> affectedChunks = new HashSet<Tuple<int,int,int>>();

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    float dx = x - gridPosF.x;
                    float dy = y - gridPosF.y;
                    float dz = z - gridPosF.z;
                    if (dx*dx + dy*dy + dz*dz <= rGrid * rGrid)
                    {
                        // clear density
                        global[x, y, z] = clearValue;
                        // mark chunk
                        int cx = x / chunkManager.GetChunkResolution();
                        int cy = y / chunkManager.GetChunkResolution();
                        int cz = z / chunkManager.GetChunkResolution();
                        affectedChunks.Add(Tuple.Create(cx, cy, cz));
                    }
                }

        // kick off rebuild
        chunkManager.RebuildChunks(affectedChunks, rebuildAsync);
        // chunkManager.RebuildChunks(affectedChunks);
    }
}
