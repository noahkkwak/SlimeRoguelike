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

    void Start()
    {
        // 씬에 미리 배치된 오브젝트라면 게임 시작 시 등록 시도
        if (GridManager.Instance != null)
        {
            Initialize(gridPosition); // 필요 시 좌표 보정 로직 추가 가능
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
        Debug.Log($"Obstacle Hit! Dmg: {damage}");

        if (type == ObstacleType.Destructible || isDestructible)
        {
            if (GridManager.Instance != null)
                GridManager.Instance.RemoveObstacle(gridPosition);

            Destroy(gameObject);
        }
    }
}