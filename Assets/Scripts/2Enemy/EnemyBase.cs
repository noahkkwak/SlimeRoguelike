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

        GridManager.Instance.RegisterUnit(currentPos, this); // 그리드 등록
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

        // 1. 공격 시도 (플레이어 OR 장애물)
        if (attackCooldownTimer >= data.attackCycle)
        {
            // [AI 업그레이드] 공격 대상 탐색
            if (TrySetAttackIntent())
            {
                intentSet = true;
            }
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

    // [신규] 공격 의도 수립 (플레이어 우선, 없으면 장애물 확인)
    bool TrySetAttackIntent()
    {
        // 내 공격 범위 안에 무엇이 있는지 확인
        // (단순화를 위해 직선 공격만 예시로 구현)

        var player = FindObjectOfType<PlayerController>();
        bool targetFound = false;

        // 공격 범위 스캔
        for (int i = 1; i <= data.attackRange; i++)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
            var tile = GridManager.Instance.GetTile(tPos);
            if (tile == null) break;

            // 1. 플레이어 발견? -> 공격 확정
            if (player != null && player.currentPos == tPos)
            {
                targetFound = true;
                break;
            }

            // 2. 장애물 발견?
            if (tile.HasObstacle)
            {
                // 파괴 가능한 장애물이면 공격 대상으로 간주
                if (tile.Obstacle.type == ObstacleType.Destructible || tile.Obstacle.type == ObstacleType.Explosive)
                {
                    targetFound = true;
                    Debug.Log($"<color=yellow>[AI]</color> {data.enemyName}: 장애물({tile.Obstacle.name}) 파괴 시도");
                    break;
                }
                // 파괴 불가능한 벽이면? 시야 막힘 -> 공격 포기
                else if (tile.Obstacle.type == ObstacleType.Indestructible)
                {
                    break;
                }
            }
        }

        if (targetFound)
        {
            SetAttackIntentRaw(); // 실제 공격 의도 설정
            return true;
        }

        return false;
    }

    void SetAttackIntentRaw()
    {
        currentIntent.type = IntentType.Attack;
        currentIntent.areaTargets.Clear();
        GridManager.Instance.TryReserveTile(currentPos);

        // 인디케이터 표시 (장애물에 막히는 것 고려)
        for (int i = 1; i <= data.attackRange; i++)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
            var tile = GridManager.Instance.GetTile(tPos);
            if (tile == null) break;

            currentIntent.areaTargets.Add(tPos);
            SpawnIndicator(attackIndicatorPrefab, tPos);

            // 시야를 막는 장애물이면 거기까지만 표시
            if (tile.HasObstacle) break;
        }
        Debug.Log($"<color=yellow>[의도]</color> {data.enemyName}: 공격 준비");
    }

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
            // [중요] 이동 시 그리드 정보 갱신
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

        // 공격 범위 내 첫 번째 대상 타격 (플레이어 로직과 동일)
        foreach (Vector2Int targetPos in currentIntent.areaTargets)
        {
            var tile = GridManager.Instance.GetTile(targetPos);
            if (tile == null) continue;

            // 1. 플레이어 피격
            if (player != null && player.currentPos == targetPos)
            {
                player.TakeDamage(data.attackPower);
                hit = true;
                break; // 관통 불가
            }

            // 2. 장애물 피격 (적이 장애물 부수기)
            if (tile.HasObstacle)
            {
                tile.Obstacle.TakeDamage(data.attackPower);
                hit = true;
                break; // 관통 불가
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
            GridManager.Instance.RemoveUnit(currentPos); // 사망 시 그리드 해제
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