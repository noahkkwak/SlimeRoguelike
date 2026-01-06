using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions _inputActions;
    public Vector2 MoveInput { get; private set; }

    // [수정/추가] 이벤트 정의
    public event Action OnAttackStarted;         // Space를 누른 순간 (차징/인디케이터 시작)
    public event Action<float> OnAttackReleased; // Space를 뗀 순간 (누른 시간을 float로 전달)

    private float _pressStartTime; // [추가] 누르기 시작한 시간 기록용

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();

        // 이동 입력 처리 (기존과 동일)
        _inputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // [수정] 공격 입력 처리 로직
        // 1. 키를 누르기 시작했을 때
        _inputActions.Player.Attack.started += ctx =>
        {
            _pressStartTime = Time.time; // 현재 시간 기록
            OnAttackStarted?.Invoke();   // "눌렀다!"라고 알림 (인디케이터용)
        };

        // 2. 키에서 손을 뗐을 때
        _inputActions.Player.Attack.canceled += ctx =>
        {
            float holdDuration = Time.time - _pressStartTime; // 뗀 시간 - 누른 시간 = 경과 시간
            OnAttackReleased?.Invoke(holdDuration);          // 계산된 시간을 전달하며 알림
        };
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }
}