using UnityEngine;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    [Header("Settings & Data")]
    public EnemyData data;

    [Header("Visuals")]
    public GameObject attackIndicatorPrefab; // 공격 예고 (빨강)
    public GameObject moveIndicatorPrefab;   // [신규] 이동 예고 (노랑/초록)

    [Header("Runtime Status")]
    public int currentHp;
    public Vector2Int currentPos;
    public Vector2Int intendedPos;

    // 쿨타임 카운터
    public int moveCounter = 0;
    public int attackCounter = 0;

    public bool isPreparingAttack = false;
    private List<Vector2Int> attackTargetTiles = new List<Vector2Int>();
    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    private bool isStunned => activeEffects.Exists(e => e.type == StatusType.Stun);

    private List<GameObject> activeIndicators = new List<GameObject>();

    public void Initialize(Vector2Int startPos)
    {
        if (data == null) return;
        currentHp = data.maxHp;
        currentPos = startPos;
        moveCounter = 0;
        attackCounter = 0;
        isPreparingAttack = false;
        UpdateVisual();
    }

    // 1단계: 계획 (이동 예측 표시)
    public void PlanTurn()
    {
        UpdateStatus();
        intendedPos = currentPos;

        // 이동 예측 인디케이터 제거 (이전 턴 잔상)
        ClearIndicators();

        if (isStunned || isPreparingAttack) return;

        // 이동 쿨타임 체크 (공격 중엔 이동 계획 없음)
        if (moveCounter + 1 >= data.moveCycle)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;

            int dirX = (player.currentPos.x > currentPos.x) ? 1 : (player.currentPos.x < currentPos.x ? -1 : 0);
            Vector2Int next = currentPos + new Vector2Int(dirX, 0);

            if (GridManager.Instance.IsWalkable(next))
            {
                intendedPos = next;
                // [신규] 이동할 위치에 미리보기 인디케이터 표시
                if (moveIndicatorPrefab) SpawnIndicator(moveIndicatorPrefab, intendedPos);
            }
        }
    }

    // 2단계: 실행
    public void ExecuteTurn()
    {
        // 실행 시 이동 예측 인디케이터는 지움 (실제 이동하니까)
        ClearIndicators();

        if (isStunned)
        {
            Debug.Log($"<color=gray>[기절]</color> {data.enemyName}");
            CancelAttack();
            UpdateVisual();
            return;
        }

        // --- Case 1: 공격 준비 중 -> 공격 실행 ---
        if (isPreparingAttack)
        {
            PerformAttack();

            // 상태 초기화
            isPreparingAttack = false;
            attackCounter = 0;
            // [수정] 이동 쿨타임은 리셋하지 않음 (공격하느라 이동만 멈췄던 것)

            UpdateVisual();
            return;
        }

        // --- Case 2: 일반 상태 (이동 및 공격 쿨타임 체크) ---

        // 2-1. 이동 처리
        bool moved = false;
        if (intendedPos != currentPos)
        {
            currentPos = intendedPos;
            moveCounter = 0; // 이동했으니 리셋
            moved = true;
        }

        // [수정] 이동을 안 했다면 쿨타임 증가 (단, 쿨타임이 이미 찼는데 이동 못 한 경우도 계속 유지 or 증가)
        if (!moved) moveCounter++;


        // 2-2. 공격 쿨타임 체크
        attackCounter++;
        if (attackCounter >= data.attackCycle)
        {
            PrepareAttack();
        }

        UpdateVisual();
    }

    void PrepareAttack()
    {
        isPreparingAttack = true;
        attackTargetTiles.Clear();
        ClearIndicators();

        Debug.Log($"<color=yellow>[공격 예고]</color> {data.enemyName}");

        if (data.attackType == AttackType.Direct)
        {
            // 직사: 장애물 만나기 전까지
            for (int i = 1; i <= data.attackRange; i++)
            {
                Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + i);
                if (!GridManager.Instance.IsInsideGrid(tPos)) break;
                if (GridManager.Instance.IsObstacle(tPos)) break;

                attackTargetTiles.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
        else if (data.attackType == AttackType.Arcing)
        {
            // [수정] 곡사: 플레이어 유무 상관없이 '최대 사거리' 타일 하나 조준
            // 혹은 4행(플레이어 라인)을 고정 타겟팅
            // 여기서는 '자신의 위치 + 사거리' 칸을 조준하도록 통일
            Vector2Int tPos = new Vector2Int(currentPos.x, currentPos.y + data.attackRange);

            if (GridManager.Instance.IsInsideGrid(tPos))
            {
                attackTargetTiles.Add(tPos);
                SpawnIndicator(attackIndicatorPrefab, tPos);
            }
        }
    }

    void PerformAttack()
    {
        var player = FindObjectOfType<PlayerController>();
        bool hit = false;

        foreach (Vector2Int targetPos in attackTargetTiles)
        {
            if (player != null && player.currentPos == targetPos)
            {
                Debug.Log($"<color=orange>[적중]</color> {data.enemyName} -> 플레이어");
                player.TakeDamage(data.attackPower);
                hit = true;
            }
        }
        if (!hit)
        {
            Debug.Log($"<color=white>[공격 회피]</color> {data.enemyName}의 공격이 빈 공간을 타격했습니다.");
        }
    }

    void CancelAttack()
    {
        isPreparingAttack = false;
        attackTargetTiles.Clear();
        ClearIndicators();
    }

    public void ApplyCollision()
    {
        TakeDamage(data.collisionDamage);
        AddStatus(StatusType.Stun, 1);
        intendedPos = currentPos;
        CancelAttack();
    }

    public void TakeDamage(int dmg)
    {
        currentHp -= dmg;
        if (currentHp <= 0)
        {
            TurnManager.Instance.OnEnemyDead(this);
            Destroy(gameObject);
        }
    }

    // 인디케이터 생성 헬퍼 (프리팹 선택 가능)
    void SpawnIndicator(GameObject prefab, Vector2Int p)
    {
        if (prefab) activeIndicators.Add(Instantiate(prefab, GridManager.Instance.GetWorldPosition(p, GridManager.Instance.indicatorHeight), Quaternion.identity));
    }
    public void ClearIndicators() { foreach (var g in activeIndicators) if (g) Destroy(g); activeIndicators.Clear(); }
    public void AddStatus(StatusType t, int d) => activeEffects.Add(new StatusEffect(t, d));
    void UpdateStatus() { for (int i = activeEffects.Count - 1; i >= 0; i--) if (--activeEffects[i].duration <= 0) activeEffects.RemoveAt(i); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    private void OnDestroy() => ClearIndicators();
}