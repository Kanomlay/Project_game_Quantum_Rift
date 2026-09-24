using UnityEngine;

// เหรียญและขวดยาบนพื้น เดินไปโดนแล้วเก็บเอง (ขอบเขต 1.3.10)
// - เหรียญ: เข้าใกล้แล้วบินเข้าหาตัว +เงินเหรียญละ amount หน่วย
// - ขวดยาฟื้นฟูพลังชีวิต: +amount ทันที ไม่เกินค่าสูงสุด / ขวดยาฟื้นฟูพลังงาน: +amount ทันที ไม่เกินค่าสูงสุด
//   ถ้าหลอดเต็มอยู่แล้วขวดจะยังวางอยู่ ไม่เสียของ กลับมาเก็บทีหลังได้
public sealed class LootPickup : FloorItem
{
    public enum Kind { Coin, HpPotion, EnergyPotion }

    const float CollectRadius = 0.5f;
    const float MagnetRadius = 2.2f;
    const float MagnetSpeed = 9f;

    public Kind kind;
    public float amount;

    public static LootPickup Create(Kind kind, float amount, Sprite sprite, float size, Transform parent, Vector2 position)
    {
        var item = Create<LootPickup>(kind.ToString(), sprite, size, parent, position);
        item.kind = kind;
        item.amount = amount;
        return item;
    }

    protected override void OnLandedUpdate(PlayerStats player)
    {
        float distance = DistanceTo(player);

        if (kind == Kind.Coin && distance <= MagnetRadius && distance > CollectRadius)
        {
            Vector2 target = (Vector2)player.transform.position + Vector2.up * 0.2f;
            transform.position = Vector2.MoveTowards(transform.position, target, MagnetSpeed * Time.deltaTime);
            return;
        }
        if (distance > CollectRadius) return;

        switch (kind)
        {
            case Kind.Coin:
                player.AddCurrency(Mathf.RoundToInt(amount));
                break;
            case Kind.HpPotion:
                if (player.currentHP >= player.maxHP) return;
                player.Heal(amount);
                break;
            case Kind.EnergyPotion:
                if (player.currentEnergy >= player.maxEnergy) return;
                player.RestoreEnergy(Mathf.RoundToInt(amount));
                break;
        }
        Destroy(gameObject);
    }
}
