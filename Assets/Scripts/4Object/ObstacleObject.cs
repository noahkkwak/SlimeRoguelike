using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [Header("Settings")]
    public ObstacleType type = ObstacleType.Destructible;
    public CollisionEffect collisionEffect = CollisionEffect.None;

    [Header("State")]
    public Vector2Int gridPosition;
    public bool isWalkable = false;
    public bool isDestructible = true;

    // 외부 접근용 프로퍼티
    public bool IsWalkable => isWalkable;

    private bool isInitialized = false; // [신규] 중복 초기화 방지 플래그

    void Start()
    {
        // 매니저에 의해 생성된 게 아니라, 씬에 미리 배치된 경우 스스로 등록
        if (!isInitialized && GridManager.Instance != null)
        {
            Initialize(gridPosition);
        }
    }

    public void Initialize(Vector2Int pos)
    {
        this.gridPosition = pos;
        this.isInitialized = true;

        if (GridManager.Instance != null)
        {
            // 위치를 그리드 좌표에 맞춰 강제 이동
            transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);

            // 그리드 매니저에게 등록
            GridManager.Instance.RegisterObstacle(pos, this);
        }
    }

    public void OnHit(int damage, Vector2Int dir)
    {
        Debug.Log($"Obstacle Hit! Dmg: {damage}");

        if (type == ObstacleType.Destructible || isDestructible)
        {
            // 파괴 처리
            if (GridManager.Instance != null)
                GridManager.Instance.RemoveObstacle(gridPosition);

            if (ObstacleManager.Instance != null)
                ObstacleManager.Instance.RemoveObstacle(this);

            Destroy(gameObject);
        }
    }
}