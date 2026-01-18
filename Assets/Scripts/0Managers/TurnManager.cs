using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    // 현재 턴의 상태 (플레이어 턴인지, 적이 행동하는 중인지)
    public TurnState currentState = TurnState.PlayerTurn;

    // 현재 살아있는 적 리스트
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;

    void Start()
    {
        // 1. 그리드 초기화 (기본 5x7 전장)
        // 추후 StageManager에서 이 값을 받아와 스테이지별로 다르게 설정 가능
        if (GridManager.Instance != null)
            GridManager.Instance.SetupGrid(5, 7);
        else
            Debug.LogError("GridManager가 씬에 없습니다!");

        // 2. 적 스폰 (StageManager가 있다면 연동, 없으면 테스트용 로직)
        if (StageManager.Instance != null)
        {
            // StageManager 로직에 따라 스폰 (이전 코드 활용)
            // SpawnEnemies(); 
        }

        // 3. 게임 시작! 첫 턴은 플레이어부터 시작
        StartPlayerTurn();
    }

    // =================================================================
    // [Phase 1 & 2] 플레이어 턴 시작 (적의 의도가 먼저 공개됨)
    // =================================================================
    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;

        // A. 모든 적에게 "다음 턴에 뭐 할지 정해서 인디케이터 띄워!" 라고 명령
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.CalculateIntent();
        }

        // B. 플레이어 상태 리셋 (방어 태세 초기화 등)
        var player = FindObjectOfType<PlayerController>();
        if (player) player.OnTurnStart();

        Debug.Log("<color=green>--- Player Turn Start (적의 의도가 표시되었습니다) ---</color>");
    }

    // 플레이어가 행동(이동/공격/방어)을 마치면 호출됨
    public void OnPlayerActionCompleted()
    {
        if (currentState == TurnState.PlayerTurn)
            StartCoroutine(ExecuteEnemyTurnPhase());
    }

    // =================================================================
    // [Phase 3] 적 행동 실행 (이동 -> 공격 순차 처리)
    // =================================================================
    IEnumerator ExecuteEnemyTurnPhase()
    {
        currentState = TurnState.EnemyTurn;

        // 1. 모든 적 이동 실행
        // (의도했던 이동 위치로 일제히 이동합니다)
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.ExecuteMove();
        }

        yield return new WaitForSeconds(0.3f); // 이동 연출 대기 시간

        // 2. 모든 적 공격 실행
        // (의도했던 타겟 위치를 타격합니다)
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.ExecuteAttack();
        }

        yield return new WaitForSeconds(0.4f); // 공격 연출 및 피격 대기 시간

        // 3. 모든 처리가 끝났으니 다시 플레이어 턴으로 넘김
        StartPlayerTurn();
    }

    // 적이 사망했을 때 리스트에서 제거
    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }
}