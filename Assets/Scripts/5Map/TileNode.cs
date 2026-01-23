using Unity.VisualScripting;
using UnityEngine;

// 타일 하나에 대한 상세 정보
[System.Serializable]
public class TileNode
{
    public Vector2Int Coordinate; // 좌표

    // 이 타일에 있는 존재들 (교체 가능하므로 null 체크 필수)
    public EnemyBase OccupyingUnit = null;      // 적
    public ObstacleBase Obstacle = null;        // 장애물
    public ZoneBase Zone = null;                // 영역 (장판)
    public ItemBase Item = null;                // 바닥에 떨어진 아이템

    // 상태 확인용 헬퍼
    public bool IsBlocked => OccupyingUnit != null || (Obstacle != null && !Obstacle.IsWalkable);
    public bool HasObstacle => Obstacle != null;
    public bool HasUnit => OccupyingUnit != null;
}