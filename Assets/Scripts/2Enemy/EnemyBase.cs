using UnityEngine;
using System.Collections;
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

    [Header("Visual Settings")]
    public float moveSpeed = 8f;

    [SerializeField] private int moveCooldownTimer = 0;
    [SerializeField] private int attackCooldownTimer = 0;

    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool IsStunned => activeEffects.Exists(e => e.type == StatusType.Stun);

    private List<GameObject> activeIndicators = new List<GameObject>();
    private Animator animator;

    // [신규] 기본 정면 방향 (카메라 쪽)
    private Vector3 defaultFacingDir = Vector3.back;

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

        // 태어날 때 정면 보기
        FaceDirection(defaultFacingDir);
    }

    // [추가] 이동 전용 코루틴
    public IEnumerator ExecuteMoveRoutine()
    {
        // 예: 의도가 이동일 경우에만 실행
        if (currentIntent.type == IntentType.Move)
        {
            if (animator) animator.SetTrigger("Move");

            // 실제 이동 로직 (Lerp 등)
            // ... 코드 생략 (기존 Move 로직을 여기로 옮기되 yield return null 포함) ...

            // 시뮬레이션용 시간 대기
            yield return new WaitForSeconds(0.2f);
        }
    }

    // [추가] 공격 전용 코루틴
    public IEnumerator ExecuteAttackRoutine()
    {
        if (currentIntent.type == IntentType.Attack)
        {
            if (animator) animator.SetTrigger("Attack");
            yield return new WaitForSeconds(0.4f); // 공격 모션 대기

            // 데미지 판정 로직
        }
    }

    public void CalculateIntent()
    {
        ClearIndicators();
        UpdateStatus();

        // [애니메이션] 턴 시작 시 충전 자세 해제
        if (animator) animator.SetBool("IsCharging", false);

        if (IsStunned)
        {
            state = EnemyState.Stunned;
            currentIntent.type = IntentType.Wait;
            if (animator) animator.SetBool("IsStunned", true);
            FaceDirection(defaultFacingDir); // 스턴이면 정면
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
            if (TrySetAttackIntent())
            {
                intentSet = true;
                // [애니메이션] 공격 준비 자세 (Bool)
                if (animator) animator.SetBool("IsCharging", true);
                FaceDirection(defaultFacingDir); // 공격은 정면으로
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
            FaceDirection(defaultFacingDir);
        }
    }

    bool TrySetAttackIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        bool targetFound = false;

        // 공격 판정 (직사/곡사)
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
            SetAttackIntentRaw();
            return true;
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
                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
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

            // [핵심 구현] 이동 방향으로 회전 (90도)
            Vector3 lookDir = new Vector3(dirX, 0, 0);
            FaceDirection(lookDir);

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
            // 1. 논리적 이동 (그리드 데이터 갱신) - 즉시 처리
            GridManager.Instance.RemoveUnit(currentPos);
            currentPos = currentIntent.targetPos;
            GridManager.Instance.RegisterUnit(currentPos, this);

            moveCooldownTimer = 0;
            ClearIndicators();

            // 2. 시각적 이동 (코루틴) - 부드럽게
            if (animator) animator.SetTrigger("Move"); // [복구] 이동 애니메이션 실행

            // 목표 월드 좌표 계산
            Vector3 targetWorldPos = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
            StartCoroutine(MoveVisual(targetWorldPos));
        }
        else
        {
            if (currentIntent.type != IntentType.Attack) moveCooldownTimer++;
        }
    }

    // [신규] 부드러운 이동 처리
    IEnumerator MoveVisual(Vector3 targetPos)
    {
        // 도착할 때까지 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // 도착 후 위치 보정
        transform.position = targetPos;

        // [중요] 이동이 끝난 후에야 정면을 바라봄
        // (이동 중에는 옆을 보고 있어야 자연스럽기 때문)
        FaceDirection(defaultFacingDir);
    }

    public void ExecuteAttack()
    {
        if (IsStunned || state == EnemyState.Dead) return;

        if (currentIntent.type == IntentType.Attack)
        {
            // [애니메이션] 실행 단계: 충전 풀고 공격
            if (animator)
            {
                animator.SetBool("IsCharging", false);
                animator.SetTrigger("Attack");
            }
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
                if (data.attackType == AttackType.Arcing || data.attackType == AttackType.Direct) break;
            }

            if (tile.HasObstacle)
            {
                Vector2Int dir = targetPos - currentPos;
                dir.Clamp(new Vector2Int(-1, -1), new Vector2Int(1, 1));

                tile.Obstacle.OnHit(data.attackPower, dir);
                hit = true;
                if (data.attackType == AttackType.Direct || data.attackType == AttackType.Arcing) break;
            }
        }
    }

    // ... (유틸리티 함수) ...
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
        FaceDirection(defaultFacingDir); // 스턴 시 정면
        currentIntent.type = IntentType.Wait;
        ClearIndicators();
    }

    void FaceDirection(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = targetRot;
    }

    public void AddStatus(StatusType t, int d) => activeEffects.Add(new StatusEffect(t, d));
    void UpdateStatus() { for (int i = activeEffects.Count - 1; i >= 0; i--) if (--activeEffects[i].duration <= 0) activeEffects.RemoveAt(i); }
    void SpawnIndicator(GameObject prefab, Vector2Int p) { if (prefab) activeIndicators.Add(Instantiate(prefab, GridManager.Instance.GetWorldPosition(p, GridManager.Instance.indicatorHeight), Quaternion.identity)); }
    public void ClearIndicators() { foreach (var g in activeIndicators) if (g) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    private void OnDestroy() => ClearIndicators();
}