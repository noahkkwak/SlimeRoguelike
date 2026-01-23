using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlimeRoguelike; // GlobalEnums

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    public int width = 5;
    public int height = 7;
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.2f;
    public float indicatorHeight = 0.1f;
    public float scrollSpeed = 0.3f; // 전장 이동 속도

    // 맵 데이터
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

    public bool IsObstacle(Vector2Int pos)
    {
        var tile = GetTile(pos);
        return tile != null && tile.HasObstacle;
    }

    // --- 예약 시스템 ---
    public bool IsReserved(Vector2Int pos) => reservedTiles.Contains(pos);
    public void ClearReservations() => reservedTiles.Clear();
    public bool TryReserveTile(Vector2Int pos)
    {
        if (!IsWalkable(pos)) return false;
        reservedTiles.Add(pos);
        return true;
    }
    public void CancelReservation(Vector2Int pos) { if (reservedTiles.Contains(pos)) reservedTiles.Remove(pos); }

    // --- 좌표 변환 ---
    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }

    // --- 등록/해제 헬퍼 ---
    public void RegisterObstacle(Vector2Int pos, ObstacleBase obs) { var t = GetTile(pos); if (t != null) t.Obstacle = obs; }
    public void RemoveObstacle(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.Obstacle = null; }
    public void RegisterUnit(Vector2Int pos, EnemyBase unit) { var t = GetTile(pos); if (t != null) t.OccupyingUnit = unit; }
    public void RemoveUnit(Vector2Int pos) { var t = GetTile(pos); if (t != null) t.OccupyingUnit = null; }
    // 필요 시 Zone, Item 등록 함수 추가

    // ==================================================================================
    // [전장 이동 시스템] 중앙 행(Row)들이 플레이어 쪽(아래)으로 이동
    // ==================================================================================
    public IEnumerator ScrollCentralRows()
    {
        // 맨 윗줄(0)과 맨 아랫줄(height-1)을 제외한 중앙 영역만 이동 (1 ~ height-2)
        int startY = 1;
        int endY = height - 2;

        // 데이터 덮어쓰기 방지를 위해 '아래쪽 행'부터 처리 (Bottom-up)
        for (int y = endY; y >= startY; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                Vector2Int nextPos = new Vector2Int(x, y + 1); // 아래로 한 칸

                MoveTileContent(currentPos, nextPos);
            }
        }

        // 시각적 이동 대기
        yield return new WaitForSeconds(scrollSpeed);
    }

    // 타일의 내용물(유닛, 장애물)을 논리적/시각적으로 이동
    void MoveTileContent(Vector2Int from, Vector2Int to)
    {
        TileNode fromNode = GetTile(from);
        TileNode toNode = GetTile(to);

        if (fromNode == null || toNode == null) return;

        // A. 유닛 이동
        if (fromNode.OccupyingUnit != null)
        {
            // 만약 목적지에 이미 무언가 있다면 충돌 처리 (지금은 단순 덮어쓰기/겹침 허용)
            // 추후 '밀려난 곳에 장애물이 있으면 유닛 사망' 로직 추가 가능

            // 데이터 이동
            toNode.OccupyingUnit = fromNode.OccupyingUnit;
            fromNode.OccupyingUnit = null;

            // 유닛 내부 좌표 갱신
            toNode.OccupyingUnit.currentPos = to;

            // 시각적 이동 (부드럽게)
            StartCoroutine(SmoothMove(toNode.OccupyingUnit.transform, GetWorldPosition(to, unitHeight)));
        }

        // B. 장애물 이동
        if (fromNode.Obstacle != null)
        {
            toNode.Obstacle = fromNode.Obstacle;
            fromNode.Obstacle = null;

            // 장애물 내부 좌표 갱신 (만약 ObstacleBase에 좌표 변수가 있다면 여기서 갱신해줘야 함)
            if (toNode.Obstacle != null) toNode.Obstacle.gridPosition = to; // *ObstacleBase에 gridPosition이 있다고 가정

            // 시각적 이동
            StartCoroutine(SmoothMove(toNode.Obstacle.transform, GetWorldPosition(to, unitHeight)));
        }

        // C. 아이템 이동 (동일 로직 적용 가능)
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