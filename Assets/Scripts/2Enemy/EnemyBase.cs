using UnityEngine;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    public EnemyData data;
    public int currentHp;
    public Vector2Int currentPos;
    public Vector2Int intendedPos;
    private int moveCounter = 0;
    private int attackCounter = 0;
    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    private bool isStunned => activeEffects.Exists(e => e.type == StatusType.Stun);

    public GameObject indicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    public void Initialize(EnemyData _data, Vector2Int startPos)
    {
        data = _data;
        currentHp = data.maxHp;
        currentPos = startPos;
        if (data.prefab != null)
        {
            GameObject model = Instantiate(data.prefab, transform);
            model.transform.localPosition = Vector3.zero;
        }
        moveCounter = data.moveCycle;
        attackCounter = data.attackCycle;
        UpdateVisual();
    }

    public void PlanTurn()
    {
        UpdateStatus();
        intendedPos = currentPos;
        if (isStunned) return;

        if (moveCounter >= data.moveCycle)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;

            int dirX = (player.currentPos.x > currentPos.x) ? 1 : (player.currentPos.x < currentPos.x ? -1 : 0);
            Vector2Int next = currentPos + new Vector2Int(dirX, 0);

            if (GridManager.Instance.IsWalkable(next))
            {
                intendedPos = next;
            }
        }
    }

    public void ApplyCollision()
    {
        TakeDamage(data.collisionDamage);
        AddStatus(StatusType.Stun, 1);
        intendedPos = currentPos; // 이동 취소
    }

    public void ExecuteTurn()
    {
        if (isStunned)
        {
            Debug.Log($"{data.enemyName}: 기절 상태로 턴을 넘깁니다.");
        }
        else
        {
            if (intendedPos != currentPos)
            {
                currentPos = intendedPos;
                moveCounter = 0;
            }
            moveCounter++;

            if (attackCounter >= data.attackCycle)
            {
                PerformAttack();
                attackCounter = 0;
            }
            else
            {
                ShowAttackRange();
            }
            attackCounter++;
        }
        UpdateVisual();
    }

    void PerformAttack()
    {
        var player = FindObjectOfType<PlayerController>();
        for (int i = 1; i <= data.attackRange; i++)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
            if (GridManager.Instance.IsObstacle(tPos) && data.attackType == AttackType.Direct) break;
            if (player.currentPos == tPos)
            {
                Debug.Log($"<color=orange>[공격]</color> {data.enemyName} -> 플레이어 피격!");
                player.TakeDamage(data.attackPower);
                return;
            }
        }
    }

    void ShowAttackRange()
    {
        ClearIndicators();
        var player = FindObjectOfType<PlayerController>();
        for (int i = 1; i <= data.attackRange; i++)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
            if (GridManager.Instance.IsObstacle(tPos)) { if (data.attackType == AttackType.Direct) break; else continue; }

            if (data.attackType == AttackType.Direct) SpawnIndicator(tPos);
            else if (player != null && tPos == player.currentPos) { SpawnIndicator(tPos); break; }
        }
    }

    public void TakeDamage(int dmg)
    {
        currentHp -= dmg;
        Debug.Log($"<color=red>[피격]</color> {data.enemyName} HP: {currentHp}");
        if (currentHp <= 0)
        {
            Debug.Log($"<color=black>[사망]</color> {data.enemyName} 처치됨.");
            TurnManager.Instance.activeEnemies.Remove(this);
            Destroy(gameObject);
        }
    }

    public void AddStatus(StatusType t, int d) => activeEffects.Add(new StatusEffect(t, d));
    void UpdateStatus()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (--activeEffects[i].duration <= 0) activeEffects.RemoveAt(i);
        }
    }
    void SpawnIndicator(Vector2Int p)
    {
        if (!GridManager.Instance.IsInsideGrid(p)) return;
        activeIndicators.Add(Instantiate(indicatorPrefab, GridManager.Instance.GetWorldPosition(p, GridManager.Instance.indicatorHeight), Quaternion.identity));
    }
    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}