using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    public class ClickToDestroy : MonoBehaviour
    {
        [SerializeField] private RemakeMeshDestroyer _meshDestroyer;
        [SerializeField] private float _destroyRadius = 3f; // 破壊する球の半径

        private　void Update()
        {
            if (Input.GetMouseButtonDown(0)) // マウスの左クリック
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _meshDestroyer.DestroySphere(hit.point, _destroyRadius);
                }
            }
        }
    }
}