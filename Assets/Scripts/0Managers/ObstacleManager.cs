using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    [System.Serializable]
    public struct ObstacleSpawnData
    {
        public string name;
        public GameObject prefab;
        public Vector2Int coordinate; // 초기 배치용
    }

    [Header("Spawn Config")]
    public List<GameObject> randomObstaclePrefabs; // 랜덤 생성될 장애물 프리팹 목록

    [Header("Setup")]
    public List<ObstacleSpawnData> initialObstacles = new List<ObstacleSpawnData>();

    [Header("Runtime State")]
    public List<ObstacleObject> activeObstacles = new List<ObstacleObject>();

    void Awake() => Instance = this;

    void Start()
    {
        SpawnInitialObstacles();
    }

    // 초기 배치
    void SpawnInitialObstacles()
    {
        if (GridManager.Instance == null) return;
        foreach (var data in initialObstacles)
        {
            SpawnObstacle(data.prefab, data.coordinate);
        }
    }

    // [신규] 턴 진행 시 오른쪽 끝에서 랜덤 스폰
    public void SpawnRandomObstacleAtRightEdge()
    {
        if (randomObstaclePrefabs.Count == 0) return;

        int rightEdgeX = GridManager.Instance.width - 1;

        // y 1~3 (Height - 2) 범위 내에서 랜덤
        int minY = 1;
        int maxY = GridManager.Instance.height - 2;

        // 이번 턴에 몇 개를 만들지? (일단 1개 시도)
        int spawnY = Random.Range(minY, maxY + 1);
        Vector2Int spawnPos = new Vector2Int(rightEdgeX, spawnY);

        // 해당 자리가 비어있는지 확인
        if (GridManager.Instance.IsWalkable(spawnPos)) // 유닛이나 장애물이 없으면
        {
            GameObject prefab = randomObstaclePrefabs[Random.Range(0, randomObstaclePrefabs.Count)];
            SpawnObstacle(prefab, spawnPos);
        }
    }

    // 공통 스폰 로직
    void SpawnObstacle(GameObject prefab, Vector2Int pos)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab);
        ObstacleObject obs = go.GetComponent<ObstacleObject>();

        if (obs != null)
        {
            obs.Initialize(pos);
            RegisterObstacle(obs);

            // 생성 연출 (선택 사항: 위에서 떨어지거나 투명해지며 나타나기)
        }
    }

    public void OnTurnStart() { }
    public void RegisterObstacle(ObstacleObject obs) { if (!activeObstacles.Contains(obs)) activeObstacles.Add(obs); }
    public void RemoveObstacle(ObstacleObject obs) { if (activeObstacles.Contains(obs)) activeObstacles.Remove(obs); }
}