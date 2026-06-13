using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class VoxelPainter : MonoBehaviour
    {
        //worldposを起点にradiusを半径として破壊
        //前回と違って破壊するのにSetVoxelを使用
        [Inject] private VoxelWorld _voxelWorld;
        [Inject] private VoxelWorldRenderer _worldRenderer;
        private const float ClearValue = -1.0f; // 空洞化時の密度値
        [SerializeField]private float _destroyRadius = 2.0f;
        [SerializeField]private int _materialID = 1; // 生成するマテリアルID
        [Button]
        public void TestPaint()
        {
            PaintSphere(this.gameObject.transform.position, _destroyRadius,_materialID);
        }

        /// <summary>
        /// 指定したワールド座標を中心に、球体状にVoxelを破壊する
        /// </summary>
        /// <param name="worldPos">Unityのワールド座標</param>
        /// <param name="radius">球の半径（Unity単位）</param>
        public void PaintSphere(Vector3 worldPos, float radius, int materialID)
        {
            float voxelSize = _voxelWorld.VoxelSize;
            Vector3 localPos = worldPos - _voxelWorld.transform.position;
            float gridRadius = radius / voxelSize;
            Vector3 gridCenter = localPos / voxelSize;
            
            // ペインターは密度（形状）を変更せず、色（マテリアルID）のみを変更するため、
            // 外側のパディングは不要で、厳密に半径内のセルのみを対象とします。
            int minX = Mathf.FloorToInt(gridCenter.x - gridRadius);
            int maxX = Mathf.CeilToInt(gridCenter.x + gridRadius);
            int minY = Mathf.FloorToInt(gridCenter.y - gridRadius);
            int maxY = Mathf.CeilToInt(gridCenter.y + gridRadius);
            int minZ = Mathf.FloorToInt(gridCenter.z - gridRadius);
            int maxZ = Mathf.CeilToInt(gridCenter.z + gridRadius);
            
            float radiusSq = gridRadius * gridRadius;
            
            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                float dx = x - gridCenter.x;
                float dy = y - gridCenter.y;
                float dz = z - gridCenter.z;
                float distanceSq = dx * dx + dy * dy + dz * dz;
                
                // 厳密に球の範囲内のみをペイントする
                if (distanceSq <= radiusSq)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    VoxelData currentData = _voxelWorld.GetVoxel(gridPos);
                    if (currentData.density <= 0) continue;
                    
                    _voxelWorld.SetVoxel(gridPos, new VoxelData(currentData.density, materialID));
                }
            }
            _worldRenderer.RebuildDirtyChunks();
        }
    }
}
