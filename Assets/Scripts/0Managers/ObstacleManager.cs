using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    [Header("Conveyor Settings")]
    public int movePeriod = 2; // 이동 주기 (2턴)
    private int currentTurnCount = 0;

    [Header("Spawn Settings")]
    public List<GameObject> obstaclePrefabs; // 생성할 장애물 목록
    public int minRow = 2; // 중앙 전장 시작 (0부터 시작하므로 3번째 줄)
    public int maxRow = 4; // 중앙 전장 끝 (5번째 줄)
    [Range(0, 100)] public int spawnChance = 40; // 장애물 생성 확률 (%)

    void Awake() => Instance = this;

    // 턴 시작 시 호출 (TurnManager에서 부름)
    public void OnTurnStart()
    {
        currentTurnCount++;

        // 2턴마다 이동 실행
        if (currentTurnCount % movePeriod == 0)
        {
            MoveConveyorBelt();
            SpawnNewColumn();
        }
    }

    // 1단계: 기존 장애물 왼쪽으로 밀기
    void MoveConveyorBelt()
    {
        Debug.Log("<color=orange>[Conveyor]</color> 전장이 이동합니다!");

        // 왼쪽부터 처리하면 덮어씌워질 위험이 있으므로,
        // 이동할 장애물들의 정보를 먼저 싹 긁어모읍니다.
        List<ObstacleBase> movingObstacles = new List<ObstacleBase>();

        for (int x = 0; x < GridManager.Instance.width; x++)
        {
            for (int y = minRow; y <= maxRow; y++)
            {
                var tile = GridManager.Instance.GetTile(new Vector2Int(x, y));
                if (tile != null && tile.HasObstacle)
                {
                    movingObstacles.Add(tile.Obstacle);
                }
            }
        }

        // 수집된 장애물들을 이동시킴
        foreach (var obs in movingObstacles)
        {
            // 그리드에서 일단 제거 (내 자리를 비움)
            GridManager.Instance.RemoveObstacle(obs.currentPos);

            Vector2Int nextPos = obs.currentPos + Vector2Int.left; // (-1, 0)

            // 맵 밖으로 나가면 파괴
            if (nextPos.x < 0)
            {
                Destroy(obs.gameObject); // 객체 파괴
            }
            else
            {
                // 새 위치로 정보 갱신
                obs.currentPos = nextPos;
                obs.transform.position = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);

                // 새 위치의 그리드에 재등록
                // (만약 거기에 누군가 있다면? 지금은 비어있다고 가정. 추후 충돌 처리 필요 시 여기서)
                GridManager.Instance.RegisterObstacle(nextPos, obs);
            }
        }
    }

    // 2단계: 오른쪽 끝에 새 장애물 채우기
    void SpawnNewColumn()
    {
        int spawnX = GridManager.Instance.width - 1; // 가장 오른쪽 열
        int obstaclesInThisColumn = 0; // 한 열에 너무 많이 생기지 않게 제한

        // 2행~4행 사이를 순회
        for (int y = minRow; y <= maxRow; y++)
        {
            // 최대 2개까지만 생성 (기획 의도 반영)
            if (obstaclesInThisColumn >= 2) continue;

            // 확률 체크
            if (Random.Range(0, 100) < spawnChance)
            {
                if (obstaclePrefabs.Count > 0)
                {
                    // 랜덤 프리팹 선택
                    GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
                    GameObject go = Instantiate(prefab);

                    ObstacleBase obs = go.GetComponent<ObstacleBase>();
                    obs.Initialize(new Vector2Int(spawnX, y));

                    obstaclesInThisColumn++;
                }
            }
        }
    }
}