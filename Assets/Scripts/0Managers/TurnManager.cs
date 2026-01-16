using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TurnState { PlayerTurn, Processing, EnemyTurn }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyData> stageEnemies;
    public GameObject enemyBasePrefab;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;
    void Start()
    {
        if (stageEnemies.Count == 0) Debug.LogError("TurnManager: StageEnemies가 비어있습니다!");
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int count = Random.Range(1, 4);
        List<int> availableX = new List<int>();
        for (int i = 0; i < GridManager.Instance.width; i++) availableX.Add(i);

        for (int i = 0; i < count; i++)
        {
            if (availableX.Count == 0) break;
            int randomIndex = Random.Range(0, availableX.Count);
            int x = availableX[randomIndex];
            availableX.RemoveAt(randomIndex);

            GameObject go = Instantiate(enemyBasePrefab);
            EnemyBase eb = go.GetComponent<EnemyBase>();
            // 0행에 스폰
            eb.Initialize(stageEnemies[Random.Range(0, stageEnemies.Count)], new Vector2Int(x, 0));
            activeEnemies.Add(eb);

            Debug.Log($"<color=cyan>[Spawn]</color> {eb.data.enemyName} 가 ({x}, 0)에 생성됨.");
        }
    }

    public EnemyBase GetEnemyAt(Vector2Int p) => activeEnemies.Find(e => e.currentPos == p);

    public void OnPlayerActionCompleted() => StartCoroutine(ProcessTurns());

    IEnumerator ProcessTurns()
    {
        currentState = TurnState.Processing;
        yield return new WaitForSeconds(0.3f);

        Debug.Log("<color=yellow>--- 적 행동 단계 시작 ---</color>");

        // 1. 계획
        foreach (var e in activeEnemies) e.PlanTurn();

        // 2. 충돌 체크 (이동하려는 칸에 2명 이상인지)
        var dict = new Dictionary<Vector2Int, List<EnemyBase>>();
        foreach (var e in activeEnemies)
        {
            if (!dict.ContainsKey(e.intendedPos)) dict[e.intendedPos] = new List<EnemyBase>();
            dict[e.intendedPos].Add(e);
        }

        foreach (var kvp in dict)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.Log($"<color=red>[충돌 발생]</color> {kvp.Key} 위치에서 적들끼리 부딪힘!");
                foreach (var e in kvp.Value) e.ApplyCollision();
            }
        }

        // 3. 실행
        foreach (var e in activeEnemies) e.ExecuteTurn();

        yield return new WaitForSeconds(0.5f);
        currentState = TurnState.PlayerTurn;
        Debug.Log("<color=green>--- 플레이어 턴 시작 ---</color>");
    }
}