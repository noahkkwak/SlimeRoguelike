using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyIntent
{
    public IntentType type;
    public Vector2Int targetPos;
    public List<Vector2Int> areaTargets = new List<Vector2Int>();
}

public class EnemyBase : MonoBehaviour
{
    [Header("Settings & Data")]
    public EnemyData data;
    public GameObject attackIndicatorPrefab;
    public GameObject moveIndicatorPrefab;

    [Header("State")]
    public EnemyState state = EnemyState.Idle;
    public int currentHp;
    public Vector2Int currentPos;

    public EnemyIntent currentIntent = new EnemyIntent();

    [SerializeField] private int moveCooldownTimer = 0;
    [SerializeField] private int attackCooldownTimer = 0;

    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool IsStunned => activeEffects.Exists(e => e.type == StatusType.Stun);

    private List<GameObject> activeIndicators = new List<GameObject>();
    private Animator animator;

    public void Initialize(Vector2Int startPos)
    {
        if (data == null) return;
        currentHp = data.maxHp;
        currentPos = startPos;
        state = EnemyState.Idle;
        moveCooldownTimer = 0;
        attackCooldownTimer = 0;
        animator = GetComponentInChildren<Animator>();
        UpdateVisual();
    }

    // [Phase 1: Plan]
    public void CalculateIntent()
    {
        ClearIndicators();
        UpdateStatus();

        if (IsStunned)
        {
            state = EnemyState.Stunned;
            currentIntent.type = IntentType.Wait;
            if (animator) animator.SetBool("IsStunned", true);
            // 기절 시에는 선점한 자리 유지 (아무것도 안 함)
            return;
        }
        else
        {
            if (animator) animator.SetBool("IsStunned", false);
        }

        state = EnemyState.Ready;
        bool intentSet = false;

        // 2. 공격 시도
        if (attackCooldownTimer >= data.attackCycle)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                int xDiff = Mathf.Abs(player.currentPos.x - currentPos.x);
                if (xDiff <= 2)
                {
                    SetAttackIntent();
                    intentSet = true;
                }
            }
        }

        // 3. 이동 시도
        if (!intentSet && moveCooldownTimer >= data.moveCycle)
        {
            intentSet = SetMoveIntent();
        }

        // 4. 대기
        if (!intentSet)
        {
            SetWaitIntent();
        }
    }

    void SetAttackIntent()
    {
        currentIntent.type = IntentType.Attack;
        currentIntent.areaTargets.Clear();

        // [중요] 이미 TurnManager에서 내 자리를 예약했으므로 추가 예약 불필요
        // 공격은 제자리에서 하므로 자리 반납도 안 함.

        if (data.attackType == AttackType.Direct)
        {
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                if (!GridManager.Instance.IsInsideGrid(tPos) || GridManager.Instance.IsObstacle(tPos)) break;
                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
        else if (data.attackType == AttackType.Arcing)
        {
            int targetY = Mathf.Min(currentPos.y + data.attackRange, GridManager.Instance.height - 1);
            Vector2Int tPos = new Vector2Int(currentPos.x, targetY);
            if (GridManager.Instance.IsInsideGrid(tPos))
            {
                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
        Debug.Log($"<color=yellow>[의도]</color> {data.enemyName}: 공격 준비");
    }

    bool SetMoveIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return false;

        int dirX = 0;
        switch (data.movePattern)
        {
            case MovePattern.Chase:
                if (player.currentPos.x > currentPos.x) dirX = 1;
                else if (player.currentPos.x < currentPos.x) dirX = -1;
                break;
            default:
                if (player.currentPos.x > currentPos.x) dirX = 1;
                else if (player.currentPos.x < currentPos.x) dirX = -1;
                break;
        }

        if (dirX == 0) return false;

        Vector2Int nextPos = currentPos + new Vector2Int(dirX, 0);

        // [핵심] 다음 자리 예약 시도
        if (GridManager.Instance.TryReserveTile(nextPos))
        {
            currentIntent.type = IntentType.Move;
            currentIntent.targetPos = nextPos;

            // [중요] 이동에 성공했으므로, 찜해뒀던 현재 자리는 풀어줌 (다른 애가 올 수 있게)
            GridManager.Instance.CancelReservation(currentPos);

            SpawnIndicator(moveIndicatorPrefab, nextPos);
            Debug.Log($"<color=green>[의도]</color> {data.enemyName}: 이동 ({nextPos})");
            return true;
        }
        else
        {
            return false;
        }
    }

    void SetWaitIntent()
    {
        currentIntent.type = IntentType.Wait;
        // 대기 시에도 자리 유지 (반납 안 함)
    }

    public void ExecuteMove()
    {
        if (IsStunned || state == EnemyState.Dead) return;

        if (currentIntent.type == IntentType.Move)
        {
            currentPos = currentIntent.targetPos;
            moveCooldownTimer = 0;
            ClearIndicators();
            UpdateVisual();
        }
        else
        {
            if (currentIntent.type != IntentType.Attack) moveCooldownTimer++;
        }
    }

    public void ExecuteAttack()
    {
        if (IsStunned || state == EnemyState.Dead) return;

        if (currentIntent.type == IntentType.Attack)
        {
            if (animator) animator.SetTrigger("Attack");
            PerformAttackLogic();
            attackCooldownTimer = 0;
            ClearIndicators();
        }
        else
        {
            attackCooldownTimer++;
        }

        state = EnemyState.Idle;
        currentIntent.type = IntentType.None;
    }

    void PerformAttackLogic()
    {
        var player = FindObjectOfType<PlayerController>();
        bool hit = false;
        foreach (Vector2Int targetPos in currentIntent.areaTargets)
        {
            if (player != null && player.currentPos == targetPos)
            {
                Debug.Log($"<color=red>[적중]</color> {data.enemyName} -> 플레이어 피격!");
                player.TakeDamage(data.attackPower);
                hit = true;
                break;
            }
        }
        if (!hit) Debug.Log($"<color=white>[빗나감]</color> {data.enemyName}");
    }

    // --- 유틸리티 ---
    public void TakeDamage(int dmg)
    {
        currentHp -= dmg;
        if (animator) animator.SetTrigger("Hit");
        if (currentHp <= 0)
        {
            state = EnemyState.Dead;
            if (animator) animator.SetBool("IsDead", true);
            TurnManager.Instance.OnEnemyDead(this);
            ClearIndicators();
            Destroy(gameObject, 0.5f);
        }
    }

    public void ApplyCollision(int damage)
    {
        TakeDamage(damage);
        AddStatus(StatusType.Stun, 1);
        if (animator) animator.SetBool("IsStunned", true);
        currentIntent.type = IntentType.Wait;
        ClearIndicators();
    }

    public void AddStatus(StatusType t, int d) => activeEffects.Add(new StatusEffect(t, d));

    void UpdateStatus()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
            if (--activeEffects[i].duration <= 0) activeEffects.RemoveAt(i);
    }

    void SpawnIndicator(GameObject prefab, Vector2Int p)
    {
        if (prefab) activeIndicators.Add(Instantiate(prefab, GridManager.Instance.GetWorldPosition(p, GridManager.Instance.indicatorHeight), Quaternion.identity));
    }

    public void ClearIndicators()
    {
        foreach (var g in activeIndicators) if (g) Destroy(g);
        activeIndicators.Clear();
    }

    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    private void OnDestroy() => ClearIndicators();
}