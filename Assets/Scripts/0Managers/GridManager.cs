using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    public int width = 5;
    public int height = 7;
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.5f;
    public float indicatorHeight = 0.05f;

    public Dictionary<Vector2Int, bool> ObstacleMap = new Dictionary<Vector2Int, bool>();

    // [신규] 이번 턴 이동 예약 장부
    private HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    void Awake() => Instance = this;

    public void SetupGrid(int w, int h)
    {
        width = w;
        height = h;
        ObstacleMap.Clear();
        reservedTiles.Clear();
    }

    // 매 턴 초기화
    public void ClearReservations() => reservedTiles.Clear();

    // 예약 시도 (성공 시 true)
    public bool TryReserveTile(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return false;
        if (IsObstacle(pos)) return false;
        if (reservedTiles.Contains(pos)) return false; // 이미 누가 찜함

        reservedTiles.Add(pos);
        return true;
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