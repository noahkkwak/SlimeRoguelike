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
    // ... (상단 변수 및 Initialize 동일) ...
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
        GridManager.Instance.RegisterUnit(currentPos, this);
        UpdateVisual();
    }

    public void CalculateIntent()
    {
        ClearIndicators();
        UpdateStatus();
        if (IsStunned)
        {
            state = EnemyState.Stunned;
            currentIntent.type = IntentType.Wait;
            if (animator) animator.SetBool("IsStunned", true);
            GridManager.Instance.TryReserveTile(currentPos);
            return;
        }
        else if (animator) animator.SetBool("IsStunned", false);

        state = EnemyState.Ready;
        bool intentSet = false;

        if (attackCooldownTimer >= data.attackCycle)
        {
            if (TrySetAttackIntent()) intentSet = true;
        }

        if (!intentSet && moveCooldownTimer >= data.moveCycle)
        {
            intentSet = SetMoveIntent();
        }

        if (!intentSet) SetWaitIntent();
    }

    bool TrySetAttackIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        bool targetFound = false;

        if (data.attackType == AttackType.Direct)
        {
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                var tile = GridManager.Instance.GetTile(tPos);
                if (tile == null) break;

                if ((player != null && player.currentPos == tPos) ||
                    (tile.HasObstacle && tile.Obstacle.type != ObstacleType.Indestructible))
                {
                    targetFound = true; break;
                }
                if (tile.HasObstacle && tile.Obstacle.type == ObstacleType.Indestructible) break;
            }
        }
        else if (data.attackType == AttackType.Arcing)
        {
            int targetY = Mathf.Min(currentPos.y + data.attackRange, GridManager.Instance.height - 1);
            Vector2Int tPos = new Vector2Int(currentPos.x, targetY);
            if (GridManager.Instance.IsInsideGrid(tPos)) targetFound = true;
        }

        if (targetFound)
        {
            SetAttackIntentRaw(); return true;
        }
        return false;
    }

    void SetAttackIntentRaw()
    {
        currentIntent.type = IntentType.Attack;
        currentIntent.areaTargets.Clear();
        GridManager.Instance.TryReserveTile(currentPos);

        if (data.attackType == AttackType.Direct)
        {
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                var tile = GridManager.Instance.GetTile(tPos);
                if (tile == null) break;
                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
                if (tile.HasObstacle) break;
            }
        }
        else if (data.attackType == AttackType.Arcing)
        {
            int targetY = Mathf.Min(currentPos.y + data.attackRange, GridManager.Instance.height - 1);
            Vector2Int tPos = new Vector2Int(currentPos.x, targetY);
            if (GridManager.Instance.IsInsideGrid(tPos))
            {
                currentIntent.areaTargets.Add(tPos); // [중요] 여기 추가된 좌표가 PerformAttackLogic에서 쓰임
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
        Debug.Log($"<color=yellow>[의도]</color> {data.enemyName}: 공격 준비");
    }

    // ... (SetMoveIntent, SetWaitIntent, ExecuteMove 는 기존 유지) ...
    bool SetMoveIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return false;
        int dirX = 0;
        if (player.currentPos.x > currentPos.x) dirX = 1;
        else if (player.currentPos.x < currentPos.x) dirX = -1;
        if (dirX == 0) return false;
        Vector2Int nextPos = currentPos + new Vector2Int(dirX, 0);
        if (GridManager.Instance.TryReserveTile(nextPos))
        {
            currentIntent.type = IntentType.Move;
            currentIntent.targetPos = nextPos;
            GridManager.Instance.CancelReservation(currentPos);
            SpawnIndicator(moveIndicatorPrefab, nextPos);
            return true;
        }
        return false;
    }
    void SetWaitIntent() { currentIntent.type = IntentType.Wait; GridManager.Instance.TryReserveTile(currentPos); }
    public void ExecuteMove()
    {
        if (IsStunned || state == EnemyState.Dead) return;
        if (currentIntent.type == IntentType.Move)
        {
            GridManager.Instance.RemoveUnit(currentPos);
            currentPos = currentIntent.targetPos;
            GridManager.Instance.RegisterUnit(currentPos, this);
            moveCooldownTimer = 0;
            ClearIndicators();
            UpdateVisual();
        }
        else { if (currentIntent.type != IntentType.Attack) moveCooldownTimer++; }
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
        else { attackCooldownTimer++; }
        state = EnemyState.Idle;
        currentIntent.type = IntentType.None;
    }

    // [버그 수정 완료] Arcing 공격 처리
    void PerformAttackLogic()
    {
        var player = FindObjectOfType<PlayerController>();
        bool hit = false;

        foreach (Vector2Int targetPos in currentIntent.areaTargets)
        {
            var tile = GridManager.Instance.GetTile(targetPos);
            if (tile == null) continue;

            // 1. 플레이어 피격 체크
            if (player != null && player.currentPos == targetPos)
            {
                Debug.Log($"<color=red>[적중]</color> {data.enemyName} -> 플레이어 Hit!");
                player.TakeDamage(data.attackPower);
                hit = true;
                // 곡사 공격은 범위가 1칸이므로 맞으면 종료
                if (data.attackType == AttackType.Arcing) break;
                // 직사 공격은 관통 불가이므로 맞으면 종료
                if (data.attackType == AttackType.Direct) break;
            }

            // 2. 장애물 피격 체크 (적이 장애물 파괴)
            if (tile.HasObstacle)
            {
                Debug.Log($"<color=red>[적중]</color> {data.enemyName} -> 장애물 Hit!");
                // 적의 공격 방향 계산 (내 위치 -> 타겟 위치)
                Vector2Int dir = targetPos - currentPos;
                // 정규화 (방향만 추출)
                dir.Clamp(new Vector2Int(-1, -1), new Vector2Int(1, 1));

                tile.Obstacle.OnHit(data.attackPower, dir);
                hit = true;
                if (data.attackType == AttackType.Direct) break; // 직사는 막힘
                // 곡사는 장애물만 있으면 때리고 끝 (단일 타겟)
                if (data.attackType == AttackType.Arcing) break;
            }
        }
        if (!hit) Debug.Log($"<color=white>[빗나감]</color> {data.enemyName}");
    }

    // ... (유틸리티 함수 기존 유지) ...
    public void TakeDamage(int dmg)
    {
        currentHp -= dmg;
        if (animator) animator.SetTrigger("Hit");
        if (currentHp <= 0)
        {
            state = EnemyState.Dead;
            if (animator) animator.SetBool("IsDead", true);
            GridManager.Instance.RemoveUnit(currentPos);
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
    void UpdateStatus() { for (int i = activeEffects.Count - 1; i >= 0; i--) if (--activeEffects[i].duration <= 0) activeEffects.RemoveAt(i); }
    void SpawnIndicator(GameObject prefab, Vector2Int p) { if (prefab) activeIndicators.Add(Instantiate(prefab, GridManager.Instance.GetWorldPosition(p, GridManager.Instance.indicatorHeight), Quaternion.identity)); }
    public void ClearIndicators() { foreach (var g in activeIndicators) if (g) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    private void OnDestroy() => ClearIndicators();
}