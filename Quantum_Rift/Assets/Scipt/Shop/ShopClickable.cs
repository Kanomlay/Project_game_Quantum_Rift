using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// พ่อค้า (NPC ร้านค้า) ในห้องปลอดภัย: เดินเข้าใกล้แล้วกด F หรือคลิกที่ร้าน เพื่อเปิดหน้าร้าน
// สินค้าสุ่มครั้งแรกที่เปิด แล้วคงไว้ตลอดการเข้าแมพครั้งนี้ (ร้านใหม่ทุกแมพ = ของใหม่ทุกครั้ง)
// ขอบเขต 1.3.9.2: ราคา ธรรมดา 10 / หายาก 20 / ตำนาน 50 · 1.3.9.3: ขวดเลือด/ขวดพลังงาน ขวดละ 10
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ShopClickable : MonoBehaviour
{
    public ShopWindow windowPrefab;

    [Header("สินค้า")]
    public LootTable itemPool;         // รายการอาวุธตามระดับ + ภาพ/ค่าขวดยา (ใช้ ChestLoot ร่วมกับกล่องสมบัติ)
    public int weaponSlots = 3;
    public int commonPrice = 10;
    public int rarePrice = 20;
    public int legendaryPrice = 50;
    public int potionPrice = 10;

    [Header("การเปิดร้าน")]
    public KeyCode openKey = KeyCode.F;
    public float interactRange = 2.4f; // ระยะจากเท้าผู้เล่นถึงฐานร้าน

    List<ShopOffer> stock;
    TextMeshPro prompt;
    Transform player;

    public IReadOnlyList<ShopOffer> Stock => stock ??= RollStock();

    List<ShopOffer> RollStock()
    {
        var offers = new List<ShopOffer>
        {
            new ShopOffer(ShopOffer.Kind.HpPotion, potionPrice),
            new ShopOffer(ShopOffer.Kind.EnergyPotion, potionPrice),
        };

        // อาวุธไม่ซ้ำกันในร้านเดียว สุ่มระดับตามน้ำหนักเดียวกับกล่องสมบัติ
        var picked = new HashSet<WeaponData>();
        for (int attempt = 0; attempt < weaponSlots * 8 && picked.Count < weaponSlots; attempt++)
        {
            var weapon = itemPool != null ? itemPool.RollAnyWeapon() : null;
            if (weapon == null) break;
            if (picked.Add(weapon)) offers.Add(new ShopOffer(ShopOffer.Kind.Weapon, PriceOf(weapon), weapon));
        }
        return offers;
    }

    int PriceOf(WeaponData weapon)
    {
        switch (weapon.rarity)
        {
            case WeaponRarity.Legendary: return legendaryPrice;
            case WeaponRarity.Rare: return rarePrice;
            default: return commonPrice;
        }
    }

    void Start() => BuildPrompt();

    // ป้าย "[F] ร้านค้า" ลอยเหนือร้าน ขึ้นเมื่อเดินมาใกล้
    void BuildPrompt()
    {
        var obj = new GameObject("Prompt");
        obj.transform.SetParent(transform, false);
        float scale = Mathf.Abs(transform.lossyScale.x) > 0.0001f ? 1f / Mathf.Abs(transform.lossyScale.x) : 1f;
        obj.transform.localScale = Vector3.one * scale; // ร้านถูกย่อไว้ ป้ายต้องขนาดเท่าป้ายอาวุธ
        obj.transform.localPosition = new Vector3(0f, 2.5f * scale, 0f);
        prompt = obj.AddComponent<TextMeshPro>();
        prompt.text = $"[{openKey}] ร้านค้า";
        prompt.fontSize = 2.6f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = new Color(1f, 0.87f, 0.35f);
        prompt.outlineWidth = 0.2f;
        prompt.outlineColor = new Color32(20, 12, 36, 255);
        prompt.rectTransform.sizeDelta = new Vector2(5f, 1f);
        obj.GetComponent<MeshRenderer>().sortingLayerName = "Effect";
        obj.SetActive(false);
    }

    bool PlayerNear()
    {
        if (player == null)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return false;
            player = hero.transform;
        }
        return Vector2.Distance(player.position, transform.position) <= interactRange;
    }

    void Update()
    {
        bool near = PlayerNear() && !ShopWindow.IsOpen && !PauseManager.isGamePaused;
        if (prompt != null && prompt.gameObject.activeSelf != near) prompt.gameObject.SetActive(near);

        // อาวุธบนพื้นใกล้ ๆ ใช้ปุ่มเดียวกัน ให้เก็บอาวุธก่อน
        if (near && Input.GetKeyDown(openKey) && !WeaponPickup.AnyInReach) ShopWindow.Open(windowPrefab, this);
    }

    // OnMouseDown ใช้ Collider2D และกล้องของฉาก; ไม่ต้องเพิ่ม PhysicsRaycaster
    void OnMouseDown()
    {
        if (ShopWindow.IsOpen || PauseManager.isGamePaused || !PlayerNear()) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        ShopWindow.Open(windowPrefab, this);
    }
}
