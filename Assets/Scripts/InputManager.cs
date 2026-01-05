using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions _inputActions;

    // 외부에서 참조할 입력 값들
    public Vector2 MoveInput { get; private set; }
    public event Action OnAttackStarted;   // Space 누른 순간 (대쉬/차지 시작)
    public event Action OnAttackCanceled;  // Space 뗀 순간 (삼키기 시전)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
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

        // 이동 입력 처리
        _inputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // 공격/스킬 입력 처리 (Tap/Hold 구분을 위해 Started와 Canceled 활용)
        _inputActions.Player.Attack.started += ctx => OnAttackStarted?.Invoke();
        _inputActions.Player.Attack.canceled += ctx => OnAttackCanceled?.Invoke();
    }

    private void OnDisable() => _inputActions.Player.Disable();
}