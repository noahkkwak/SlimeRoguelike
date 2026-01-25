using UnityEngine;

// namespace 제거함 (접근성 문제 해결)

// 턴 상태 (기존 코드와 호환 + 환경 턴 추가)
public enum TurnState
{
    PlayerTurn,     // 기존 코드 호환 (PlayerInput 대체)
    PlayerAct,      // 플레이어 행동 중
    EnvironmentAct, // [신규] 전장 이동
    EnemyTurn,      // 기존 코드 호환 (EnemyAct 대체)
    Processing      // 연산 대기
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

// 적의 의도
public enum IntentType
{
    None, Move, Attack, Wait, Buff, Debuff
}

// 상태이상
public enum StatusType
{
    Stun, Poison, Burn
}

// 플레이어 행동
public enum PlayerAction
{
    None, Attack, Defend
}

// 장애물 타입
public enum ObstacleType
{
    Destructible, Indestructible, Volatile
}

// 충돌 효과
public enum CollisionEffect
{
    None, Damage, Stun, Push
}

// 적 상태 (EnemyBase 호환용)
public enum EnemyState
{
    Idle, Ready, Move, Attack, Stunned, Dead
}

// 아이템 타입 (ItemBase 호환용)
public enum ItemType
{
    Weapon, Armor, Consumable
}

// 영역 타입 (ZoneBase 호환용)
public enum ZoneType
{
    Damage, Heal, Slow
}