// Assets/Scripts/ChunkManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using VoxReader;
using VoxReader.Interfaces;
using UnityEngine;
using R3;
using Vector3 = UnityEngine.Vector3;
/// <summary>
/// ChunkManager（シーム生成対応版）
/// - グローバル密度をチャンクに分割して描画
/// - 破壊時は該当チャンクだけ再メッシュ化
/// - さらに、隣接チャンク間の“つなぎ目”を埋めるシームメッシュを自動生成/更新
/// 
/// ポイント：
///   * シームは X/Y/Z それぞれの隣接ペアに対して独立した小さなメッシュとして生成
///   * 頂点位置はグローバル密度から Surface Nets と同じ規則で算出（平均交点）
///   * 生成後に密度勾配ベースで三角形の向きを補正し、滑らかな法線を計算
/// </summary>
public class SeamlessChunkManager : MonoBehaviour
{
    [Header("Chunk / Grid")]
    public int chunkResolution = 16; // cells per chunk
    public int chunksX = 2;
    public int chunksY = 1;
    public int chunksZ = 2;
    public float voxelSize = 0.5f;
    public float isoLevel = 0f;

    [Header("Initial shape")]
    public bool useTorus = true;
    public float torusOuterR = 6f;
    public float torusInnerR = 2f;

    [Header("Rendering")]
    public Material chunkMaterial;

    [Header("MagicaVoxel")]
    public bool useMagicaVoxelModel = false;
    public string magicaVoxelFileName = "my_model.vox"; 
    public bool useMagicaVoxelModelMode;
    public event Action OnVoxelRecreated;

    // ====== グローバル密度 ======
    float[,,] globalDensity;
    Color32[,,] globalColors; // ← この行を追加
    int globalSizeX, globalSizeY, globalSizeZ;

    // ====== チャンク情報 ======
    class Chunk
    {
        public int ix, iy, iz;             // チャンク座標
        public int startX, startY, startZ; // グローバル密度の開始インデックス
        public GameObject go;
        public SeamlessMeshCreator mc;
    }
    Chunk[,,] chunks;

    
    void Start()
    {
        if (useMagicaVoxelModelMode)
        {
            BuildGlobalDensity();
        }
        else
        {
            BuildGlobalDensity_Default();
        }
        CreateChunks();
        
    }

    // -----------------------------
    // 初期密度の構築
    // -----------------------------
    void BuildGlobalDensity_Default()
    {
        globalSizeX = chunksX * chunkResolution + 1;
        globalSizeY = chunksY * chunkResolution + 1;
        globalSizeZ = chunksZ * chunkResolution + 1;

        if (useTorus)
        {
            // 便宜上、立方グリッドで生成し、必要なら中央寄せコピー
            int size = Mathf.Max(globalSizeX, Mathf.Max(globalSizeY, globalSizeZ));
            var tmp = BreakableShapeGenerator.GenerateTorusDensity(size, torusOuterR, torusInnerR, voxelSize);
            globalDensity = new float[globalSizeX, globalSizeY, globalSizeZ];
            int offX = (globalSizeX - size) / 2;
            int offY = (globalSizeY - size) / 2;
            int offZ = (globalSizeZ - size) / 2;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                    {
                        int nx = x + offX, ny = y + offY, nz = z + offZ;
                        if (nx >= 0 && nx < globalSizeX && ny >= 0 && ny < globalSizeY && nz >= 0 && nz < globalSizeZ)
                            globalDensity[nx, ny, nz] = tmp[x, y, z];
                    }
        }
        else
        {
            globalDensity = BreakableShapeGenerator.GenerateCubeDensity(globalSizeX, globalSizeY, globalSizeZ, chunkResolution / 2);
        }
    }
    // -----------------------------
    // 初期密度の構築
    // -----------------------------
    // -----------------------------
    // 初期密度の構築（MagicaVoxel対応版）
    // -----------------------------
    void BuildGlobalDensity()
    {
        if (useMagicaVoxelModel)
        {
            // StreamingAssetsフォルダからのパスを構築
            string filePath = Path.Combine(Application.streamingAssetsPath, magicaVoxelFileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"MagicaVoxelファイルが見つかりません: {filePath}");
                // ファイルがない場合は、デフォルトの形状生成に切り替える
                BuildGlobalDensity_Default();
                return;
            }

            // --- ステップ1: .vox ファイルを読み込む ---
            IVoxFile voxFileContent = VoxReader.VoxReader.Read(filePath);
            if (voxFileContent == null || voxFileContent.Models.Length == 0)
            {
                Debug.LogError("MagicaVoxelファイルの読み込みに失敗したか、モデルが含まれていません。");
                BuildGlobalDensity_Default();
                return;
            }

            // 最初のモデルを使用する
            IModel model = voxFileContent.Models[0];
            
            // --- ステップ2: バイナリ密度データを作成 ---
            globalSizeX = (int)model.LocalSize.X;
            globalSizeY = (int)model.LocalSize.Y;
            globalSizeZ = (int)model.LocalSize.Z;
            
            // 「ある(1.0f)」「ない(0.0f)」だけの配列を作る
            float[,,] binaryDensity = new float[globalSizeX, globalSizeY, globalSizeZ];
            globalColors = new Color32[globalSizeX, globalSizeY, globalSizeZ]; // ← 配列を初期化
            // デフォルトの色を黒に設定（ボクセルがない場所の色）
            Color32 defaultColor = new Color32(0, 0, 0, 255);
            for (int x = 0; x < globalSizeX; x++)
            for (int y = 0; y < globalSizeY; y++)
            for (int z = 0; z < globalSizeZ; z++)
            {
                globalColors[x, y, z] = defaultColor;
            }

            foreach (Voxel voxel in model.Voxels)
            {
                // 注意: MagicaVoxelとUnityの座標系が違う場合、軸の入れ替えが必要
                // MagicaVoxel: Yが上、Zが奥
                // Unity:       Yが上、Zが奥
                // 基本的には一致しますが、モデルの向きが90度違う場合などは
                // YとZを入れ替える(binaryDensity[voxel.X, voxel.Z, voxel.Y] = 1.0f;)などを試してください。
                //binaryDensity[voxel.X, voxel.Y, voxel.Z] = 1.0f;
                int x = (int)voxel.LocalPosition.X;
                int y = (int)voxel.LocalPosition.Y;
                int z = (int)voxel.LocalPosition.Z;

                binaryDensity[x, y, z] = 1.0f;
                // VoxReaderのColorはbyte(0-255)なので、Color32に直接変換できる
                globalColors[x, y, z] = new Color32(voxel.Color.R, voxel.Color.G, voxel.Color.B, 255);
            }

            Debug.Log($"MagicaVoxelモデルを読み込みました。サイズ: ({globalSizeX}, {globalSizeY}, {globalSizeZ})");

            // --- ステップ3: 密度データの平滑化（ブラー処理） ---
            // ※このApplyGaussianBlurメソッドは、後述のものをChunkManager.cs内に追加してください。
            // 2回目のブラーをかけるとより滑らかになります。回数や強度は見た目を見ながら調整してください。
            float[,,] blurredDensity = ApplyGaussianBlur(binaryDensity, 2, 1.5f);
            blurredDensity = ApplyGaussianBlur(blurredDensity, 2, 1.5f);

            // --- ステップ4: 密度データの最終調整 ---
            globalDensity = new float[globalSizeX, globalSizeY, globalSizeZ];
            for (int x = 0; x < globalSizeX; x++)
            for (int y = 0; y < globalSizeY; y++)
            for (int z = 0; z < globalSizeZ; z++)
            {
                // isoLevel(0f)を基準にするため、値の範囲を -0.5f ～ 0.5f にシフト
                globalDensity[x, y, z] = blurredDensity[x, y, z] - 0.5f;
            }

            // 読み込んだ密度データからチャンク数を再計算
            chunksX = Mathf.CeilToInt((float)(globalSizeX - 1) / chunkResolution);
            chunksY = Mathf.CeilToInt((float)(globalSizeY - 1) / chunkResolution);
            chunksZ = Mathf.CeilToInt((float)(globalSizeZ - 1) / chunkResolution);
        }
        else 
        {
            BuildGlobalDensity_Default();
        }
    }

    // -----------------------------
    // チャンクの生成
    // -----------------------------
    void CreateChunks()
    {
        chunks = new Chunk[chunksX, chunksY, chunksZ];
        for (int cx = 0; cx < chunksX; cx++)
            for (int cy = 0; cy < chunksY; cy++)
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    int startX = cx * chunkResolution;
                    int startY = cy * chunkResolution;
                    int startZ = cz * chunkResolution;

                    const int margin = 1;
                    // 取得範囲を前後にmargin分広げる
                    int extractStartX = startX - margin;
                    int extractStartY = startY - margin;
                    int extractStartZ = startZ - margin;
                    int extractSize = chunkResolution + 1 + (margin * 2);

                    float[,,] local = ExtractDensitySlice(globalDensity, extractStartX, extractStartY, extractStartZ, extractSize);
                    Color32[,,] localColors = ExtractColorSlice(globalColors, extractStartX, extractStartY, extractStartZ, extractSize); // ← 色データを抽出

                    // float[,,] local = ExtractDensitySlice(globalDensity, startX, startY, startZ, chunkResolution + 1);

                    GameObject go = new GameObject($"chunk_{cx}_{cy}_{cz}");
                    go.transform.parent = this.transform;
                    go.transform.localPosition = new Vector3(startX * voxelSize, startY * voxelSize, startZ * voxelSize);
                    go.transform.localRotation = Quaternion.identity;

                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = chunkMaterial != null ? chunkMaterial : new Material(Shader.Find("Standard"));

                    var mc = go.AddComponent<SeamlessMeshCreator>();
                    mc.voxelSize = voxelSize;
                    mc.isoLevel = isoLevel;
                    mc.centerMesh = false;
                    mc.addMeshCollider = true;

                    // mc (BreakableMeshCreator) に渡すsamplerの基準座標も修正が必要
                    Func<Vector3, float> sampler = (Vector3 posLocal) =>
                    {
                        // posLocalは、extractしたスライスのローカル座標なので、
                        // グローバル座標に変換するには extractStart を足す
                        Vector3 gridGlobal = (posLocal / voxelSize) + new Vector3(extractStartX, extractStartY, extractStartZ);
                        return TrilinearSampleGlobal(globalDensity, gridGlobal);
                    };

                    // mc.BuildMeshFromDensity(local, sampler);
                    mc.BuildMeshFromDensity(local, localColors, sampler); // ← 色データを渡す

                    chunks[cx, cy, cz] = new Chunk
                    {
                        ix = cx,
                        iy = cy,
                        iz = cz,
                        startX = startX,
                        startY = startY,
                        startZ = startZ,
                        go = go,
                        mc = mc
                    };
                }
        OnVoxelRecreated?.Invoke();
    }

    // -----------------------------
    // シームの作成/更新
    // -----------------------------



    // -----------------------------
    // チャンク再生成（※シームも更新）
    // -----------------------------
    public void RebuildChunk(int cx, int cy, int cz)
    {
        if (cx < 0 || cy < 0 || cz < 0 || cx >= chunksX || cy >= chunksY || cz >= chunksZ) return;
        Chunk c = chunks[cx, cy, cz];
        if (c == null) return;
        const int margin = 1;
        // 取得範囲を前後にmargin分広げる
        int extractStartX = c.startX - margin;
        int extractStartY = c.startY - margin;
        int extractStartZ = c.startZ - margin;
        int extractSize = chunkResolution + 1 + (margin * 2);

        float[,,] local = ExtractDensitySlice(globalDensity, extractStartX, extractStartY, extractStartZ, extractSize);
        Color32[,,] localColors = ExtractColorSlice(globalColors, extractStartX, extractStartY, extractStartZ, extractSize); // ← 色データを抽出

        // float[,,] local = ExtractDensitySlice(globalDensity, c.startX, c.startY, c.startZ, chunkResolution + 1);
        Func<Vector3, float> sampler = (Vector3 posLocal) =>
        {
            // posLocalは、extractしたスライスのローカル座標なので、
            // グローバル座標に変換するには extractStart を足す
            Vector3 gridGlobal = (posLocal / voxelSize) + new Vector3(extractStartX, extractStartY, extractStartZ);
            return TrilinearSampleGlobal(globalDensity, gridGlobal);
        };
        // c.mc.BuildMeshFromDensity(local, sampler);
        c.mc.BuildMeshFromDensity(local, localColors, sampler); // ← 色データを渡す

        OnVoxelRecreated?.Invoke();

    }

    public void RebuildChunks(IEnumerable<Tuple<int,int,int>> chunkIndices, bool spreadOverFrames = true)
    {
        if (!spreadOverFrames)
        {
            foreach (var t in chunkIndices) RebuildChunk(t.Item1, t.Item2, t.Item3);
        }
        else
        {
            StartCoroutine(RebuildChunksCoroutine(chunkIndices));
        }
    }

    System.Collections.IEnumerator RebuildChunksCoroutine(IEnumerable<Tuple<int,int,int>> chunkIndices)
    {
        foreach (var t in chunkIndices)
        {
            RebuildChunk(t.Item1, t.Item2, t.Item3);
            yield return null; // 1フレームに1チャンク
        }
    }

    // -----------------------------
    // 補助：局所スライス抽出 / 三重線形補間
    // -----------------------------
    float[,,] ExtractDensitySlice(float[,,] src, int startX, int startY, int startZ, int len)
    {
        float[,,] outd = new float[len, len, len];
        int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
        for (int x = 0; x < len; x++)
        for (int y = 0; y < len; y++)
        for (int z = 0; z < len; z++)
        {
            int gx = startX + x;
            int gy = startY + y;
            int gz = startZ + z;
            if (gx >= 0 && gx < sx && gy >= 0 && gy < sy && gz >= 0 && gz < sz)
                outd[x, y, z] = src[gx, gy, gz];
            else
                outd[x, y, z] = -100000f;
        }
        return outd;
    }

    float TrilinearSampleGlobal(float[,,] src, UnityEngine.Vector3 gridPos)
    {
        int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
        float x = gridPos.x, y = gridPos.y, z = gridPos.z;
        if (x < 0f || y < 0f || z < 0f || x > sx - 1 || y > sy - 1 || z > sz - 1)
            return -100000f;

        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        int iz = Mathf.FloorToInt(z);
        float fx = x - ix, fy = y - iy, fz = z - iz;

        int ix1 = Mathf.Min(ix + 1, sx - 1), iy1 = Mathf.Min(iy + 1, sy - 1), iz1 = Mathf.Min(iz + 1, sz - 1);

        float c000 = src[ix, iy, iz];
        float c100 = src[ix1, iy, iz];
        float c010 = src[ix, iy1, iz];
        float c110 = src[ix1, iy1, iz];
        float c001 = src[ix, iy, iz1];
        float c101 = src[ix1, iy, iz1];
        float c011 = src[ix, iy1, iz1];
        float c111 = src[ix1, iy1, iz1];

        float c00 = Mathf.Lerp(c000, c100, fx);
        float c10 = Mathf.Lerp(c010, c110, fx);
        float c01 = Mathf.Lerp(c001, c101, fx);
        float c11 = Mathf.Lerp(c011, c111, fx);

        float c0 = Mathf.Lerp(c00, c10, fy);
        float c1 = Mathf.Lerp(c01, c11, fy);

        return Mathf.Lerp(c0, c1, fz);
    }
    // ↓↓ このヘルパーメソッドを ChunkManager.cs のクラスの末尾などに追加してください ↓↓
    
    /// <summary>
    /// 3D密度データにガウシアンブラーを適用して滑らかにする
    /// </summary>
    /// <param name="src">元の密度データ</param>
    /// <param name="radius">ブラーを適用する半径</param>
    /// <param name="strength">ブラーの強さ</param>
    /// <returns>ブラー適用後の密度データ</returns>
    float[,,] ApplyGaussianBlur(float[,,] src, int radius, float strength)
    {
        int sizeX = src.GetLength(0);
        int sizeY = src.GetLength(1);
        int sizeZ = src.GetLength(2);
        float[,,] dest = new float[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++) {
        for (int y = 0; y < sizeY; y++) {
        for (int z = 0; z < sizeZ; z++) {
            float total = 0f;
            float weightSum = 0f;
            for (int dx = -radius; dx <= radius; dx++) {
            for (int dy = -radius; dy <= radius; dy++) {
            for (int dz = -radius; dz <= radius; dz++) {
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;
                if (nx >= 0 && nx < sizeX && ny >= 0 && ny < sizeY && nz >= 0 && nz < sizeZ)
                {
                    float distSq = dx * dx + dy * dy + dz * dz;
                    float weight = Mathf.Exp(-distSq / (2 * strength * strength));
                    total += src[nx, ny, nz] * weight;
                    weightSum += weight;
                }
            }}}
            if (weightSum > 0) dest[x, y, z] = total / weightSum;
        }}}
        return dest;
    }
    // ChunkManager.cs の末尾などに追加

    Color32[,,] ExtractColorSlice(Color32[,,] src, int startX, int startY, int startZ, int len)
    {
        Color32[,,] outd = new Color32[len, len, len];
        int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
        for (int x = 0; x < len; x++)
            for (int y = 0; y < len; y++)
                for (int z = 0; z < len; z++)
                {
                    int gx = startX + x;
                    int gy = startY + y;
                    int gz = startZ + z;
                    if (gx >= 0 && gx < sx && gy >= 0 && gy < sy && gz >= 0 && gz < sz)
                        outd[x, y, z] = src[gx, gy, gz];
                    else
                        outd[x, y, z] = new Color32(0, 0, 0, 255);
                }
        return outd;
    }



    // ====== 公開 API（Destroyer から使う） ======
    public float[,,] GetGlobalDensity() => globalDensity;
    public int GetChunkResolution() => chunkResolution;
    public int GetChunksX() => chunksX;
    public int GetChunksY() => chunksY;
    public int GetChunksZ() => chunksZ;
    /// <summary>
    /// 指定された範囲内のすべてのボクセルが「空」（密度がisoLevel未満）であるかを判定
    /// </summary>
    /// <param name="startPoint">チェックを開始するグローバルグリッド座標</param>
    /// <param name="range">チェックする範囲（サイズ）</param>
    /// <returns>すべて空ならtrue、一つでも詰まっていればfalseを返す</returns>
    public bool IsAllEmpty(Vector3Int startPoint, Vector3Int range)
    {
        // 指定された範囲をループしてチェック
        for (int x = 0; x < range.x; x++)
        {
            for (int y = 0; y < range.y; y++)
            {
                for (int z = 0; z < range.z; z++)
                {
                    // チェック対象のグローバル座標を計算
                    int checkX = startPoint.x + x;
                    int checkY = startPoint.y + y;
                    int checkZ = startPoint.z + z;

                    // --- 境界チェック ---
                    // もしチェック対象が密度データの範囲外なら、そこは「空」として扱う
                    if (checkX < 0 || checkX >= globalSizeX ||
                        checkY < 0 || checkY >= globalSizeY ||
                        checkZ < 0 || checkZ >= globalSizeZ)
                    {
                        continue; // 次のボクセルへ
                    }

                    // --- 密度チェック ---
                    // 密度がisoLevel以上（＝固形物）のボクセルが一つでも見つかった場合
                    if (globalDensity[checkX, checkY, checkZ] >= isoLevel)
                    {
                        return false; // その時点で「すべて空ではない」と確定。即座にfalseを返す
                    }
                }
            }
        }

        // ループをすべて完走した場合、固形物は一つも見つからなかった
        return true; // よって「すべて空である」と確定。trueを返す
    }
}
