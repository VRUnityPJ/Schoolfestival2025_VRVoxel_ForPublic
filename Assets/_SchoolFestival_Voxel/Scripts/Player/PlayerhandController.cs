using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerhandController : MonoBehaviour
    {
        [SerializeField]private GameObject _leftHummer;
        [SerializeField]private GameObject _rightHummer;

        public void InGame()
        {
            _leftHummer.transform.localPosition = new Vector3(0,0,1.5f);
            _rightHummer.transform.localPosition = new Vector3(0,0,1.5f);
            _leftHummer.SetActive(true);
            _rightHummer.SetActive(true);
        }

        public void OutGame()
        {
            _leftHummer.transform.localPosition = new Vector3(0,0,-10.5f);
            _rightHummer.transform.localPosition = new Vector3(0,0,-10.5f);
            _leftHummer.SetActive(false);
            _rightHummer.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
