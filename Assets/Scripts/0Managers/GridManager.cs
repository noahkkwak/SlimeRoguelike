using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    [Tooltip("실제 플레이 영역 + 예고 영역(1칸)을 포함한 값으로 설정하세요. (예: 5->6, 7->8)")]
    public int width = 7;
    public int height = 5;

    [Header("Layout Settings")]
    public float tileSize = 1.0f;
    public float unitHeight = 0.5f;
    public float indicatorHeight = 0.55f;

    [Header("Visual")]
    public GameObject tilePrefab;

    // 데이터 관리
    public Dictionary<Vector2Int, TileNode> Tiles = new Dictionary<Vector2Int, TileNode>();
    private Dictionary<Vector2Int, TileVisual> tileVisuals = new Dictionary<Vector2Int, TileVisual>();
    private HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        Tiles.Clear();
        foreach (var visual in tileVisuals.Values)
        {
            if (visual != null) Destroy(visual.gameObject);
        }
        tileVisuals.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // [오류 해결] TileNode 생성자 호출 정상화
                TileNode node = new TileNode(pos, true);
                Tiles[pos] = node;

                SpawnTileVisual(pos);
            }
        }
    }

    void SpawnTileVisual(Vector2Int pos)
    {
        if (tilePrefab == null) return;

        Vector3 worldPos = GetWorldPosition(pos, 0);
        GameObject go = Instantiate(tilePrefab, worldPos, Quaternion.identity);
        go.name = $"Tile_{pos.x}_{pos.y}";
        go.transform.SetParent(this.transform);

        TileVisual visual = go.GetComponent<TileVisual>();
        if (visual == null) visual = go.AddComponent<TileVisual>();

        visual.Initialize();
        tileVisuals[pos] = visual;
    }

    public void ShiftTerrain()
    {
        // 1. 소멸 (x=0)
        for (int y = 0; y < height; y++)
        {
            Vector2Int firstPos = new Vector2Int(0, y);
            if (tileVisuals.ContainsKey(firstPos))
            {
                TileVisual oldTile = tileVisuals[firstPos];
                tileVisuals.Remove(firstPos);
                oldTile.FadeOutAndDestroy(3.0f);
            }
        }

        // 2. 이동 (나머지)
        Dictionary<Vector2Int, TileVisual> newVisuals = new Dictionary<Vector2Int, TileVisual>();
        for (int x = 1; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                Vector2Int nextPos = new Vector2Int(x - 1, y);

                if (tileVisuals.ContainsKey(currentPos))
                {
                    TileVisual visual = tileVisuals[currentPos];
                    visual.MoveTo(GetWorldPosition(nextPos, 0));
                    newVisuals[nextPos] = visual;
                }
            }
        }
        tileVisuals = newVisuals;

        // 3. 생성 (예고 구역)
        int spawnX = width - 1;
        for (int y = 0; y < height; y++)
        {
            SpawnTileVisual(new Vector2Int(spawnX, y));
        }
    }

    // --- Helper Functions ---

    public Vector3 GetWorldPosition(Vector2Int gridPos, float yOffset)
    {
        return new Vector3(gridPos.x * tileSize, yOffset, gridPos.y * tileSize);
    }

    public bool IsInsideGrid(Vector2Int p) => Tiles.ContainsKey(p);

    public bool IsWalkable(Vector2Int p)
    {
        if (p.x >= width - 1) return false; // 예고 구역 진입 불가
        if (!IsInsideGrid(p)) return false;

        var tile = Tiles[p];
        // [오류 해결] IsWalkable (대문자) 사용
        return tile.IsWalkable && !tile.HasObstacle && !tile.HasUnit && !IsReserved(p);
    }

    public TileNode GetTile(Vector2Int p) => Tiles.ContainsKey(p) ? Tiles[p] : null;

    // [오류 해결] ObstacleManager에서 사용하는 헬퍼 함수 복구
    public bool IsObstacle(Vector2Int p)
    {
        if (!IsInsideGrid(p)) return false;
        return Tiles[p].HasObstacle;
    }

    // --- Unit / Obstacle / Zone / Item Registration ---

    public void RegisterUnit(Vector2Int p, EnemyBase u) { if (Tiles.ContainsKey(p)) Tiles[p].OccupyingUnit = u; }
    public void RemoveUnit(Vector2Int p) { if (Tiles.ContainsKey(p)) Tiles[p].OccupyingUnit = null; }

    public void RegisterObstacle(Vector2Int p, ObstacleBase o) { if (Tiles.ContainsKey(p)) Tiles[p].Obstacle = o; }
    public void RemoveObstacle(Vector2Int p) { if (Tiles.ContainsKey(p)) Tiles[p].Obstacle = null; }

    public void RegisterZone(Vector2Int p, ZoneBase z) { if (Tiles.ContainsKey(p)) Tiles[p].Zone = z; }
    public void RemoveZone(Vector2Int p) { if (Tiles.ContainsKey(p)) Tiles[p].Zone = null; }

    // [오류 해결] Item 관련 함수 추가
    public void RegisterItem(Vector2Int p, ItemBase i) { if (Tiles.ContainsKey(p)) Tiles[p].Item = i; }
    public void RemoveItem(Vector2Int p) { if (Tiles.ContainsKey(p)) Tiles[p].Item = null; }

    // --- Reservation ---

    public bool TryReserveTile(Vector2Int p)
    {
        if (IsWalkable(p))
        {
            reservedTiles.Add(p);
            return true;
        }
        return false;
    }

    public void CancelReservation(Vector2Int p) { if (reservedTiles.Contains(p)) reservedTiles.Remove(p); }
    public void ClearReservations() { reservedTiles.Clear(); }
    public bool IsReserved(Vector2Int p) { return reservedTiles.Contains(p); }
}