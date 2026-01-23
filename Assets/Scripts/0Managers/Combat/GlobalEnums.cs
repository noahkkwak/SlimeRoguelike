using UnityEngine;

// namespace SlimeRoguelike { } <-- 이 부분을 제거해서 장벽을 없앰

// 턴의 진행 상태
public enum TurnState
{
    PlayerInput,    // 플레이어 입력 대기
    PlayerAct,      // 플레이어 행동 실행 중
    EnvironmentAct, // [신규] 전장(타일) 이동
    EnemyAct,       // 적 행동 실행 중
    Processing      // 기타 연산 중
}

// 유닛 상태
public enum UnitState
{
    Idle, Move, Aiming, Attack, Guard, Block, Charging, Hit, Stunned, Die, Ready
}

// 타일 종류
public enum TileType
{
    Ground, Obstacle, Zone, Empty
}

// 공격 방식
public enum AttackType
{
    Direct, Arcing
}

// 적의 의도 (EnemyBase)
public enum IntentType
{
    None, Move, Attack, Wait, Buff, Debuff
}

// 상태이상 타입
public enum StatusType
{
    Stun, Poison, Burn
}

// [누락되었던 타입들 추가]

// 플레이어 행동
public enum PlayerAction
{
    None, Attack, Defend
}

// 적 이동 패턴 (EnemyData)
public enum MovePattern
{
    Follow, Random, Stationary
}

// 아이템 타입 (ItemBase)
public enum ItemType
{
    Weapon, Armor, Consumable
}

// 장애물 타입 (ObstacleBase)
public enum ObstacleType
{
    Destructible, Indestructible, Volatile
}

// 영역 타입 (ZoneBase)
public enum ZoneType
{
    Damage, Heal, Slow
}

// 충돌 효과 (ObstacleBase)
public enum CollisionEffect
{
    None, Damage, Stun, Push
}

// 적 상태 (EnemyState) - UnitState와 중복될 수 있으나 기존 코드 호환을 위해 추가
public enum EnemyState
{
    Idle, Ready, Move, Attack, Stunned, Dead
}