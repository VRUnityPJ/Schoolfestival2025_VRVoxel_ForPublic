using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchoolFestival_Voxel.Scripts.Voxel.Remake;
using VoxReader;
using VoxReader.Interfaces;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public readonly struct VoxelData
{
    public readonly float density;
    public readonly int materialID;

    public VoxelData(float density, int materialID)
    {
        this.density = density;
        this.materialID = materialID;
    }
}
[System.Serializable]
public class ColorMaterialPair
{
    public Color32 color;
    public Material material;
}
public class ChunkManager : MonoBehaviour
{
    [Header("Chunk / Grid")]
    [SerializeField]
    private int chunkResolution = 16;
    public float voxelSize = 1.0f;
    
    [Header("MagicaVoxel")]
    [SerializeField]
    private string magicaVoxelFileName = "my_model.vox";

    /// <summary>
    /// MagicaVoxelのRGB値とマテリアルをここで紐づけます
    /// </summary>
    [Header("Materials")]
    [SerializeField]
    private List<ColorMaterialPair> _colorMaterialPairs;

    // ====== チャンク情報 ======
    private readonly Dictionary<Vector3Int, ChunkRenderer> _chunkDic = new Dictionary<Vector3Int, ChunkRenderer>();

    // ====== グローバルVoxelデータ ======
    private VoxelData[,,] _globalVoxelData;
    private int _globalSizeX, _globalSizeY, _globalSizeZ;
    
    // 実行時に使用するマテリアル関連のデータ
    private Material[] _activeMaterials; // 実際にモデルで使われるマテリアルの配列
    private Dictionary<int, int> _paletteIndexToSubMeshIndexMap; // .voxパレットID -> サブメッシュID の対応表

    private void Start()
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
        
        var colorToMaterialLookup = _colorMaterialPairs.ToDictionary(p => p.color, p => p.material);
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
            _paletteIndexToSubMeshIndexMap = new Dictionary<int, int>();
        }

        var distinctMaterials = paletteIndexToMaterial.Values.Distinct().ToList();
        _activeMaterials = distinctMaterials.ToArray();

        var materialToSubmeshIndex = distinctMaterials
            .Select((m, i) => new { Material = m, Index = i })
            .ToDictionary(x => x.Material, x => x.Index);

        _paletteIndexToSubMeshIndexMap = new Dictionary<int, int>();
        foreach (var pair in paletteIndexToMaterial)
        {
            _paletteIndexToSubMeshIndexMap[pair.Key] = materialToSubmeshIndex[pair.Value];
        }

        // --- Voxelデータに情報を格納 ---
        _globalSizeX = (int)model.LocalSize.X;
        _globalSizeY = (int)model.LocalSize.Y;
        _globalSizeZ = (int)model.LocalSize.Z;
        _globalVoxelData = new VoxelData[_globalSizeX, _globalSizeY, _globalSizeZ];

        foreach (Voxel voxel in model.Voxels)
        {
            int x = (int)voxel.LocalPosition.X;
            int y = (int)voxel.LocalPosition.Z;
            int z = (int)voxel.LocalPosition.Y;
            
            if (x < 0 || x >= _globalSizeX || y < 0 || y >= _globalSizeY || z < 0 || z >= _globalSizeZ) continue;

            if (_paletteIndexToSubMeshIndexMap.TryGetValue(voxel.ColorIndex, out int subMeshIndex))
            {
                _globalVoxelData[x, y, z] = new VoxelData(1, subMeshIndex);
            }
            else
            {
                // 保険で１番最初のマテリアルを設定
                _globalVoxelData[x, y, z] = new VoxelData(1,0);
            }
        }
        Debug.Log($"MagicaVoxelモデルを読み込みました。サイズ: ({_globalSizeX}, {_globalSizeY}, {_globalSizeZ})");
    }

    // public Material GetMaterial(Vector3Int globalPos, float isoLevel, VoxelData[,,] globalVoxelData)
    // {
    //      
    // }

    private void CreateChunks()
    {
        if (_globalVoxelData == null) return;
        int chunksX = Mathf.CeilToInt((float)_globalSizeX / chunkResolution);
        int chunksY = Mathf.CeilToInt((float)_globalSizeY / chunkResolution);
        int chunksZ = Mathf.CeilToInt((float)_globalSizeZ / chunkResolution);

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
                    chunkRenderer.GenerateSurfaceNetsMesh(_globalVoxelData, chunkStartPos, chunkResolution, voxelSize, _activeMaterials);
                    _chunkDic.Add(chunkCoord, chunkRenderer);
                }
            }
        }
    }

    public VoxelData[,,] GetGlobalVoxelData()
    {
        return _globalVoxelData;
    }

    public int GetChunkResolution()
    {
        return chunkResolution;
    }
    
    public bool IsSolid(Vector3Int point)
    {
        // --- 境界チェック ---
        // もしチェック対象が密度データの範囲外なら、そこは「空」として扱う
        if (point.x < 0 || point.x >= _globalSizeX ||
            point.y < 0 || point.y >= _globalSizeY ||
            point.z < 0 || point.z >= _globalSizeZ)
        {
            return false; // 
        }
        if (_globalVoxelData[point.x, point.y, point.z].density > 0)
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
            if (_chunkDic.TryGetValue(chunkCoord, out ChunkRenderer chunkRenderer))
            {
                Vector3Int chunkStartPos = new Vector3Int(chunkCoord.x * chunkResolution, chunkCoord.y * chunkResolution, chunkCoord.z * chunkResolution);
                chunkRenderer.GenerateSurfaceNetsMesh(_globalVoxelData, chunkStartPos, chunkResolution, voxelSize, _activeMaterials);
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
                    if (checkX < 0 || checkX >= _globalSizeX ||
                        checkY < 0 || checkY >= _globalSizeY ||
                        checkZ < 0 || checkZ >= _globalSizeZ)
                    {
                        continue; // 次のボクセルへ
                    }

                    // --- 密度チェック ---
                    // 密度がisoLevel以上（＝固形物）のボクセルが一つでも見つかった場合
                    if (_globalVoxelData[checkX, checkY, checkZ].density >= 0)
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

