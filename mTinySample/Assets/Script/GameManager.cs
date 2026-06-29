using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject robotPrefab;
    public Transform startPoint;

    private GameObject robot;

    void Awake()
    {
        robot = Instantiate(robotPrefab,startPoint.position,Quaternion.identity);
    }
}