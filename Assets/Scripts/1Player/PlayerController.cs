using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.2f;   // 이동 시간
    [SerializeField] private float gridSize = 1f;         // 그리드 크기 (보통 1)
    [SerializeField] private LayerMask obstacleLayer;     // 벽/장애물 레이어

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Status")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    // [중요] 다른 스크립트(EnemyBase, ObstacleBase)에서 플레이어 위치를 파악하기 위해 필수
    public Vector2Int currentPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

    // 상태 관리 플래그
    private bool isBusy = false;          // 이동/공격 중에는 입력 차단
    private Vector3? bufferedMoveInput = null; // 선입력 저장 (턴제 반응성 향상)

    private void Awake()
    {
        // 1. 컴포넌트 자동 할당 (자식 오브젝트까지 꼼꼼하게 탐색)
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 행동 중(이동/공격)일 때는 입력 무시
        if (isBusy) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. 이동 입력 처리 (상하좌우)
        if (h != 0) bufferedMoveInput = new Vector3(h, 0, 0);
        else if (v != 0) bufferedMoveInput = new Vector3(0, v, 0);
        else bufferedMoveInput = null;

        if (bufferedMoveInput.HasValue)
        {
            AttemptMove(bufferedMoveInput.Value);
            bufferedMoveInput = null;
        }

        // 2. 공격 입력 처리 (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // =========================================================
    // [외부 연동 메서드] TurnManager, EnemyBase 등에서 호출
    // =========================================================

    // 턴이 시작될 때 호출 (상태 초기화)
    public void OnTurnStart()
    {
        isBusy = false;
        bufferedMoveInput = null;
    }

    // 선입력된 행동이 있다면 실행 (TurnManager 정책에 따라 사용)
    public void ResolveBufferedAction()
    {
        if (bufferedMoveInput.HasValue)
        {
            AttemptMove(bufferedMoveInput.Value);
            bufferedMoveInput = null;
        }
    }

    // 적이나 장애물에 의해 피격될 때 호출
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Player] 피격! 남은 체력: {currentHealth}");

        if (animator) animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // =========================================================
    // [핵심 행동 로직] 이동 및 공격
    // =========================================================

    private void AttemptMove(Vector3 direction)
    {
        // 1. 방향 전환 (이동하려는 쪽 바라보기)
        UpdateFacing(direction.x);

        // 2. 목표 지점 계산
        Vector3 targetPos = transform.position + (direction * gridSize);

        // 3. 장애물 체크 후 이동 시작
        if (IsWalkable(targetPos))
        {
            StartCoroutine(MoveRoutine(targetPos));
        }
        else
        {
            // 이동 불가 시 피드백 (필요 시 추가)
            Debug.Log("[Player] 장애물에 막힘");
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        isBusy = true; // 중복 행동 방지 잠금

        if (animator) animator.SetTrigger("Move");

        float elapsedTime = 0f;
        Vector3 startPos = transform.position;

        // 부드러운 이동 (Lerp)
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // 위치값 정확하게 보정

        // [중요] 행동 종료 보고 -> 턴 넘김
        NotifyTurnEnd();
    }

    private IEnumerator AttackRoutine()
    {
        isBusy = true;

        if (animator) animator.SetTrigger("Attack");

        // 공격 애니메이션 시간만큼 대기 (0.4초는 예시, 실제 애니메이션 길이에 맞게 조절)
        yield return new WaitForSeconds(0.4f);

        // 여기서 실제 적 타격 로직 호출 가능 (예: AttackFrontEnemy();)

        // [중요] 행동 종료 보고 -> 턴 넘김
        NotifyTurnEnd();
    }

    // =========================================================
    // [유틸리티] 턴 종료 처리 및 보조 기능
    // =========================================================

    private void NotifyTurnEnd()
    {
        Debug.Log("[Player] 행동 종료. 턴을 넘깁니다.");

        // ▼▼▼ [필수 설정] 사용 중인 TurnManager 코드에 맞춰 주석을 해제하세요 ▼▼▼

        // 예시 1: 싱글톤 패턴을 사용하는 경우
        //TurnManager.Instance.EndPlayerTurn(); 

        // 예시 2: 이벤트를 사용하는 경우
        TurnManager.Instance.OnPlayerActionFinished();

        // ▲▲▲ TurnManager 연결 전까지는 테스트를 위해 스스로 잠금 해제 ▲▲▲
        // TurnManager 연동 코드를 넣으셨다면 아래 줄(isBusy = false)은 삭제하셔도 됩니다.
        // (연동이 안 된 상태에서 게임이 멈추는 것을 방지하기 위한 임시 코드입니다)
    }

    private void UpdateFacing(float xDir)
    {
        if (spriteRenderer == null || xDir == 0) return;

        // 이동 방향에 따라 Flip (우측 이동: false, 좌측 이동: true)
        // 만약 원본 이미지가 왼쪽을 보고 있다면 반대로 설정 필요
        if (xDir > 0) spriteRenderer.flipX = false;
        else if (xDir < 0) spriteRenderer.flipX = true;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        // obstacleLayer에 해당하는 물체가 0.2반경 내에 있으면 이동 불가
        return !Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer);
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
        // 게임 오버 처리
    }
}