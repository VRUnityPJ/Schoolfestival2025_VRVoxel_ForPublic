using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using VContainer;
using VoxReader.Interfaces;

namespace _SchoolFestival_Voxel.Scripts.Voxel
{
    public class VoxelBuilder : MonoBehaviour
    {
        //worldposを起点にradiusを半径として破壊
        //前回と違って破壊するのにSetVoxelを使用
        [Inject] private VoxelWorld _voxelWorld;
        [Inject] private VoxelWorldRenderer _worldRenderer;
        [Inject] private VoxelMaterialDatabase _db;
        
        private const float ClearValue = -1.0f; // 空洞化時の密度値
        [SerializeField]private float _destroyRadius = 2.0f;
        [SerializeField]private int _materialID = 1; // 生成するマテリアルID

        // パース済みのモデルデータをキャッシュ (パフォーマンス用)
        private static readonly Dictionary<string, List<(Vector3Int offsetPos, int materialID)>> _modelCache = new();
        [Button]
        public void TestBuild()
        {
            BuildSphere(this.gameObject.transform.position, _destroyRadius,_materialID);
        }

        /// <summary>
        /// 指定したワールド座標を中心に、球体状にVoxelを破壊する
        /// </summary>
        /// <param name="worldPos">Unityのワールド座標</param>
        /// <param name="radius">球の半径（Unity単位）</param>
        public void BuildSphere(Vector3 worldPos, float radius, int materialID)
        {
            float voxelSize = _voxelWorld.VoxelSize;
            Vector3 localPos = worldPos - _voxelWorld.transform.position;
            float gridRadius = radius / voxelSize;
            Vector3 gridCenter = localPos / voxelSize;
            // ★重要: 球の外側2マス分（パディング）も含めてループを回す
            int padding = 2;
            int minX = Mathf.FloorToInt(gridCenter.x - gridRadius - padding);
            int maxX = Mathf.CeilToInt(gridCenter.x + gridRadius + padding);
            int minY = Mathf.FloorToInt(gridCenter.y - gridRadius - padding);
            int maxY = Mathf.CeilToInt(gridCenter.y + gridRadius + padding);
            int minZ = Mathf.FloorToInt(gridCenter.z - gridRadius - padding);
            int maxZ = Mathf.CeilToInt(gridCenter.z + gridRadius + padding);
            
            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                float dx = x - gridCenter.x;
                float dy = y - gridCenter.y;
                float dz = z - gridCenter.z;
                float distance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                if (distance <= gridRadius + padding)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    VoxelData currentData = _voxelWorld.GetVoxel(gridPos);
                    // 新しい球の密度を計算
                    float sphereDensity = 0.5f + (gridRadius - distance);
                    sphereDensity = Mathf.Clamp(sphereDensity, -1.0f, 1.5f);
                    // ★CSG 結合（Union）処理★
                    // 新しい球の密度のほうが「濃い（大きい）」場合のみ、データとマテリアルを更新する！
                    if (sphereDensity > currentData.density)
                    {
                        _voxelWorld.SetVoxel(gridPos, new VoxelData(sphereDensity, materialID));
                    }
                    // そうではない場合（すでに既存の硬い地面 1.0f などがある場所）は、何もしない（消去を防ぐ）
                }
            }
            _worldRenderer.RebuildDirtyChunks();
        }

        /// <summary>
        /// 始点から終点に向けて、指定の半径で直線状にVoxelを配置する（レール生成）
        /// </summary>
        public void BuildLine(Vector3 start, Vector3 end, float radius, int materialID)
        {
            float voxelSize = _voxelWorld.VoxelSize;
            float distance = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;
            
            // voxelSizeの半分以下のステップ幅でサンプリング
            float stepSize = voxelSize * 0.4f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / stepSize));
            
            Vector3 worldOrigin = _voxelWorld.transform.position;
            float gridRadius = radius / voxelSize;
            int padding = 2;

            Debug.Log($"[VoxelBuilder] BuildLine start. Distance: {distance:F2}m, Steps: {steps}, Radius: {radius:F2}m, Direction: {direction}");

            for (int i = 0; i <= steps; i++)
            {
                Vector3 samplePoint = start + direction * (i * stepSize);
                Vector3 localPos = samplePoint - worldOrigin;
                Vector3 gridCenter = localPos / voxelSize;

                int minX = Mathf.FloorToInt(gridCenter.x - gridRadius - padding);
                int maxX = Mathf.CeilToInt(gridCenter.x + gridRadius + padding);
                int minY = Mathf.FloorToInt(gridCenter.y - gridRadius - padding);
                int maxY = Mathf.CeilToInt(gridCenter.y + gridRadius + padding);
                int minZ = Mathf.FloorToInt(gridCenter.z - gridRadius - padding);
                int maxZ = Mathf.CeilToInt(gridCenter.z + gridRadius + padding);

                for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);

                    float dx = x - gridCenter.x;
                    float dy = y - gridCenter.y;
                    float dz = z - gridCenter.z;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (dist <= gridRadius + padding)
                    {
                        VoxelData currentData = _voxelWorld.GetVoxel(gridPos);
                        float sphereDensity = 0.5f + (gridRadius - dist);
                        sphereDensity = Mathf.Clamp(sphereDensity, -1.0f, 1.5f);

                        if (sphereDensity > currentData.density)
                        {
                            _voxelWorld.SetVoxel(gridPos, new VoxelData(sphereDensity, materialID));
                        }
                    }
                }
            }

            Debug.Log($"[VoxelBuilder] BuildLine finished.");
            _worldRenderer.RebuildDirtyChunks();
        }

        /// <summary>
        /// 指定された位置にMagicaVoxelモデルを建築する
        /// </summary>
        public void BuildModel(Vector3 hitPoint, Vector3 normal, string modelName)
        {
            if (_db == null)
            {
                _db = FindObjectOfType<VoxelMaterialDatabase>();
            }

            var voxels = GetOrLoadModel(modelName);
            if (voxels == null || voxels.Count == 0) return;

            // ヒットした面のすぐ外側（法線方向）を基準点とする
            Vector3Int hitGrid = Vector3Int.FloorToInt((hitPoint + normal * 0.1f) / _voxelWorld.VoxelSize);

            foreach (var voxel in voxels)
            {
                Vector3Int targetPos = hitGrid + voxel.offsetPos;
                _voxelWorld.SetVoxel(targetPos, new VoxelData(1.0f, voxel.materialID));
            }

            _worldRenderer.RebuildDirtyChunks();
        }

        private List<(Vector3Int offsetPos, int materialID)> GetOrLoadModel(string modelName)
        {
            if (_modelCache.TryGetValue(modelName, out var cached))
            {
                return cached;
            }

            string filePath = Path.Combine(Application.streamingAssetsPath, modelName);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"MagicaVoxelファイルが見つかりません: {filePath}");
                return null;
            }

            IVoxFile voxFileContent = VoxReader.VoxReader.Read(filePath);
            if (voxFileContent == null || voxFileContent.Models.Length == 0)
            {
                Debug.LogError($"MagicaVoxelファイルの読み込みに失敗しました: {modelName}");
                return null;
            }

            IModel model = voxFileContent.Models[0];
            
            // 色のマッピングを行う
            var paletteIndexToMaterialID = new Dictionary<int, int>();
            var usedColorIndices = new HashSet<int>();
            foreach (var voxel in model.Voxels)
            {
                usedColorIndices.Add(voxel.ColorIndex);
            }

            if (_db != null)
            {
                for (int i = 0; i < voxFileContent.Palette.Colors.Length; i++)
                {
                    var voxColor = voxFileContent.Palette.Colors[i];
                    var unityColor = new Color32(voxColor.R, voxColor.G, voxColor.B, 255);

                    if (usedColorIndices.Contains(i))
                    {
                        paletteIndexToMaterialID[i] = _db.GetMaterialID(unityColor);
                    }
                    else
                    {
                        paletteIndexToMaterialID[i] = 0;
                    }
                }
            }

            // モデルのサイズ（境界ボックス）を取得して、底部中心が (0, 0, 0) になるようにオフセットを計算
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (var voxel in model.Voxels)
            {
                int vx = voxel.LocalPosition.X;
                int vy = voxel.LocalPosition.Z; // 座標軸の入れ替え
                int vz = voxel.LocalPosition.Y;

                if (vx < minX) minX = vx; if (vx > maxX) maxX = vx;
                if (vy < minY) minY = vy; if (vy > maxY) maxY = vy;
                if (vz < minZ) minZ = vz; if (vz > maxZ) maxZ = vz;
            }

            int offsetX = -(minX + maxX) / 2;
            int offsetY = -minY; // 底部がヒット位置に乗るように設定
            int offsetZ = -(minZ + maxZ) / 2;

            var result = new List<(Vector3Int offsetPos, int materialID)>();
            foreach (var voxel in model.Voxels)
            {
                Vector3Int localOffset = new Vector3Int(
                    voxel.LocalPosition.X + offsetX,
                    voxel.LocalPosition.Z + offsetY, // 座標軸の入れ替え
                    voxel.LocalPosition.Y + offsetZ
                );

                paletteIndexToMaterialID.TryGetValue(voxel.ColorIndex, out int matID);
                result.Add((localOffset, matID));
            }

            _modelCache[modelName] = result;
            return result;
        }
    }
}
