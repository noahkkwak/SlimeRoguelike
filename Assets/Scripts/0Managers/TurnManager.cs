using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TurnState 정의 포함
public enum TurnState { PlayerTurn, Processing, EnemyTurn, GameOver }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public TurnState currentState = TurnState.PlayerTurn;

    // [변경] EnemyData 리스트와 BasePrefab 변수를 제거했습니다.
    // 대신 스폰된 적들을 관리하는 리스트만 남깁니다.
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;

    void Start()
    {
        // StageManager가 없으면 에러 방지를 위해 체크
        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager가 씬에 없습니다!");
            return;
        }
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        // 1행(y=0)에 랜덤하게 1~3마리 배치
        int count = Random.Range(1, 4);

        // 0~6 인덱스 중 랜덤 선택 (중복 방지)
        List<int> availableX = new List<int>();
        for (int i = 0; i < GridManager.Instance.width; i++) availableX.Add(i);

        for (int i = 0; i < count; i++)
        {
            if (availableX.Count == 0) break;
            int randomIndex = Random.Range(0, availableX.Count);
            int x = availableX[randomIndex];
            availableX.RemoveAt(randomIndex);

            // [변경] StageManager에게 프리팹 요청
            GameObject prefabToSpawn = StageManager.Instance.GetRandomEnemyPrefab();

            if (prefabToSpawn != null)
            {
                GameObject go = Instantiate(prefabToSpawn); // 프리팹 생성
                EnemyBase eb = go.GetComponent<EnemyBase>();

                // 위치 초기화 (데이터는 프리팹에 있는 것을 그대로 사용)
                eb.Initialize(new Vector2Int(x, 0));
                activeEnemies.Add(eb);

                Debug.Log($"<color=cyan>[Spawn]</color> {eb.data.enemyName} (Stage {StageManager.Instance.currentStage})");
            }
        }
    }

    public EnemyBase GetEnemyAt(Vector2Int p) => activeEnemies.Find(e => e.currentPos == p);

    public void OnPlayerActionCompleted()
    {
        if (currentState == TurnState.PlayerTurn)
            StartCoroutine(ProcessTurns());
    }

    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
        // 모든 적 처치 시 로직 추가 가능 (예: StageManager.Instance.NextStage())
    }

    IEnumerator ProcessTurns()
    {
        currentState = TurnState.Processing;
        yield return new WaitForSeconds(0.2f);

        // 1. 계획
        foreach (var e in activeEnemies) e.PlanTurn();

        // 2. 충돌 체크
        Dictionary<Vector2Int, List<EnemyBase>> moveTargets = new Dictionary<Vector2Int, List<EnemyBase>>();
        foreach (var e in activeEnemies)
        {
            if (!moveTargets.ContainsKey(e.intendedPos)) moveTargets[e.intendedPos] = new List<EnemyBase>();
            moveTargets[e.intendedPos].Add(e);
        }
        foreach (var kvp in moveTargets)
        {
            if (kvp.Value.Count > 1) foreach (var e in kvp.Value) e.ApplyCollision();
        }

        // 3. 실행 (리스트 복사본 사용)
        List<EnemyBase> enemyListSnapshot = new List<EnemyBase>(activeEnemies);
        foreach (var e in enemyListSnapshot)
        {
            if (e != null) e.ExecuteTurn();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.3f);
        currentState = TurnState.PlayerTurn;

        // [신규] 플레이어에게 턴 시작을 알려 상태(방어 등)를 리셋시킴
        var player = FindObjectOfType<PlayerController>();
        if (player) player.OnTurnStart();

        Debug.Log("<color=green>--- 플레이어 턴 시작 ---</color>");
    }
}