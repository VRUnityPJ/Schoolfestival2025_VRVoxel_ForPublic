using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolFestival_Voxel.Scripts.Voxel.Remake;
using VoxReader;
using VoxReader.Interfaces;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public struct VoxelData
{
    public float density;
    public int materialID;
}
[System.Serializable]
public struct ColorMaterialPair
{
    public Color32 color;
    public Material material;
}
public class ChunkManager : MonoBehaviour
{
    [Header("Chunk / Grid")]
    public int chunkResolution = 16;
    public float voxelSize = 1.0f;
    
    [Header("MagicaVoxel")]
    public string magicaVoxelFileName = "my_model.vox";

    [Header("Materials")]
    // MagicaVoxelのRGB値とマテリアルをここで紐づけます
    public List<ColorMaterialPair> colorMaterialPairs;

    // ====== チャンク情報 ======
    private readonly Dictionary<Vector3Int, ChunkRenderer> chunkDic = new Dictionary<Vector3Int, ChunkRenderer>();

    // ====== グローバルVoxelデータ ======
    public VoxelData[,,] globalVoxelData;
    private int globalSizeX, globalSizeY, globalSizeZ;
    
    // 実行時に使用するマテリアル関連のデータ
    private Material[] _activeMaterials; // 実際にモデルで使われるマテリアルの配列
    private Dictionary<int, int> _paletteIndexToSubmeshIndexMap; // .voxパレットID -> サブメッシュID の対応表

    void Start()
    {
        LoadVoxelModel();
        CreateChunks();
    }

    private void LoadVoxelModel()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, magicaVoxelFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"MagicaVoxelファイルが見つかりません: {filePath}");
            return;
        }
        IVoxFile voxFileContent = VoxReader.VoxReader.Read(filePath);
        if (voxFileContent == null || voxFileContent.Models.Length == 0)
        {
            Debug.LogError("MagicaVoxelファイルの読み込みに失敗したか、モデルが含まれていません。");
            return;
        }
        IModel model = voxFileContent.Models[0];
        
        // --- ここからマテリアルのマッピング処理 ---
        
        var colorToMaterialLookup = colorMaterialPairs.ToDictionary(p => p.color, p => p.material);
        var paletteIndexToMaterial = new Dictionary<int, Material>();

        // ★ 修正点 1 & 2: Palette.Colors を使用してループとインデックスアクセスを行う
        for (int i = 0; i < voxFileContent.Palette.Colors.Length; i++)
        {
            var voxColor = voxFileContent.Palette.Colors[i];
            var unityColor = new Color32(voxColor.R, voxColor.G, voxColor.B, 255);

            if (colorToMaterialLookup.TryGetValue(unityColor, out Material mat))
            {
                paletteIndexToMaterial[i] = mat; // MagicaVoxelのパレットIDは1-based
            }
            Debug.Log(voxColor);
        }

        // ★ デバッグログを追加
        if (paletteIndexToMaterial.Count == 0)
        {
            Debug.LogWarning("モデルの色に対応するマテリアルが一つも見つかりませんでした。Inspectorの[Color Material Pairs]の設定、または[Color Match Threshold]の値を確認してください。");
            // 処理を中断しないために、空のリストで続行する
            _activeMaterials = new Material[0];
            _paletteIndexToSubmeshIndexMap = new Dictionary<int, int>();
        }

        var distinctMaterials = paletteIndexToMaterial.Values.Distinct().ToList();
        _activeMaterials = distinctMaterials.ToArray();

        var materialToSubmeshIndex = distinctMaterials
            .Select((m, i) => new { Material = m, Index = i })
            .ToDictionary(x => x.Material, x => x.Index);

        _paletteIndexToSubmeshIndexMap = new Dictionary<int, int>();
        foreach (var pair in paletteIndexToMaterial)
        {
            _paletteIndexToSubmeshIndexMap[pair.Key] = materialToSubmeshIndex[pair.Value];
        }

        // --- Voxelデータに情報を格納 ---
        globalSizeX = (int)model.LocalSize.X;
        globalSizeY = (int)model.LocalSize.Y;
        globalSizeZ = (int)model.LocalSize.Z;
        globalVoxelData = new VoxelData[globalSizeX, globalSizeY, globalSizeZ];

        foreach (Voxel voxel in model.Voxels)
        {
            int x = (int)voxel.LocalPosition.X;
            int y = (int)voxel.LocalPosition.Z;
            int z = (int)voxel.LocalPosition.Y;
            
            if (x < 0 || x >= globalSizeX || y < 0 || y >= globalSizeY || z < 0 || z >= globalSizeZ) continue;

            if (_paletteIndexToSubmeshIndexMap.TryGetValue(voxel.ColorIndex, out int submeshIndex))
            {
                globalVoxelData[x, y, z] = new VoxelData { density = 1.0f, materialID = submeshIndex };
            }
            else
            {
                // 保険で１番最初のマテリアルを設定
                globalVoxelData[x, y, z] = new VoxelData { density = 1.0f, materialID = 0 };
            }
        }
        Debug.Log($"MagicaVoxelモデルを読み込みました。サイズ: ({globalSizeX}, {globalSizeY}, {globalSizeZ})");
    }

    void CreateChunks()
    {
        if (globalVoxelData == null) return;
        int chunksX = Mathf.CeilToInt((float)globalSizeX / chunkResolution);
        int chunksY = Mathf.CeilToInt((float)globalSizeY / chunkResolution);
        int chunksZ = Mathf.CeilToInt((float)globalSizeZ / chunkResolution);

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                for (int z = 0; z < chunksZ; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    Vector3Int chunkStartPos = new Vector3Int(x * chunkResolution, y * chunkResolution, z * chunkResolution);

                    GameObject chunkObject = new GameObject($"Chunk ({x}, {y}, {z})");
                    chunkObject.transform.parent = this.transform;
                    chunkObject.transform.position = (Vector3)chunkStartPos * voxelSize;
                    
                    ChunkRenderer chunkRenderer = chunkObject.AddComponent<ChunkRenderer>();
                    
                    // chunkRenderer.GenerateMarchingCubesMesh(globalVoxelData, chunkStartPos, chunkResolution, voxelSize, _activeMaterials);
                    chunkRenderer.GenerateSurfaceNetsMesh(globalVoxelData, chunkStartPos, chunkResolution, voxelSize, _activeMaterials);
                    chunkDic.Add(chunkCoord, chunkRenderer);
                }
            }
        }
    }

    public VoxelData[,,] GetGlobalVoxelData()
    {
        return globalVoxelData;
    }

    public int GetChunkResolution()
    {
        return chunkResolution;
    }
    public bool IsSolid(Vector3Int point)
    {
        // --- 境界チェック ---
        // もしチェック対象が密度データの範囲外なら、そこは「空」として扱う
        if (point.x < 0 || point.x >= globalSizeX ||
            point.y < 0 || point.y >= globalSizeY ||
            point.z < 0 || point.z >= globalSizeZ)
        {
            return false; // 
        }
        if (globalVoxelData[point.x, point.y, point.z].density > 0)
        {
            //1なのでVoxelが存在している
            return true;
        }
        return false;
    }

    public void RebuildChunks(HashSet<Tuple<int,int,int>> affectedChunks)
    {
        foreach (var chunkTuple in affectedChunks)
        {
            Vector3Int chunkCoord = new Vector3Int(chunkTuple.Item1, chunkTuple.Item2, chunkTuple.Item3);
            if (chunkDic.TryGetValue(chunkCoord, out ChunkRenderer chunkRenderer))
            {
                Vector3Int chunkStartPos = new Vector3Int(chunkCoord.x * chunkResolution, chunkCoord.y * chunkResolution, chunkCoord.z * chunkResolution);
                chunkRenderer.GenerateSurfaceNetsMesh(globalVoxelData, chunkStartPos, chunkResolution, voxelSize, _activeMaterials);
            }
        }
    }
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
                    if (globalVoxelData[checkX, checkY, checkZ].density >= 0)
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

