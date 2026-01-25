using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    // [수정] Base -> Object
    public List<ObstacleObject> obstacles = new List<ObstacleObject>();

    void Awake() => Instance = this;

    public void OnTurnStart()
    {
        // 턴 시작 시 장애물 관련 로직이 있다면 수행
    }

    public void RegisterObstacle(ObstacleObject obs)
    {
        if (!obstacles.Contains(obs)) obstacles.Add(obs);
    }

    public void RemoveObstacle(ObstacleObject obs)
    {
        if (obstacles.Contains(obs)) obstacles.Remove(obs);
    }
}