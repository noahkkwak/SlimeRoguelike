using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotationSpeed = 15f;

    private CharacterController _controller;

    private void Start() => _controller = GetComponent<CharacterController>();

    private void Update()
    {
        // 1. InputManager로부터 값 읽기
        Vector2 input = InputManager.Instance.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;

        // 2. 이동 및 회전 로직
        if (moveDir.magnitude >= 0.1f)
        {
            // 부드러운 회전
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 이동
            _controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // 중력 처리 (기본)
        if (!_controller.isGrounded)
            _controller.Move(Vector3.down * 9.81f * Time.deltaTime);
    }
}