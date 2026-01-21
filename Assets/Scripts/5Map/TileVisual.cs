using UnityEngine;
using System.Collections;

public class TileVisual : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private float moveSpeed = 5f; // 지형 이동 속도 (적절히 느리게)

    public void Initialize()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
    }

    // 부드러운 이동
    public void MoveTo(Vector3 targetPos)
    {
        StopAllCoroutines(); // 혹시 실행 중인 게 있으면 중단
        StartCoroutine(MoveRoutine(targetPos));
    }

    IEnumerator MoveRoutine(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
    }

    // 소멸 (페이드 아웃)
    public void FadeOutAndDestroy(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    IEnumerator FadeOutRoutine(float duration)
    {
        float timer = 0f;

        // 아래로 살짝 떨어지면서 사라지는 연출 추가 (선택 사항)
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // 1. 투명도 조절
            if (meshRenderer != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(1f, 0f, progress);
                meshRenderer.material.color = c;
            }

            // 2. 낙하 (천천히)
            transform.position = Vector3.Lerp(startPos, endPos, progress);

            yield return null;
        }

        Destroy(gameObject);
    }
}