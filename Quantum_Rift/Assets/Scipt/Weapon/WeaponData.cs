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
    Gun,    // ยิงกระสุนจาก AttackPoint (ปากกระบอก)
    Bow,    // ง้างแล้วปล่อยลูกธนูจาก AttackPoint
    Claw    // กรงเล็บ ฟันระยะประชิดด้วยท่าเดียวกับดาบ (ต้องต่อท้ายเสมอ ค่าเดิมใน asset เก็บเป็นตัวเลข)
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

    [Header("อาวุธยิง (ปืน / ธนู)")]
    public GameObject projectilePrefab;   // กระสุนหรือลูกธนูที่ยิงออกไป ต้องมี PlayerProjectile
    public float projectileSpeed = 14f;   // หน่วยต่อวินาที
    public float projectileLifetime = 1.5f; // ระยะยิงไกลสุด = ความเร็ว × เวลานี้

    public bool IsRanged => weaponType == WeaponType.Gun || weaponType == WeaponType.Bow;

    [Header("ความสามารถพิเศษ (Special Ability)")]
    [TextArea] public string abilityDescription; 
    public GameObject specialEffectPrefab;
}