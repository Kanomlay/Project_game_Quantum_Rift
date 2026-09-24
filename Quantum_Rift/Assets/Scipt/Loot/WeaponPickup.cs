using System.Collections.Generic;
using TMPro;
using UnityEngine;

// อาวุธที่ตกอยู่บนพื้น เดินเข้าใกล้แล้วกด F เพื่อเก็บ
// ช่องอาวุธที่ 2 ยังว่าง = เก็บเข้าช่อง 2 แล้วถือเลย, เต็มแล้ว = สลับกับอาวุธในมือ อาวุธเดิมวางลงพื้นตรงนั้น
// ป้ายชื่อขึ้นเฉพาะชิ้นที่ใกล้ที่สุด สีตามระดับ (ธรรมดา ขาว / หายาก ฟ้า / ตำนาน ทอง)
public sealed class WeaponPickup : FloorItem
{
    public const KeyCode PickKey = KeyCode.F;
    const float PickRadius = 1.1f;
    const float Size = 1f;

    static readonly List<WeaponPickup> all = new List<WeaponPickup>();

    public WeaponData weapon;
    TextMeshPro label;
    float readyAt;

    public static WeaponPickup Create(WeaponData weapon, Transform parent, Vector2 position)
    {
        var item = Create<WeaponPickup>("Weapon - " + weapon.weaponName, weapon.weaponIcon, Size, parent, position);
        item.weapon = weapon;
        item.BuildLabel();
        return item;
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void BuildLabel()
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        label = obj.AddComponent<TextMeshPro>();
        label.text = $"[{PickKey}] {weapon.weaponName}";
        label.fontSize = 2.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = RarityColor(weapon.rarity);
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(20, 12, 36, 255);
        label.rectTransform.sizeDelta = new Vector2(5f, 1f);
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Effect";
        obj.SetActive(false);
    }

    public static Color RarityColor(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Rare: return new Color(0.35f, 0.72f, 1f);
            case WeaponRarity.Legendary: return new Color(1f, 0.74f, 0.2f);
            default: return new Color(0.93f, 0.93f, 0.93f);
        }
    }

    protected override void OnLandedUpdate(PlayerStats player)
    {
        float distance = DistanceTo(player);
        bool nearest = distance <= PickRadius && IsNearest(player, distance);
        if (label != null && label.gameObject.activeSelf != nearest) label.gameObject.SetActive(nearest);

        if (!nearest || Time.time < readyAt || ShopWindow.IsOpen) return;
        if (!Input.GetKeyDown(PickKey)) return;

        var dropped = player.PickUpWeapon(weapon);
        if (dropped != null)
        {
            // วางอาวุธเดิมไว้ตรงนี้ หน่วงนิดนึงกันกด F ครั้งเดียวแล้วเก็บกลับทันที
            var swapped = Create(dropped, transform.parent, transform.position);
            swapped.readyAt = Time.time + 0.3f;
        }
        Destroy(gameObject);
    }

    bool IsNearest(PlayerStats player, float distance)
    {
        foreach (var other in all)
            if (other != this && other.Landed && other.DistanceTo(player) < distance) return false;
        return true;
    }
}
