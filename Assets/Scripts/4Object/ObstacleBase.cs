using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
    public string objName;
    public ObstacleType type;
    public int maxHp = 1;
    public int currentHp;
    public Vector2Int currentPos;
    public bool IsWalkable = false; // 기본적으로 못 지나감

    public void Initialize(Vector2Int pos)
    {
        currentPos = pos;
        currentHp = maxHp;
        transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);

        // 그리드에 나 자신 등록
        GridManager.Instance.RegisterObstacle(pos, this);
    }

    public void TakeDamage(int damage)
    {
        if (type == ObstacleType.Indestructible) return;

        currentHp -= damage;
        // 피격 효과 (애니메이션 등)

        if (currentHp <= 0)
        {
            DestroyObstacle();
        }
    }

    public virtual void DestroyObstacle()
    {
        // 그리드에서 제거
        GridManager.Instance.RemoveObstacle(currentPos);

        // TODO: 영역 생성 or 아이템 드랍 로직 호출

        Destroy(gameObject);
    }

    // 밀치기 로직 (추후 구현)
    public bool TryPush(Vector2Int direction)
    {
        // 연쇄 충돌 로직이 들어갈 곳
        return false;
    }
}