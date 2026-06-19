using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Voxel.Test
{
    /// <summary>
    /// 弾丸などの発射物（プロジェクタイル）が飛行中に自身の周囲のVoxelコライダーを有効化するための参考用サンプルスクリプト。
    /// 自身の生成時に VoxelWorld に物理ターゲットとして登録し、破壊された際に自動的に登録解除します。
    /// </summary>
    public class VoxelBulletPhysicsTracker : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _physicsActivationRadius = 16f; // 弾丸の有効判定範囲（通常16m=1チャンク分で十分）

        private VoxelWorld _voxelWorld;

        private void Start()
        {
            // VoxelWorldを検索
            _voxelWorld = FindObjectOfType<VoxelWorld>();
            
            if (_voxelWorld != null)
            {
                // 自身を物理ターゲットリストに登録（狭めの半径を指定）
                _voxelWorld.RegisterPhysicsTarget(this.transform, _physicsActivationRadius);
            }
        }

        private void OnDestroy()
        {
            if (_voxelWorld != null)
            {
                // 破棄時に必ず登録解除
                _voxelWorld.UnregisterPhysicsTarget(this.transform);
            }
        }
    }
}
