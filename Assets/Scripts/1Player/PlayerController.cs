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

        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.W)) SelectAction(PlayerAction.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(PlayerAction.Defend);
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

    public void ResolveBufferedAction()
    {
        if (selectedAction == PlayerAction.Attack) PerformAttack();
        ClearIndicators();
    }

    // [핵심 수정] 공격 판정 로직
    void PerformAttack()
    {
        Debug.Log($"<color=cyan>[플레이어 공격 발동]</color>");

        // 내 앞(y-1)부터 0까지 순차 검색
        for (int y = currentPos.y - 1; y >= 0; y--)
        {
            Vector2Int tPos = new Vector2Int(currentPos.x, y);
            var tile = GridManager.Instance.GetTile(tPos);

            if (tile == null) continue;

            // 1. 장애물 발견?
            if (tile.HasObstacle)
            {
                Debug.Log($"<color=orange>HIT OBSTACLE!</color> {tile.Obstacle.name}");
                tile.Obstacle.TakeDamage(attackPower);
                return; // [중요] 관통하지 않고 종료
            }

            // 2. 적 발견?
            if (tile.HasUnit)
            {
                Debug.Log($"<color=red>HIT ENEMY!</color> {tile.OccupyingUnit.data.enemyName}");
                tile.OccupyingUnit.TakeDamage(attackPower);
                return; // [중요] 관통하지 않고 종료
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
            var tile = GridManager.Instance.GetTile(tPos);

            if (tile != null)
            {
                // 인디케이터 생성
                activeIndicators.Add(Instantiate(attackIndicatorPrefab, GridManager.Instance.GetWorldPosition(tPos, GridManager.Instance.indicatorHeight), Quaternion.identity));

                // [시각적 디테일] 장애물이 있으면 거기까지만 표시하고 루프 종료 (시야 막힘)
                if (tile.HasObstacle) break;
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        int finalDmg = dmg;
        if (selectedAction == PlayerAction.Defend) finalDmg = Mathf.RoundToInt(dmg * 0.5f);
        currentHp -= finalDmg;

        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.SetTrigger("Hit");

        if (currentHp <= 0) { isDead = true; Debug.Log("<color=red>GAME OVER</color>"); }
    }

    void FinishTurn()
    {
        TurnManager.Instance.OnPlayerActionCompleted();
    }

    void ClearIndicators() { foreach (var g in activeIndicators) Destroy(g); activeIndicators.Clear(); }
    void UpdateVisual() => transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
}