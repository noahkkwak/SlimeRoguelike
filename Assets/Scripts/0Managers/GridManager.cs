using System.Collections;
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
    public float scrollSpeed = 0.3f;

    public Dictionary<Vector2Int, TileNode> Tiles = new Dictionary<Vector2Int, TileNode>();
    private HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    void Awake() => Instance = this;

    void Start()
    {
        if (Tiles.Count == 0) SetupGrid(width, height);
    }

    public void SetupGrid(int w, int h)
    {
        width = w;
        height = h;
        Tiles.Clear();
        reservedTiles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Tiles[pos] = new TileNode { Coordinate = pos };
            }
        }
    }

    public TileNode GetTile(Vector2Int pos) => Tiles.ContainsKey(pos) ? Tiles[pos] : null;
    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    public bool IsWalkable(Vector2Int pos)
    {
        if (!IsInsideGrid(pos)) return false;
        var tile = GetTile(pos);
        if (tile == null) return false;
        if (IsReserved(pos)) return false;
        return !tile.IsBlocked;
    }

    public bool IsObstacle(Vector2Int pos)
    {
        var tile = GetTile(pos);
        return tile != null && tile.HasObstacle;
    }

    public bool IsReserved(Vector2Int pos) => reservedTiles.Contains(pos);
    public void ClearReservations() => reservedTiles.Clear();
    public bool TryReserveTile(Vector2Int pos)
    {
        if (!IsWalkable(pos)) return false;
        reservedTiles.Add(pos);
        return true;
    }
    public void CancelReservation(Vector2Int pos) { if (reservedTiles.Contains(pos)) reservedTiles.Remove(pos); }

    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }

    // --- 등록/해제 헬퍼 (CS1061 에러 해결) ---
    public void RegisterObstacle(Vector2Int pos, ObstacleObject obs)
    {
        var t = GetTile(pos);
        if (t != null) t.Obstacle = obs;
    }

    public void RemoveObstacle(Vector2Int pos)
    {
        var t = GetTile(pos);
        if (t != null) t.Obstacle = null;
    }

    public void RegisterUnit(Vector2Int pos, EnemyBase unit)
    {
        var t = GetTile(pos);
        if (t != null) t.OccupyingUnit = unit;
    }

    public void RemoveUnit(Vector2Int pos)
    {
        var t = GetTile(pos);
        if (t != null) t.OccupyingUnit = null;
    }

    // [복구] Zone 및 Item 관련 메서드
    public void RegisterZone(Vector2Int pos, ZoneBase zone)
    {
        var t = GetTile(pos);
        if (t != null) t.Zone = zone;
    }

    public void RemoveZone(Vector2Int pos)
    {
        var t = GetTile(pos);
        if (t != null) t.Zone = null;
    }

    public void RegisterItem(Vector2Int pos, ItemBase item)
    {
        var t = GetTile(pos);
        if (t != null) t.Item = item;
    }

    public void RemoveItem(Vector2Int pos)
    {
        var t = GetTile(pos);
        if (t != null) t.Item = null;
    }

    // --- 전장 이동 시스템 ---
    public IEnumerator ScrollCentralRows()
    {
        int startY = 1;
        int endY = height - 2;

        for (int y = endY; y >= startY; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                Vector2Int nextPos = new Vector2Int(x, y + 1);
                MoveTileContent(currentPos, nextPos);
            }
        }
        yield return new WaitForSeconds(scrollSpeed);
    }

    void MoveTileContent(Vector2Int from, Vector2Int to)
    {
        TileNode fromNode = GetTile(from);
        TileNode toNode = GetTile(to);

        if (fromNode == null || toNode == null) return;

        // Unit Move
        if (fromNode.OccupyingUnit != null)
        {
            toNode.OccupyingUnit = fromNode.OccupyingUnit;
            fromNode.OccupyingUnit = null;
            toNode.OccupyingUnit.currentPos = to;
            StartCoroutine(SmoothMove(toNode.OccupyingUnit.transform, GetWorldPosition(to, unitHeight)));
        }

        // Obstacle Move
        if (fromNode.Obstacle != null)
        {
            toNode.Obstacle = fromNode.Obstacle;
            fromNode.Obstacle = null;
            if (toNode.Obstacle != null) toNode.Obstacle.gridPosition = to;
            StartCoroutine(SmoothMove(toNode.Obstacle.transform, GetWorldPosition(to, unitHeight)));
        }

        // Item Move
        if (fromNode.Item != null)
        {
            toNode.Item = fromNode.Item;
            fromNode.Item = null;
            StartCoroutine(SmoothMove(toNode.Item.transform, GetWorldPosition(to, 0.1f)));
        }
    }

    IEnumerator SmoothMove(Transform target, Vector3 endPos)
    {
        float t = 0;
        Vector3 startPos = target.position;
        while (t < 1)
        {
            t += Time.deltaTime / scrollSpeed;
            target.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        target.position = endPos;
    }
}