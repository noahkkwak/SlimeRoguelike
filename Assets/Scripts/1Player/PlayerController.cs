using UnityEngine;
using System.Collections.Generic;

public enum PlayerAction { None, Attack, Defend }

public class PlayerController : MonoBehaviour
{
    public Vector2Int currentPos = new Vector2Int(2, 4);
    private PlayerAction selectedAction = PlayerAction.None;

    [Header("Visuals")]
    public GameObject attackIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    void Start()
    {
        UpdateVisualPosition();
    }

    void Update()
    {
        // TurnManager 상태 확인 (Null 체크 포함으로 더 안전하게)
        if (TurnManager.Instance == null || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        // 1. 이동 조작 (즉시 실행)
        if (Input.GetKeyDown(KeyCode.A)) { if (TryMove(Vector2Int.left)) FinishTurn(); }
        else if (Input.GetKeyDown(KeyCode.D)) { if (TryMove(Vector2Int.right)) FinishTurn(); }

        // 2. 행동 선택 (W:공격, S:방어)
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        else if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);

        // 3. 행동 확정 실행 (Space)
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None)
        {
            ExecuteAction();
        }
    }

    void SelectAction(PlayerAction action)
    {
        ClearIndicators();
        selectedAction = action;
        Debug.Log($"행동 선택됨: {action} (Space를 눌러 확정)");
        if (action == PlayerAction.Attack) ShowAttackRange();
    }

    bool TryMove(Vector2Int dir)
    {
        // 이동 시에는 기존에 선택했던 공격/방어 준비를 취소함
        ClearIndicators();
        selectedAction = PlayerAction.None;

        Vector2Int nextPos = currentPos + dir;

        // 5행(Index 4) 영역에서만 움직이도록 제한
        if (nextPos.y == 4 && GridManager.Instance.IsInsideGrid(nextPos) && !GridManager.Instance.IsBlocked(nextPos))
        {
            currentPos = nextPos;
            UpdateVisualPosition();
            return true;
        }
        return false;
    }

    void ExecuteAction()
    {
        // 실제 공격 및 방어 로직은 이후에 추가 예정
        if (selectedAction == PlayerAction.Attack) Debug.Log("플레이어: 공격 실행!");
        else if (selectedAction == PlayerAction.Defend) Debug.Log("플레이어: 방어 실행!");

        FinishTurn();
    }

    void FinishTurn()
    {
        ClearIndicators();
        selectedAction = PlayerAction.None;
        TurnManager.Instance.OnPlayerActionCompleted();
    }

    void ShowAttackRange()
    {
        // 정면 방향(y 감소 방향)으로 장애물이 없을 때까지 범위 표시
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int targetPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsBlocked(targetPos)) break;
            SpawnIndicator(targetPos);
        }
    }

    void SpawnIndicator(Vector2Int pos)
    {
        if (attackIndicatorPrefab == null) return;
        Vector3 worldPos = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.indicatorHeight);
        GameObject go = Instantiate(attackIndicatorPrefab, worldPos, Quaternion.identity);
        activeIndicators.Add(go);
    }

    void ClearIndicators()
    {
        foreach (var go in activeIndicators)
        {
            if (go != null) Destroy(go);
        }
        activeIndicators.Clear();
    }

    void UpdateVisualPosition()
    {
        if (GridManager.Instance == null) return;
        transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    }
}