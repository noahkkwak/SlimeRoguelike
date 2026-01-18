using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public string charName = "Slime";
    public int maxHp = 10;
    public int currentHp;
    public int attackPower = 2;

    // 초기값은 의미가 없어짐 (Start에서 덮어씌움)
    public Vector2Int currentPos;

    public PlayerAction SelectedAction => selectedAction;
    private PlayerAction selectedAction = PlayerAction.None;
    private bool isDead = false;

    [Header("Visuals")]
    public GameObject attackIndicatorPrefab;
    public GameObject defenseIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    void Start()
    {
        currentHp = maxHp;

        // [수정] 맵 크기에 맞춰 중앙 하단으로 위치 자동 설정
        if (GridManager.Instance != null)
        {
            int centerX = GridManager.Instance.width / 2; // 5칸이면 2
            int bottomY = GridManager.Instance.height - 1; // 7칸이면 6
            currentPos = new Vector2Int(centerX, bottomY);
        }

        UpdateVisual();
    }

    // 턴 시작 시 상태 리셋
    public void OnTurnStart()
    {
        if (isDead) return;

        selectedAction = PlayerAction.None;
        ClearIndicators();
    }

    void Update()
    {
        if (isDead || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None) ExecuteAction();
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int next = currentPos + dir;

        // 이동 조건: 맵 안쪽이고, 장애물이 없고, 플레이어는 맨 아랫줄(height-1)에서만 이동 가능
        // (기획 의도에 따라 Y축 이동을 허용하려면 next.y 조건 수정)
        bool isBottomRow = (next.y == GridManager.Instance.height - 1);

        if (GridManager.Instance.IsWalkable(next) && isBottomRow)
        {
            currentPos = next;
            UpdateVisual();
            FinishTurn();
        }
    }

    void SelectAction(PlayerAction a)
    {
        ClearIndicators();
        selectedAction = a;
        if (a == PlayerAction.Attack) ShowAttackRange();
        else if (a == PlayerAction.Defend) ShowDefenseVisual();
    }

    void ShowDefenseVisual()
    {
        Debug.Log($"<color=#00FFFF>[방어 준비]</color> 다음 공격을 대비합니다.");
        if (defenseIndicatorPrefab)
            activeIndicators.Add(Instantiate(defenseIndicatorPrefab, GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.indicatorHeight), Quaternion.identity));
    }

    void ExecuteAction()
    {
        if (selectedAction == PlayerAction.Attack) PerformAttack();
        FinishTurn();
    }

    void PerformAttack()
    {
        // 전방 모든 칸 공격
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) return; // 장애물에 막힘

            EnemyBase target = TurnManager.Instance.GetEnemyAt(tPos);
            if (target != null) { target.TakeDamage(attackPower); return; }
        }
    }

    void ShowAttackRange()
    {
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) break;
            activeIndicators.Add(Instantiate(attackIndicatorPrefab, GridManager.Instance.GetWorldPosition(tPos, GridManager.Instance.indicatorHeight), Quaternion.identity));
        }
    }

    public void TakeDamage(int dmg)
    {
        int finalDmg = dmg;
        if (selectedAction == PlayerAction.Defend)
        {
            finalDmg = Mathf.RoundToInt(dmg * 0.5f);
            Debug.Log($"<color=blue>[방어 성공]</color> 대미지 감소 ({dmg} -> {finalDmg})");
        }

        currentHp -= finalDmg;
        Debug.Log($"<color=red>[플레이어 피격]</color> 남은 HP: {currentHp}");

        // [애니메이션 연결 포인트]
        // GetComponentInChildren<Animator>().SetTrigger("Hit");

        if (currentHp <= 0) { isDead = true; Debug.Log("<color=red>GAME OVER</color>"); }
    }

    void FinishTurn()
    {
        if (selectedAction != PlayerAction.Defend) ClearIndicators();
        TurnManager.Instance.OnPlayerActionCompleted();
    }

    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}