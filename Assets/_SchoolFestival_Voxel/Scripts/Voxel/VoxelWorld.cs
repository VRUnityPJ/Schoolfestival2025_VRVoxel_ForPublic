using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class VoxelWorld: MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] 
        private float _voxelSize = 1.0f;
        // 読み取り専用で外部に公開する
        public float VoxelSize => _voxelSize;
        [SerializeField]
        private int _dirtyUpdateMargin = 2;
        // 更新が必要なチャンクの座標リスト
        private HashSet<Vector3Int> _dirtyChunks = new HashSet<Vector3Int>();
        // 外部から「汚れたチャンク」を参照するためのプロパティ
        public IReadOnlyCollection<Vector3Int> DirtyChunks => _dirtyChunks;
        /// <summary>
        /// worldChunkData: ワールド全体のチャンクデータを管理する辞書
        /// </summary>
        private Dictionary<Vector3Int, VoxelChunkData> _worldChunkData = new Dictionary<Vector3Int, VoxelChunkData>();
        
        /// <summary>
        /// 対応チャンクの検索
        /// </summary>
        /// <param name="globalPos"></param>
        /// <returns></returns>
        public Vector3Int GetChunkCoordinate(Vector3Int globalPos)
        {
            int cx = Mathf.FloorToInt((float)globalPos.x / VoxelChunkData.ChunkSize);
            int cy = Mathf.FloorToInt((float)globalPos.y / VoxelChunkData.ChunkSize);
            int cz = Mathf.FloorToInt((float)globalPos.z / VoxelChunkData.ChunkSize);
            return new Vector3Int(cx, cy, cz);
        }
        /// <summary>
        /// 指定されたチャンク座標のチャンクデータを直接取得する（高速化用）
        /// </summary>
        public VoxelChunkData GetChunk(Vector3Int chunkCoord)
        {
            if (_worldChunkData.TryGetValue(chunkCoord, out var chunk))
            {
                return chunk;
            }
            return null; // 存在しない（まだ生成されていない）場合はnullを返す
        }
        // グローバル座標からVoxelを取得
        public VoxelData GetVoxel(Vector3Int globalPos)
        {
            Vector3Int chunkCoord = GetChunkCoordinate(globalPos);
            if (_worldChunkData.TryGetValue(chunkCoord, out var chunk))
            {
                // グローバル座標をチャンク内のローカル座標に変換して取得
                int localX = globalPos.x - chunkCoord.x * VoxelChunkData.ChunkSize;
                int localY = globalPos.y - chunkCoord.y * VoxelChunkData.ChunkSize;
                int localZ = globalPos.z - chunkCoord.z * VoxelChunkData.ChunkSize;
                return chunk.GetVoxel(localX, localY, localZ);
            }
            return new VoxelData(0, 0); // チャンクが存在しなければ「空気」を返す
        }
        // グローバル座標にVoxelをセット
        public void SetVoxel(Vector3Int globalPos, VoxelData data)
        {
            Vector3Int chunkCoord = GetChunkCoordinate(globalPos);
            if (!_worldChunkData.ContainsKey(chunkCoord))
            {
                _worldChunkData[chunkCoord] = new VoxelChunkData(chunkCoord);
            }
    
            int localX = globalPos.x - chunkCoord.x * VoxelChunkData.ChunkSize;
            int localY = globalPos.y - chunkCoord.y * VoxelChunkData.ChunkSize;
            int localZ = globalPos.z - chunkCoord.z * VoxelChunkData.ChunkSize;
    
            _worldChunkData[chunkCoord].SetVoxel(localX, localY, localZ, data);
    
            // 影響を受けるセル(8つのセル)の頂点位置を決定するために参照されるボクセルデータの範囲、
            // および坂の滑らかさやマテリアルブレンドの影響範囲をカバーするため、
            // 変更されたボクセル座標の周辺一定範囲（_dirtyUpdateMargin）に含まれるすべてのチャンクを更新対象にする。
            Vector3Int minChunk = GetChunkCoordinate(globalPos - new Vector3Int(_dirtyUpdateMargin, _dirtyUpdateMargin, _dirtyUpdateMargin));
            Vector3Int maxChunk = GetChunkCoordinate(globalPos + new Vector3Int(_dirtyUpdateMargin, _dirtyUpdateMargin, _dirtyUpdateMargin));
            
            for (int cx = minChunk.x; cx <= maxChunk.x; cx++)
            {
                for (int cy = minChunk.y; cy <= maxChunk.y; cy++)
                {
                    for (int cz = minChunk.z; cz <= maxChunk.z; cz++)
                    {
                        _dirtyChunks.Add(new Vector3Int(cx, cy, cz));
                    }
                }
            }
        }
        public int GetCellMaterialID(Vector3Int globalPos, float isoLevel)
        {
            int uniqueCount = 0;
            int[] ids = new int[8];
            int[] counts = new int[8];
            for (int i = 0; i < 8; i++)
            {
                // ChunkUtilityのOffset定数を参照する
                Vector3Int cornerPos = globalPos + ChunkUtility.CornerOffsets[i];
                VoxelData data = GetVoxel(cornerPos); // 自身のGetVoxelを直接呼ぶ
                if (data.density >= isoLevel && data.materialID != -1)
                {
                    int foundIdx = -1;
                    for (int j = 0; j < uniqueCount; j++)
                    {
                        if (ids[j] == data.materialID)
                        {
                            foundIdx = j;
                            break;
                        }
                    }
                    if (foundIdx != -1)
                    {
                        counts[foundIdx]++;
                    }
                    else
                    {
                        ids[uniqueCount] = data.materialID;
                        counts[uniqueCount] = 1;
                        uniqueCount++;
                    }
                }
            }
            int maxCount = 0;
            int bestId = -1;
            for (int i = 0; i < uniqueCount; i++)
            {
                if (counts[i] > maxCount)
                {
                    maxCount = counts[i];
                    bestId = ids[i];
                }
            }
            return bestId;
        }
        public void ClearDirtyChunks()
        {
            _dirtyChunks.Clear();
        }

        public bool HasChunkData(Vector3Int chunkCoord)
        {
            // 1. 自分自身に中身があるなら当然描画する
            if (_worldChunkData.TryGetValue(chunkCoord, out var chunk) && !chunk.IsEmpty)
            {
                return true;
            }
            // 2. 自分自身が空でも、周囲26方向のいずれかに中身があるなら、
            // 境界や角のメッシュを描画するためにこのチャンクを表示する
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue; // 自分自身はスキップ
                        Vector3Int neighborCoord = chunkCoord + new Vector3Int(dx, dy, dz);
                        if (_worldChunkData.TryGetValue(neighborCoord, out var neighbor) && !neighbor.IsEmpty)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        //以下は当たり判定用の関数
        
        /// <summary>
        /// 任意のローカル座標（浮動小数）における補間された密度を取得する（トリリニア補間）
        /// </summary>
        public float GetSmoothDensity(Vector3 localPos)
        {
            float gx = localPos.x;
            float gy = localPos.y;
            float gz = localPos.z;
            int x0 = Mathf.FloorToInt(gx);
            int y0 = Mathf.FloorToInt(gy);
            int z0 = Mathf.FloorToInt(gz);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            int z1 = z0 + 1;
            float tx = gx - x0;
            float ty = gy - y0;
            float tz = gz - z0;
            // 周囲8マスの密度を取得
            float d000 = GetVoxel(new Vector3Int(x0, y0, z0)).density;
            float d100 = GetVoxel(new Vector3Int(x1, y0, z0)).density;
            float d010 = GetVoxel(new Vector3Int(x0, y1, z0)).density;
            float d110 = GetVoxel(new Vector3Int(x1, y1, z0)).density;
            float d001 = GetVoxel(new Vector3Int(x0, y0, z1)).density;
            float d101 = GetVoxel(new Vector3Int(x1, y0, z1)).density;
            float d011 = GetVoxel(new Vector3Int(x0, y1, z1)).density;
            float d111 = GetVoxel(new Vector3Int(x1, y1, z1)).density;
            // X軸の補間
            float d00 = Mathf.Lerp(d000, d100, tx);
            float d01 = Mathf.Lerp(d001, d101, tx);
            float d10 = Mathf.Lerp(d010, d110, tx);
            float d11 = Mathf.Lerp(d011, d111, tx);
            // Y軸の補間
            float d0 = Mathf.Lerp(d00, d10, ty);
            float d1 = Mathf.Lerp(d01, d11, ty);
            // Z軸の補間
            return Mathf.Lerp(d0, d1, tz);
        }
        /// <summary>
        /// 任意のローカル座標における法線ベクトル（坂の傾き向き）を取得する、これで滑ることが可能なはず
        /// </summary>
        public Vector3 GetSmoothNormal(Vector3 localPos)
        {
            float eps = 0.1f; // 微小変化量
    
            // 各軸方向の密度の傾き（グラディエント）を計算
            float dx = GetSmoothDensity(localPos + new Vector3(eps, 0, 0)) - GetSmoothDensity(localPos - new Vector3(eps, 0, 0));
            float dy = GetSmoothDensity(localPos + new Vector3(0, eps, 0)) - GetSmoothDensity(localPos - new Vector3(0, eps, 0));
            float dz = GetSmoothDensity(localPos + new Vector3(0, 0, eps)) - GetSmoothDensity(localPos - new Vector3(0, 0, eps));
    
            // 密度は内部が濃く（正）、外部が薄い（負）ため、マイナスをかけて外側を向く法線にする
            Vector3 normal = -new Vector3(dx, dy, dz);
    
            if (normal.sqrMagnitude < 0.001f) return Vector3.up; // 平坦な場合
            return normal.normalized;
        }
        /// <summary>
        /// 指定されたローカル座標(X, Z)において、密度がちょうど 0.5 になる滑らかな地面のY座標を逆算する
        /// </summary>
        public float GetSmoothGroundHeight(float localX, float currentLocalY, float localZ, float isoLevel = 0.5f)
        {
            int startGy = Mathf.FloorToInt(currentLocalY);
    
            // 足元の上下3マス（合計7マスの高さ）をスキャンして、
            // 「下が固体（>=0.5）で、上が空気（<0.5）」になっている境界線を探す
            int searchRange = 3; 
            // スキャン開始位置の密度を取得
            float prevDensity = GetSmoothDensity(new Vector3(localX, startGy - searchRange, localZ));
            for (int y = startGy - searchRange + 1; y <= startGy + searchRange; y++)
            {
                float density = GetSmoothDensity(new Vector3(localX, y, localZ));
                // 下が固体で、上が空気という「地面の表面」の遷移を見つけたら
                if (prevDensity >= isoLevel && density < isoLevel)
                {
                    float d1 = prevDensity;
                    float d2 = density;
            
                    // 線形補間(Lerp)で、密度がちょうど isoLevel になる小数の高さを計算
                    float t = (isoLevel - d1) / (d2 - d1);
                    return (y - 1) + t; // 正しい地表のY座標を返す
                }
                prevDensity = density;
            }
            // 範囲内に地面が見つからなかった場合のみ、現在の高さを維持する
            return currentLocalY;
        }

        /// <summary>
        /// 指定された範囲内のすべてのボクセルの密度が0以下（空気）であるか判定する
        /// </summary>
        public bool IsAllEmpty(Vector3Int globalStartPoint, Vector3Int range)
        {
            for (int x = 0; x < range.x; x++)
            {
                for (int y = 0; y < range.y; y++)
                {
                    for (int z = 0; z < range.z; z++)
                    {
                        Vector3Int checkPos = new Vector3Int(
                            globalStartPoint.x + x,
                            globalStartPoint.y + y,
                            globalStartPoint.z + z
                        );
                        if (GetVoxel(checkPos).density > 0)
                        {
                            return false; // 固形物が見つかったら即座にfalse
                        }
                    }
                }
            }
            return true; // すべて空気ならtrue
        }

        /// <summary>
        /// ワールドのボクセルデータを完全に初期化する
        /// </summary>
        public void ClearWorld()
        {
            _worldChunkData.Clear();
            _dirtyChunks.Clear();
            _registeredLockItems.Clear();
        }

        // 各チャンク座標に属するVoxelLockItemのリスト
        private Dictionary<Vector3Int, List<VoxelLockItem>> _registeredLockItems = new Dictionary<Vector3Int, List<VoxelLockItem>>();

        public void RegisterLockItem(Vector3Int chunkCoord, VoxelLockItem item)
        {
            if (!_registeredLockItems.TryGetValue(chunkCoord, out var list))
            {
                list = new List<VoxelLockItem>();
                _registeredLockItems[chunkCoord] = list;
            }
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        public void UnregisterLockItem(Vector3Int chunkCoord, VoxelLockItem item)
        {
            if (_registeredLockItems.TryGetValue(chunkCoord, out var list))
            {
                list.Remove(item);
                if (list.Count == 0)
                {
                    _registeredLockItems.Remove(chunkCoord);
                }
            }
        }

        public IReadOnlyList<VoxelLockItem> GetLockItemsInChunk(Vector3Int chunkCoord)
        {
            if (_registeredLockItems.TryGetValue(chunkCoord, out var list))
            {
                return list;
            }
            return null;
        }

        /// <summary>
        /// 仮想ボクセル世界に対する高速なグリッド走査（DDA）レイキャスト
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out VoxelRaycastHit hit)
        {
            hit = default;
            if (direction.sqrMagnitude < 0.001f) return false;

            Vector3 dir = direction.normalized;

            // ワールド座標からボクセルローカル空間へ変換
            Vector3 localOrigin = (origin - transform.position) / _voxelSize;

            // ローカル空間での開始ボクセル座標
            int x = Mathf.FloorToInt(localOrigin.x);
            int y = Mathf.FloorToInt(localOrigin.y);
            int z = Mathf.FloorToInt(localOrigin.z);

            // 各軸のステップ方向
            int stepX = (dir.x >= 0) ? 1 : -1;
            int stepY = (dir.y >= 0) ? 1 : -1;
            int stepZ = (dir.z >= 0) ? 1 : -1;

            // 次のボクセル境界までの距離 tMax
            float tMaxX = (dir.x != 0) ? ((x + (stepX > 0 ? 1 : 0)) - localOrigin.x) / dir.x : float.PositiveInfinity;
            float tMaxY = (dir.y != 0) ? ((y + (stepY > 0 ? 1 : 0)) - localOrigin.y) / dir.y : float.PositiveInfinity;
            float tMaxZ = (dir.z != 0) ? ((z + (stepZ > 0 ? 1 : 0)) - localOrigin.z) / dir.z : float.PositiveInfinity;

            // 各軸を1ボクセル進むのに必要な t の変化量
            float tDeltaX = (dir.x != 0) ? Mathf.Abs(1.0f / dir.x) : float.PositiveInfinity;
            float tDeltaY = (dir.y != 0) ? Mathf.Abs(1.0f / dir.y) : float.PositiveInfinity;
            float tDeltaZ = (dir.z != 0) ? Mathf.Abs(1.0f / dir.z) : float.PositiveInfinity;

            float t = 0f;
            Vector3 lastNormal = Vector3.zero;

            // 走査ループ
            float maxLocalDistance = maxDistance / _voxelSize;
            while (t <= maxLocalDistance)
            {
                Vector3Int currentCell = new Vector3Int(x, y, z);
                VoxelData data = GetVoxel(currentCell);

                // 密度が 0.5 以上の場合は「ヒット」と判定
                if (data.density >= 0.5f)
                {
                    hit.voxelPosition = currentCell;
                    hit.hitPoint = origin + dir * (t * _voxelSize);
                    hit.normal = lastNormal;
                    hit.distance = t * _voxelSize;
                    hit.voxelData = data;
                    return true;
                }

                // 次のボクセルへ移動
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        x += stepX;
                        t = tMaxX;
                        tMaxX += tDeltaX;
                        lastNormal = new Vector3(-stepX, 0, 0);
                    }
                    else
                    {
                        z += stepZ;
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        lastNormal = new Vector3(0, 0, -stepZ);
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        y += stepY;
                        t = tMaxY;
                        tMaxY += tDeltaY;
                        lastNormal = new Vector3(0, -stepY, 0);
                    }
                    else
                    {
                        z += stepZ;
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        lastNormal = new Vector3(0, 0, -stepZ);
                    }
                }
            }

            return false;
        }
        
        //計算の簡略化
        private static readonly Vector3Int[] NeighborDirections = {
            Vector3Int.left, Vector3Int.right,
            Vector3Int.down, Vector3Int.up,
            Vector3Int.back, Vector3Int.forward
        };

        // --- 物理ターゲット登録システム ---
        public struct PhysicsTargetInfo
        {
            public Transform transform;
            public float radius;

            public PhysicsTargetInfo(Transform transform, float radius)
            {
                this.transform = transform;
                this.radius = radius;
            }
        }

        private readonly List<PhysicsTargetInfo> _activePhysicsTargets = new List<PhysicsTargetInfo>();
        public IReadOnlyList<PhysicsTargetInfo> ActivePhysicsTargets => _activePhysicsTargets;

        public void RegisterPhysicsTarget(Transform target, float radius = 48f)
        {
            if (target == null) return;
            if (!_activePhysicsTargets.Exists(t => t.transform == target))
            {
                _activePhysicsTargets.Add(new PhysicsTargetInfo(target, radius));
            }
        }

        public void UnregisterPhysicsTarget(Transform target)
        {
            if (target == null) return;
            _activePhysicsTargets.RemoveAll(t => t.transform == target || t.transform == null);
        }
    }

    public class VoxelChunkData
    {
        public const int ChunkSize = 16; 
        public Vector3Int ChunkCoordinate { get; private set; } 
    
        private VoxelData[] _voxels; 
    
        // ★追加: 固体（密度 > 0）のVoxelの数をキャッシュする
        private int _solidVoxelCount = 0;
        // ★追加: 固体ブロックがゼロなら空っぽと判定する
        public bool IsEmpty => _solidVoxelCount == 0;
        public VoxelChunkData(Vector3Int coord)
        {
            ChunkCoordinate = coord;
            _voxels = new VoxelData[ChunkSize * ChunkSize * ChunkSize];
            _solidVoxelCount = 0;
        }
        public void SetVoxel(int localX, int localY, int localZ, VoxelData data)
        {
            if (!IsInBounds(localX, localY, localZ)) return;
            int index = GetIndex(localX, localY, localZ);
            VoxelData oldData = _voxels[index];
            bool wasSolid = oldData.density > 0;
            bool isSolid = data.density > 0;
            if (!wasSolid && isSolid) _solidVoxelCount++;
            else if (wasSolid && !isSolid) _solidVoxelCount--;
            _voxels[index] = data;
        }
        // チャンク内ローカル座標(0~15)からインデックスを計算
        private int GetIndex(int x, int y, int z) => x + (y * ChunkSize) + (z * ChunkSize * ChunkSize);
        public bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < ChunkSize &&
                   y >= 0 && y < ChunkSize &&
                   z >= 0 && z < ChunkSize;
        }
        public VoxelData GetVoxel(int localX, int localY, int localZ)
        {
            if (!IsInBounds(localX, localY, localZ))
            {
                return new VoxelData(0, 0);
            }
            return _voxels[GetIndex(localX, localY, localZ)];
        }
        public VoxelData GetVoxelDirect(int index)
        {
            // すでにループの範囲内(0~4095)であることが保証されているため、
            // 境界チェックを省いて最速で配列データを返します
            return _voxels[index];
        }
        
    }
    public readonly struct VoxelData
    {
        public readonly float density;
        public readonly int materialID;
        public readonly float durability;
        public readonly bool isEmpty => density <= 0;

        public VoxelData(float density, int materialID)
        {
            this.density = density;
            this.materialID = materialID;
            this.durability = 1;
        }
    }

    public struct VoxelRaycastHit
    {
        public Vector3Int voxelPosition; // 衝突したボクセルのグリッド座標
        public Vector3 hitPoint;        // ワールド空間における衝突点
        public Vector3 normal;          // 衝突面の法線ベクトル
        public float distance;          // レイの起点から衝突点までの距離
        public VoxelData voxelData;     // 衝突したボクセルのデータ
    }
}
