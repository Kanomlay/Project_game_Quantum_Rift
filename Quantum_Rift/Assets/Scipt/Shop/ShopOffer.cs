using UnityEngine;

// สินค้าหนึ่งช่องในร้าน (ขวดยา หรือ อาวุธ) ขายได้ครั้งเดียว ร้านแต่ละร้านสุ่มชุดของตัวเอง
[System.Serializable]
public sealed class ShopOffer
{
    public enum Kind { HpPotion, EnergyPotion, Weapon, RunBuff }
    public string buffName, buffNameEnglish;
    public int hpDelta,energyDelta,damageDelta;
    public bool risky;

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

    // ชื่อตามภาษาที่ตั้งไว้ ชื่อขวดยาอังกฤษตามเอกสารขอบเขต 1.3.9.3 (HP Potion / Mana Potion)
    public string DisplayName => NameIn(LanguageSettings.IsThai);
    public string NameIn(bool thai) => kind switch
    {
        Kind.HpPotion => thai ? "ขวดยาเลือด" : "HP Potion",
        Kind.EnergyPotion => thai ? "ขวดยาพลังงาน" : "Mana Potion",
        Kind.RunBuff => thai || string.IsNullOrEmpty(buffNameEnglish) ? buffName : buffNameEnglish,
        _ => weapon != null ? weapon.NameIn(thai) : "-",
    };

    public Color NameColor => kind == Kind.Weapon && weapon != null
        ? WeaponPickup.RarityColor(weapon.rarity)
        : kind == Kind.HpPotion ? new Color(1f, 0.55f, 0.6f) : new Color(0.5f, 0.8f, 1f);

    public Sprite Icon(LootTable pool) => kind switch
    {
        Kind.HpPotion => pool != null ? pool.hpPotionSprite : null,
        Kind.EnergyPotion => pool != null ? pool.energyPotionSprite : null,
        Kind.RunBuff => pool!=null?(hpDelta>0?pool.hpPotionSprite:pool.energyPotionSprite):null,
        _ => weapon != null ? weapon.weaponIcon : null,
    };

    // คำอธิบายตอนชี้เมาส์
    public string Describe(LootTable pool)
    {
        bool th = LanguageSettings.IsThai;
        switch (kind)
        {
            case Kind.RunBuff:
                string text="";
                if(hpDelta!=0)text+=th?$"เลือดสูงสุด {hpDelta:+0;-0} หน่วย\n":$"Max health {hpDelta:+0;-0}\n";
                if(energyDelta!=0)text+=th?$"พลังงานสูงสุด {energyDelta:+0;-0} หน่วย\n":$"Max energy {energyDelta:+0;-0}\n";
                if(damageDelta!=0)text+=th?$"ดาเมจ {damageDelta:+0;-0} หน่วย\n":$"Damage {damageDelta:+0;-0}\n";
                return text+(th?"มีผลจนจบรอบการเล่น":"Lasts until the run ends");
            case Kind.HpPotion:
                float hp = pool != null ? pool.hpRestore : 3f;
                return th ? $"ฟื้นฟูพลังชีวิต {hp:0.#} หน่วยทันที (ไม่เกินค่าสูงสุด)" : $"Restores {hp:0.#} health instantly (up to your maximum)";
            case Kind.EnergyPotion:
                float energy = pool != null ? pool.energyRestore : 100;
                return th ? $"ฟื้นฟูพลังงาน {energy:0.#} หน่วยทันที (ไม่เกินค่าสูงสุด)" : $"Restores {energy:0.#} energy instantly (up to your maximum)";
        }
        if (weapon == null) return "";
        string line = th
            ? $"ดาเมจ {weapon.attackDamage:0.#} · {weapon.attackSpeed:0.#} ครั้ง/วินาที · พลังงาน {weapon.energyCost}"
            : $"Damage {weapon.attackDamage:0.#} · {weapon.attackSpeed:0.#} hits/sec · Energy {weapon.energyCost}";
        string ability = weapon.AbilityText;
        return string.IsNullOrEmpty(ability) ? line : $"{line}\n{ability}";
    }
}
