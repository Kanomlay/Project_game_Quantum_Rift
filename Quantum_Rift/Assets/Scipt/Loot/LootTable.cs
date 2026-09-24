using UnityEngine;

// ของในกล่องสมบัติ (ขอบเขต 1.3.8 / 1.3.9.1 / 1.3.10)
// - เงิน 5–10 หน่วยต่อห้อง (เหรียญละ 1 หน่วย กระเด็นออกมาให้เก็บ)
// - ขวดยาฟื้นฟูพลังชีวิต 1 ขวด (+3) และขวดยาฟื้นฟูพลังงาน 1 ขวด (+100) ทุกกล่อง
// - อาวุธสุ่มตามระดับ ธรรมดา 60% / หายาก 30% / ตำนาน 10% (ระดับที่ยังไม่มีอาวุธในรายการจะข้ามไปสุ่มระดับที่มี)
// ใส่ไว้ที่ MapData.chestLoot ของแมพที่มีกล่อง (แมพ 1 และ 2) แมพที่ไม่ใส่จะไม่มีกล่อง
[CreateAssetMenu(fileName = "ChestLoot", menuName = "Game Data/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("กล่อง")]
    public TreasureChest chestPrefab;

    [Header("เงิน")]
    public int currencyMin = 5;
    public int currencyMax = 10;
    public Sprite coinSprite;
    public float coinSize = 0.3f;

    [Header("ขวดยา")]
    public int hpPotionCount = 1;
    public int energyPotionCount = 1;
    public float hpRestore = 3f;
    public int energyRestore = 100;
    public Sprite hpPotionSprite;
    public Sprite energyPotionSprite;
    public float potionSize = 0.5f;

    [Header("อาวุธ")]
    [Range(0f, 1f)] public float weaponChance = 1f;
    public float commonWeight = 60f;
    public float rareWeight = 30f;
    public float legendaryWeight = 10f;
    public WeaponData[] commonWeapons;
    public WeaponData[] rareWeapons;
    public WeaponData[] legendaryWeapons;

    public int RollCurrency() => Random.Range(currencyMin, currencyMax + 1);

    // สุ่มระดับก่อนแล้วค่อยสุ่มอาวุธในระดับนั้น ระดับที่รายการว่างไม่นับน้ำหนัก ไม่มีอาวุธเลยคืน null
    public WeaponData RollWeapon()
    {
        if (Random.value > weaponChance) return null;
        float common = Has(commonWeapons) ? commonWeight : 0f;
        float rare = Has(rareWeapons) ? rareWeight : 0f;
        float legendary = Has(legendaryWeapons) ? legendaryWeight : 0f;
        float total = common + rare + legendary;
        if (total <= 0f) return null;

        float roll = Random.value * total;
        WeaponData[] pool = roll < common ? commonWeapons : roll < common + rare ? rareWeapons : legendaryWeapons;
        return Pick(pool);
    }

    static bool Has(WeaponData[] list)
    {
        if (list == null) return false;
        foreach (var weapon in list) if (weapon != null) return true;
        return false;
    }

    static WeaponData Pick(WeaponData[] list)
    {
        int count = 0;
        foreach (var weapon in list) if (weapon != null) count++;
        int index = Random.Range(0, count);
        foreach (var weapon in list)
        {
            if (weapon == null) continue;
            if (index-- == 0) return weapon;
        }
        return null;
    }
}
