using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float gridSize = 1f;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Status")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    // 외부 참조용 좌표 (반올림하여 정확한 정수 좌표 반환)
    public Vector2Int currentPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

    private bool isBusy = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 시작 시 적 방향(화면 안쪽 혹은 설정된 방향) 바라보기
        UpdateFacing(1);
    }

    private void Update()
    {
        // 턴 매니저가 '플레이어 턴' 상태일 때만 입력 허용
        if (isBusy || TurnManager.Instance.currentState != TurnState.PlayerTurn) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = Vector3.zero;

        // [버그 수정] W/S 입력을 Y축(높이)이 아닌 Z축(깊이)으로 변경
        if (h != 0) moveDir = new Vector3(h, 0, 0);
        else if (v != 0) moveDir = new Vector3(0, 0, v);

        if (moveDir != Vector3.zero)
        {
            // 이동하려는 좌표 계산
            Vector2Int targetGridPos = currentPos + new Vector2Int((int)moveDir.x, (int)moveDir.z);

            // [LayerMask 제거] GridManager에게 해당 타일이 비어있는지 물어봄
            // 주의: GridManager에 IsWalkable 함수가 없다면 아래 3번 항목의 GridManager 수정 코드를 참고하세요.
            TileNode targetTile = GridManager.Instance.GetTile(targetGridPos);

            // 타일이 존재하고, 장애물이 없고, 유닛이 없으면 이동 가능
            bool canMove = (targetTile != null && !targetTile.HasObstacle && !targetTile.HasUnit);

            if (canMove)
            {
                // 턴 매니저에게 "이동 행동" 요청
                TurnManager.Instance.ProcessTurn(PlayerActionType.Move, moveDir);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            // 턴 매니저에게 "공격 행동" 요청
            TurnManager.Instance.ProcessTurn(PlayerActionType.Attack, Vector3.zero);
        }
    }

    // --- TurnManager가 호출하는 실제 행동 함수들 ---

    public IEnumerator ExecuteMove(Vector3 direction)
    {
        isBusy = true;
        UpdateFacing(direction.x);

        if (animator) animator.SetTrigger("Move");

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (direction * gridSize);

        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // 위치 보정
        isBusy = false;
    }

    public IEnumerator ExecuteAttack()
    {
        isBusy = true;
        if (animator) animator.SetTrigger("Attack");

        // 공격 애니메이션 타이밍 대기
        yield return new WaitForSeconds(0.4f);

        // TODO: 공격 범위 내의 적 타격 로직 추가

        isBusy = false;
    }

    private void UpdateFacing(float xDir)
    {
        if (spriteRenderer == null || xDir == 0) return;
        // xDir > 0 (오른쪽) -> flipX = false
        // xDir < 0 (왼쪽) -> flipX = true
        spriteRenderer.flipX = (xDir < 0);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (animator) animator.SetTrigger("Hit");
        if (currentHealth <= 0) Debug.Log("Player Died");
    }
}