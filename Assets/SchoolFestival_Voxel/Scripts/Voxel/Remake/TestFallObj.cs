using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    public class TestFallObj : MonoBehaviour
    {
        [SerializeField] private ChunkManager _chunkManager;
        void Update()
        {
            Vector3Int gridPos = Vector3Int.FloorToInt(transform.position / _chunkManager.voxelSize);
            Vector3 fallPos = transform.position;
            fallPos.y -= 0.01f;
            if (!_chunkManager.IsSolid(gridPos))
                transform.position = fallPos;
        }
    }
}
