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
        else
        {
            if (animator) animator.SetBool("IsStunned", false);
        }

        state = EnemyState.Ready;
        bool intentSet = false;

        // 1. 공격 시도
        if (attackCooldownTimer >= data.attackCycle)
        {
            if (TrySetAttackIntent()) intentSet = true;
        }

        // 2. 이동 시도
        if (!intentSet && moveCooldownTimer >= data.moveCycle)
        {
            intentSet = SetMoveIntent();
        }

        // 3. 대기
        if (!intentSet)
        {
            SetWaitIntent();
        }
    }

    // 공격 대상 탐색 (곡사/직사 분기 처리)
    bool TrySetAttackIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        bool targetFound = false;

        // A. 직사 (Direct): 경로상의 첫 번째 장애물/플레이어 확인
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
                    targetFound = true;
                    break;
                }
                if (tile.HasObstacle && tile.Obstacle.type == ObstacleType.Indestructible) break; // 벽에 막힘
            }
        }
        // B. 곡사 (Arcing): 사거리 끝(또는 맵 끝) 지점만 확인
        else if (data.attackType == AttackType.Arcing)
        {
            // 목표 지점 계산 (내 위치 + 사거리)
            // 단, 맵을 벗어나면 맵의 끝(height-1)을 타격
            int targetY = Mathf.Min(currentPos.y + data.attackRange, GridManager.Instance.height - 1);
            Vector2Int tPos = new Vector2Int(currentPos.x, targetY);

            // 그곳에 플레이어가 있거나 파괴 가능한 장애물이 있으면 공격 의도 설정
            if (GridManager.Instance.IsInsideGrid(tPos))
            {
                var tile = GridManager.Instance.GetTile(tPos);
                if ((player != null && player.currentPos == tPos) ||
                    (tile != null && tile.HasObstacle && tile.Obstacle.type != ObstacleType.Destructible))
                {
                    // 곡사는 보통 플레이어를 노리지만, 기획에 따라 무조건 쏘게 할 수도 있음.
                    // 현재는 '사거리에 닿으면 무조건 쏨'으로 설정하여 압박감을 줌
                    targetFound = true;
                }
                // 곡사는 빈 땅이어도 쏘는 경우가 많으므로(지역 장악), 일단 true로 설정해도 됨
                targetFound = true;
            }
        }

        if (targetFound)
        {
            SetAttackIntentRaw();
            return true;
        }
        return false;
    }

    // [핵심 수정] 실제 의도 데이터 채우기 및 인디케이터 표시
    void SetAttackIntentRaw()
    {
        currentIntent.type = IntentType.Attack;
        currentIntent.areaTargets.Clear();
        GridManager.Instance.TryReserveTile(currentPos);

        // A. 직사: 경로를 쭉 그림
        if (data.attackType == AttackType.Direct)
        {
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                var tile = GridManager.Instance.GetTile(tPos);
                if (tile == null) break;

                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);

                if (tile.HasObstacle) break; // 시야 막힘
            }
        }
        // B. 곡사: 목표 지점 '하나'만 찍음 (경로 무시)
        else if (data.attackType == AttackType.Arcing)
        {
            int targetY = Mathf.Min(currentPos.y + data.attackRange, GridManager.Instance.height - 1);
            Vector2Int tPos = new Vector2Int(currentPos.x, targetY);

            if (GridManager.Instance.IsInsideGrid(tPos))
            {
                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
                Debug.Log($"<color=yellow>[의도]</color> {data.enemyName}: 곡사 조준 ({tPos})");
            }
        }

        Debug.Log($"<color=yellow>[의도]</color> {data.enemyName}: 공격 준비");
    }

    // ... (이하 SetMoveIntent, ExecuteMove 등 기존 코드 유지) ...

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

    void SetWaitIntent()
    {
        currentIntent.type = IntentType.Wait;
        GridManager.Instance.TryReserveTile(currentPos);
    }

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
            var tile = GridManager.Instance.GetTile(targetPos);
            if (tile == null) continue;

            if (player != null && player.currentPos == targetPos)
            {
                player.TakeDamage(data.attackPower);
                hit = true;
                // 곡사는 범위 내 대상을 다 때리는지, 하나만 때리는지에 따라 break 여부 결정
                // 현재는 단일 타겟팅이므로 break 해도 무방
                break;
            }

            if (tile.HasObstacle)
            {
                tile.Obstacle.TakeDamage(data.attackPower);
                hit = true;
                break;
            }
        }
        if (!hit) Debug.Log($"<color=white>[빗나감]</color> {data.enemyName}");
    }

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