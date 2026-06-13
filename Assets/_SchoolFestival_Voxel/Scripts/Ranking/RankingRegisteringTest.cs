using Shinkan2025_Cooking.Ranking.Scripts;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Ranking
{
    public class RankingRegisteringTest : MonoBehaviour
    {
        [SerializeField]
        private RankingStorage _rankingStorage;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _rankingStorage.Register();
                Debug.Log("Registered");
            }
        }
    }
}
