using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "GameData/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
    [Header("Stats")]
    public int maxHp;
    public int attackPower;
    public int attackRange = 5;
    public int collisionDamage = 1;
    [Header("Behavior")]
    public AttackType attackType;
    public MovePattern movePattern;
    public int moveCycle = 2;   // 이동 주기
    public int attackCycle = 2; // 공격 주기
}