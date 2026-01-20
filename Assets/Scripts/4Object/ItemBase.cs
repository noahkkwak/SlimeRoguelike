using UnityEngine;

public class ItemBase : MonoBehaviour
{
    public ItemType type;
    public Vector2Int currentPos;

    public void Initialize(Vector2Int pos)
    {
        currentPos = pos;
        // 아이템은 잘 보이게 약간 위로 띄웁니다 (높이 0.3f)
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetWorldPosition(pos, 0.3f);
            GridManager.Instance.RegisterItem(pos, this); // 그리드에 등록
        }
    }

    // 획득 시 호출 (추후 사용)
    public void OnPickedUp()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.RemoveItem(currentPos);

        Destroy(gameObject);
    }
}