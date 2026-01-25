using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    public int width = 7;
    public int height = 5;
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.2f;

    // [복구 완료] 이 변수가 없어서 에러가 났었어.
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

    // --- 조회 및 검사 ---
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

    // --- 유틸리티 ---
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
        float zOffset = (height - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, gridPos.y * cellSpacing - zOffset);
    }

    // --- 등록/해제 헬퍼 ---
    public void RegisterObstacle(Vector2Int pos, ObstacleObject obs) { var t = GetTile(pos); if (t != null) t.Obstacle = obs; }
    public void RemoveObstacle(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.Obstacle = null; }
    public void RegisterUnit(Vector2Int pos, EnemyBase unit) { var t = GetTile(pos); if (t != null) t.OccupyingUnit = unit; }
    public void RemoveUnit(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.OccupyingUnit = null; }
    public void RegisterItem(Vector2Int pos, ItemBase item) { var t = GetTile(pos); if (t != null) t.Item = item; }
    public void RemoveItem(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.Item = null; }
    public void RegisterZone(Vector2Int pos, ZoneBase zone) { var t = GetTile(pos); if (t != null) t.Zone = zone; }
    public void RemoveZone(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.Zone = null; }


    // ==================================================================================
    // [가로형 전장 이동 시스템]
    // ==================================================================================
    public IEnumerator ScrollCentralRowsLeft()
    {
        int safeRowBottom = height - 1;

        // [수정] safeRowTop 경고 제거 및 로직 단순화
        // y 1부터 height-2 까지 반복 (중앙 행)
        for (int y = 1; y < safeRowBottom; y++)
        {
            // 1. 맨 왼쪽(0) 소멸
            TileNode firstNode = GetTile(new Vector2Int(0, y));
            if (firstNode != null)
            {
                if (firstNode.Obstacle != null) Destroy(firstNode.Obstacle.gameObject);
                if (firstNode.OccupyingUnit != null) firstNode.OccupyingUnit.TakeDamage(9999);
                if (firstNode.Item != null) Destroy(firstNode.Item.gameObject);

                firstNode.Obstacle = null;
                firstNode.OccupyingUnit = null;
                firstNode.Item = null;
            }

            // 2. 왼쪽으로 이동
            for (int x = 1; x < width; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                Vector2Int targetPos = new Vector2Int(x - 1, y);
                MoveTileContent(currentPos, targetPos);
            }
        }

        yield return new WaitForSeconds(scrollSpeed);
    }

    void MoveTileContent(Vector2Int from, Vector2Int to)
    {
        TileNode fromNode = GetTile(from);
        TileNode toNode = GetTile(to);

        if (fromNode == null || toNode == null) return;

        if (fromNode.OccupyingUnit != null)
        {
            toNode.OccupyingUnit = fromNode.OccupyingUnit;
            fromNode.OccupyingUnit = null;
            toNode.OccupyingUnit.currentPos = to;
            StartCoroutine(SmoothMove(toNode.OccupyingUnit.transform, GetWorldPosition(to, unitHeight)));
        }

        if (fromNode.Obstacle != null)
        {
            toNode.Obstacle = fromNode.Obstacle;
            fromNode.Obstacle = null;
            toNode.Obstacle.Initialize(to);
            StartCoroutine(SmoothMove(toNode.Obstacle.transform, GetWorldPosition(to, unitHeight)));
        }

        if (fromNode.Item != null)
        {
            toNode.Item = fromNode.Item;
            fromNode.Item = null;
            StartCoroutine(SmoothMove(toNode.Item.transform, GetWorldPosition(to, 0.1f)));
        }
    }

    IEnumerator SmoothMove(Transform target, Vector3 endPos)
    {
        if (target == null) yield break;
        float t = 0;
        Vector3 startPos = target.position;
        while (t < 1)
        {
            t += Time.deltaTime / scrollSpeed;
            if (target != null) target.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        if (target != null) target.position = endPos;
    }
}