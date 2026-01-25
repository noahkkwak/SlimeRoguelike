using UnityEngine;
// namespace SlimeRoguelike {  <-- 이거 지워야 함!

[System.Serializable]
public class TileNode
{
    public Vector2Int Coordinate;
    public EnemyBase OccupyingUnit = null;
    public ObstacleObject Obstacle = null;
    public ZoneBase Zone = null;
    public ItemBase Item = null;

    public bool IsBlocked => OccupyingUnit != null || (Obstacle != null && !Obstacle.IsWalkable);
    public bool HasObstacle => Obstacle != null;
    public bool HasUnit => OccupyingUnit != null;
}
// } <-- 맨 아래 이것도 지워야 함!