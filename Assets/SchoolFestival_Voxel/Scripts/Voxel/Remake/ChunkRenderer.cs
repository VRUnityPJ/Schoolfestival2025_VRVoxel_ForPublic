using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly Dictionary<int, List<int>> _submeshTriangles = new Dictionary<int, List<int>>();
        private float _voxelSize;
        private Material[] _materials;
        
        public void GenerateSurfaceNetsMesh(VoxelData[,,] globalVoxelData, Vector3Int chunkStartPos, int resolution, float voxelsize, Material[] sharedMaterials)
        {
            _voxelSize = voxelsize;
            _materials = sharedMaterials;
            float isoLevel = 0.5f;

            _vertices.Clear();
            _submeshTriangles.Clear();
            
            // セル座標から頂点インデックスへのマッピング
            var vertexMap = new Dictionary<Vector3Int, int>();

            // =================================================================
            // パス 1: 頂点の生成
            // サーフェスが横切る各セルに1つの頂点を、交点の平均位置に作成します。
            // =================================================================
            for (int x = 0; x < resolution+1; x++)
            {
                for (int y = 0; y < resolution+1; y++)
                {
                    for (int z = 0; z < resolution+1; z++)
                    {
                        Vector3Int localPos = new Vector3Int(x, y, z);
                        Vector3Int globalPos = chunkStartPos + localPos;

                        // セルの8つの角の密度からcubeIndexを計算（Marching Cubesと同じ）
                        int cubeIndex = 0;
                        var cornerDensities = new float[8];
                        for(int i = 0; i < 8; i++)
                        {
                            var cornerPos = globalPos + _cornerOffsets[i];
                            cornerDensities[i] = GetVoxelData(cornerPos.x, cornerPos.y, cornerPos.z, globalVoxelData).density;
                            if (cornerDensities[i] >= isoLevel)
                            {
                                cubeIndex |= (1 << i);
                            }
                        }

                        // セルが完全に内側または外側にある場合はスキップ
                        if (Tables.edgeTable[cubeIndex] == 0) continue;

                        // このセルのマテリアルIDを決定（最も多いものを採用）
                         int materialID = GetCellMaterialID(globalPos, isoLevel, globalVoxelData);
                         if (materialID == -1) continue;


                        // サーフェスが交差するエッジの中点を集計し、その平均点を頂点とする
                        var crossingPoints = new List<Vector3>();
                        for (int i = 0; i < 12; i++)
                        {
                            if ((Tables.edgeTable[cubeIndex] & (1 << i)) != 0)
                            {
                                Vector3 p1 = globalPos + _cornerOffsets[_edgeConnections[i, 0]];
                                Vector3 p2 = globalPos + _cornerOffsets[_edgeConnections[i, 1]];
                                // 線形補間を使ってより正確な交点を見つけることも可能
                                Vector3 intersection = (p1 + p2) / 2.0f;
                                crossingPoints.Add(intersection);
                            }
                        }
                        
                        if (crossingPoints.Count > 0)
                        {
                            Vector3 vertexPosition = Vector3.zero;
                            foreach (var p in crossingPoints)
                            {
                                vertexPosition += p;
                            }
                            vertexPosition /= crossingPoints.Count;

                            vertexMap[localPos] = _vertices.Count;
                            _vertices.Add((vertexPosition - (Vector3)chunkStartPos) * _voxelSize);
                        }
                    }
                }
            }
            // パス 2: 面（四角形）の生成
            // 各軸に沿ってグリッドをスキャンし、密度の符号が変わるエッジを探します。
            // そのエッジを共有する4つのセルの頂点を結んで四角形を生成します。
            for (int x = 0; x < resolution+1; x++)
            {
                for (int y = 0; y < resolution+1; y++)
                {
                    for (int z = 0; z < resolution+1; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        var globalP = chunkStartPos + p;

                        // 基準となるボクセルが固体かどうかを判定
                        bool pIsSolid = GetVoxelData(globalP.x, globalP.y, globalP.z, globalVoxelData).density >= isoLevel;

                        // Z軸に沿った面
                        if (pIsSolid != (GetVoxelData(globalP.x, globalP.y, globalP.z + 1, globalVoxelData).density >= isoLevel))
                        {
                            int mat = pIsSolid ? GetCellMaterialID(globalP, isoLevel, globalVoxelData) : GetCellMaterialID(globalP + Vector3Int.forward, isoLevel, globalVoxelData);
                            TryCreateQuad_R(mat, pIsSolid,
                                new Vector3Int(x, y, z), new Vector3Int(x - 1, y, z),
                                new Vector3Int(x, y - 1, z), new Vector3Int(x - 1, y - 1, z),
                                vertexMap);
                        }

                        // Y軸に沿った面
                        if (pIsSolid != (GetVoxelData(globalP.x, globalP.y + 1, globalP.z, globalVoxelData).density >= isoLevel))
                        {
                            int mat = pIsSolid ? GetCellMaterialID(globalP, isoLevel, globalVoxelData) : GetCellMaterialID(globalP + Vector3Int.up, isoLevel, globalVoxelData);
                            TryCreateQuad_R(mat, pIsSolid,
                                new Vector3Int(x, y, z), new Vector3Int(x, y, z - 1),
                                new Vector3Int(x - 1, y, z), new Vector3Int(x - 1, y, z - 1),
                                vertexMap);
                        }

                        // X軸に沿った面
                        if (pIsSolid != (GetVoxelData(globalP.x + 1, globalP.y, globalP.z, globalVoxelData).density >= isoLevel))
                        {
                            int mat = pIsSolid ? GetCellMaterialID(globalP, isoLevel, globalVoxelData) : GetCellMaterialID(globalP + Vector3Int.right, isoLevel, globalVoxelData);
                            TryCreateQuad_R(mat, pIsSolid,
                                new Vector3Int(x, y, z), new Vector3Int(x, y - 1, z),
                                new Vector3Int(x, y, z - 1), new Vector3Int(x, y - 1, z - 1),
                                vertexMap);
                        }
                    }
                }
            }

            BuildMesh();
        }
        
        private void TryCreateQuad_R(int materialID, bool reverseWinding, Vector3Int c1, Vector3Int c2, Vector3Int c3, Vector3Int c4, Dictionary<Vector3Int, int> map)
        {
            if (materialID == -1) return;
            if (map.TryGetValue(c1, out int i1) && map.TryGetValue(c2, out int i2) &&
                map.TryGetValue(c3, out int i3) && map.TryGetValue(c4, out int i4))
            {
                if (!_submeshTriangles.ContainsKey(materialID))
                {
                    _submeshTriangles[materialID] = new List<int>();
                }

                if (!reverseWinding)
                {
                    // 反時計回りの巻順 (i1 -> i3 -> i2)
                    _submeshTriangles[materialID].Add(i1);
                    _submeshTriangles[materialID].Add(i3);
                    _submeshTriangles[materialID].Add(i2);

                    _submeshTriangles[materialID].Add(i3);
                    _submeshTriangles[materialID].Add(i4);
                    _submeshTriangles[materialID].Add(i2);
                }
                else
                {
                    // 時計回りの巻順 (i1 -> i2 -> i3)
                    _submeshTriangles[materialID].Add(i1);
                    _submeshTriangles[materialID].Add(i2);
                    _submeshTriangles[materialID].Add(i3);

                    _submeshTriangles[materialID].Add(i3);
                    _submeshTriangles[materialID].Add(i2);
                    _submeshTriangles[materialID].Add(i4);
                }
            }
        }
        
        /// <summary>
        /// 指定されたグローバル座標のセルのマテリアルIDを取得します。
        /// </summary>
        private int GetCellMaterialID(Vector3Int globalPos, float isoLevel, VoxelData[,,] globalVoxelData)
        {
             return Enumerable.Range(0, 8)
                .Select(i => GetVoxelData(globalPos.x + _cornerOffsets[i].x, globalPos.y + _cornerOffsets[i].y, globalPos.z + _cornerOffsets[i].z, globalVoxelData))
                .Where(d => d.density >= isoLevel && d.materialID != -1)
                .GroupBy(d => d.materialID)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .DefaultIfEmpty(-1)
                .First();
        }

    
        private VoxelData GetVoxelData(int x, int y, int z, VoxelData[,,] data)
        {
            if (x < 0 || x >= data.GetLength(0) || y < 0 || y >= data.GetLength(1) || z < 0 || z >= data.GetLength(2))
            {
                return new VoxelData { density = 0, materialID = -1 };
            }
            return data[x, y, z];
        }

        private void BuildMesh()
        {
            if (_vertices.Count == 0)
            {
                GetComponent<MeshFilter>().mesh = null;
                return;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = _vertices.ToArray();

            var sortedSubmeshes = _submeshTriangles.OrderBy(pair => pair.Key).ToList();
            mesh.subMeshCount = sortedSubmeshes.Count;
            var activeMaterials = new Material[sortedSubmeshes.Count];

            for (int i = 0; i < sortedSubmeshes.Count; i++)
            {
                var pair = sortedSubmeshes[i];
                int submeshIndex = pair.Key;
                List<int> triangles = pair.Value;

                mesh.SetTriangles(triangles.ToArray(), i);

                if (submeshIndex < _materials.Length)
                {
                    activeMaterials[i] = _materials[submeshIndex];
                }
                else
                {
                    activeMaterials[i] = new Material(Shader.Find("Standard"));
                }
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshRenderer>().materials = activeMaterials;
        }

        private readonly Vector3Int[] _cornerOffsets = {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 1), new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 1, 1), new Vector3Int(0, 1, 1)
        };
        private readonly int[,] _edgeConnections = {
            {0, 1}, {1, 2}, {2, 3}, {3, 0}, {4, 5}, {5, 6}, {6, 7}, {7, 4},
            {0, 4}, {1, 5}, {2, 6}, {3, 7}
        };
    }
}

