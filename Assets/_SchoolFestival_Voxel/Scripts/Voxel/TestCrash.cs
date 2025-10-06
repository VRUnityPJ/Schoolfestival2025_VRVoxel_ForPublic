using UnityEngine;
using SchoolFestival_Voxel.Scripts.Voxel.Remake;

public class TestCrash : MonoBehaviour
{
    [SerializeField] private RemakeMeshDestroyer _meshDestroyer;
    [SerializeField] private ChunkManager _chunkManager;
    [SerializeField] private float _destroyRadius = 0.5f; // 破壊する球の半径


    // Update is called once per frame
    void Update()
    {
        Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / _chunkManager.voxelSize);
        if (_chunkManager.IsSolid(gridPos))
        {
            _meshDestroyer.DestroySphere(this.gameObject.transform.position, _destroyRadius);
        }
    }
}
