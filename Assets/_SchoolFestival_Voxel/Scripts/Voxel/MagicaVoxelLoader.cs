using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using VContainer;
using VoxReader.Interfaces;
using ZLinq;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class MagicaVoxelLoader : MonoBehaviour
    {
        
        [Inject] [SerializeField]
        private VoxelWorld _voxelWorld;
        [SerializeField]
        private List<DebugLoadModel> _debugLoadModels;
        
        [Inject] [SerializeField] private VoxelMaterialDatabase _db;
        [Inject] [SerializeField] private VoxelWorldRenderer _worldRenderer;
        
        /// <summary>
        /// MagicaVoxelのファイルを読み込んで、VoxelWorldに配置する
        /// モデルを複数使用可能にする目的
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="chunkStartPos"></param>
        private void LoadVoxelModel(string modelName,Vector3Int chunkStartPos)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, modelName);
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
            // モデル内で実際に使用されているパレットインデックスを収集
            var usedColorIndices = new HashSet<int>();
            foreach (var voxel in model.Voxels)
            {
                usedColorIndices.Add(voxel.ColorIndex);
            }
            Debug.Log($"[MagicaVoxelLoader] モデル内の総ボクセル数: {model.Voxels.Length}, 使用されているユニーク色数: {usedColorIndices.Count}");

            var paletteIndexToMaterialID = new Dictionary<int, int>();

            // Palette.Colors を使用してループとインデックスアクセスを行う
            for (int i = 0; i < voxFileContent.Palette.Colors.Length; i++)
            {
                var voxColor = voxFileContent.Palette.Colors[i];
                var unityColor = new Color32(voxColor.R, voxColor.G, voxColor.B, 255);

                if (usedColorIndices.Contains(i))
                {
                    // 実際にモデルで使われている色のみデータベースから取得
                    int materialID = _db.GetMaterialID(unityColor);
                    paletteIndexToMaterialID[i] = materialID;
                    Debug.Log($"[MagicaVoxelLoader] 使用中のパレット色 [{i}]: Color={unityColor} ➔ マッピングされたVoxel ID={materialID}");
                }
                else
                {
                    // 使われていない色は警告を出さずにデフォルト（0）を割り当て
                    paletteIndexToMaterialID[i] = 0;
                }
            }
            
            foreach(var voxel in model.Voxels)
            {
                Vector3Int globalPos = chunkStartPos + new Vector3Int(
                    voxel.LocalPosition.X, 
                    voxel.LocalPosition.Z, // Unityように座標軸を変更
                    voxel.LocalPosition.Y  
                );
                
                paletteIndexToMaterialID.TryGetValue(voxel.ColorIndex, out int materialID);
                _voxelWorld.SetVoxel(globalPos, new VoxelData(1.0f, materialID));
            }
        }

        [Button]
        public void DebugLoad()
        {
            LoadAllModels();
        }

        /// <summary>
        /// ワールド全体を初期化し、すべての登録モデルを再ロード・再描画する
        /// </summary>
        public void LoadAllModels()
        {
            _voxelWorld.ClearWorld();
            _worldRenderer.ResetRenderer();
            
            foreach (var model in _debugLoadModels)
            {
                LoadVoxelModel(model.modelName, model.chunkStartPos);
            }
            
            _worldRenderer.RebuildDirtyChunks();
        }
    }
    [System.Serializable]
    public class DebugLoadModel
    {
        public string modelName;
        public Vector3Int chunkStartPos;
    }
}
