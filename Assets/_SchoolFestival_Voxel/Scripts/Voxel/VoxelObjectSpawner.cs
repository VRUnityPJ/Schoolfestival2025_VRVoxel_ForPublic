using UnityEngine;
using VContainer;
using VContainer.Unity; // Instantiateの拡張メソッドを使用するために必須

namespace _SchoolFestival_Voxel.Scripts.Voxel.Remake_0528
{
    public class VoxelObjectSpawner : MonoBehaviour
    {

        // VContainerのコンテナ参照（自動注入される）
        [Inject] private IObjectResolver _resolver;

        /// <summary>
        /// 指定されたプレハブを生成し、同時にVoxelPhysicsController等の依存注入を行う
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            // Object.Instantiate(prefab) の代わりに、resolver.Instantiate を使用する！
            // これにより、インスタンス化された瞬間に、プレハブ内の VoxelPhysicsController 等に
            // VoxelWorld や VoxelMaterialDatabase などが自動で [Inject] されます。
            return _resolver.Instantiate(prefab, position, rotation);
        }
    }
}