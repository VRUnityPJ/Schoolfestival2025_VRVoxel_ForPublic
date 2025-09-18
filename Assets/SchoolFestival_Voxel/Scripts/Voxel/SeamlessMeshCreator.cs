using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Naive Surface Nets 実装（チャンク化対応）
/// - Execute(...) は density スライスを受け取りメッシュを作る
/// - sampleDensity デリゲートを渡すと、三角形向き修正でグローバル密度を参照できる
/// - BuildMeshFromDensity(...) は Execute → Apply を行う便利関数
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SeamlessMeshCreator : MonoBehaviour
{
    public float voxelSize = 1f;
    public float isoLevel = 0f;
    public bool centerMesh = false; // チャンク毎には通常 false に設定する
    public bool addMeshCollider = false;

    const float EPS = 1e-6f;

    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Color32[] colors;
    }

    /// <summary>
    /// 高レベル: density から直接メッシュ作成（sampleDensity を渡してグローバル参照可能）
    /// sampleDensity(posLocal) : posLocal は Execute 内で使われている "頂点座標系" と同じ基準（ワールド単位、density の grid-origin に依存）
    /// </summary>
    public void BuildMeshFromDensity(float[,,] density,Color32[,,] colors,  Func<Vector3, float> sampleDensity = null)
    {
        // MeshData md = Execute(density, isoLevel, voxelSize, centerMesh, sampleDensity);
        MeshData md = Execute(density, colors, isoLevel, voxelSize, centerMesh, sampleDensity);
        ApplyMesh(md);
    }

    /// <summary>
    /// Naive Surface Nets コア
    /// density : [sizeX,sizeY,sizeZ] のスカラー場（>iso が内部）
    /// sampleDensity : (posLocal) -> float, posLocal は Execute が作った頂点と同じ座標系（ワールド単位として解釈可能）
    /// </summary>
    
    public MeshData Execute(float[,,] density,Color32[,,] colors, float iso, float voxelSize = 1f, bool centerMesh = true, Func<Vector3, float> sampleDensity = null)
    {
        int sizeX = density.GetLength(0);
        int sizeY = density.GetLength(1);
        int sizeZ = density.GetLength(2);

        int cx = Mathf.Max(0, sizeX - 1);
        int cy = Mathf.Max(0, sizeY - 1);
        int cz = Mathf.Max(0, sizeZ - 1);
        Color startColor = new Color(0, 0, 0, 0);
        Color errColor = new Color(0, 0, 0, 200);


        Vector3Int[] cornerOffset = new Vector3Int[8] {
            new Vector3Int(0,0,0),
            new Vector3Int(1,0,0),
            new Vector3Int(1,1,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,0,1),
            new Vector3Int(1,0,1),
            new Vector3Int(1,1,1),
            new Vector3Int(0,1,1)
        };

        int[,] edges = new int[12, 2] {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        int[,,] cellIndex = new int[cx, cy, cz];
        for (int i = 0; i < cx; i++)
            for (int j = 0; j < cy; j++)
                for (int k = 0; k < cz; k++)
                    cellIndex[i, j, k] = -1;

        List<Vector3> verts = new List<Vector3>();
        List<Color32> vertColors = new List<Color32>();

        // 1) 各セルに対して1頂点を作る（辺の交点を平均）
        for (int x = 0; x < cx; x++)
        {
            for (int y = 0; y < cy; y++)
            {
                for (int z = 0; z < cz; z++)
                {
                    float[] cval = new float[8];
                    float min = float.MaxValue, max = float.MinValue;
                    for (int c = 0; c < 8; c++)
                    {
                        int ix = x + cornerOffset[c].x;
                        int iy = y + cornerOffset[c].y;
                        int iz = z + cornerOffset[c].z;
                        float v = density[ix, iy, iz] - iso;
                        cval[c] = v;
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }


                    Color avgColor = startColor;
                    
                    int colorSampleCount = 0; // 色を採取した回数をカウント
                    for (int c = 0; c < 8; c++)
                    {
                        int ix = x + cornerOffset[c].x;
                        int iy = y + cornerOffset[c].y;
                        int iz = z + cornerOffset[c].z;

                        // この隅がメッシュの内側かどうかを密度で判断
                        if (density[ix, iy, iz] >= isoLevel)
                        {
                            avgColor += colors[ix, iy, iz];
                            colorSampleCount++;
                        }
                    }

                    if (colorSampleCount > 0)
                    {
                        avgColor /= colorSampleCount; // 採取できた色の数だけで平均する
                    }
                    else
                    {
                        avgColor = errColor;
                    }

                    Vector3 avg = Vector3.zero;
                    int cnt = 0;

                    for (int e = 0; e < 12; e++)
                    {
                        int c0 = edges[e, 0];
                        int c1 = edges[e, 1];
                        float v0 = cval[c0];
                        float v1 = cval[c1];

                        bool neg0 = v0 < 0f, neg1 = v1 < 0f;
                        if (neg0 == neg1) continue;

                        Vector3 p0 = new Vector3(x + cornerOffset[c0].x, y + cornerOffset[c0].y, z + cornerOffset[c0].z);
                        Vector3 p1 = new Vector3(x + cornerOffset[c1].x, y + cornerOffset[c1].y, z + cornerOffset[c1].z);
                        float denom = (v0 - v1);
                        float t = Mathf.Abs(denom) > EPS ? (v0 / denom) : 0.5f;
                        Vector3 ip = p0 + t * (p1 - p0);

                        avg += ip;
                        cnt++;
                    }

                    if (cnt == 0) continue;
                    Vector3 finalGrid = avg / cnt; // グリッド空間（格子単位）
                    Vector3 finalPos = finalGrid * voxelSize; // ワールド単位（ただし origin はこの density スライスの origin）
                    cellIndex[x, y, z] = verts.Count;
                    verts.Add(finalPos);
                    vertColors.Add(avgColor); // ← 計算した色をリストに追加
                }
            }
        }
        
        // 2) 隣接セルをつないで quad -> triangles
        List<int> triangles = new List<int>();
        void AddQuadInt(int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        for (int x = 0; x < cx; x++)
        {
            for (int y = 0; y < cy; y++)
            {
                for (int z = 0; z < cz; z++)
                {
                    int v = cellIndex[x, y, z];
                    if (v < 0) continue;

                    if (x + 1 < cx && y + 1 < cy)
                    {
                        int a = cellIndex[x, y, z];
                        int b = cellIndex[x + 1, y, z];
                        int c = cellIndex[x + 1, y + 1, z];
                        int d = cellIndex[x, y + 1, z];
                        if (a >= 0 && b >= 0 && c >= 0 && d >= 0) AddQuadInt(a, b, c, d);
                    }

                    if (y + 1 < cy && z + 1 < cz)
                    {
                        int a = cellIndex[x, y, z];
                        int b = cellIndex[x, y + 1, z];
                        int c = cellIndex[x, y + 1, z + 1];
                        int d = cellIndex[x, y, z + 1];
                        if (a >= 0 && b >= 0 && c >= 0 && d >= 0) AddQuadInt(a, b, c, d);
                    }

                    if (z + 1 < cz && x + 1 < cx)
                    {
                        int a = cellIndex[x, y, z];
                        int b = cellIndex[x, y, z + 1];
                        int c = cellIndex[x + 1, y, z + 1];
                        int d = cellIndex[x + 1, y, z];
                        if (a >= 0 && b >= 0 && c >= 0 && d >= 0) AddQuadInt(a, b, c, d);
                    }
                }
            }
        }
        
        

        // 中心化オフセット（チャンクでは通常 false にする。true の場合はスライス全体を中心化）
Vector3 offset = Vector3.zero;
        if (centerMesh)
        {
            Vector3 ext = new Vector3(sizeX - 1, sizeY - 1, sizeZ - 1) * voxelSize;
            offset = ext * 0.5f;
            for (int i = 0; i < verts.Count; i++) verts[i] -= offset;
        }

        Vector3[] vArray = verts.ToArray();
        Color32[] cArray = vertColors.ToArray(); // ← 色のリストも配列に変換
        int[] triArray = triangles.ToArray();

        // sampleDensity が渡されない場合はローカル density を使う sampler を自動生成
        Func<Vector3, float> sampler = sampleDensity;
        if (sampler == null)
        {
            sampler = (Vector3 posLocal) =>
            {
                // posLocal は Execute が生成した頂点座標系（voxelSize 単位）と同じもの
                // -> gridPos = posLocal / voxelSize
                Vector3 gridPos = posLocal / voxelSize;
                // trilinear sample within density bounds
                return TrilinearSampleLocal(density, gridPos);
            };
        }

        // 三角形の向きを density 勾配に合わせて修正
        FixWindingByGradient(ref vArray, ref triArray, sampler, voxelSize);

        // 平滑法線を計算
        Vector3[] normals = ComputeVertexNormals(vArray, triArray);

        // return new MeshData { vertices = vArray, triangles = triArray, normals = normals };
        return new MeshData { vertices = vArray, triangles = triArray, normals = normals, colors = cArray };
    }

    void ApplyMesh(MeshData md)
    {
        if (md.vertices == null || md.vertices.Length == 0)
        {
            Debug.LogWarning("MeshCreator: empty mesh, skip apply.");
            return;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null) mr.sharedMaterial = new Material(Shader.Find("Standard"));

        Mesh mesh = new Mesh();
        if (md.vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = md.vertices;
        mesh.triangles = md.triangles;
        mesh.colors32 = md.colors; // ← この行を追加
        if (md.normals != null && md.normals.Length == md.vertices.Length) mesh.normals = md.normals;
        else mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        if (addMeshCollider)
        {
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }
    }

    // ---------------------------------
    // 補助: ローカル density に対する三次補間
    // gridPos : 浮動のグリッド座標
    // ---------------------------------
    float TrilinearSampleLocal(float[,,] density, Vector3 gridPos)
    {
        int sx = density.GetLength(0);
        int sy = density.GetLength(1);
        int sz = density.GetLength(2);

        float x = gridPos.x; float y = gridPos.y; float z = gridPos.z;
        if (x < 0f || y < 0f || z < 0f || x > sx - 1 || y > sy - 1 || z > sz - 1)
        {
            // outside -> treat as very negative (outside)
            return -100000f;
        }

        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        int iz = Mathf.FloorToInt(z);
        float fx = x - ix, fy = y - iy, fz = z - iz;
        int ix1 = Mathf.Min(ix + 1, sx - 1), iy1 = Mathf.Min(iy + 1, sy - 1), iz1 = Mathf.Min(iz + 1, sz - 1);

        float c000 = density[ix, iy, iz];
        float c100 = density[ix1, iy, iz];
        float c010 = density[ix, iy1, iz];
        float c110 = density[ix1, iy1, iz];
        float c001 = density[ix, iy, iz1];
        float c101 = density[ix1, iy, iz1];
        float c011 = density[ix, iy1, iz1];
        float c111 = density[ix1, iy1, iz1];

        float c00 = Mathf.Lerp(c000, c100, fx);
        float c10 = Mathf.Lerp(c010, c110, fx);
        float c01 = Mathf.Lerp(c001, c101, fx);
        float c11 = Mathf.Lerp(c011, c111, fx);

        float c0 = Mathf.Lerp(c00, c10, fy);
        float c1 = Mathf.Lerp(c01, c11, fy);

        return Mathf.Lerp(c0, c1, fz);
    }

    // ---------------------------------
    // FixWinding: density 勾配を使って面の表裏を揃える
    // sampler(posLocal) -> density
    // ---------------------------------
    void FixWindingByGradient(ref Vector3[] verts, ref int[] tris, Func<Vector3, float> sampler, float voxelSize)
    {
        if (sampler == null) return;
        float delta = 0.5f * voxelSize; // central difference step

        for (int t = 0; t + 2 < tris.Length; t += 3)
        {
            int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
            if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;

            Vector3 p0 = verts[i0], p1 = verts[i1], p2 = verts[i2];
            Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
            if (faceNormal.sqrMagnitude < 1e-12f) continue;
            faceNormal.Normalize();

            Vector3 centroid = (p0 + p1 + p2) / 3f;

            float sx1 = sampler(centroid + new Vector3(delta, 0, 0));
            float sx0 = sampler(centroid - new Vector3(delta, 0, 0));
            float sy1 = sampler(centroid + new Vector3(0, delta, 0));
            float sy0 = sampler(centroid - new Vector3(0, delta, 0));
            float sz1 = sampler(centroid + new Vector3(0, 0, delta));
            float sz0 = sampler(centroid - new Vector3(0, 0, delta));

            Vector3 grad = new Vector3((sx1 - sx0) * 0.5f / delta, (sy1 - sy0) * 0.5f / delta, (sz1 - sz0) * 0.5f / delta);
            if (grad.sqrMagnitude < 1e-12f) continue;
            grad.Normalize();

            Vector3 desiredOut = -grad; // interior -> exterior
            float d = Vector3.Dot(faceNormal, desiredOut);
            if (d < 0f)
            {
                // flip triangle
                int tmp = tris[t + 1];
                tris[t + 1] = tris[t + 2];
                tris[t + 2] = tmp;
            }
        }
    }

    // ---------------------------------
    // Vertex normals: adjacent面の平均
    // ---------------------------------
    Vector3[] ComputeVertexNormals(Vector3[] verts, int[] tris)
    {
        Vector3[] normals = new Vector3[verts.Length];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.zero;

        for (int t = 0; t + 2 < tris.Length; t += 3)
        {
            int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
            if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;

            Vector3 v0 = verts[i0], v1 = verts[i1], v2 = verts[i2];
            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
            if (faceNormal.sqrMagnitude < 1e-12f) continue;
            faceNormal.Normalize();

            normals[i0] += faceNormal;
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            if (normals[i].sqrMagnitude > 1e-12f) normals[i].Normalize();
            else normals[i] = Vector3.up;
        }
        return normals;
    }
}
