using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public string charName = "Player";
    public int maxHp = 10;
    public int currentHp;
    public int attackPower = 2;

    public Vector2Int currentPos;
    public PlayerAction SelectedAction => selectedAction;
    private PlayerAction selectedAction = PlayerAction.None;
    private bool isDead = false;

    [Header("Visuals & Animation")]
    public float moveSpeed = 10f; // 이동 속도
    private Animator animator;    // 애니메이터 참조
    private bool isMovingVisual = false; // 이동 중 입력 방지

    public GameObject attackIndicatorPrefab;
    public GameObject defenseIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentHp = maxHp;

        // GridManager 초기화 안전장치
        if (GridManager.Instance != null && GridManager.Instance.Tiles.Count > 0)
            InitPosition();
        else
            StartCoroutine(WaitForGrid());
    }

    IEnumerator WaitForGrid()
    {
        yield return null;
        InitPosition();
    }

    void InitPosition()
    {
        int centerX = GridManager.Instance.width / 2;
        int bottomY = GridManager.Instance.height - 1;
        currentPos = new Vector2Int(centerX, bottomY);
        transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    }

    public void OnTurnStart()
    {
        if (isDead) return;
        selectedAction = PlayerAction.None;
        ClearIndicators();

        // 턴 시작 시 방어 자세 등 초기화
        if (animator)
        {
            animator.SetBool("IsGuarding", false);
            animator.SetBool("IsAiming", false);
        }
    }

    void Update()
    {
        if (isDead || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;
        if (isMovingVisual) return;

        // 이동 (A/D)
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);

        // 행동 선택 (W: 공격준비, S: 방어준비)
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);

        // 행동 확정 (Space)
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None) FinishTurn();
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int next = currentPos + dir;
        bool isBottomRow = (next.y == GridManager.Instance.height - 1);

        if (GridManager.Instance.IsWalkable(next) && isBottomRow)
        {
            currentPos = next;

            // [애니메이션] 이동 시 준비 자세들 모두 해제
            if (animator)
            {
                animator.SetBool("IsAiming", false);
                animator.SetBool("IsGuarding", false);
                animator.SetTrigger("Move");
            }

            // [시각적] 부드러운 이동 실행
            StartCoroutine(MoveVisual(GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight)));

            selectedAction = PlayerAction.None;
            FinishTurn();
        }
    }

    IEnumerator MoveVisual(Vector3 targetPos)
    {
        isMovingVisual = true;
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMovingVisual = false;
    }

    void SelectAction(PlayerAction a)
    {
        ClearIndicators();
        selectedAction = a;

        // [애니메이션] 준비 단계 (State 유지)
        if (animator)
        {
            // 일단 다 끄고
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsGuarding", false);

            // 선택된 것만 킴
            if (a == PlayerAction.Attack) animator.SetBool("IsAiming", true);
            else if (a == PlayerAction.Defend) animator.SetBool("IsGuarding", true);
        }

        if (a == PlayerAction.Attack) ShowAttackRange();
        else if (a == PlayerAction.Defend) ShowDefenseVisual();
    }

    public void ResolveBufferedAction()
    {
        if (selectedAction == PlayerAction.Attack) PerformAttack();

        ClearIndicators();
        // 참고: 방어(IsGuarding)나 조준(IsAiming) 해제 시점은 PerformAttack 혹은 OnTurnStart에서 처리
    }

    void PerformAttack()
    {
        Debug.Log($"<color=cyan>[플레이어 공격 발동]</color>");

        // [애니메이션] 실행 단계
        if (animator)
        {
            animator.SetBool("IsAiming", false); // 조준 끝
            animator.SetTrigger("Attack");       // 공격!
        }

        Vector2Int attackDir = new Vector2Int(0, -1); // 위쪽으로 공격

        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            var tile = GridManager.Instance.GetTile(tPos);
            if (tile == null) continue;

            if (tile.HasObstacle)
            {
                Debug.Log($"<color=orange>HIT OBSTACLE!</color> {tile.Obstacle.name}");
                tile.Obstacle.OnHit(attackPower, attackDir); // 밀치기 방향 전달
                return;
            }
            if (tile.HasUnit)
            {
                Debug.Log($"<color=red>HIT ENEMY!</color> {tile.OccupyingUnit.data.enemyName}");
                tile.OccupyingUnit.TakeDamage(attackPower);
                return;
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        int finalDmg = dmg;
        if (selectedAction == PlayerAction.Defend) finalDmg = Mathf.RoundToInt(dmg * 0.5f);

        currentHp -= finalDmg;

        if (animator) animator.SetTrigger("Hit");

        if (currentHp <= 0)
        {
            isDead = true;
            if (animator) animator.SetBool("IsDead", true);
            Debug.Log("<color=red>GAME OVER</color>");
        }
    }

    void ShowDefenseVisual()
    {
        if (defenseIndicatorPrefab)
            activeIndicators.Add(Instantiate(defenseIndicatorPrefab, GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.indicatorHeight), Quaternion.identity));
    }

    void ShowAttackRange()
    {
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            var tile = GridManager.Instance.GetTile(tPos);

            if (tile != null)
            {
                activeIndicators.Add(Instantiate(attackIndicatorPrefab, GridManager.Instance.GetWorldPosition(tPos, GridManager.Instance.indicatorHeight), Quaternion.identity));
                if (tile.HasObstacle) break;
            }
        }
    }

    void FinishTurn() { TurnManager.Instance.OnPlayerActionCompleted(); }
    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
}