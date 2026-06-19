using System.Collections.Generic;
using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using UnityEngine;
using VContainer;

namespace _SchoolFestival_Voxel.Scripts.Voxel
{
    public struct VoxelContactPoint
    {
        public Vector3 point;
        public Vector3 normal;
        public float penetration;
        public Vector3Int gridPos;
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VoxelPhysicsController : MonoBehaviour
    {
        [Inject] private VoxelWorld _voxelWorld;

        private readonly List<VoxelContactPoint> _activeContacts = new List<VoxelContactPoint>();
        public IReadOnlyList<VoxelContactPoint> ActiveContacts => _activeContacts;

        private bool _isGrounded;
        public bool IsGrounded => _isGrounded;

        private Rigidbody _rb;
        private Collider _collider;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.sleepThreshold = 0.0f; // 標準のコライダーに移行したため、スリープによる衝突検知の停止を防ぐ
            
            // コライダーの形状タイプを判定してキャッシュする。
            // プレイヤーのように複数のコライダー（トリガー等）を持つ場合があるため、
            // 物理挙動に影響の大きい CharacterController / CapsuleCollider を優先的に検出する。
            var character = GetComponent<CharacterController>();
            var capsule = GetComponent<CapsuleCollider>();
            var sphere = GetComponent<SphereCollider>();
            var box = GetComponent<BoxCollider>();

            if (character != null)
            {
                _collider = character;
            }
            else if (capsule != null)
            {
                _collider = capsule;
            }
            else if (sphere != null)
            {
                _collider = sphere;
            }
            else if (box != null)
            {
                _collider = box;
            }
            else
            {
                _collider = GetComponent<Collider>();
            }
        }

        private void Start()
        {
            // VContainer経由でインジェクトされなかった場合のフォールバック
            if (_voxelWorld == null)
            {
                _voxelWorld = FindObjectOfType<VoxelWorld>();
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_voxelWorld == null) return;

            _isGrounded = false;
            _activeContacts.Clear();

            float voxelSize = _voxelWorld.VoxelSize;
            Vector3 worldOffset = _voxelWorld.transform.position;

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);

                // 接地判定（上方向の法線を持つ接触があるか。傾斜角約45度以下）
                if (contact.normal.y > 0.7f)
                {
                    _isGrounded = true;
                }

                // 衝突点のめり込み量（separation は負の値になるため反転。penetration は正の値）
                float penetration = Mathf.Max(0f, -contact.separation);

                Vector3 localPos = (contact.point - worldOffset) / voxelSize;
                Vector3Int gridPos = new Vector3Int(
                    Mathf.FloorToInt(localPos.x),
                    Mathf.FloorToInt(localPos.y),
                    Mathf.FloorToInt(localPos.z)
                );

                _activeContacts.Add(new VoxelContactPoint
                {
                    point = contact.point,
                    normal = contact.normal,
                    penetration = penetration,
                    gridPos = gridPos
                });
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            _isGrounded = false;
            _activeContacts.Clear();
        }
    }
}