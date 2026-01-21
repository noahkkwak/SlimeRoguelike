using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.2f;  // 이동 시간
    [SerializeField] private float gridSize = 1f;        // 그리드 크기
    [SerializeField] private LayerMask obstacleLayer;    // 장애물 레이어

    [Header("Sprite Settings")]
    [Tooltip("체크 해제: 원본이 오른쪽 봄 / 체크: 원본이 왼쪽 봄")]
    [SerializeField] private bool spriteOriginalFaceLeft = false;

    [Header("Status")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    // 외부 참조용 (EnemyBase, TurnManager 등에서 사용)
    public Vector2Int currentPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

    private bool isMoving = false;
    private Vector3? bufferedMoveInput = null;

    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;

        if (spriteRenderer == null) Debug.LogError("PlayerController: SpriteRenderer가 없습니다.");
    }

    private void Start()
    {
        // [초기 시선 처리] 게임 시작 시 화면 중앙(적 방향)을 바라보게 설정
        float initialDir = transform.position.x < 0 ? 1f : -1f;
        UpdateFacing(initialDir);
    }

    private void Update()
    {
        if (isMoving) return;
        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 대각선 이동 방지
        if (h != 0) bufferedMoveInput = new Vector3(h, 0, 0);
        else if (v != 0) bufferedMoveInput = new Vector3(0, v, 0);
        else bufferedMoveInput = null;

        // 즉시 반응 (TurnManager가 구현되면 ResolveBufferedAction으로 제어권 이양 가능)
        if (bufferedMoveInput.HasValue)
        {
            AttemptMove(bufferedMoveInput.Value);
            bufferedMoveInput = null;
        }
    }

    // --- 외부 호출 메서드 (오류 방지 및 로직 연동) ---

    public void OnTurnStart()
    {
        isMoving = false;
    }

    public void ResolveBufferedAction()
    {
        if (bufferedMoveInput.HasValue)
        {
            AttemptMove(bufferedMoveInput.Value);
            bufferedMoveInput = null;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"플레이어 피격! 남은 체력: {currentHealth}");

        // 피격도 Trigger로 가정 (만약 Hit 트리거가 없다면 이 줄은 주석 처리)
        if (animator) animator.SetTrigger("Hit");

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
        // 게임 오버 처리 로직
    }

    // --- 이동 핵심 로직 ---

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
        isMoving = true;

        // [수정 완료] IsMoving(Bool) 대신 Move(Trigger) 사용
        if (animator != null)
        {
            animator.SetTrigger("Move");
        }

        float elapsedTime = 0f;
        Vector3 startPos = transform.position;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // 위치 정확하게 보정
        isMoving = false;
    }

    // 방향 전환 (스프라이트 반전)
    private void UpdateFacing(float xDir)
    {
        if (spriteRenderer == null || xDir == 0) return;

        bool lookRight = xDir > 0;

        // 원본 이미지 방향에 따라 flipX 설정
        if (spriteOriginalFaceLeft)
        {
            spriteRenderer.flipX = lookRight;
        }
        else
        {
            spriteRenderer.flipX = !lookRight;
        }
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        // 장애물 레이어 감지
        return !Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer);
    }
}