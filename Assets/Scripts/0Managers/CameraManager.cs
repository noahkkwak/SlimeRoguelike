using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;     // 추적할 대상 (Player)
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10); // 유지할 거리/각도

    [Header("Smooth Settings")]
    [SerializeField] private float smoothSpeed = 0.125f; // 따라가는 부드러움 정도

    private void LateUpdate()
    {
        if (target == null) return;

        // [MODIFY] 타겟의 회전과 관계없이 타겟의 위치 + 오프셋 값으로만 목표 위치 설정
        Vector3 desiredPosition = target.position + offset;

        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // [ADD] 카메라는 항상 타겟을 바라보되, 회전값은 고정된 형태가 됨
        // 만약 완전 고정된 각도를 원하시면 아래 LookAt 대신 인스펙터에서 직접 각도를 설정해도 됩니다.
        transform.LookAt(target.position);
    }

    // 에디터에서 타겟을 쉽게 할당하기 위한 함수
    public void SetTarget(Transform newTarget) => target = newTarget;
}