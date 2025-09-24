using Ranking.Scripts;
using Shinkan2024.Scripts.Game.Point;
using UnityEngine;
using R3;
using Ranking.Demo.Scripts.DemoGame;

public class RankingRegisteringTest : MonoBehaviour
{
    [SerializeField]
    private PlayerScoreHolder playerScoreHolder;
    [SerializeField]
    private RankingStorage rankingStorage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ReactiveProperty<Point> point = new ReactiveProperty<Point>();
            // point.Value = new Point(122);
            // pointHolder.SendData(point.Value);
            rankingStorage.Register();
            Debug.Log("Registered");
        }
    }
}
