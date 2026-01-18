// 행동의 종류 (의도)
public enum IntentType
{
    None,
    Wait,       // 대기 (쿨타임 중)
    Move,       // 이동 예고
    Attack,     // 공격 예고
    Skill       // 스킬 예고 (확장용)
}

// 적의 현재 상태 (FSM)
public enum EnemyState
{
    Idle,       // 대기 중
    Ready,      // 행동 결정됨 (Intent 보유)
    Stunned,    // 기절
    Dead        // 사망
}

// 기존 Enum 유지
public enum PlayerAction { None, Attack, Defend }
public enum AttackType { Direct, Arcing }
public enum MovePattern { MaintainDist, SimpleChase }
public enum StatusType { None, Stun }