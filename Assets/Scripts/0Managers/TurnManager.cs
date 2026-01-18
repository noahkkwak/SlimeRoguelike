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
        // [수정] 강제 크기 설정 삭제. 이제 GridManager의 Inspector 설정을 따릅니다.
        // if (GridManager.Instance != null) GridManager.Instance.SetupGrid(5, 7); 

        // 2. 적 스폰
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
        if (StageManager.Instance == null) return; // StageManager가 없으면 스폰 안 함 (테스트 시 주의)

        // 에러 방지: StageManager가 없어도 테스트하려면 아래 주석을 해제하고 임의 코드를 넣으세요.
        // 하지만 기획자님은 StageManager를 쓰시므로 유지합니다.

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

        GridManager.Instance.ClearReservations();

        // 1. 모든 적들이 현재 위치를 먼저 선점 (겹침 방지)
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
        yield return new WaitForSeconds(0.4f);

        StartPlayerTurn();
    }

    public void OnEnemyDead(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    // [복구] 이 함수가 있어야 PlayerController 오류가 해결됩니다.
    public EnemyBase GetEnemyAt(Vector2Int p)
    {
        return activeEnemies.Find(e => e.currentPos == p);
    }
}