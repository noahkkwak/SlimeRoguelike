using UnityEngine;

// [통합] 네임스페이스 제거 (접근성 문제 해결)

// 턴 상태
public enum TurnState
{
    PlayerTurn,     // 플레이어 입력 대기
    PlayerAct,      // 플레이어 행동 실행 중
    EnvironmentAct, // 전장(타일) 이동
    EnemyTurn,      // 적 행동 실행 중
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

// 적 상태
public enum EnemyState
{
    Idle, Ready, Move, Attack, Stunned, Dead
}

// 아이템 타입
public enum ItemType
{
    Weapon, Armor, Consumable
}

// 영역 타입
public enum ZoneType
{
    Damage, Heal, Slow
}

// [복구 완료] 적 이동 패턴 (EnemyData에서 사용)
public enum MovePattern
{
    Follow,     // 플레이어 추적
    Random,     // 무작위 이동
    Stationary  // 고정형 (이동 안 함)
}