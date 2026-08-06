using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "Game Data/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน")]
    public string monsterName;
    public GameObject monsterPrefab; 

    [Header("สเตตัสการต่อสู้")]
    public float maxHealth = 22f;
    public float attackDamage = 0.5f;
    public float moveSpeed = 1.5f;
    
    [Header("ระบบระยะและเวลาหน่วง")]
    public float attackRange = 1.2f;      
    public float attackCooldown = 1.5f;   
}