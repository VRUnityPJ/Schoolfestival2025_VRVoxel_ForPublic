using UnityEngine;

namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    [RequireComponent(typeof(Rigidbody))]
    public class TestPlayerMove : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [SerializeField]private float _speed = 5f;
        private Rigidbody _rb;
        void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                _rb.AddForce(Vector3.forward * (_speed * Time.deltaTime), ForceMode.Impulse);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                _rb.AddForce(Vector3.back * (_speed * Time.deltaTime), ForceMode.Impulse);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                _rb.AddForce(Vector3.left * (_speed * Time.deltaTime), ForceMode.Impulse);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                _rb.AddForce(Vector3.right * (_speed * Time.deltaTime), ForceMode.Impulse);
            }
        }
    }
}
