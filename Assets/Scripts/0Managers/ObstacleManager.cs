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
        // ... (이동 로직은 기존과 동일하므로 생략, 그대로 유지해주세요) ...
        // [이전 코드의 MoveConveyorBelt 내용을 그대로 쓰시면 됩니다]
        Debug.Log("<color=orange>[Conveyor]</color> 전장이 이동합니다!");

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

        // 1. 장애물 생성
        for (int y = minRow; y <= maxRow; y++)
        {
            // [조건 1] 한 열에 최대 2개까지만 (총량 제한)
            if (spawnedCount >= 2) break;

            // [조건 2] 기획자님 제안: 인접(상/하/좌) 장애물이 2개 이상이면 생성 금지
            // 즉, 인접 장애물은 최대 1개여야 함
            if (CountAdjacentObstacles(spawnX, y) >= 2)
            {
                continue; // 이번 칸은 건너뜀
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

        // 2. 영역 생성 (장애물 없는 곳에)
        for (int y = minRow; y <= maxRow; y++)
        {
            if (GridManager.Instance.IsObstacle(new Vector2Int(spawnX, y))) continue;

            if (Random.Range(0, 100) < zoneSpawnChance)
            {
                if (zonePrefabs != null && zonePrefabs.Count > 0)
                {
                    SpawnObject(zonePrefabs, spawnX, y, false);
                }
            }
        }
    }

    // [신규] 인접한 타일(좌, 상, 하)의 장애물 개수를 셉니다.
    int CountAdjacentObstacles(int x, int y)
    {
        int count = 0;

        // 1. 왼쪽 (x-1) : 방금 이동해서 자리 잡은 녀석 확인
        if (GridManager.Instance.IsObstacle(new Vector2Int(x - 1, y))) count++;

        // 2. 아래쪽 (y-1) : 이번 루프에서 방금 생성된 녀석 확인
        if (GridManager.Instance.IsObstacle(new Vector2Int(x, y - 1))) count++;

        // 3. 위쪽 (y+1) : 보통 아직 생성 전이지만, 혹시 모르니 체크
        if (GridManager.Instance.IsObstacle(new Vector2Int(x, y + 1))) count++;

        return count;
    }

    void SpawnObject(List<GameObject> prefabs, int x, int y, bool isObstacle)
    {
        if (prefabs.Count == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        GameObject go = Instantiate(prefab);
        Vector2Int pos = new Vector2Int(x, y);

        if (isObstacle)
        {
            ObstacleBase obs = go.GetComponent<ObstacleBase>();
            obs.Initialize(pos);
        }
        else
        {
            ZoneBase zone = go.GetComponent<ZoneBase>();
            zone.Initialize(pos);
        }
    }
}