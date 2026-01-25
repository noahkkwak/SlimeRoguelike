using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    [Header("Game Flow")]
    public int currentTurnCount = 0; // 턴 카운터

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
        // (기존 스폰 로직 유지 - 필요 시 수정 가능)
        activeEnemies.Clear();
        if (StageManager.Instance == null) return;
        // 예시: 적은 오른쪽 끝(width-1)이나 특정 위치에 생성
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;
        currentTurnCount++; // 턴 시작 시 카운트 증가
        Debug.Log($"<color=cyan>Turn {currentTurnCount}</color>");

        GridManager.Instance.ClearReservations();

        // 1. [순서 1] 적 행동 예고 (Intent)
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
    }

    public void OnPlayerActionCompleted()
    {
        if (currentState == TurnState.PlayerTurn)
            StartCoroutine(ExecuteTurnPhase());
    }

    // [핵심] 네가 요청한 턴 구조 (3~5번 단계)
    IEnumerator ExecuteTurnPhase()
    {
        currentState = TurnState.EnvironmentAct;

        // [순서 3] 장애물 이동 및 생성 (2턴마다)
        // currentTurnCount가 2의 배수일 때 이동 (2, 4, 6...)
        if (currentTurnCount % 2 == 0)
        {
            // 3-1. 전장 이동 (왼쪽으로 밀기)
            yield return StartCoroutine(GridManager.Instance.ScrollCentralRowsLeft());

            // 3-2. 새로운 장애물 스폰
            if (ObstacleManager.Instance != null)
            {
                ObstacleManager.Instance.SpawnRandomObstacleAtRightEdge();
            }
            yield return new WaitForSeconds(0.2f);
        }

        currentState = TurnState.EnemyTurn;

        // [순서 4] 적 행동
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteMove();
        yield return new WaitForSeconds(0.2f);

        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteAttack();
        yield return new WaitForSeconds(0.3f);

        // [순서 5] 플레이어/적 행동 결과 판정 (피격, 사망 등은 각 ExecuteAttack 내부에서 즉시 처리됨)
        // 필요하다면 여기서 지연된 데미지 처리 등을 할 수 있음

        StartPlayerTurn();
    }

    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    public EnemyBase GetEnemyAt(Vector2Int p) => activeEnemies.Find(e => e.currentPos == p);
}