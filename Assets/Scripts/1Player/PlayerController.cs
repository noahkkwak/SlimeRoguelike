using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public string charName = "Slime";
    public int maxHp = 10;
    public int currentHp;
    public int attackPower = 2;
    public Vector2Int currentPos = new Vector2Int(3, 4);

    // [중요] 외부에서 확인 가능하도록 public get 접근자 추가
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
        UpdateVisual();
    }

    // [신규] 턴이 시작될 때 상태를 리셋하는 함수 (TurnManager가 호출)
    public void OnTurnStart()
    {
        if (isDead) return;

        // 방어 태세 등 이전 턴의 행동 상태 초기화
        selectedAction = PlayerAction.None;
        ClearIndicators();
        // Debug.Log("플레이어 턴 시작: 상태 초기화 완료");
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
        if (GridManager.Instance.IsWalkable(next) && next.y == 4)
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
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsObstacle(tPos)) return;
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
        // 방어 상태일 때만 대미지 감소
        if (selectedAction == PlayerAction.Defend)
        {
            finalDmg = Mathf.RoundToInt(dmg * 0.5f);
            Debug.Log($"<color=blue>[방어 성공]</color> 대미지 감소 ({dmg} -> {finalDmg})");
            // 기획 의도: 방어는 '한 번 맞으면 풀리는' 게 아니라 '이번 턴 동안 유지'라면 여기서 selectedAction을 초기화하지 않습니다.
            // 턴 시작 시(OnTurnStart)에 초기화되므로 이번 턴 적들의 집중 포화는 다 막습니다.
        }

        currentHp -= finalDmg;
        Debug.Log($"<color=red>[플레이어 피격]</color> 남은 HP: {currentHp}");
        if (currentHp <= 0) { isDead = true; Debug.Log("<color=red>GAME OVER</color>"); }
    }

    void FinishTurn()
    {
        // 턴을 넘길 때는 인디케이터를 지우지 않음 (방어/공격 이펙트 유지 or 적 턴에 시각적 정보 필요 시)
        // 단, 기획적으로 '방어 인디케이터'는 적이 때릴 때까지 보여야 하므로 유지.
        if (selectedAction != PlayerAction.Defend) ClearIndicators();

        TurnManager.Instance.OnPlayerActionCompleted();
    }

    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}