using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Config")]
    public int width = 5;  // 기본값
    public int height = 5; // 기본값
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.5f;
    public float indicatorHeight = 0.05f;

    public Dictionary<Vector2Int, bool> ObstacleMap = new Dictionary<Vector2Int, bool>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // [신규] 스테이지 시작 시 전장 크기 재설정 기능
    public void SetupGrid(int w, int h)
    {
        width = w;
        height = h;
        ObstacleMap.Clear(); // 이전 스테이지 장애물 초기화
        Debug.Log($"전장 초기화 완료: {width}x{height}");
    }

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    // 이동 가능 여부 (장애물 없고 맵 안쪽)
    public bool IsWalkable(Vector2Int pos) => IsInsideGrid(pos) && !IsObstacle(pos);

    public bool IsObstacle(Vector2Int pos) => ObstacleMap.ContainsKey(pos) && ObstacleMap[pos];

    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        // 중앙 정렬을 위한 Offset 계산 (크기가 바뀌어도 항상 중앙)
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }
}