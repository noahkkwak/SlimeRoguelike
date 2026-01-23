using UnityEngine;
using SlimeRoguelike; // GlobalEnums 사용을 위해

[System.Serializable]
public class TileNode
{
    public Vector2Int Coordinate; // 좌표

    // 이 타일에 있는 존재들 (이동 시 이 정보들이 같이 옮겨짐)
    public EnemyBase OccupyingUnit = null;      // 적 유닛
                                                // 플레이어는 별도 관리하기도 하지만, 충돌 체크를 위해 여기 넣기도 함 (설계에 따라 다름)
                                                // 현재 구조에서는 GridManager.RegisterUnit을 통해 적만 등록되는 것으로 보임.

    public ObstacleBase Obstacle = null;        // 장애물
    public ZoneBase Zone = null;                // 영역 (장판)
    public ItemBase Item = null;                // 아이템

    // 상태 확인용 헬퍼 프로퍼티
    public bool IsBlocked => OccupyingUnit != null || (Obstacle != null && !Obstacle.IsWalkable);
    public bool HasObstacle => Obstacle != null;
    public bool HasUnit => OccupyingUnit != null;
}