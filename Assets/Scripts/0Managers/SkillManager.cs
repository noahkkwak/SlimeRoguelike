using UnityEngine;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Swallow Settings")]
    [SerializeField] private float swallowRadius = 4f;
    [SerializeField] private float holdThreshold = 0.5f;

    private PlayerController _player;

    private void Awake() => Instance = this;

    private void Start()
    {
        // [MODIFY] 플레이어를 찾는 방식을 더 안전하게 변경
        _player = FindFirstObjectByType<PlayerController>();

        if (_player == null)
        {
            Debug.LogError("SkillManager: 씬에서 PlayerController를 찾을 수 없습니다!");
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackReleased += HandleAttack;
        }
    }

    private void HandleAttack(float duration)
    {
        // 플레이어가 없으면 스킬 실행 불가
        if (_player == null) return;

        if (duration < holdThreshold)
            StartCoroutine(PerformDash());
        else
            PerformSwallow();

        if (IndicatorManager.Instance != null)
            IndicatorManager.Instance.HideAll();
    }

    private IEnumerator PerformDash()
    {
        Debug.Log("대쉬 시전!");

        // [ADD] 다시 한번 체크 (안전장치)
        if (_player == null || _player.CharController == null)
        {
            Debug.LogError("Player 또는 CharacterController가 없습니다!");
            yield break;
        }

        Vector3 dashDir = _player.transform.forward;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            // [FIX] _player.CharController를 통해 확실히 접근
            _player.CharController.Move(dashDir * (dashDistance / dashDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PerformSwallow()
    {
        Debug.Log("삼키기 시전!");
        Collider[] hitEnemies = Physics.OverlapSphere(_player.transform.position, swallowRadius);

        bool foundEnemy = false;
        foreach (var enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Debug.Log($"{enemy.name}을(를) 삼켰습니다!");
                foundEnemy = true;
            }
        }

        if (!foundEnemy) Debug.Log("삼킬 수 있는 적이 범위 내에 없습니다.");
    }

    public float GetSwallowRadius() => swallowRadius;
    public float GetHoldThreshold() => holdThreshold;
}