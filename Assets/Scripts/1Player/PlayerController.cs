using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public string charName = "Slime";
    public int maxHp = 10;
    public int currentHp;
    public int attackPower = 2;

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
        if (GridManager.Instance != null)
        {
            int centerX = GridManager.Instance.width / 2;
            int bottomY = GridManager.Instance.height - 1;
            currentPos = new Vector2Int(centerX, bottomY);
        }
        UpdateVisual();
    }

    public void OnTurnStart()
    {
        if (isDead) return;
        selectedAction = PlayerAction.None;
        ClearIndicators();
    }

    void Update()
    {
        if (isDead || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        // 이동은 즉시 실행 (전략적 위치 선점)
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);

        // 행동 선택
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);

        // 행동 확정 (Space 누르면 턴 종료 -> 공격은 나중에 실행됨)
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None) FinishTurn();
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int next = currentPos + dir;
        bool isBottomRow = (next.y == GridManager.Instance.height - 1);

        if (GridManager.Instance.IsWalkable(next) && isBottomRow)
        {
            currentPos = next;
            UpdateVisual();

            // 이동했다면 공격/방어는 못함 (행동 종료)
            selectedAction = PlayerAction.None;
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

    // [핵심 변경] TurnManager가 적 이동 후에 호출하는 함수
    public void ResolveBufferedAction()
    {
        if (selectedAction == PlayerAction.Attack)
        {
            PerformAttack(); // 이제 적이 이동한 뒤에 때립니다!
        }

        ClearIndicators();
    }

    void PerformAttack()
    {
        Debug.Log($"<color=cyan>[플레이어 공격 발동]</color>");
        // 전방 모든 칸 공격
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) return;

            // 적이 이동해 온 위치를 검사
            EnemyBase target = TurnManager.Instance.GetEnemyAt(tPos);
            if (target != null)
            {
                Debug.Log($"<color=red>HIT!</color> 이동해온 {target.data.enemyName} 요격 성공!");
                target.TakeDamage(attackPower);
                return;
            }
        }
        Debug.Log("공격이 허공을 갈랐습니다.");
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

        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.SetTrigger("Hit");

        if (currentHp <= 0) { isDead = true; Debug.Log("<color=red>GAME OVER</color>"); }
    }

    void FinishTurn()
    {
        // 인디케이터는 지우지 않고 유지 (공격 실행 시까지 보여줌)
        TurnManager.Instance.OnPlayerActionCompleted();
    }

    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}