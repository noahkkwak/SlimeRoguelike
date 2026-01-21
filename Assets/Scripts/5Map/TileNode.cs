using UnityEngine;

[System.Serializable]
public class TileNode
{
    public Vector2Int Coordinate;
    public bool IsWalkable; // 대문자 I로 수정 (표준)

    // 이 타일 위에 있는 것들
    public EnemyBase OccupyingUnit;
    public ObstacleBase Obstacle;
    public ZoneBase Zone;
    public ItemBase Item; // [신규] 아이템 슬롯 추가

    // [신규] 생성자 (오류 CS1729 해결)
    public TileNode(Vector2Int coord, bool walkable)
    {
        this.Coordinate = coord;
        this.IsWalkable = walkable;
        this.OccupyingUnit = null;
        this.Obstacle = null;
        this.Zone = null;
        this.Item = null;
    }

    // 편의 속성
    public bool HasUnit => OccupyingUnit != null;
    public bool HasObstacle => Obstacle != null;
    public bool HasZone => Zone != null;
    public bool HasItem => Item != null;
}