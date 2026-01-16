using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public int width = 7;
    public int height = 5;
    public float cellSpacing = 2.0f;
    public float unitHeight = 0.2f; // 모델이 바닥에 묻히지 않게 조금 높임
    public float indicatorHeight = 0.3f;

    public Dictionary<Vector2Int, bool> ObstacleMap = new Dictionary<Vector2Int, bool>();

    void Awake() => Instance = this;

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    public bool IsWalkable(Vector2Int pos) => IsInsideGrid(pos) && !IsObstacle(pos);
    public bool IsObstacle(Vector2Int pos) => ObstacleMap.ContainsKey(pos) && ObstacleMap[pos];

    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        // 중앙 정렬 계산식 검증 완료
        float xOffset = (width - 1) * cellSpacing * 0.5f;
        float zOffset = (height - 1) * cellSpacing * 0.0f; // y축(z방향)은 0행부터 시작하므로 0
        return new Vector3(gridPos.x * cellSpacing - xOffset, yPos, -gridPos.y * cellSpacing);
    }
}