using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -2); // 80도 각도를 위한 초기 오프셋
    [SerializeField] private float smoothSpeed = 0.125f;

    private void LateUpdate()
    {
        if (target == null) return;

        // 카메라의 위치를 부드럽게 캐릭터 추적하도록 설정
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 카메라는 항상 타겟(캐릭터)을 바라봄
        transform.LookAt(target.position);
    }
}