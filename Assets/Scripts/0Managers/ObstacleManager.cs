using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    [Header("Conveyor Settings")]
    public int movePeriod = 2;
    private int currentTurnCount = 0;

    [Header("Spawn Settings (Obstacle)")]
    public List<GameObject> obstaclePrefabs;
    public int minRow = 1;
    public int maxRow = 3;
    [Range(0, 100)] public int obstacleSpawnChance = 40;

    [Header("Spawn Settings (Zone)")]
    public List<GameObject> zonePrefabs;
    [Range(0, 100)] public int zoneSpawnChance = 30;

    void Awake() => Instance = this;

    public void OnTurnStart()
    {
        currentTurnCount++;
        if (currentTurnCount % movePeriod == 0)
        {
            MoveConveyorBelt();
            SpawnNewColumn();
        }
    }

    void MoveConveyorBelt()
    {
        // ... (이동 로직은 기존과 100% 동일, 생략 없이 그대로 유지) ...
        Debug.Log("<color=orange>[Conveyor]</color> 전장이 이동합니다!");
        GridManager.Instance.ShiftTerrain();

        List<ObstacleBase> movingObstacles = new List<ObstacleBase>();
        List<ZoneBase> movingZones = new List<ZoneBase>();

        for (int x = 0; x < GridManager.Instance.width; x++)
        {
            for (int y = minRow; y <= maxRow; y++)
            {
                var tile = GridManager.Instance.GetTile(new Vector2Int(x, y));
                if (tile == null) continue;

                if (tile.HasObstacle) movingObstacles.Add(tile.Obstacle);
                if (tile.Zone != null) movingZones.Add(tile.Zone);
            }
        }

        foreach (var obs in movingObstacles)
        {
            GridManager.Instance.RemoveObstacle(obs.currentPos);
            Vector2Int nextPos = obs.currentPos + Vector2Int.left;

            if (nextPos.x < 0) Destroy(obs.gameObject);
            else
            {
                obs.currentPos = nextPos;
                obs.transform.position = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                GridManager.Instance.RegisterObstacle(nextPos, obs);
            }
        }

        foreach (var zone in movingZones)
        {
            GridManager.Instance.RemoveZone(zone.currentPos);
            Vector2Int nextPos = zone.currentPos + Vector2Int.left;

            if (nextPos.x < 0) Destroy(zone.gameObject);
            else
            {
                zone.currentPos = nextPos;
                zone.transform.position = GridManager.Instance.GetWorldPosition(nextPos, 0.02f);
                GridManager.Instance.RegisterZone(nextPos, zone);
            }
        }
    }

    void SpawnNewColumn()
    {
        int spawnX = GridManager.Instance.width - 1;
        int spawnedCount = 0;

        for (int y = minRow; y <= maxRow; y++)
        {
            if (spawnedCount >= 2) break;

            // [수정] 8방향 체크: 주변에 이미 2개 이상 있으면 생성 금지 (뭉침 방지)
            if (CountObstaclesAround8(spawnX, y) >= 2) continue;

            // [수정] 가로 연속 배치 방지: 바로 왼쪽에 장애물이 있으면 90% 확률로 생성 안 함
            if (GridManager.Instance.IsObstacle(new Vector2Int(spawnX - 1, y)))
            {
                if (Random.Range(0, 100) < 90) continue;
            }

            if (Random.Range(0, 100) < obstacleSpawnChance)
            {
                if (obstaclePrefabs.Count > 0)
                {
                    SpawnObject(obstaclePrefabs, spawnX, y, true);
                    spawnedCount++;
                }
            }
        }

        // 영역 생성 (기존 유지)
        for (int y = minRow; y <= maxRow; y++)
        {
            if (GridManager.Instance.IsObstacle(new Vector2Int(spawnX, y))) continue;

            if (Random.Range(0, 100) < zoneSpawnChance)
            {
                if (zonePrefabs != null && zonePrefabs.Count > 0)
                    SpawnObject(zonePrefabs, spawnX, y, false);
            }
        }
    }

    // [신규] 8방향 주변 장애물 개수 체크 (본인 제외)
    int CountObstaclesAround8(int cx, int cy)
    {
        int count = 0;
        for (int x = cx - 1; x <= cx + 1; x++)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            {
                if (x == cx && y == cy) continue; // 나 자신 제외
                if (GridManager.Instance.IsObstacle(new Vector2Int(x, y))) count++;
            }
        }
        return count;
    }

    void SpawnObject(List<GameObject> prefabs, int x, int y, bool isObstacle)
    {
        if (prefabs.Count == 0) return;
        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        GameObject go = Instantiate(prefab);
        Vector2Int pos = new Vector2Int(x, y);

        if (isObstacle) go.GetComponent<ObstacleBase>().Initialize(pos);
        else go.GetComponent<ZoneBase>().Initialize(pos);
    }
}