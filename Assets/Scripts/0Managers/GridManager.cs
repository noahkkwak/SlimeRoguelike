using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    // [중요] 에디터에서 설정한 값을 존중하기 위해 코드에서 강제로 값을 대입하지 않습니다.
    public int width = 5;
    public int height = 7;
    public float cellSpacing = 2.0f;

    // [기획 요청] 높이값 수정 완료
    public float unitHeight = 0.2f;
    public float indicatorHeight = 0.1f;

    public Dictionary<Vector2Int, bool> ObstacleMap = new Dictionary<Vector2Int, bool>();

    // 타일 예약 장부 (적 이동 겹침 방지용)
    private HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    void Awake() => Instance = this;

    // 스테이지 매니저 등에서 필요할 때만 호출하여 크기 변경
    public void SetupGrid(int w, int h)
    {
        width = w;
        height = h;
        ObstacleMap.Clear();
        reservedTiles.Clear();
    }

    public void ClearReservations() => reservedTiles.Clear();

    public bool TryReserveTile(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return false;
        if (IsObstacle(pos)) return false;
        if (reservedTiles.Contains(pos)) return false;

        reservedTiles.Add(pos);
        return true;
    }

    public void CancelReservation(Vector2Int pos)
    {
        if (reservedTiles.Contains(pos))
            reservedTiles.Remove(pos);
    }

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    public bool IsWalkable(Vector2Int pos) => IsInsideGrid(pos) && !IsObstacle(pos);
    public bool IsObstacle(Vector2Int pos) => ObstacleMap.ContainsKey(pos) && ObstacleMap[pos];

    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }
}