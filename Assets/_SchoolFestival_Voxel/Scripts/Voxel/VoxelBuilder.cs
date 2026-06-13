using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class VoxelBuilder : MonoBehaviour
    {
        //worldposを起点にradiusを半径として破壊
        //前回と違って破壊するのにSetVoxelを使用
        [Inject] private VoxelWorld _voxelWorld;
        [Inject] private VoxelWorldRenderer _worldRenderer;
        private const float ClearValue = -1.0f; // 空洞化時の密度値
        [SerializeField]private float _destroyRadius = 2.0f;
        [SerializeField]private int _materialID = 1; // 生成するマテリアルID
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
    }
}
