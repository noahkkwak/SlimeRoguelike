using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
    public Vector2Int gridPosition; // 현재 그리드 좌표
    public bool isWalkable = false; // 이동 가능 여부 (일반적으론 false)
    public bool isDestructible = false; // 파괴 가능 여부

    // 에디터에서 배치 후 시작할 때 그리드에 등록
    void Start()
    {
        // 1. 월드 좌표를 그리드 좌표로 변환 (간단히 반올림 처리 예시)
        // 만약 GridManager에 WorldToGrid 함수가 있다면 그걸 쓰는 게 정확함.
        // 여기서는 수동 할당 혹은 초기화 로직이 필요함.

        // 예시: 외부(StageManager 등)에서 Init을 호출해준다면 이 Start는 비워둬도 됨.
        // 하지만 씬에 미리 배치된 오브젝트라면 아래처럼 스스로 등록해야 함.
        if (GridManager.Instance != null)
        {
            // 임시: 현재 위치를 기준으로 좌표 계산 (GridManager의 셀 크기 반영 필요)
            // 정확한 구현을 위해선 GridManager에 WorldToCell 메서드를 추가하는 것이 좋음.
            // 일단은 외부에서 Initialize를 호출한다고 가정하고 비워둠.
        }
    }

    public void Initialize(Vector2Int pos)
    {
        this.gridPosition = pos;
        transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);

        // **중요: 그리드 매니저에게 나 여기 있다고 알림**
        GridManager.Instance.RegisterObstacle(pos, this);
    }

    public void OnHit(int damage, Vector2Int dir)
    {
        // 피격 로직 (밀치기 or 파괴)
        Debug.Log($"Obstacle Hit! Dmg: {damage}");

        if (isDestructible)
        {
            GridManager.Instance.RemoveObstacle(gridPosition);
            Destroy(gameObject);
        }
    }
}