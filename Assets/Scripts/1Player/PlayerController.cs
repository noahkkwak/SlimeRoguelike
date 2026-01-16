using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public int maxHp = 10;
    public int currentHp;
    public int attackPower = 2;
    public Vector2Int currentPos = new Vector2Int(3, 4);
    private PlayerAction selectedAction = PlayerAction.None;
    private bool isDead = false;

    public GameObject indicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    void Start() { currentHp = maxHp; UpdateVisual(); }

    void Update()
    {
        if (isDead || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None) ExecuteAction();
    }

    void TryMove(Vector2Int dir) { Vector2Int next = currentPos + dir; if (GridManager.Instance.IsWalkable(next) && next.y == 4) { currentPos = next; UpdateVisual(); FinishTurn(); } }

    void SelectAction(PlayerAction a) { ClearIndicators(); selectedAction = a; if (a == PlayerAction.Attack) ShowRange(); }

    void ExecuteAction()
    {
        if (selectedAction == PlayerAction.Attack) PerformAttack();
        FinishTurn();
    }

    void PerformAttack()
    {
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) return;
            EnemyBase target = TurnManager.Instance.GetEnemyAt(tPos);
            if (target != null) { target.TakeDamage(attackPower); return; }
        }
    }

    void ShowRange()
    {
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) break;
            activeIndicators.Add(Instantiate(indicatorPrefab, GridManager.Instance.GetWorldPosition(tPos, GridManager.Instance.indicatorHeight), Quaternion.identity));
        }
    }

    public void TakeDamage(int dmg)
    {
        if (selectedAction == PlayerAction.Defend) dmg -= Mathf.RoundToInt(dmg * 0.5f);
        currentHp -= dmg;
        if (currentHp <= 0) { isDead = true; Debug.Log("PLAYER DEAD"); }
    }

    void FinishTurn() { ClearIndicators(); if (selectedAction != PlayerAction.Defend) selectedAction = PlayerAction.None; TurnManager.Instance.OnPlayerActionCompleted(); }
    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}