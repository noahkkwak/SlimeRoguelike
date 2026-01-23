using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using SlimeRoguelike; <-- 삭제됨

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
            ObstacleManager.Instance.OnTurnStart();

        GridManager.Instance.ClearReservations();

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.CalculateIntent();
                GridManager.Instance.TryReserveTile(enemy.currentPos);
            }
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

    IEnumerator ExecuteTurnPhase()
    {
        currentState = TurnState.EnemyTurn;

        // 1. 적 이동
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteMove();
        yield return new WaitForSeconds(0.3f);

        // 2. 환경(전장) 이동
        currentState = TurnState.EnvironmentAct;
        yield return StartCoroutine(GridManager.Instance.ScrollCentralRows());

        // 3. 플레이어 공격 판정
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResolveBufferedAction();
        }
        yield return new WaitForSeconds(0.2f);

        // 4. 적 공격 실행
        currentState = TurnState.EnemyAct;
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