using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;
    void Start()
    {
        if (GridManager.Instance != null) GridManager.Instance.SetupGrid(5, 7);
        if (StageManager.Instance != null) { /* 스폰 로직 */ }
        StartPlayerTurn();
    }

    // =================================================================
    // [Phase 1 & 2] 플레이어 턴 시작 (적의 의도가 먼저 공개됨)
    // =================================================================
    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;

        // [신규] 적들이 생각하기 전에 예약판 초기화
        GridManager.Instance.ClearReservations();

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
            StartCoroutine(ExecuteEnemyTurnPhase());
    }

    IEnumerator ExecuteEnemyTurnPhase()
    {
        currentState = TurnState.EnemyTurn;

        // 1. 이동 실행
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteMove();
        yield return new WaitForSeconds(0.3f);

        // 2. 공격 실행
        foreach (var enemy in activeEnemies) if (enemy != null) enemy.ExecuteAttack();
        yield return new WaitForSeconds(0.4f); // 공격 연출 및 피격 대기 시간

        // 3. 모든 처리가 끝났으니 다시 플레이어 턴으로 넘김
        StartPlayerTurn();
    }

    // 적이 사망했을 때 리스트에서 제거
    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }
    public EnemyBase GetEnemyAt(Vector2Int p)
    {
        return activeEnemies.Find(e => e.currentPos == p);
    }
}