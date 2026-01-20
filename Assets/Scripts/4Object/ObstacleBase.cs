using UnityEngine;

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
    public bool isPushable = false; // true면 피격 시 밀려남 (Explosive 등)
    public int collisionDamage = 1; // 충돌 시 상대에게 줄 피해량
    public CollisionEffect collisionEffect = CollisionEffect.DamageOnly; // 충돌 효과

    public void Initialize(Vector2Int pos)
    {
        currentPos = pos;
        currentHp = maxHp;
        transform.position = GridManager.Instance.GetWorldPosition(pos, GridManager.Instance.unitHeight);
        GridManager.Instance.RegisterObstacle(pos, this);
    }

    // [수정] 공격을 받을 때 방향 정보도 함께 받음
    public void OnHit(int damage, Vector2Int attackDirection)
    {
        if (type == ObstacleType.Indestructible) return;

        // 1. 밀리는 장애물인가? (체력 감소 대신 이동 처리)
        if (isPushable)
        {
            // 공격 방향과 동일한 방향(밀려나는 방향)으로 슬라이딩 시도
            SlideAndCollide(attackDirection);
        }
        else
        {
            // 2. 일반 장애물은 그냥 체력 감소
            TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        // 피격 연출 (흔들림 등) 추가 가능

        if (currentHp <= 0)
        {
            DestroyObstacle();
        }
    }

    public virtual void DestroyObstacle()
    {
        GridManager.Instance.RemoveObstacle(currentPos);
        Destroy(gameObject);
    }

    // [핵심] 슬라이딩 및 충돌 로직 ('Into the Breach' 스타일)
    void SlideAndCollide(Vector2Int direction)
    {
        // 1. 내 자리 비우기 (이동 시작)
        GridManager.Instance.RemoveObstacle(currentPos);

        Vector2Int checkPos = currentPos;
        bool collided = false;

        // 2. 루프를 돌며 미끄러짐
        while (true)
        {
            Vector2Int nextPos = checkPos + direction;

            // A. 맵 밖으로 나감 -> 소멸
            if (!GridManager.Instance.IsInsideGrid(nextPos))
            {
                Debug.Log($"<color=cyan>[Slide]</color> {objName} 맵 밖으로 낙하");
                Destroy(gameObject); // 자폭
                return;
            }

            var tile = GridManager.Instance.GetTile(nextPos);

            // B. 장애물 충돌
            if (tile.HasObstacle)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> {tile.Obstacle.objName} 충돌!");
                ApplyCollisionEffect(tile.Obstacle); // 상대에게 효과 부여
                collided = true;
                break;
            }

            // C. 적(Unit) 충돌
            if (tile.HasUnit)
            {
                Debug.Log($"<color=red>[Crash]</color> {objName} -> {tile.OccupyingUnit.data.enemyName} 충돌!");
                ApplyCollisionEffect(tile.OccupyingUnit); // 상대에게 효과 부여
                collided = true;
                break;
            }

            // D. 빈 공간 -> 계속 이동
            checkPos = nextPos;
        }

        // 충돌했으면 나는 파괴됨 (폭발성 장애물이므로)
        if (collided)
        {
            // TODO: 여기서 폭발 이펙트 생성
            Destroy(gameObject);
        }
        else
        {
            // (혹시 루프가 이상하게 끝나서 멈춘 경우 안전장치, 보통 A에서 처리됨)
            // 슬라이딩 끝지점에 안착하는 로직이 필요하다면 여기에 작성하지만, 
            // 현재 기획은 "끝까지 가서 사라지거나 부딪혀 터짐"이므로 불필요.
        }
    }

    // 충돌 대상이 '적'일 때
    void ApplyCollisionEffect(EnemyBase target)
    {
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both)
        {
            target.TakeDamage(collisionDamage);
        }

        if (collisionEffect == CollisionEffect.StunOnly || collisionEffect == CollisionEffect.Both)
        {
            target.ApplyCollision(0); // EnemyBase의 ApplyCollision은 기절을 포함함
        }
    }

    // 충돌 대상이 '다른 장애물'일 때
    void ApplyCollisionEffect(ObstacleBase target)
    {
        // 장애물끼리 부딪히면 상대방도 대미지 입음 (연쇄 파괴 가능성)
        if (collisionEffect == CollisionEffect.DamageOnly || collisionEffect == CollisionEffect.Both)
        {
            target.TakeDamage(collisionDamage);
        }
        // 장애물은 기절 개념이 없으므로 Stun은 무시
    }
}