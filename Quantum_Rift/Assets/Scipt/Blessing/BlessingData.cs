using UnityEngine;

// พรควอนตัม 9 แบบ (ชุดไอคอน image/UI_Image/QuantumRift-BuffIcons-v1) สะสมได้สูงสุด 4 พรต่อหนึ่งรอบการเล่น
// ผลของแต่ละพรเขียนไว้ใน BlessingManager ตาม type ส่วนตัวเลขปรับได้ที่ asset นี้ (สร้างโดย BlessingBuilder)
public enum BlessingType
{
    BulletCleave,    // คมสลายมิติ
    PhasePiercing,   // กระสุนทะลุมิติ
    ChargedWave,     // คลื่นสะสม
    EmergencyShield, // เกราะฉุกเฉิน
    RiftStep,        // ก้าวพ้นรอยแยก
    BulletSlowField, // สนามชะลอกระสุน
    EnergyHarvest,   // เก็บเกี่ยวพลังงาน
    HealingPulse,    // ชีพจรฟื้นฟู
    EnergyReserve,   // พลังงานสำรอง
}

public enum BlessingCategory { Attack, Defense, Resource } // สีกรอบไอคอน: ม่วง / ฟ้า / เขียวอมฟ้า

[CreateAssetMenu(fileName = "NewBlessing", menuName = "Game Data/Blessing")]
public class BlessingData : ScriptableObject
{
    public BlessingType type;
    public BlessingCategory category;
    public Sprite icon;    // 128 px การ์ดในหน้าต่างเลือกพร
    public Sprite hudIcon; // 64 px แถบพรบน HUD

    [Header("ข้อความ (ไทย / อังกฤษ)")]
    public string nameThai;
    public string nameEnglish;
    [TextArea(2, 4)] public string abilityThai;
    [TextArea(2, 4)] public string abilityEnglish;
    [TextArea(2, 4)] public string limitThai;
    [TextArea(2, 4)] public string limitEnglish;

    [Header("ตัวเลขของพร (แต่ละพรใช้เฉพาะบางช่อง ดู Tooltip)")]
    [Tooltip("คมสลายมิติ: กระสุนสูงสุดต่อการฟัน\nกระสุนทะลุมิติ: ทะลุเพิ่มกี่ตัว\nคลื่นสะสม: โจมตีโดนกี่ครั้งถึงปล่อยคลื่น\n" +
             "เก็บเกี่ยวพลังงาน: พลังงานที่ได้ต่อศัตรู 1 ตัว\nพลังงานสำรอง: การโจมตีครั้งที่เท่านี้ไม่เสียพลังงาน")]
    public int count;
    [Tooltip("กระสุนทะลุมิติ: ดาเมจตัวถัดไป (0.6 = 60%)\nคลื่นสะสม: ดาเมจคลื่นเทียบอาวุธ\nเกราะฉุกเฉิน: เกราะเทียบเลือดสูงสุด\n" +
             "ก้าวพ้นรอยแยก: ความเร็วที่เพิ่ม (0.25 = 25%)\nสนามชะลอกระสุน: ความเร็วกระสุนที่ลดลง\nชีพจรฟื้นฟู: เลือดที่ฟื้นเทียบเลือดสูงสุด")]
    public float amount;
    [Tooltip("เกราะฉุกเฉิน: เลือดลดถึงสัดส่วนนี้แล้วเกราะขึ้น (0.3 = 30%)")]
    public float threshold;
    [Tooltip("เกราะฉุกเฉิน / ก้าวพ้นรอยแยก: อยู่นานกี่วินาที")]
    public float duration;
    [Tooltip("ก้าวพ้นรอยแยก: คูลดาวน์ (วินาที)")]
    public float cooldown;
    [Tooltip("สนามชะลอกระสุน: รัศมีรอบตัว\nคลื่นสะสม: รัศมีโดนของคลื่น")]
    public float radius;
    [Tooltip("เก็บเกี่ยวพลังงาน: ฟื้นได้สูงสุดต่อห้อง")]
    public int perRoomCap;

    [Header("คลื่นสะสม: ภาพและการบินของคลื่น")]
    public Sprite[] waveFrames; // 3 เฟรมแบบคลื่นดาบผ่ามิติ: แสงวาบ / คลื่นตอนบิน / สลาย
    public float waveScale = 1.5f;
    public float waveSpeed = 11f;
    public float waveRange = 7f;
    public Color waveTint = new Color(0.6f, 0.95f, 1f);

    public string DisplayName => LanguageSettings.IsThai ? nameThai : Fallback(nameEnglish, nameThai);
    public string Ability => LanguageSettings.IsThai ? abilityThai : Fallback(abilityEnglish, abilityThai);
    public string Limit => LanguageSettings.IsThai ? limitThai : Fallback(limitEnglish, limitThai);

    static string Fallback(string wanted, string other) => string.IsNullOrEmpty(wanted) ? other : wanted;

    public static Color CategoryColor(BlessingCategory category)
    {
        switch (category)
        {
            case BlessingCategory.Attack: return new Color(0.72f, 0.4f, 1f);
            case BlessingCategory.Defense: return new Color(0.3f, 0.82f, 1f);
            default: return new Color(0.3f, 1f, 0.78f);
        }
    }
}
