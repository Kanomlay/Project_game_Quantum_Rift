using UnityEngine;

public enum WeaponRarity
{
    Starter,
    Common,
    Rare,
    Legendary
}

public enum WeaponType
{
    Sword,  // ฟันด้วยการหมุน sprite
    Gun,    // ยิงกระสุน (ยังไม่ได้ทำ)
    Bow     // ยิงลูกธนู (ยังไม่ได้ทำ)
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน (Basic Info)")]
    public string weaponName;
    public Sprite weaponIcon; 
    public GameObject weaponPrefab;
    public WeaponType weaponType;
    public WeaponRarity rarity;

    [Header("ค่าสเตตัส (Stats)")]
    public float attackDamage;
    public float attackSpeed;
    public float attackRange = 1.5f;
    public float attackAngle = 90f;
    public int energyCost;

    [Header("เอฟเฟกต์ตอนโจมตี")]
    public GameObject slashEffectPrefab; // คลื่นดาบที่เสกตอนฟัน (เว้นว่างไว้ได้ถ้าอาวุธนี้ไม่ต้องการ)

    [Header("ความสามารถพิเศษ (Special Ability)")]
    [TextArea] public string abilityDescription; 
    public GameObject specialEffectPrefab;
}