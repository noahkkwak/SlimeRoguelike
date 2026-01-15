using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int width = 5;
    public int height = 5;
    public float cellSpacing = 2.0f;

    [Header("Height Settings (Y-Axis)")]
    [Tooltip("캐릭터와 적이 위치할 높이")]
    public float unitHeight = 0.1f;
    [Tooltip("공격 범위 표시기가 위치할 높이")]
    public float indicatorHeight = 0.3f;

    public Dictionary<Vector2Int, bool> ObstacleMap = new Dictionary<Vector2Int, bool>();

    void Awake()
    {
        Instance = this;
        // 테스트용 장애물 (2,2) 배치
        ObstacleMap[new Vector2Int(2, 2)] = true;
    }

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    public bool IsBlocked(Vector2Int pos) => !IsInsideGrid(pos) || (ObstacleMap.ContainsKey(pos) && ObstacleMap[pos]);

    // 설정된 높이값에 따라 월드 좌표를 반환하는 헬퍼 함수
    public Vector3 GetWorldPosition(Vector2Int gridPos, float yPos)
    {
        return new Vector3(gridPos.x * cellSpacing, yPos, -gridPos.y * cellSpacing);
    }
}