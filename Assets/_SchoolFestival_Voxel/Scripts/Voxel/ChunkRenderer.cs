using System;
using System.Collections.Generic;
using UnityEngine;
using ZLinq;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class ChunkRenderer : MonoBehaviour
    {
        // 最適化用の一時バッファ
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>(); // 頂点カラー格納バッファ
        private readonly Dictionary<int, List<int>> _submeshTriangles = new Dictionary<int, List<int>>();
        private readonly float[] _cornerDensities = new float[8];
        private readonly Vector3[] _crossingPoints = new Vector3[12];
        
        // 動的にリサイズされる頂点マップ用1次元配列
        private int[] _vertexMap;
        private float _voxelSize;

        /// <summary>
        /// 空間のローカル座標( -1 ~ resolution+1 )から、安全に頂点インデックスを取得する
        /// </summary>
        private int GetVertexMapValue(int x, int y, int z, int resolution)
        {
            int mapSize = resolution + 3; // 境界 -1 ~ resolution+1 をカバーするため +3
            int rx = x + 1;
            int ry = y + 1;
            int rz = z + 1;
            if (rx < 0 || rx >= mapSize || ry < 0 || ry >= mapSize || rz < 0 || rz >= mapSize)
                return -1;
            return _vertexMap[rx + (ry * mapSize) + (rz * mapSize * mapSize)];
        }

        /// <summary>
        /// 頂点インデックスを vertexMap に登録する
        /// </summary>
        private void SetVertexMapValue(int x, int y, int z, int value, int resolution)
        {
            int mapSize = resolution + 3;
            int rx = x + 1;
            int ry = y + 1;
            int rz = z + 1;
            _vertexMap[rx + (ry * mapSize) + (rz * mapSize * mapSize)] = value;
        }

        public bool GenerateMesh(VoxelWorld world, Vector3Int chunkCoord, float voxelSize, VoxelMaterialDatabase db)
        {
            _voxelSize = voxelSize;
            float isoLevel = 0.5f;
            int resolution = VoxelChunkData.ChunkSize; // 16
            
            _vertices.Clear();
            _colors.Clear();
            _submeshTriangles.Clear();

            // 必要な配列サイズを計算して確保 (遅延初期化)
            int mapSize = resolution + 3;
            int requiredLength = mapSize * mapSize * mapSize;
            if (_vertexMap == null || _vertexMap.Length != requiredLength)
            {
                _vertexMap = new int[requiredLength];
            }
            Array.Fill(_vertexMap, -1); // マップを未登録の -1 で初期化
            
            Vector3Int chunkStartPos = chunkCoord * resolution;
            
            // チャンク内のフラット配列からマテリアルIDを直接スキャンする（超高速化）
            HashSet<int> uniqueMaterials = new HashSet<int>();
            var chunk = world.GetChunk(chunkCoord);
            if (chunk != null)
            {
                // チャンク内の全4096マスを直接ループ（GetCellMaterialIDを通さない）
                for (int i = 0; i < VoxelChunkData.ChunkSize * VoxelChunkData.ChunkSize * VoxelChunkData.ChunkSize; i++)
                {
                    VoxelData data = chunk.GetVoxelDirect(i); // 配列のインデックス直接取得メソッド
                    if (data.density >= isoLevel && data.materialID != -1)
                    {
                        uniqueMaterials.Add(data.materialID);
                    }
                }
            }

            // チャンク境界（x=16, y=16, z=16）のボクセルマテリアルも追加収集して色の不整合を防ぐ
            for (int x = 0; x <= resolution; x++)
            {
                for (int y = 0; y <= resolution; y++)
                {
                    Vector3Int globalPos = chunkStartPos + new Vector3Int(x, y, resolution);
                    VoxelData data = world.GetVoxel(globalPos);
                    if (data.density >= isoLevel && data.materialID != -1)
                    {
                        uniqueMaterials.Add(data.materialID);
                    }
                }
            }
            for (int x = 0; x <= resolution; x++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    Vector3Int globalPos = chunkStartPos + new Vector3Int(x, resolution, z);
                    VoxelData data = world.GetVoxel(globalPos);
                    if (data.density >= isoLevel && data.materialID != -1)
                    {
                        uniqueMaterials.Add(data.materialID);
                    }
                }
            }
            for (int y = 0; y <= resolution; y++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    Vector3Int globalPos = chunkStartPos + new Vector3Int(resolution, y, z);
                    VoxelData data = world.GetVoxel(globalPos);
                    if (data.density >= isoLevel && data.materialID != -1)
                    {
                        uniqueMaterials.Add(data.materialID);
                    }
                }
            }
            /*
            // -----------------------------------------------------------------
            // プレスキャン: このチャンク内で使用されているマテリアルID（最大4つ）を収集
            // -----------------------------------------------------------------
            HashSet<int> uniqueMaterials = new HashSet<int>();
            for (int x = 0; x < resolution + 1; x++)
            {
                for (int y = 0; y < resolution + 1; y++)
                {
                    for (int z = 0; z < resolution + 1; z++)
                    {
                        Vector3Int localPos = new Vector3Int(x, y, z);
                        Vector3Int globalPos = chunkStartPos + localPos;
                        int materialID = world.GetCellMaterialID(globalPos, isoLevel);
                        if (materialID != -1)
                        {
                            uniqueMaterials.Add(materialID);
                        }
                    }
                }
            }*/
            List<int> chunkMaterials = new List<int>(uniqueMaterials);
            while (chunkMaterials.Count < 4)
            {
                chunkMaterials.Add(0); // 4スロットになるようにダミー（0）でパディング
            }

            // =================================================================
            // パス 1: 頂点の生成（セル中心に滑らかな頂点を配置）
            // =================================================================
            for (int x = 0; x < resolution + 1; x++)
            {
                for (int y = 0; y < resolution + 1; y++)
                {
                    for (int z = 0; z < resolution + 1; z++)
                    {
                        Vector3Int localPos = new Vector3Int(x, y, z);
                        Vector3Int globalPos = chunkStartPos + localPos;
                        int cubeIndex = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3Int cornerPos = globalPos + ChunkUtility.CornerOffsets[i];
                            _cornerDensities[i] = world.GetVoxel(cornerPos).density;
                            if (_cornerDensities[i] >= isoLevel)
                            {
                                cubeIndex |= (1 << i);
                            }
                        }
                        // セルが完全に内部または外部ならスキップ
                        if (ChunkUtility.edgeTable[cubeIndex] == 0) continue;
                        
                        // 代表マテリアルIDチェック（セルが空でなければ続行）
                        int materialID = world.GetCellMaterialID(globalPos, isoLevel);
                        if (materialID == -1) continue;
                        
                        int crossingCount = 0;
                        for (int i = 0; i < 12; i++)
                        {
                            if ((ChunkUtility.edgeTable[cubeIndex] & (1 << i)) != 0)
                            {
                                Vector3 p1 = globalPos + ChunkUtility.CornerOffsets[ChunkUtility.EdgeConnections[i, 0]];
                                Vector3 p2 = globalPos + ChunkUtility.CornerOffsets[ChunkUtility.EdgeConnections[i, 1]];
                                float d1 = _cornerDensities[ChunkUtility.EdgeConnections[i, 0]];
                                float d2 = _cornerDensities[ChunkUtility.EdgeConnections[i, 1]];
                                float t = (isoLevel - d1) / (d2 - d1);
                                _crossingPoints[crossingCount] = Vector3.Lerp(p1, p2, t);
                                crossingCount++;
                            }
                        }
                        if (crossingCount > 0)
                        {
                            Vector3 vertexPosition = Vector3.zero;
                            for (int i = 0; i < crossingCount; i++)
                            {
                                vertexPosition += _crossingPoints[i];
                            }
                            vertexPosition /= crossingCount;
                            
                            SetVertexMapValue(x, y, z, _vertices.Count, resolution);
                            _vertices.Add((vertexPosition - (Vector3)chunkStartPos) * _voxelSize);

                            // 周囲8つのVoxelコーナーからマテリアル情報を収集してブレンドウェイトを計算する
                            float w0 = 0f, w1 = 0f, w2 = 0f, w3 = 0f;
                            float totalWeight = 0f;
                            int m0 = chunkMaterials[0];
                            int m1 = chunkMaterials[1];
                            int m2 = chunkMaterials[2];
                            int m3 = chunkMaterials[3];
                            for (int i = 0; i < 8; i++)
                            {
                                Vector3Int cornerPos = globalPos + ChunkUtility.CornerOffsets[i];
                                VoxelData cornerData = world.GetVoxel(cornerPos);
                                if (cornerData.density >= isoLevel && cornerData.materialID != -1)
                                {
                                    Vector3 cornerLocalPos = localPos + (Vector3)ChunkUtility.CornerOffsets[i];
                                    float dist = Vector3.Distance(vertexPosition - (Vector3)chunkStartPos, cornerLocalPos);
                                    float w = 1.0f / Mathf.Max(dist, 0.01f);
                                    int matID = cornerData.materialID;
                                    // 4つのスロットのどれに一致するかでウェイトを加算
                                    if (matID == m0) w0 += w;
                                    else if (matID == m1) w1 += w;
                                    else if (matID == m2) w2 += w;
                                    else if (matID == m3) w3 += w;
                                    totalWeight += w;
                                }
                            }
                            // 正規化処理
                            if (totalWeight > 0f)
                            {
                                float sum = w0 + w1 + w2 + w3;
                                if (sum > 0f)
                                {
                                    w0 /= sum;
                                    w1 /= sum;
                                    w2 /= sum;
                                    w3 /= sum;
                                }
                                else
                                {
                                    w0 = 1.0f;
                                }
                            }
                            else
                            {
                                w0 = 1.0f;
                            }
                            // 頂点カラーにウェイトを格納
                            Color vertexColor = new Color(w0, w1, w2, w3);
                            _colors.Add(vertexColor);
                            /*
                            var materialWeights = new Dictionary<int, float>();
                            float totalWeight = 0f;

                            for (int i = 0; i < 8; i++)
                            {
                                Vector3Int cornerPos = globalPos + ChunkUtility.CornerOffsets[i];
                                VoxelData cornerData = world.GetVoxel(cornerPos);

                                // 密度が閾値以上（ソリッド）で、有効なマテリアルIDの場合
                                if (cornerData.density >= isoLevel && cornerData.materialID != -1)
                                {
                                    // コーナーのローカル座標
                                    Vector3 cornerLocalPos = localPos + (Vector3)ChunkUtility.CornerOffsets[i];
                                    float dist = Vector3.Distance(vertexPosition - (Vector3)chunkStartPos, cornerLocalPos);

                                    // 距離の逆数を影響度とする
                                    float w = 1.0f / Mathf.Max(dist, 0.01f);
                                    if (materialWeights.TryGetValue(cornerData.materialID, out float currentWeight))
                                    {
                                        materialWeights[cornerData.materialID] = currentWeight + w;
                                    }
                                    else
                                    {
                                        materialWeights[cornerData.materialID] = w;
                                    }
                                    totalWeight += w;
                                }
                            }

                            // 収集したウェイトを、チャンクの共通4スロットに対応させてColor(R,G,B,A)に格納
                            float w0 = 0f, w1 = 0f, w2 = 0f, w3 = 0f;
                            if (totalWeight > 0f)
                            {
                                materialWeights.TryGetValue(chunkMaterials[0], out w0);
                                materialWeights.TryGetValue(chunkMaterials[1], out w1);
                                materialWeights.TryGetValue(chunkMaterials[2], out w2);
                                materialWeights.TryGetValue(chunkMaterials[3], out w3);

                                // 正規化
                                float sum = w0 + w1 + w2 + w3;
                                if (sum > 0f)
                                {
                                    w0 /= sum;
                                    w1 /= sum;
                                    w2 /= sum;
                                    w3 /= sum;
                                }
                                else
                                {
                                    w0 = 1.0f;
                                }
                            }
                            else
                            {
                                w0 = 1.0f;
                            }

                            // 頂点カラーにウェイトを格納 (R=マテリアル0の重み, G=1, B=2, A=3)
                            Color vertexColor = new Color(w0, w1, w2, w3);
                            _colors.Add(vertexColor);*/
                        }
                    }
                }
            }

            // =================================================================
            // パス 2: 面（三角形）の生成（境界での重複を防ぐため、各軸の面生成権を制限）
            // =================================================================
            for (int x = 0; x < resolution + 1; x++)
            {
                for (int y = 0; y < resolution + 1; y++)
                {
                    for (int z = 0; z < resolution + 1; z++)
                    {
                        Vector3Int p = new Vector3Int(x, y, z);
                        Vector3Int globalP = chunkStartPos + p;
                        bool pIsSolid = world.GetVoxel(globalP).density >= isoLevel;
                        
                        // Z軸に沿った面（Z方向の辺と交差）
                        if (z < resolution)
                        {
                            Vector3Int zNeighbor = globalP + Vector3Int.forward;
                            if (pIsSolid != (world.GetVoxel(zNeighbor).density >= isoLevel))
                            {
                                TryCreateQuad(0, pIsSolid,
                                    new Vector3Int(x, y, z), new Vector3Int(x - 1, y, z),
                                    new Vector3Int(x, y - 1, z), new Vector3Int(x - 1, y - 1, z),
                                    resolution);
                            }
                        }
                        
                        // Y軸に沿った面（Y方向の辺と交差）
                        if (y < resolution)
                        {
                            Vector3Int yNeighbor = globalP + Vector3Int.up;
                            if (pIsSolid != (world.GetVoxel(yNeighbor).density >= isoLevel))
                            {
                                TryCreateQuad(0, pIsSolid,
                                    new Vector3Int(x, y, z), new Vector3Int(x, y, z - 1),
                                    new Vector3Int(x - 1, y, z), new Vector3Int(x - 1, y, z - 1),
                                    resolution);
                            }
                        }
                        
                        // X軸に沿った面（X方向の辺と交差）
                        if (x < resolution)
                        {
                            Vector3Int xNeighbor = globalP + Vector3Int.right;
                            if (pIsSolid != (world.GetVoxel(xNeighbor).density >= isoLevel))
                            {
                                TryCreateQuad(0, pIsSolid,
                                    new Vector3Int(x, y, z), new Vector3Int(x, y - 1, z),
                                    new Vector3Int(x, y, z - 1), new Vector3Int(x, y - 1, z - 1),
                                    resolution);
                            }
                        }
                    }
                }
            }
            BuildMesh(db, chunkMaterials);
            return _vertices.Count > 0;
        }

        private void TryCreateQuad(int materialID, bool reverseWinding, 
            Vector3Int c1, Vector3Int c2, Vector3Int c3, Vector3Int c4, int resolution)
        {
            if (materialID == -1) return;
            int i1 = GetVertexMapValue(c1.x, c1.y, c1.z, resolution);
            int i2 = GetVertexMapValue(c2.x, c2.y, c2.z, resolution);
            int i3 = GetVertexMapValue(c3.x, c3.y, c3.z, resolution);
            int i4 = GetVertexMapValue(c4.x, c4.y, c4.z, resolution);
            
            if (i1 != -1 && i2 != -1 && i3 != -1 && i4 != -1)
            {
                if (!_submeshTriangles.TryGetValue(materialID, out var list))
                {
                    list = new List<int>();
                    _submeshTriangles[materialID] = list;
                }
                if (!reverseWinding)
                {
                    list.Add(i1); list.Add(i3); list.Add(i2);
                    list.Add(i3); list.Add(i4); list.Add(i2);
                }
                else
                {
                    list.Add(i1); list.Add(i2); list.Add(i3);
                    list.Add(i3); list.Add(i2); list.Add(i4);
                }
            }
        }

        private async void BuildMesh(VoxelMaterialDatabase db, List<int> chunkMaterials)
        {
            TryGetComponent<MeshFilter>(out var meshFilter);
            TryGetComponent<MeshRenderer>(out var meshRenderer);
            
            // MeshColliderを自動取得（存在しない場合は追加）
            if (!TryGetComponent<MeshCollider>(out var meshCollider))
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            // 三角形の面データが存在するかチェック
            bool hasTriangles = _submeshTriangles.TryGetValue(0, out var triangles) && triangles != null && triangles.Count >= 3;

            if (_vertices.Count < 3 || !hasTriangles)
            {
                meshFilter.mesh = null;
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = null;
                    meshCollider.enabled = false;
                }
                Debug.Log($"[ChunkRenderer] BuildMesh: 頂点数（{_vertices.Count}）または面データ数が不足しているため、メッシュ構築をスキップします。");
                return;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = _vertices.ToArray();
            mesh.colors = _colors.ToArray(); // 頂点カラーにウェイトを割り当て

            // 4つのマテリアルID情報をすべての頂点の uv2 に一定値として埋め込む（これで補間バグを回避）
            List<Vector4> uv2List = new List<Vector4>();
            Vector4 matIDs = new Vector4(chunkMaterials[0], chunkMaterials[1], chunkMaterials[2], chunkMaterials[3]);
            for (int i = 0; i < _vertices.Count; i++)
            {
                uv2List.Add(matIDs);
            }
            mesh.SetUVs(1, uv2List); // uv2 = TEXCOORD1

            // 単一サブメッシュとして構築
            mesh.subMeshCount = 1;
            mesh.SetTriangles(triangles.ToArray(), 0);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;

            if (db != null && db.MasterMaterial != null)
            {
                meshRenderer.materials = new Material[] { db.MasterMaterial };
                Debug.Log($"[ChunkRenderer] マテリアル適用成功: {db.MasterMaterial.name} (Shader: {db.MasterMaterial.shader.name}) ➔ GameObject: {gameObject.name}");
            }
            else
            {
                meshRenderer.materials = new Material[] { new Material(Shader.Find("Standard")) };
                Debug.LogWarning($"[ChunkRenderer] MasterMaterialが空（Null）です。一時的にStandardシェーダーを適用します ➔ GameObject: {gameObject.name}");
            }

            // --- コライダー物理データの非同期クッキング（メインスレッドのフリーズ防止） ---
            if (meshCollider != null)
            {
                int meshID = mesh.GetInstanceID();
                
                await System.Threading.Tasks.Task.Run(() =>
                {
                    Physics.BakeMesh(meshID, false);
                });

                // クッキング完了後、MeshColliderに適用（メインスレッドで行う）
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = mesh;
                    meshCollider.enabled = true;
                }
            }
        }
    }
}
