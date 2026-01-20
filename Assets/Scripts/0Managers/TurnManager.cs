using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;

    void Start()
    {
        SpawnEnemies();
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return null;
        StartPlayerTurn();
    }

    void SpawnEnemies()
    {
        activeEnemies.Clear();
        if (StageManager.Instance == null) return;

        int spawnCount = Random.Range(1, 3);
        List<int> availableX = Enumerable.Range(0, GridManager.Instance.width).ToList();

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableX.Count == 0) break;
            int randIndex = Random.Range(0, availableX.Count);
            int xPos = availableX[randIndex];
            availableX.RemoveAt(randIndex);

            GameObject enemyPrefab = StageManager.Instance.GetRandomEnemyPrefab();
            if (enemyPrefab != null)
            {
                GameObject go = Instantiate(enemyPrefab);
                EnemyBase enemy = go.GetComponent<EnemyBase>();
                enemy.Initialize(new Vector2Int(xPos, 0));
                activeEnemies.Add(enemy);
            }
        }
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;

        if (ObstacleManager.Instance != null)
        {
            ObstacleManager.Instance.OnTurnStart();
        }

        GridManager.Instance.ClearReservations();

        // 1. 적 선점
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) GridManager.Instance.TryReserveTile(enemy.currentPos);
        }

        // 2. 의도 계산
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.CalculateIntent();
        }

        var player = FindObjectOfType<PlayerController>();
        if (player) player.OnTurnStart();

        Debug.Log("<color=green>--- Player Turn Start ---</color>");
    }

    public void OnPlayerActionCompleted()
    {
        if (currentState == TurnState.PlayerTurn)
            StartCoroutine(ExecuteTurnPhase());
    }

    // [핵심 변경] 턴 실행 순서 재구성
    IEnumerator ExecuteTurnPhase()
    {
        currentState = TurnState.EnemyTurn;

        // 1. 적들이 먼저 이동함 (플레이어는 구경 중)
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteMove();
        yield return new WaitForSeconds(0.3f);

        // 2. 적이 이동을 마친 후, 플레이어의 예약된 액션(공격) 발동!
        // (이때 적이 내 사거리 안으로 들어왔다면 피격됨)
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResolveBufferedAction();
        }
        yield return new WaitForSeconds(0.2f); // 타격감 연출 시간

        // 3. 살아남은 적들이 플레이어를 공격
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteAttack();
        yield return new WaitForSeconds(0.4f);

        StartPlayerTurn();
    }

    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    public EnemyBase GetEnemyAt(Vector2Int p)
    {
        return activeEnemies.Find(e => e.currentPos == p);
    }
}