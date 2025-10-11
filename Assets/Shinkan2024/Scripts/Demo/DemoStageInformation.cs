using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoStageInformation : MonoBehaviour
{
    [SerializeField] private GameObject _rightWall, _leftWall;
    [SerializeField] private Transform _start, _end;
    public float RoadWidth => _rightWall.transform.position.x - _leftWall.transform.position.x;
    public float RoadLength => _end.position.z - _start.position.z;
}
