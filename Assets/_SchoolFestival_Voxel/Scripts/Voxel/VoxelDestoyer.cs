using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class VoxelDestoyer : MonoBehaviour
    {
        //worldposを起点にradiusを半径として破壊
        //前回と違って破壊するのにSetVoxelを使用
        [Inject] private VoxelWorld _voxelWorld;
        [Inject] private VoxelWorldRenderer _worldRenderer;
        private const float ClearValue = -1.0f; // 空洞化時の密度値
        [SerializeField]private float _destroyRadius = 2.0f;
        [Button]
        public void TestCrash()
        {
            DestroySphere(this.gameObject.transform.position, _destroyRadius);
        }

        /// <summary>
        /// 指定したワールド座標を中心に、球体状にVoxelを破壊する
        /// </summary>
        /// <param name="worldPos">Unityのワールド座標</param>
        /// <param name="radius">球の半径（Unity単位）</param>
        public void DestroySphere(Vector3 worldPos, float radius)
        {
            // 1. ワールド座標をボクセルグリッドのローカル座標に変換
            Vector3 worldOrigin = _voxelWorld.transform.position;
            Vector3 localPos = worldPos - worldOrigin;
            // 2. 半径と座標をグリッド単位にスケーリング
            float gridRadius = radius / _voxelWorld.VoxelSize;
            Vector3 gridCenter = localPos / _voxelWorld.VoxelSize;
            // 3. ループを回すバウンディングボックスの範囲を計算
            // （※無限空間になったため、配列の最大サイズ sx, sy, sz による境界チェックが不要になります！）
            int minX = Mathf.FloorToInt(gridCenter.x - gridRadius);
            int maxX = Mathf.CeilToInt(gridCenter.x + gridRadius);
            int minY = Mathf.FloorToInt(gridCenter.y - gridRadius);
            int maxY = Mathf.CeilToInt(gridCenter.y + gridRadius);
            int minZ = Mathf.FloorToInt(gridCenter.z - gridRadius);
            int maxZ = Mathf.CeilToInt(gridCenter.z + gridRadius);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3Int gridPos = new Vector3Int(x, y, z);

                        // 現在のVoxelデータを取得
                        VoxelData currentData = _voxelWorld.GetVoxel(gridPos);
                        // ★最適化: すでに空気（density <= 0）の部分はスキップする
                        // これを行わないと、何もない空間を壊した時に「空のチャンク」が新規作成されてしまい、メモリの無駄になります
                        if (currentData.density <= 0) continue;
                        // 球の内側に入っているか判定
                        float dx = x - gridCenter.x;
                        float dy = y - gridCenter.y;
                        float dz = z - gridCenter.z;
                        if (dx * dx + dy * dy + dz * dz <= gridRadius * gridRadius)
                        {
                            // 密度を削り落とし、マテリアルIDは維持して上書き
                            _voxelWorld.SetVoxel(gridPos, new VoxelData(ClearValue, currentData.materialID));
                        }
                    }
                }
            }

            // 4. 汚れたチャンクを一括で再ビルド
            _worldRenderer.RebuildDirtyChunks();
        }
    }
}
