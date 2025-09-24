using UnityEngine;
using ZLinq;

namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    /// <summary>
    /// チャンクの生成に必要な機能をまとめたクラス
    /// </summary>
    public static class ChunkUtility
    {
        private static readonly int[] CornerIndices = { 0, 1, 2, 3, 4, 5, 6, 7 };
        
        public static readonly Vector3Int[] CornerOffsets = {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 1), new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 1, 1), new Vector3Int(0, 1, 1)
        };
        
        public static readonly int[,] EdgeConnections = {
            {0, 1}, {1, 2}, {2, 3}, {3, 0}, {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };
        
        /// <summary>
        /// 指定されたグローバル座標のセルのマテリアルIDを取得します。
        /// </summary>
        public static int GetCellMaterialID(Vector3Int globalPos, float isoLevel, VoxelData[,,] globalVoxelData)
        {
            return CornerIndices
                .AsValueEnumerable()
                .Select(i => GetVoxelData(globalPos.x + CornerOffsets[i].x, globalPos.y + CornerOffsets[i].y, globalPos.z + CornerOffsets[i].z, globalVoxelData))
                .Where(d => d.density >= isoLevel && d.materialID != -1)
                .GroupBy(d => d.materialID)
                .OrderByDescending(g => g.AsValueEnumerable().Count())
                .Select(g => g.Key)
                .FirstOrDefault(-1);
        }
        
        public static VoxelData GetVoxelData(int x, int y, int z, VoxelData[,,] data)
        {
            if (x < 0 || x >= data.GetLength(0) || y < 0 || y >= data.GetLength(1) || z < 0 || z >= data.GetLength(2))
            {
                return new VoxelData(0,-1);
            }
            return data[x, y, z];
        }
        
    }
}