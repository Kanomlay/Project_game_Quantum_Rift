using UnityEngine;

public enum WeaponRarity
{
    Starter,    
    Common,     
    Rare,       
    Legendary  
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน (Basic Info)")]
    public string weaponName;
    public Sprite weaponIcon; 
    public GameObject weaponPrefab;
    public WeaponRarity rarity;

    [Header("ค่าสเตตัส (Stats)")]
    public float attackDamage;
    public float attackSpeed;
    public float attackRange = 1.5f;
    public float attackAngle = 90f;
    public int energyCost;

    [Header("ความสามารถพิเศษ (Special Ability)")]
    [TextArea] public string abilityDescription; 
    public GameObject specialEffectPrefab;
}