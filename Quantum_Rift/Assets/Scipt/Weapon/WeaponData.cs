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
    Claw,   // กรงเล็บ ฟันระยะประชิดด้วยท่าเดียวกับดาบ (ต้องต่อท้ายเสมอ ค่าเดิมใน asset เก็บเป็นตัวเลข)
    Spear,  // หอก แทงพุ่งไปข้างหน้าตามแนวเล็ง
    Hammer  // ค้อน ตอนนี้ใช้ท่าฟันแบบดาบไปก่อน (ไม่มีคลื่นฟัน) รอแยกท่าทุบของตัวเอง
}

// ความสามารถพิเศษประจำอาวุธระดับตำนาน (ตาราง 1.5)
public enum WeaponSpecial
{
    None,
    ComboWave,   // ดาบผ่ามิติ: ฟันครั้งที่ comboCount ในคอมโบปล่อยคลื่นพลังพุ่งออกไป
    ChargeWave,  // หอกไอออน-X: กดค้าง chargeTime วินาทีแล้วปล่อย แทงพร้อมคลื่นพลัง
    Ricochet,    // ธนูยิงกระจาย: ยิงหลายลูกเป็นพัด ลูกธนูชิ่งกำแพงได้ bounces ครั้ง
    Explosive,   // เครื่องยิงจรวด: กระสุนระเบิดเป็นวง ทำดาเมจพื้นที่
    GroundPulse  // ค้อนควอนตัม: ทุบแล้วเกิดวงพลังที่พื้น ทำดาเมจพื้นที่ต่อเนื่อง specialDuration วินาที
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

    [Header("ท่าตีและความรู้สึกตอนโดน (เว้นว่าง = ค่าตั้งต้นตามชนิดอาวุธ)")]
    public AttackMotion motion;

    public AttackMotion Motion => motion != null ? motion : AttackMotion.Default(weaponType);

    [Header("เอฟเฟกต์ตอนโจมตี")]
    public GameObject slashEffectPrefab; // คลื่นดาบที่เสกตอนฟัน (เว้นว่างไว้ได้ถ้าอาวุธนี้ไม่ต้องการ)

    [Header("อาวุธยิง (ปืน / ธนู)")]
    public GameObject projectilePrefab;   // กระสุนหรือลูกธนูที่ยิงออกไป ต้องมี PlayerProjectile
    public float projectileSpeed = 14f;   // หน่วยต่อวินาที
    public float projectileLifetime = 1.5f; // ระยะยิงไกลสุด = ความเร็ว × เวลานี้

    public bool IsRanged => weaponType == WeaponType.Gun || weaponType == WeaponType.Bow;

    [Header("ยิงหลายลูก (ปืน / ธนู)")]
    [Min(1)] public int projectileCount = 1; // ยิงทีละกี่ลูก กระจายเป็นพัด
    public float spreadAngle;                // มุมห่างระหว่างลูก (องศา)

    [Header("ความสามารถพิเศษ (Special Ability)")]
    [TextArea] public string abilityDescription; 
    public GameObject specialEffectPrefab;
    public WeaponSpecial special;
    public Sprite[] specialFrames;           // เอฟเฟกต์ 3 เฟรมจากชุดภาพอาวุธ
    public float specialScale = 1f;          // ขนาดภาพเอฟเฟกต์ในฉาก
    public float specialDamage;              // 0 = ใช้ attackDamage
    public float specialRadius = 1.4f;       // วงระเบิด / วงพลัง / ขนาดคลื่นตรวจโดน
    public float specialSpeed = 11f;         // คลื่นพลังบินเร็วเท่านี้
    public float specialRange = 7f;          // คลื่นพลังบินไกลสุดเท่านี้
    public float specialDuration = 0.5f;     // GroundPulse: ทำดาเมจต่อเนื่องนานเท่านี้
    [Min(1)] public int comboCount = 3;      // ComboWave: ฟันครั้งที่เท่านี้ในคอมโบ
    public float chargeTime = 1f;            // ChargeWave: ต้องง้างค้างนานเท่านี้
    public int bounces;                      // Ricochet: ชิ่งกำแพงได้กี่ครั้ง

    public float SpecialDamage => specialDamage > 0f ? specialDamage : attackDamage;
}