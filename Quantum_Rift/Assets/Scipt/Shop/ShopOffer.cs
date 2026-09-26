using UnityEngine;

// สินค้าหนึ่งช่องในร้าน (ขวดยา หรือ อาวุธ) ขายได้ครั้งเดียว ร้านแต่ละร้านสุ่มชุดของตัวเอง
[System.Serializable]
public sealed class ShopOffer
{
    public enum Kind { HpPotion, EnergyPotion, Weapon }

    public Kind kind;
    public WeaponData weapon;
    public int price;
    public bool sold;

    public ShopOffer(Kind kind, int price, WeaponData weapon = null)
    {
        this.kind = kind;
        this.price = price;
        this.weapon = weapon;
    }

    public string DisplayName => kind switch
    {
        Kind.HpPotion => "ขวดยาเลือด",
        Kind.EnergyPotion => "ขวดยาพลังงาน",
        _ => weapon != null ? weapon.weaponName : "-",
    };

    public Color NameColor => kind == Kind.Weapon && weapon != null
        ? WeaponPickup.RarityColor(weapon.rarity)
        : kind == Kind.HpPotion ? new Color(1f, 0.55f, 0.6f) : new Color(0.5f, 0.8f, 1f);

    public Sprite Icon(LootTable pool) => kind switch
    {
        Kind.HpPotion => pool != null ? pool.hpPotionSprite : null,
        Kind.EnergyPotion => pool != null ? pool.energyPotionSprite : null,
        _ => weapon != null ? weapon.weaponIcon : null,
    };

    // คำอธิบายตอนชี้เมาส์
    public string Describe(LootTable pool)
    {
        switch (kind)
        {
            case Kind.HpPotion:
                return $"ฟื้นฟูพลังชีวิต {(pool != null ? pool.hpRestore : 3f):0.#} หน่วยทันที (ไม่เกินค่าสูงสุด)";
            case Kind.EnergyPotion:
                return $"ฟื้นฟูพลังงาน {(pool != null ? pool.energyRestore : 100)} หน่วยทันที (ไม่เกินค่าสูงสุด)";
        }
        if (weapon == null) return "";
        string line = $"ดาเมจ {weapon.attackDamage:0.#} · {weapon.attackSpeed:0.#} ครั้ง/วินาที · พลังงาน {weapon.energyCost}";
        return string.IsNullOrEmpty(weapon.abilityDescription) ? line : $"{line}\n{weapon.abilityDescription}";
    }
}
