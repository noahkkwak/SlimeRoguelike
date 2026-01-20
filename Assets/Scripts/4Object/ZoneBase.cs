using UnityEngine;

public class ZoneBase : MonoBehaviour
{
    public ZoneType type;
    public Vector2Int currentPos;

    public void Initialize(Vector2Int pos)
    {
        currentPos = pos;
        // 영역은 바닥에 장판처럼 깔립니다 (높이 0.02f)
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetWorldPosition(pos, 0.02f);
            GridManager.Instance.RegisterZone(pos, this); // 그리드에 등록
        }
    }
}