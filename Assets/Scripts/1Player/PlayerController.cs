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

        // [중요] 맵 크기가 변해도 항상 중앙 하단에 위치하도록 자동 계산
        if (GridManager.Instance != null)
        {
            int centerX = GridManager.Instance.width / 2; // 예: 7칸이면 3 (0,1,2,[3],4,5,6)
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

        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);
        if (Input.GetKeyDown(KeyCode.Space) && selectedAction != PlayerAction.None) ExecuteAction();
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int next = currentPos + dir;

        // 플레이어는 맵의 맨 아랫줄(Height-1)에서만 이동 가능
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
        if (selectedAction == PlayerAction.Defend)
        {
            finalDmg = Mathf.RoundToInt(dmg * 0.5f);
            Debug.Log($"<color=blue>[방어 성공]</color> 대미지 감소 ({dmg} -> {finalDmg})");
        }

        currentHp -= finalDmg;

        // 피격 애니메이션 (있으면)
        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.SetTrigger("Hit");

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