public enum TurnState
{
    PlayerTurn, EnemyTurn, GameOver
}

public enum IntentType
{
    None, Wait, Move, Attack
}

public enum EnemyState
{
    Idle, Ready, Stunned, Dead
}

public enum PlayerAction
{
    None, Attack, Defend
}

public enum AttackType
{
    Direct, Arcing
}

public enum StatusType
{
    None, Stun
}

public enum MovePattern
{
    Chase,          // 기본 추적
    MaintainDist,   // 거리 유지
    Flee            // 도망
}