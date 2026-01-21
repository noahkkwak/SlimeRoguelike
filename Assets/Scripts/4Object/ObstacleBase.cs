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
    public float destroyDelayAfterCrash = 0.1f;

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

        bool collided = false; // 벽/적/플레이어와 충돌해서 터져야 하는가?
        bool momentumTransferred = false; // 다른 장애물에게 운동 에너지를 넘겨줬는가?

        EnemyBase hitEnemy = null;
        PlayerController hitPlayer = null; // [추가] 플레이어 피격
        ObstacleBase hitObstacle = null;
        Vector3 targetVisualPos = transform.position;

        // 플레이어 참조 가져오기
        var player = FindObjectOfType<PlayerController>();

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

            // B. 플레이어 충돌 [신규 구현]
            if (player != null && player.currentPos == nextPos)
            {
                hitPlayer = player;
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                collided = true; // 플레이어랑 박으면 터짐
                break;
            }

            // C. 적 충돌
            if (tile.HasUnit)
            {
                hitEnemy = tile.OccupyingUnit;
                targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                collided = true; // 적이랑 박으면 터짐
                break;
            }

            // D. 장애물 충돌 [연쇄 밀치기 구현]
            if (tile.HasObstacle)
            {
                ObstacleBase targetObs = tile.Obstacle;

                if (targetObs.isPushable)
                {
                    // [연쇄 작용] 상대방도 밀리는 놈이라면?
                    // 나는 여기서 멈추고(폭발 X), 상대를 민다. (당구공 효과)
                    targetVisualPos = GridManager.Instance.GetWorldPosition(checkPos, GridManager.Instance.unitHeight); // 충돌 직전 위치(checkPos)에 멈춤

                    // 상대방에게 운동 에너지 전달 (재귀적 호출과 유사 효과)
                    Debug.Log($"<color=cyan>[Chain Push]</color> {objName} -> {targetObs.objName}");
                    targetObs.OnHit(0, direction);

                    // 나는 터지지 않음. 그냥 자리를 잡음.
                    collided = false;
                    momentumTransferred = true;

                    // 내 위치 갱신 (checkPos에 안착)
                    currentPos = checkPos;
                    // 주의: 이동 루프는 여기서 끝내야 함
                    break;
                }
                else
                {
                    // 벽(파괴 불가 등)이면 그냥 들이박고 터짐
                    hitObstacle = targetObs;
                    targetVisualPos = GridManager.Instance.GetWorldPosition(nextPos, GridManager.Instance.unitHeight);
                    collided = true;
                    break;
                }
            }

            // E. 빈 공간 -> 계속 이동
            checkPos = nextPos;
        }

        StartCoroutine(ProcessSlideVisuals(targetVisualPos, collided, momentumTransferred, hitEnemy, hitPlayer, hitObstacle));
    }

    IEnumerator ProcessSlideVisuals(Vector3 targetPos, bool isCrash, bool momentumTransferred, EnemyBase hitEnemy, PlayerController hitPlayer, ObstacleBase hitObstacle)
    {
        // 이동 연출
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos; // 위치 보정

        // 1. 충돌 폭발 (적/플레이어/벽)
        if (isCrash)
        {
            if (hitEnemy != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> Enemy({hitEnemy.data.enemyName})");
                ApplyCollisionEffect(hitEnemy);
            }
            if (hitPlayer != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> Player");
                ApplyCollisionEffect(hitPlayer);
            }
            if (hitObstacle != null)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> Obstacle({hitObstacle.objName})");
                ApplyCollisionEffect(hitObstacle);
            }

            yield return new WaitForSeconds(destroyDelayAfterCrash);
            Destroy(gameObject); // 자폭
        }
        // 2. 연쇄 밀치기 (당구공)
        else if (momentumTransferred)
        {
            // 나는 터지지 않고 이 자리에 멈춤
            // 그리드에 다시 나를 등록해야 함 (아까 Remove했으므로)
            GridManager.Instance.RegisterObstacle(currentPos, this);
        }
        // 3. 맵 밖으로 나감
        else
        {
            // 그리드 등록 안 함 (이미 Remove됨)
            Destroy(gameObject);
        }
    }

    // --- 효과 적용 오버로딩 ---

    void ApplyCollisionEffect(EnemyBase target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both) target.TakeDamage(collisionDamage);
        if (collisionEffect == CollisionEffect.StunOnly || collisionEffect == CollisionEffect.Both) target.ApplyCollision(0);
    }

    // [신규] 플레이어에게 효과 적용
    void ApplyCollisionEffect(PlayerController target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both)
            target.TakeDamage(collisionDamage);

        // 플레이어 기절 로직은 아직 없으므로 데미지만 처리하거나, 추후 추가
        // if (collisionEffect == Stun...) target.Stun();
    }

    void ApplyCollisionEffect(ObstacleBase target)
    {
        // 벽 같은 것에 박았을 때
        target.TakeDamage(collisionDamage);
    }
}