using UnityEngine;
using System.Collections;

public class ObstacleBase : MonoBehaviour
{
    [Header("Basic Info")]
    public string objName;
    public ObstacleType type;
    public int maxHp = 1;
    public int currentHp;
    public Vector2Int currentPos;
    public bool IsWalkable = false;

    [Header("Push & Collision Settings")]
    public bool isPushable = false;
    public int collisionDamage = 1;
    public CollisionEffect collisionEffect = CollisionEffect.DamageOnly;

    [Header("Visual Settings")]
    public float slideSpeed = 15f;
    public float destroyDelayAfterCrash = 0.05f; // 충돌 후 즉시 삭제에 가깝게

    public void Initialize(Vector2Int pos)
    {
        currentPos = pos;
        currentHp = maxHp;
        transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);
        GridManager.Instance.RegisterObstacle(pos, this);
    }

    public void OnHit(int damage, Vector2Int attackDirection)
    {
        if (type == ObstacleType.Indestructible) return;

        if (isPushable)
        {
            // 밀리는 장애물은 대미지 0이어도 물리력(방향)만 있으면 밀림
            SlideAndCollide(attackDirection);
        }
        else
        {
            TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0) DestroyObstacle();
    }

    public virtual void DestroyObstacle()
    {
        GridManager.Instance.RemoveObstacle(currentPos);
        Destroy(gameObject);
    }

    void SlideAndCollide(Vector2Int direction)
    {
        // 1. 그리드에서 나를 지움 (이동 시작)
        GridManager.Instance.RemoveObstacle(currentPos);

        Vector2Int checkPos = currentPos;
        bool collided = false;

        // 충돌 대상들
        EnemyBase hitEnemy = null;
        PlayerController hitPlayer = null;
        ObstacleBase hitObstacle = null;
        ObstacleBase pushTarget = null; // [신규] 내가 밀어버릴 대상

        Vector3 targetVisualPos = transform.position;

        // 2. 경로 탐색
        while (true)
        {
            Vector2Int nextPos = checkPos + direction;

            // A. 맵 밖으로 나감
            if (!GridManager.Instance.IsInsideGrid(nextPos))
            {
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos + (direction * 5), GridManager.Instance.unitHeight);
                collided = false;
                break;
            }

            var tile = GridManager.Instance.GetTile(nextPos);

            // B. 플레이어 충돌
            if (FindObjectOfType<PlayerController>() != null && FindObjectOfType<PlayerController>().currentPos == nextPos)
            {
                hitPlayer = FindObjectOfType<PlayerController>();
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                collided = true;
                break;
            }

            // C. 적 충돌
            if (tile.HasUnit)
            {
                hitEnemy = tile.OccupyingUnit;
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                collided = true;
                break;
            }

            // D. 장애물 충돌 (벽이든 박스든 일단 부딪힘)
            if (tile.HasObstacle)
            {
                hitObstacle = tile.Obstacle;

                // [수정] 충돌 위치를 상대방 위치(nextPos)로 설정하여 '들이받는' 느낌 구현
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                collided = true;

                if (hitObstacle.isPushable)
                {
                    // 상대를 밀어야 함을 기록
                    pushTarget = hitObstacle;
                }

                // 여기서 멈추고 터짐 (A도 피해를 입고 소멸해야 하므로 Loop 종료)
                break;
            }

            // E. 빈 공간 -> 계속 이동
            checkPos = nextPos;
        }

        StartCoroutine(ProcessSlideVisuals(targetVisualPos, collided, hitEnemy, hitPlayer, hitObstacle, pushTarget, direction));
    }

    IEnumerator ProcessSlideVisuals(Vector3 targetPos, bool isCrash, EnemyBase hitEnemy, PlayerController hitPlayer, ObstacleBase hitObstacle, ObstacleBase pushTarget, Vector2Int pushDirection)
    {
        // 이동 연출
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos; // 도착

        // 1. 충돌 처리
        if (isCrash)
        {
            // [중요] 연쇄 밀치기 먼저 실행 (싱크 맞추기)
            // 내가 도착해서 쾅 박았으니, 상대방도 이제 밀려나야 함
            if (pushTarget != null)
            {
                Debug.Log($"<color=cyan>[Chain Push]</color> {objName} -> {pushTarget.objName}");
                pushTarget.OnHit(0, pushDirection); // 대미지 0, 물리력 전달
            }
            // 일반 장애물(벽)이면 대미지 주기
            else if (hitObstacle != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> {hitObstacle.objName}");
                ApplyCollisionEffect(hitObstacle);
            }

            // 유닛 충돌 처리
            if (hitEnemy != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> Enemy");
                ApplyCollisionEffect(hitEnemy);
            }
            if (hitPlayer != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> Player");
                ApplyCollisionEffect(hitPlayer);
            }

            // 나 자신도 파괴 (폭탄이므로 충돌 후 소멸)
            yield return new WaitForSeconds(destroyDelayAfterCrash);
            Destroy(gameObject);
        }
        // 2. 맵 밖으로 나감
        else
        {
            Destroy(gameObject);
        }
    }

    void ApplyCollisionEffect(EnemyBase target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both) target.TakeDamage(collisionDamage);
        if (collisionEffect == CollisionEffect.StunOnly || collisionEffect == CollisionEffect.Both) target.ApplyCollision(0);
    }

    void ApplyCollisionEffect(PlayerController target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both) target.TakeDamage(collisionDamage);
    }

    void ApplyCollisionEffect(ObstacleBase target)
    {
        target.TakeDamage(collisionDamage);
    }
}