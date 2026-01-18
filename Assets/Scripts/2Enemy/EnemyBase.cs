using UnityEngine;
using System.Collections.Generic;

// [데이터 구조] 적이 이번 턴에 하려는 행동의 구체적 정보
[System.Serializable]
public class EnemyIntent
{
    public IntentType type;         // 행동 종류 (Move, Attack, Wait)
    public Vector2Int targetPos;    // 이동할 목표 좌표
    public List<Vector2Int> areaTargets = new List<Vector2Int>(); // 공격할 타일들 (인디케이터용)
}

public class EnemyBase : MonoBehaviour
{
    [Header("Settings & Data")]
    public EnemyData data;
    public GameObject attackIndicatorPrefab; // 공격 예고 (빨강)
    public GameObject moveIndicatorPrefab;   // 이동 예고 (노랑/화살표)

    [Header("State (Debug)")]
    public EnemyState state = EnemyState.Idle;
    public int currentHp;
    public Vector2Int currentPos;

    // [핵심] 현재 확정된 다음 행동 의도
    public EnemyIntent currentIntent = new EnemyIntent();

    // 행동 패턴용 쿨타임 카운터
    [SerializeField] private int moveCooldownTimer = 0;
    [SerializeField] private int attackCooldownTimer = 0;

    // 상태 이상 및 컴포넌트
    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool IsStunned => activeEffects.Exists(e => e.type == StatusType.Stun);

    private List<GameObject> activeIndicators = new List<GameObject>();
    private Animator animator; // 애니메이터 (없으면 무시됨)

    public void Initialize(Vector2Int startPos)
    {
        if (data == null) { Debug.LogError("EnemyData Missing!"); return; }

        currentHp = data.maxHp;
        currentPos = startPos;
        state = EnemyState.Idle;

        // 쿨타임 초기화
        moveCooldownTimer = 0;
        attackCooldownTimer = 0;

        // 애니메이터 가져오기
        animator = GetComponentInChildren<Animator>();

        UpdateVisual();
    }

    // ==================================================================================
    // [Phase 1: Plan] 의도 수립 - 턴 시작 시 호출되어 '다음 행동'을 결정하고 예고함
    // ==================================================================================
    public void CalculateIntent()
    {
        ClearIndicators(); // 이전 턴의 잔상 제거
        UpdateStatus();    // 상태 이상 턴 차감

        // 1. 기절 상태 체크
        if (IsStunned)
        {
            state = EnemyState.Stunned;
            currentIntent.type = IntentType.Wait;
            if (animator) animator.SetBool("IsStunned", true);
            return;
        }
        else
        {
            if (animator) animator.SetBool("IsStunned", false);
        }

        state = EnemyState.Ready;

        // 2. AI 우선순위 판단 (공격 > 이동)
        // A. 공격 쿨타임이 찼는가?
        if (attackCooldownTimer >= data.attackCycle)
        {
            SetAttackIntent();
        }
        // B. 이동 쿨타임이 찼는가? (공격을 안 할 때만)
        else if (moveCooldownTimer >= data.moveCycle)
        {
            SetMoveIntent();
        }
        // C. 둘 다 아니면 대기
        else
        {
            SetWaitIntent();
        }
    }

    // --- 의도 설정 헬퍼 함수들 ---

    void SetAttackIntent()
    {
        currentIntent.type = IntentType.Attack;
        currentIntent.areaTargets.Clear();

        // 공격 범위 계산
        if (data.attackType == AttackType.Direct)
        {
            // 직사: 장애물 전까지 쭉
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                // 맵 밖이거나 장애물이면 중단
                if (!GridManager.Instance.IsInsideGrid(tPos) || GridManager.Instance.IsObstacle(tPos)) break;

                currentIntent.areaTargets.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
        else if (data.attackType == AttackType.Arcing)
        {
            // 곡사: 최대 사거리 타일 하나 조준
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

    void SetMoveIntent()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) { SetWaitIntent(); return; }

        // 간단한 추적 AI (X축 이동)
        int dirX = 0;
        if (player.currentPos.x > currentPos.x) dirX = 1;
        else if (player.currentPos.x < currentPos.x) dirX = -1;

        Vector2Int nextPos = currentPos + new Vector2Int(dirX, 0);

        if (GridManager.Instance.IsWalkable(nextPos))
        {
            currentIntent.type = IntentType.Move;
            currentIntent.targetPos = nextPos;
            SpawnIndicator(moveIndicatorPrefab, nextPos); // 노란색 이동 예고
            Debug.Log($"<color=green>[의도]</color> {data.enemyName}: 이동 예고 ({nextPos})");
        }
        else
        {
            SetWaitIntent(); // 막혔으면 대기
        }
    }

    void SetWaitIntent()
    {
        currentIntent.type = IntentType.Wait;
    }


    // ==================================================================================
    // [Phase 3: Execute] 실행 - 플레이어 턴 종료 후 호출되어 '의도'를 실제 행동으로 옮김
    // ==================================================================================

    // 1. 이동 실행 함수
    public void ExecuteMove()
    {
        if (IsStunned || state == EnemyState.Dead) return;

        if (currentIntent.type == IntentType.Move)
        {
            currentPos = currentIntent.targetPos;
            moveCooldownTimer = 0; // 이동했으니 쿨타임 리셋
            ClearIndicators();     // 이동 예고 타일 삭제
            UpdateVisual();
        }
        else
        {
            // 공격 의도가 아닐 때만 이동 쿨타임 증가 (공격 중엔 이동 쿨타임 멈춤)
            if (currentIntent.type != IntentType.Attack)
            {
                moveCooldownTimer++;
            }
        }
    }

    // 2. 공격 실행 함수
    public void ExecuteAttack()
    {
        if (IsStunned || state == EnemyState.Dead) return;

        if (currentIntent.type == IntentType.Attack)
        {
            // 애니메이션 트리거
            if (animator) animator.SetTrigger("Attack");

            PerformAttackLogic();

            attackCooldownTimer = 0; // 공격했으니 쿨타임 리셋
            ClearIndicators();       // 공격 예고 타일 삭제
        }
        else
        {
            attackCooldownTimer++; // 공격하지 않았다면 쿨타임 증가
        }

        // 턴 종료 처리
        state = EnemyState.Idle;
        currentIntent.type = IntentType.None;
    }

    void PerformAttackLogic()
    {
        var player = FindObjectOfType<PlayerController>();
        bool hit = false;

        // Plan 단계에서 계산해둔 타겟 타일들에 현재 플레이어가 있는지 확인
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
        if (!hit) Debug.Log($"<color=white>[빗나감]</color> {data.enemyName} 공격 허탕");
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
            Destroy(gameObject, 0.5f); // 0.5초 뒤 파괴 (사망 모션 등 고려)
        }
    }

    public void ApplyCollision(int damage)
    {
        TakeDamage(damage);
        AddStatus(StatusType.Stun, 1);
        if (animator) animator.SetBool("IsStunned", true);

        // 충돌 시 이번 턴 의도 취소
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