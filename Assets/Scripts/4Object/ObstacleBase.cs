using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
    [Header("Settings")]
    public ObstacleType type = ObstacleType.Destructible;
    public CollisionEffect collisionEffect = CollisionEffect.None;

    [Header("State")]
    public Vector2Int gridPosition; // 현재 그리드 좌표
    public bool isWalkable = false; // 이동 가능 여부
    public bool isDestructible = true; // 파괴 가능 여부

    // 에디터에서 배치 후 시작할 때 그리드에 등록
    void Start()
    {
        // 씬에 미리 배치된 오브젝트라면 게임 시작 시 등록 시도
        // (TurnManager나 StageManager에서 일괄 처리한다면 이 부분은 없어도 됨)
        if (GridManager.Instance != null)
        {
            // 위치 보정 등은 생략하고 등록만 시도할 수도 있음
        }
    }

    public void Initialize(Vector2Int pos)
    {
        this.gridPosition = pos;

        // 위치를 그리드에 맞게 강제 조정
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);

            // **중요: 그리드 매니저에게 나 여기 있다고 알림**
            GridManager.Instance.RegisterObstacle(pos, this);
        }
    }

    public void OnHit(int damage, Vector2Int dir)
    {
        // 피격 로직 (밀치기 or 파괴)
        Debug.Log($"Obstacle Hit! Dmg: {damage}");

        if (type == ObstacleType.Destructible || isDestructible)
        {
            // 파괴 로직
            if (GridManager.Instance != null)
                GridManager.Instance.RemoveObstacle(gridPosition);

            Destroy(gameObject);
        }
        else if (type == ObstacleType.Volatile)
        {
            // 폭발 로직 등...
        }
    }
}