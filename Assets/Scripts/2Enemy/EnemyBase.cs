using UnityEngine;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    public Vector2Int currentPos = new Vector2Int(2, 0);
    private bool isAttackPending = false;

    [Header("Visuals")]
    public GameObject attackIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    void Start() => UpdateVisualPosition();

    public void ExecuteTurn()
    {
        if (isAttackPending) PerformAttack();
        else DecideNextAction();
    }

    void DecideNextAction()
    {
        int playerX = FindObjectOfType<PlayerController>().currentPos.x;

        if (playerX == currentPos.x)
        {
            isAttackPending = true;
            ShowAttackRange();
        }
        else
        {
            int dirX = (playerX > currentPos.x) ? 1 : -1;
            currentPos.x += dirX;
            UpdateVisualPosition();
        }
    }

    void ShowAttackRange()
    {
        for (int y = 1; y < 5; y++)
        {
            Vector2Int targetPos = new Vector2Int(currentPos.x, y);
            if (GridManager.Instance.IsBlocked(targetPos)) break;
            SpawnIndicator(targetPos);
        }
    }

    void PerformAttack()
    {
        ClearIndicators();
        isAttackPending = false;
    }

    void SpawnIndicator(Vector2Int pos)
    {
        // 적의 예고 인디케이터도 0.3f 높이 적용
        Vector3 worldPos = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.indicatorHeight);
        GameObject go = Instantiate(attackIndicatorPrefab, worldPos, Quaternion.identity);
        activeIndicators.Add(go);
    }

    void ClearIndicators()
    {
        foreach (var go in activeIndicators) Destroy(go);
        activeIndicators.Clear();
    }

    void UpdateVisualPosition()
    {
        // 적 유닛도 0.1f 높이 적용
        transform.position = GridManager.Instance.GetWorldPosition(currentPos, GridManager.Instance.unitHeight);
    }
}