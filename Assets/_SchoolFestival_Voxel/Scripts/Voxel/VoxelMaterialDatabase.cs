using System.Collections.Generic;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel
{
    [CreateAssetMenu(fileName = "VoxelMaterialDatabase", menuName = "Voxel/Material Database")]
    public class VoxelMaterialDatabase : ScriptableObject
    {
        [Header("Master Material")]
        [SerializeField]
        private Material _masterMaterial;
        public Material MasterMaterial => _masterMaterial;

        [Header("Generated Arrays")]
        [SerializeField]
        private Texture2DArray _albedoArray;
        public Texture2DArray AlbedoArray
        {
            get => _albedoArray;
            set => _albedoArray = value;
        }

        [SerializeField]
        private Texture2DArray _normalArray;
        public Texture2DArray NormalArray
        {
            get => _normalArray;
            set => _normalArray = value;
        }

        [Header("Voxel Configurations")]
        [SerializeField]
        private List<VoxelMaterialEntry> _voxelEntries = new List<VoxelMaterialEntry>();
        public List<VoxelMaterialEntry> VoxelEntries => _voxelEntries;

        // 高速検索用のキャッシュ辞書
        private Dictionary<Color32, int> _colorToIdMap;

        // ゲーム開始時、または必要時に辞書をビルドする
        public void Initialize()
        {
            _colorToIdMap = new Dictionary<Color32, int>();
            Debug.Log($"[VoxelMaterialDatabase] Initialize: リスト内の要素数 = {_voxelEntries.Count}");
            for (int i = 0; i < _voxelEntries.Count; i++)
            {
                var entry = _voxelEntries[i];
                _colorToIdMap[entry.color] = i; 
                Debug.Log($"[VoxelMaterialDatabase] 登録データ: Index=[{i}], Name={entry.name}, Color={entry.color} (R:{entry.color.r}, G:{entry.color.g}, B:{entry.color.b}, A:{entry.color.a})");
            }
        }

        // RGB値からマテリアルID(データベースリスト内のインデックス)を取得する
        public int GetMaterialID(Color32 color)
        {
            if (_colorToIdMap == null) Initialize(); // 遅延初期化
            if (_colorToIdMap.TryGetValue(color, out int id))
            {
                Debug.Log($"[VoxelMaterialDatabase] 色一致 成功: 入力色={color} ➔ 一致インデックス=[{id}] ({_voxelEntries[id].name})");
                return id;
            }
            
            Debug.LogWarning($"[VoxelMaterialDatabase] 色一致 失敗: 入力色={color} (R:{color.r}, G:{color.g}, B:{color.b}, A:{color.a}) ➔ デフォルト値 0 を返します。");
            return 0; // デフォルトは最初のテクスチャ
        }

        // 互換性維持のためのダミー実装
        public Material[] GetAllMaterials()
        {
            if (_masterMaterial != null)
            {
                return new Material[] { _masterMaterial };
            }
            return new Material[0];
        }
    }

    [System.Serializable]
    public class VoxelMaterialEntry
    {
        public string name; // 開発者用の名前ラベル
        public Color32 color;
        public Texture2D albedoTexture;
        public Texture2D normalTexture;
    }
}
