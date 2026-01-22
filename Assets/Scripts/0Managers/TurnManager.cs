using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 플레이어 행동 타입 정의
public enum PlayerActionType { Move, Attack, Wait }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentState = TurnState.PlayerTurn;
    public List<EnemyBase> activeEnemies = new List<EnemyBase>();

    private void Awake() => Instance = this;

    private void Start()
    {
        SpawnEnemies();
        StartPlayerTurn(); // 게임 시작 시 플레이어 턴으로 시작
    }

    // =========================================================
    // [1. 턴 시작 및 입력 처리]
    // =========================================================

    // 플레이어의 입력을 받으면 바로 턴 실행 시퀀스로 진입
    public void ProcessTurn(PlayerActionType playerAction, Vector3 direction)
    {
        if (currentState != TurnState.PlayerTurn) return;

        StartCoroutine(ExecuteTurnSequence(playerAction, direction));
    }

    // =========================================================
    // [2. 턴 실행 시퀀스 (이동 -> 공격)]
    // =========================================================

    private IEnumerator ExecuteTurnSequence(PlayerActionType playerAction, Vector3 playerMoveDir)
    {
        currentState = TurnState.EnemyTurn; // 입력 차단
        PlayerController player = FindObjectOfType<PlayerController>();

        // -----------------------------------------------------
        // [Phase 1: 이동] 플레이어와 적의 이동을 동시에 처리
        // -----------------------------------------------------
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        // A. 플레이어 이동 (이동 명령인 경우)
        if (playerAction == PlayerActionType.Move && player != null)
        {
            moveCoroutines.Add(StartCoroutine(player.ExecuteMove(playerMoveDir)));
        }

        // B. 적 이동
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            // 적의 의도 계산 (이동할지 공격할지 결정)
            enemy.CalculateIntent();

            // 이동 전용 루틴 실행
            var routine = enemy.ExecuteMoveRoutine();
            if (routine != null) moveCoroutines.Add(StartCoroutine(routine));
        }

        // 모든 이동이 끝날 때까지 대기 (가장 긴 이동 시간 기준)
        foreach (var c in moveCoroutines) yield return c;

        // 이동 후 잠시 대기 (연출)
        yield return new WaitForSeconds(0.1f);


        // -----------------------------------------------------
        // [Phase 2: 공격] 플레이어와 적의 공격을 처리
        // -----------------------------------------------------
        List<Coroutine> attackCoroutines = new List<Coroutine>();

        // A. 플레이어 공격 (공격 명령인 경우)
        if (playerAction == PlayerActionType.Attack && player != null)
        {
            attackCoroutines.Add(StartCoroutine(player.ExecuteAttack()));
        }

        // B. 적 공격
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var routine = enemy.ExecuteAttackRoutine();
                if (routine != null) attackCoroutines.Add(StartCoroutine(routine));
            }
        }

        // 모든 공격 애니메이션 대기
        foreach (var c in attackCoroutines) yield return c;

        yield return new WaitForSeconds(0.2f); // 턴 종료 딜레이

        // -----------------------------------------------------
        // [3. 턴 종료 및 다음 턴 준비]
        // -----------------------------------------------------
        EndTurnAndPrepareNext();
    }

    private void EndTurnAndPrepareNext()
    {
        // null인 적(사망한 적) 리스트에서 정리
        activeEnemies.RemoveAll(e => e == null);

        // 다음 턴 적 의도 미리 계산 (플레이어에게 예고 표시용)
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.CalculateIntent();
        }

        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;
        Debug.Log("--- Player Turn Start ---");
    }

    // =========================================================
    // [유틸리티 및 오류 복구 함수]
    // =========================================================

    // [복구됨] EnemyBase에서 호출하는 함수
    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    // [복구됨] 적 스폰 로직 (기존 로직 유지)
    void SpawnEnemies()
    {
        activeEnemies.Clear();

        if (StageManager.Instance != null)
        {
            // 테스트용: 하단(SpawnY)에 적 생성
            int spawnY = GridManager.Instance.height - 1;
            int spawnCount = Random.Range(1, 3);

            List<int> availableX = new List<int>();
            for (int x = 0; x < GridManager.Instance.width - 1; x++) availableX.Add(x);

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

                    // Z축 기반 좌표계라면 (x, spawnY)를 적절히 변환하여 배치 필요
                    // EnemyBase.Initialize 내부 구현에 따름
                    enemy.Initialize(new Vector2Int(xPos, spawnY));
                    activeEnemies.Add(enemy);
                }
            }
        }
    }

    // PlayerController에서 호출하는 완료 신호 (현재 구조에선 ExecuteTurnSequence가 제어하므로 비워둠)
    public void OnPlayerActionCompleted() { }
}