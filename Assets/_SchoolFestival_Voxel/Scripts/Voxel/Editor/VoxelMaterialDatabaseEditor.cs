using UnityEditor;
using UnityEngine;
using System.IO;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528.Editor
{
    [CustomEditor(typeof(VoxelMaterialDatabase))]
    public class VoxelMaterialDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 標準フィールドの描画
            DrawDefaultInspector();

            VoxelMaterialDatabase db = (VoxelMaterialDatabase)target;

            GUILayout.Space(20);
            if (GUILayout.Button("Generate Texture Arrays", GUILayout.Height(35)))
            {
                GenerateTextureArrays(db);
            }
        }

        private void GenerateTextureArrays(VoxelMaterialDatabase db)
        {
            var entries = db.VoxelEntries;
            if (entries == null || entries.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "Voxel Entriesリストが空です。まずはテクスチャの設定を行ってください。", "OK");
                return;
            }

            // ターゲット解像度の設定 (2048x2048)
            int targetWidth = 2048;
            int targetHeight = 2048;

            // プログレスバーの表示
            EditorUtility.DisplayProgressBar("Texture2DArrayの生成", "テクスチャの生成準備中...", 0f);

            try
            {
                // Albedo用のテクスチャ配列を作成
                Texture2DArray albedoTexArray = new Texture2DArray(targetWidth, targetHeight, entries.Count, TextureFormat.RGBA32, true);
                albedoTexArray.filterMode = FilterMode.Bilinear;
                albedoTexArray.wrapMode = TextureWrapMode.Repeat;

                // Normal用のテクスチャ配列を作成
                Texture2DArray normalTexArray = new Texture2DArray(targetWidth, targetHeight, entries.Count, TextureFormat.RGBA32, true);
                normalTexArray.filterMode = FilterMode.Bilinear;
                normalTexArray.wrapMode = TextureWrapMode.Repeat;

                // デフォルトの法線マップ（平坦）テクスチャを作成
                Texture2D defaultNormal = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color flatNormal = new Color(0.5f, 0.5f, 1.0f, 1.0f);
                defaultNormal.SetPixel(0, 0, flatNormal);
                defaultNormal.SetPixel(1, 0, flatNormal);
                defaultNormal.SetPixel(0, 1, flatNormal);
                defaultNormal.SetPixel(1, 1, flatNormal);
                defaultNormal.Apply();

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    float progress = (float)i / entries.Count;
                    EditorUtility.DisplayProgressBar("Texture2DArrayの生成", $"テクスチャを処理中 ({i + 1}/{entries.Count}): {entry.name}...", progress);

                    // 1. Albedo画像のサイズ調整とコピー (設定されていなければ白色テクスチャを使用)
                    Texture2D albedoSource = entry.albedoTexture != null ? entry.albedoTexture : Texture2D.whiteTexture;
                    Texture2D resizedAlbedo = RescaleTexture(albedoSource, targetWidth, targetHeight, false);
                    for (int mip = 0; mip < resizedAlbedo.mipmapCount; mip++)
                    {
                        Graphics.CopyTexture(resizedAlbedo, 0, mip, albedoTexArray, i, mip);
                    }
                    DestroyImmediate(resizedAlbedo);

                    // 2. Normal画像のサイズ調整とコピー (設定されていなければデフォルト法線を使用)
                    Texture2D normalSource = entry.normalTexture != null ? entry.normalTexture : defaultNormal;
                    Texture2D resizedNormal = RescaleTexture(normalSource, targetWidth, targetHeight, true);
                    for (int mip = 0; mip < resizedNormal.mipmapCount; mip++)
                    {
                        Graphics.CopyTexture(resizedNormal, 0, mip, normalTexArray, i, mip);
                    }
                    DestroyImmediate(resizedNormal);
                }

                DestroyImmediate(defaultNormal);

                // データベースアセットと同じフォルダに保存する
                string assetPath = AssetDatabase.GetAssetPath(db);
                string directory = Path.GetDirectoryName(assetPath);

                string albedoSavePath = Path.Combine(directory, $"{db.name}_AlbedoArray.asset");
                string normalSavePath = Path.Combine(directory, $"{db.name}_NormalArray.asset");

                // アルベドアレイの保存
                AssetDatabase.CreateAsset(albedoTexArray, albedoSavePath);
                db.AlbedoArray = albedoTexArray;

                // ノーマルアレイの保存
                AssetDatabase.CreateAsset(normalTexArray, normalSavePath);
                db.NormalArray = normalTexArray;

                // 変更をシリアライズしてアセットを保存
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("成功", $"テクスチャ配列を作成し、登録しました！\nアルベド: {albedoSavePath}\n法線マップ: {normalSavePath}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"テクスチャ配列生成中にエラーが発生しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", $"テクスチャ配列の生成に失敗しました:\n{ex.Message}", "OK");
            }
        }

        private Texture2D RescaleTexture(Texture2D source, int width, int height, bool isNormal)
        {
            // RenderTextureを使用してGPUでテクスチャサイズを変更する
            // 法線マップの場合はガンマ補正を無効にするためLinearを使用
            RenderTextureReadWrite readWrite = isNormal ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, readWrite);
            
            // コピーと拡大縮小
            Graphics.Blit(source, rt);

            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, true, isNormal);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(true); // ミップマップの生成

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
