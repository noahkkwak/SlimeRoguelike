// 턴의 진행 상태
public enum TurnState
{
    PlayerTurn, // 플레이어 행동 단계
    EnemyTurn,  // 적 행동 실행 단계
    GameOver    // 게임 종료
}

// 적이 하려는 행동의 종류 (의도)
public enum IntentType
{
    None,
    Wait,       // 대기 (쿨타임 중 or 막힘)
    Move,       // 이동
    Attack      // 공격
}

// 적의 현재 상태 (FSM)
public enum EnemyState
{
    Idle,       // 기본 대기
    Ready,      // 행동 준비 완료
    Stunned,    // 기절
    Dead        // 사망
}

// 플레이어의 행동
public enum PlayerAction
{
    None,
    Attack,
    Defend
}

// 적의 공격 타입
public enum AttackType
{
    Direct, // 직사
    Arcing  // 곡사
}

// 상태 이상 종류
public enum StatusType
{
    None,
    Stun
}