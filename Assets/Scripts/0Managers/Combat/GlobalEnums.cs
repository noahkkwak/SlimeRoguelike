// Assets/Scripts/0Managers/Combat/GlobalEnums.cs

public enum TurnState { PlayerTurn, EnemyTurn, GameOver }
public enum IntentType { None, Wait, Move, Attack }
public enum EnemyState { Idle, Ready, Stunned, Dead }
public enum PlayerAction { None, Attack, Defend }
public enum AttackType { Direct, Arcing }
public enum StatusType { None, Stun }
public enum MovePattern { Chase, MaintainDist, Flee }

// [신규] 장애물 유형
public enum ObstacleType
{
    Indestructible, // 파괴 불가 (벽)
    Destructible,   // 파괴 가능 (상자)
    Explosive       // 폭발성 (밀치면 터짐)
}

// [신규] 영역(Zone) 유형
public enum ZoneType
{
    None,
    Fire,   // 턴 시작 시 대미지
    Poison, // 중독 상태 부여
    Ice     // 이동 불가 등
}

// [신규] 아이템 유형
public enum ItemType
{
    None,
    Consumable, // 체력 회복 등
    Currency,   // 골드
    SkillItem   // 스킬 해금
}