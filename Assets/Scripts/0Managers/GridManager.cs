using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    public int width = 5;
    public int height = 7;
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.2f;
    public float indicatorHeight = 0.1f;

    // [변경] 단순 bool 맵 대신 상세 정보를 담는 노드 딕셔너리 사용
    public Dictionary<Vector2Int, TileNode> Tiles = new Dictionary<Vector2Int, TileNode>();

    // 예약 시스템 (기존 유지)
    private HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    void Awake() => Instance = this;

    void Start()
    {
        // 딕셔너리가 비어있다면 초기화 진행
        if (Tiles.Count == 0)
        {
            SetupGrid(width, height);
        }
    }

    public void SetupGrid(int w, int h)
    {
        width = w;
        height = h;
        Tiles.Clear();
        reservedTiles.Clear();

        // 그리드 초기화 (빈 노드 생성)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Tiles[pos] = new TileNode { Coordinate = pos };
            }
        }
    }

    // --- 조회 및 상태 확인 ---

    public TileNode GetTile(Vector2Int pos)
    {
        if (Tiles.ContainsKey(pos)) return Tiles[pos];
        return null;
    }

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    // 이동 가능 여부: 맵 안쪽 + 예약 안됨 + 유닛 없음 + (장애물이 없거나 통과 가능함)
    public bool IsWalkable(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return false;

        var tile = GetTile(pos);
        if (tile == null) return false;

        // 예약된 타일인가?
        if (IsReserved(pos)) return false;

        // 막혀있는가? (유닛이 있거나 통과 불가 장애물)
        return !tile.IsBlocked;
    }

    // (구) ObstacleMap 호환용 -> 장애물이 있으면 true
    public bool IsObstacle(Vector2Int pos)
    {
        var tile = GetTile(pos);
        return tile != null && tile.HasObstacle;
    }

    // --- 예약 시스템 (기존 유지) ---
    public bool IsReserved(Vector2Int pos) => reservedTiles.Contains(pos);
    public void ClearReservations() => reservedTiles.Clear();

    public bool TryReserveTile(Vector2Int pos)
    {
        if (!IsWalkable(pos)) return false; // Walkable 검사 내부에 IsReserved 체크 포함됨
        reservedTiles.Add(pos);
        return true;
    }

    public void CancelReservation(Vector2Int pos)
    {
        if (reservedTiles.Contains(pos)) reservedTiles.Remove(pos);
    }

    // --- 유틸리티 ---
    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }

    // [신규] 오브젝트 등록/해제 헬퍼
    public void RegisterObstacle(Vector2Int pos, ObstacleBase obs)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Obstacle = obs;
    }

    public void RemoveObstacle(Vector2Int pos)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Obstacle = null;
    }

    public void RegisterUnit(Vector2Int pos, EnemyBase unit)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.OccupyingUnit = unit;
    }

    public void RegisterZone(Vector2Int pos, ZoneBase zone)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Zone = zone;
    }

    public void RemoveZone(Vector2Int pos)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Zone = null;
    }

    public void RegisterItem(Vector2Int pos, ItemBase item)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Item = item;
    }

    public void RemoveItem(Vector2Int pos)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.Item = null;
    }

    public void RemoveUnit(Vector2Int pos)
    {
        var tile = GetTile(pos);
        if (tile != null) tile.OccupyingUnit = null;
    }
}