using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    // [신규] 에디터 설정용 데이터 구조
    [System.Serializable]
    public struct ObstacleSpawnData
    {
        public string name;             // 그냥 보기 편하라고 넣은 이름표
        public GameObject prefab;       // 설치할 프리팹 (ObstacleObject 컴포넌트 필수)
        public Vector2Int coordinate;   // 배치할 좌표
    }

    [Header("Setup")]
    // 여기에 장애물 프리팹과 좌표를 등록하면 됨
    public List<ObstacleSpawnData> initialObstacles = new List<ObstacleSpawnData>();

    [Header("Runtime State")]
    // 현재 게임에 존재하는 장애물 명단
    public List<ObstacleObject> activeObstacles = new List<ObstacleObject>();

    void Awake() => Instance = this;

    void Start()
    {
        // 게임 시작 시 등록된 데이터대로 장애물 생성
        SpawnInitialObstacles();
    }

    void SpawnInitialObstacles()
    {
        if (GridManager.Instance == null) return;

        foreach (var data in initialObstacles)
        {
            if (data.prefab == null) continue;

            // 1. 해당 좌표가 유효한지 체크
            if (!GridManager.Instance.IsInsideGrid(data.coordinate))
            {
                Debug.LogWarning($"[ObstacleManager] 좌표 {data.coordinate}는 맵 밖입니다.");
                continue;
            }

            // 2. 이미 무언가 있다면 패스
            var tile = GridManager.Instance.GetTile(data.coordinate);
            if (tile.IsBlocked)
            {
                Debug.LogWarning($"[ObstacleManager] 좌표 {data.coordinate}는 이미 막혀있습니다.");
                continue;
            }

            // 3. 생성 및 초기화
            GameObject go = Instantiate(data.prefab);
            ObstacleObject obs = go.GetComponent<ObstacleObject>();

            if (obs != null)
            {
                // 생성된 오브젝트의 이름 변경 (선택사항)
                go.name = string.IsNullOrEmpty(data.name) ? data.prefab.name : data.name;

                // 위치 설정 및 그리드 등록
                obs.Initialize(data.coordinate);

                // 관리 리스트에 추가
                RegisterObstacle(obs);
            }
            else
            {
                Debug.LogError($"[ObstacleManager] 프리팹 {data.prefab.name}에 'ObstacleObject' 컴포넌트가 없습니다!");
            }
        }
    }

    public void OnTurnStart()
    {
        // 턴 시작 시 장애물 관련 로직이 있다면 여기서 수행
    }

    // 장애물이 생성될 때 명단에 추가
    public void RegisterObstacle(ObstacleObject obs)
    {
        if (!activeObstacles.Contains(obs)) activeObstacles.Add(obs);
    }

    // 장애물이 파괴될 때 명단에서 제거
    public void RemoveObstacle(ObstacleObject obs)
    {
        if (activeObstacles.Contains(obs)) activeObstacles.Remove(obs);
    }
}