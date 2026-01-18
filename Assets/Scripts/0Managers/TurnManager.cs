using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 리스트 처리를 위해 추가

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    void Awake() => Instance = this;

    void Start()
    {
        // 1. 그리드 초기화 (기본 5x7 전장)
        if (GridManager.Instance != null)
            GridManager.Instance.SetupGrid(7, 5);
        else
            Debug.LogError("GridManager가 씬에 없습니다!");

        // 2. 적 스폰 (StageManager 연동)
        SpawnEnemies();

        // 3. 게임 시작! 첫 턴은 플레이어부터 시작
        // (적 스폰 후 약간의 딜레이를 주어 안정적으로 시작)
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return null; // 한 프레임 대기
        StartPlayerTurn();
    }

    // [신규 구현] 테스트용 랜덤 적 스폰 (1~2마리)
    void SpawnEnemies()
    {
        activeEnemies.Clear();

        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager가 없습니다! 적을 생성할 수 없습니다.");
            return;
        }

        // 1~2마리 랜덤 결정
        int spawnCount = Random.Range(1, 3);

        // 스폰 가능한 X좌표 리스트 (0 ~ Width-1)
        List<int> availableX = Enumerable.Range(0, GridManager.Instance.width).ToList();

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableX.Count == 0) break;

            // 랜덤 위치 선택
            int randIndex = Random.Range(0, availableX.Count);
            int xPos = availableX[randIndex];
            availableX.RemoveAt(randIndex); // 중복 방지

            // 랜덤 적 프리팹 가져오기
            GameObject enemyPrefab = StageManager.Instance.GetRandomEnemyPrefab();

            if (enemyPrefab != null)
            {
                // 적 생성 (위치는 0행 고정)
                GameObject go = Instantiate(enemyPrefab);
                EnemyBase enemy = go.GetComponent<EnemyBase>();

                // 초기화 및 리스트 추가
                enemy.Initialize(new Vector2Int(xPos, 0));
                activeEnemies.Add(enemy);

                Debug.Log($"<color=red>[Spawn]</color> {enemy.data.enemyName} 생성됨 (위치: {xPos}, 0)");
            }
        }
    }

    // =================================================================
    // [Phase 1 & 2] 플레이어 턴 시작
    // =================================================================
    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;

        // 예약 초기화
        GridManager.Instance.ClearReservations();

        // 적 의도 계산
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.CalculateIntent();
        }

        // 플레이어 상태 리셋
        var player = FindObjectOfType<PlayerController>();
        if (player) player.OnTurnStart();

        Debug.Log("<color=green>--- Player Turn Start ---</color>");
    }

    public void OnPlayerActionCompleted()
    {
        if (currentState == TurnState.PlayerTurn)
            StartCoroutine(ExecuteEnemyTurnPhase());
    }

    // =================================================================
    // [Phase 3] 적 행동 실행
    // =================================================================
    IEnumerator ExecuteEnemyTurnPhase()
    {
        currentState = TurnState.EnemyTurn;

        // 1. 이동 실행
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.ExecuteMove();
        }

        yield return new WaitForSeconds(0.3f);

        // 2. 공격 실행
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.ExecuteAttack();
        }

        yield return new WaitForSeconds(0.4f);

        StartPlayerTurn();
    }

    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    // 특정 위치에 있는 적 반환 (플레이어 공격용)
    public EnemyBase GetEnemyAt(Vector2Int p)
    {
        return activeEnemies.Find(e => e.currentPos == p);
    }
}