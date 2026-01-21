using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 필요

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
    public float slideSpeed = 15f; // 밀리는 속도 (빠를수록 타격감 좋음)
    public float destroyDelayAfterCrash = 0.1f; // 충돌 후 잠시 멈췄다 사라짐

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
            // 밀리는 녀석은 데미지 대신 이동 처리
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

    // [핵심 수정] 논리 계산과 시각적 연출의 분리
    void SlideAndCollide(Vector2Int direction)
    {
        // 1. 논리적 처리: 일단 그리드에서 나는 사라짐 (내 자리는 비워짐)
        GridManager.Instance.RemoveObstacle(currentPos);

        Vector2Int checkPos = currentPos;
        bool collided = false;

        // 충돌 대상 정보
        EnemyBase hitEnemy = null;
        ObstacleBase hitObstacle = null;
        Vector3 targetVisualPos = transform.position;

        // 2. 미래 예측: 어디까지 미끄러질지 미리 계산
        while (true)
        {
            Vector2Int nextPos = checkPos + direction;

            // A. 맵 밖으로 나감
            if (!GridManager.Instance.IsInsideGrid(nextPos))
            {
                // [기획 반영] 맵 밖으로 나가도 시각적으로는 멀리까지 날아가야 함
                // 방향대로 10칸 정도 더 멀리 날려보냄
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos + (direction * 5), GridManager.Instance.unitHeight);
                collided = false;
                break;
            }

            var tile = GridManager.Instance.GetTile(nextPos);

            // B. 장애물 충돌
            if (tile.HasObstacle)
            {
                hitObstacle = tile.Obstacle;
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                // 겹치지 않게 살짝 전(0.8칸)에서 멈추는 디테일? or 그냥 들이박기? -> 일단 들이박기(해당 위치)
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

            // D. 빈 공간 -> 계속 이동
            checkPos = nextPos;
        }

        // 3. 시각적 연출 시작 (코루틴)
        StartCoroutine(ProcessSlideVisuals(targetVisualPos, collided, hitEnemy, hitObstacle));
    }

    IEnumerator ProcessSlideVisuals(Vector3 targetPos, bool isCrash, EnemyBase hitEnemy, ObstacleBase hitObstacle)
    {
        // A. 목표 지점까지 고속 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }

        // B. 상황별 처리
        if (isCrash)
        {
            // 충돌 시점에 대미지/효과 적용 (타격감 싱크 맞추기)
            if (hitEnemy != null)
            {
                Debug.Log($"<color=red>[쾅!]</color> {objName} -> {hitEnemy.data.enemyName}");
                ApplyCollisionEffect(hitEnemy);
            }
            if (hitObstacle != null)
            {
                Debug.Log($"<color=red>[쾅!]</color> {objName} -> {hitObstacle.objName}");
                ApplyCollisionEffect(hitObstacle);
            }

            // 잠시 멈췄다가 소멸 (충격 연출)
            yield return new WaitForSeconds(destroyDelayAfterCrash);
            Destroy(gameObject);
        }
        else
        {
            // 맵 밖으로 나가는 경우: 이미 충분히 멀리 날아갔으므로 자연스럽게 소멸
            // (위 while문에서 targetPos를 아주 멀리 잡았으므로 여기까지 오면 화면 밖임)
            Debug.Log($"<color=cyan>[Out]</color> {objName} 장외 홈런");
            Destroy(gameObject);
        }
    }

    void ApplyCollisionEffect(EnemyBase target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both)
            target.TakeDamage(collisionDamage);

        if (collisionEffect == CollisionEffect.StunOnly || collisionEffect == CollisionEffect.Both)
            target.ApplyCollision(0);
    }

    void ApplyCollisionEffect(ObstacleBase target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both)
            target.TakeDamage(collisionDamage);
    }
}