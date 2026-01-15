using UnityEngine;
using System.Collections;

public enum TurnState { PlayerTurn, Processing }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public TurnState currentState = TurnState.PlayerTurn;

    void Awake() => Instance = this;

    public void OnPlayerActionCompleted()
    {
        StartCoroutine(ProcessTurns());
    }

    private IEnumerator ProcessTurns()
    {
        currentState = TurnState.Processing;
        yield return new WaitForSeconds(0.1f);

        // 모든 적의 행동 실행
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (var enemy in enemies) enemy.ExecuteTurn();

        yield return new WaitForSeconds(0.3f);

        currentState = TurnState.PlayerTurn;
        Debug.Log("--- 플레이어 턴 시작 ---");
    }
}