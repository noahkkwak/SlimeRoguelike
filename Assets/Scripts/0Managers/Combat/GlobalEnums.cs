using UnityEngine;

namespace SlimeRoguelike
{
    // 턴의 진행 상태
    public enum TurnState
    {
        PlayerInput,    // 플레이어 입력 대기
        PlayerAct,      // 플레이어 행동 실행 중
        EnvironmentAct, // [신규] 전장(타일) 이동 및 환경 기믹 처리
        EnemyAct,       // 적 행동 실행 중
        Processing      // 기타 연산 중
    }

    // 유닛 상태
    public enum UnitState
    {
        Idle, Move, Aiming, Attack, Guard, Block, Charging, Hit, Stunned, Die
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

    // 의도(Intent) 타입 (EnemyBase에서 사용)
    public enum IntentType
    {
        None, Move, Attack, Wait, Buff, Debuff
    }

    // 상태이상 타입
    public enum StatusType
    {
        Stun, Poison, Burn
    }
}