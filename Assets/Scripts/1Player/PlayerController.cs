using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Status")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    // [ObstacleBase, EnemyBase 연동용] 현재 그리드 좌표 반환
    public Vector2Int currentPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

    // 상태 관리
    private bool isBusy = false;
    private Vector3? bufferedInput = null; // 이동/공격 선입력 저장

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 초기 시선 처리 (화면 중앙 방향)
        float initialDir = transform.position.x < 0 ? 1f : -1f;
        UpdateFacing(initialDir);
    }

    private void Update()
    {
        // 행동 중(이동/공격)이거나 턴 매니저가 플레이어 턴이 아니라고 판단하면 조작 불가
        // TurnManager.currentState 확인 추가
        if (isBusy || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. 이동 입력
        if (h != 0) bufferedInput = new Vector3(h, 0, 0);
        else if (v != 0) bufferedInput = new Vector3(0, v, 0);
        else if (Input.GetKeyDown(KeyCode.Space)) bufferedInput = Vector3.zero; // 공격 신호(Vector3.zero)로 임시 사용
        else bufferedInput = null;

        if (bufferedInput.HasValue)
        {
            if (bufferedInput.Value == Vector3.zero)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                AttemptMove(bufferedInput.Value);
            }
            bufferedInput = null;
        }
    }

    // =========================================================
    // [TurnManager 연동]
    // =========================================================

    public void OnTurnStart()
    {
        isBusy = false;
        bufferedInput = null;
    }

    // TurnManager의 ExecuteTurnPhase에서 호출됨 (예약된 액션이 있다면 실행)
    public void ResolveBufferedAction()
    {
        // 현재 로직상 즉시 반응하므로 비워두거나,
        // 추후 '턴 종료 후 예약된 공격 발동'을 구현할 때 사용
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Player] 피격! 남은 체력: {currentHealth}");

        if (animator) animator.SetTrigger("Hit");

        if (currentHealth <= 0) Die();
    }

    // =========================================================
    // [행동 로직]
    // =========================================================

    private void AttemptMove(Vector3 direction)
    {
        UpdateFacing(direction.x);

        Vector3 targetPos = transform.position + (direction * gridSize);

        if (IsWalkable(targetPos))
        {
            StartCoroutine(MoveRoutine(targetPos));
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        isBusy = true;
        if (animator) animator.SetTrigger("Move");

        float elapsedTime = 0f;
        Vector3 startPos = transform.position;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // 위치 보정

        // [핵심] 이동 완료 -> 턴 매니저에게 알림
        NotifyTurnEnd();
    }

    private IEnumerator AttackRoutine()
    {
        isBusy = true;
        if (animator) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.4f); // 애니메이션 시간 대기

        // [핵심] 공격 완료 -> 턴 매니저에게 알림
        NotifyTurnEnd();
    }

    private void NotifyTurnEnd()
    {
        Debug.Log("행동 종료. 턴을 넘깁니다.");

        // [수정 완료] 공유해주신 TurnManager 코드에 있는 함수 호출
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerActionCompleted();
        }
        else
        {
            Debug.LogError("TurnManager가 씬에 없습니다!");
            isBusy = false; // 비상 탈출
        }
    }

    // =========================================================
    // [보조 기능]
    // =========================================================

    private void UpdateFacing(float xDir)
    {
        if (spriteRenderer == null || xDir == 0) return;
        if (xDir > 0) spriteRenderer.flipX = false;
        else if (xDir < 0) spriteRenderer.flipX = true;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        // 당장은 LayerMask를 사용해 프로토타입 구동 확인
        return !Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer);
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
    }
}