using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [Header("Settings")]
    public ObstacleType type = ObstacleType.Destructible;
    public CollisionEffect collisionEffect = CollisionEffect.None;

    [Header("State")]
    public Vector2Int gridPosition;
    public bool isWalkable = false; // 소문자 변수
    public bool isDestructible = true;

    // [중요] 외부에서 접근할 대문자 프로퍼티 추가 (CS1061 에러 해결)
    public bool IsWalkable => isWalkable;

    void Start()
    {
        if (GridManager.Instance != null)
        {
            Initialize(gridPosition);
        }
    }

    public void Initialize(Vector2Int pos)
    {
        this.gridPosition = pos;

        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);
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