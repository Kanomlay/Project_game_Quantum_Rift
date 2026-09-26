using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// หน้าร้านใช้ร่วมกันทุกร้าน; สร้างเพียงครั้งเดียวแล้วเปิด/ปิดซ้ำ แสดงสินค้าของร้านที่เปิดอยู่ (ShopClickable)
// ขอบเขต 1.3.9.2 / 1.3.9.3: ช่อง 1–2 ขวดยาเลือด/พลังงาน, ช่อง 3–5 อาวุธสุ่ม ราคาตามระดับ ซื้อได้ครั้งละ 1 ชิ้น
// - อาวุธ: เข้าช่อง 2 ถ้ายังว่าง ไม่งั้นสลับกับอาวุธในมือ อาวุธเดิมวางไว้ที่พื้นข้างตัว
// - ขวดยา: วางลงที่เท้า เก็บทันทีถ้าหลอดยังไม่เต็ม (เต็มอยู่ก็วางรอไว้ ไม่เสียของ)
public sealed class ShopWindow : MonoBehaviour
{
    public GameObject content;

    [Header("ส่วนแสดงผล (สร้างโดย ShopUIBuilder)")]
    public ShopSlotView[] slots;
    public TMP_Text currencyText;
    public TMP_Text messageText;
    [Tooltip("ชี้เมาส์ที่สินค้าแล้วแสดงรายละเอียดในกล่องล่าง (ปิดไว้ก่อน กล่องล่างแสดงคำแนะนำ/ผลการซื้อเท่านั้น)")]
    public bool showDetailsOnHover = false;

    const string Hint = "คลิกที่ราคาเพื่อซื้อ · ซื้อได้ครั้งละ 1 ชิ้น";
    static readonly Color HintColor = new Color(0.75f, 0.85f, 1f);
    static readonly Color GoodColor = new Color(0.55f, 1f, 0.6f);
    static readonly Color BadColor = new Color(1f, 0.45f, 0.45f);

    static ShopWindow current;
    static int closedFrame = -1;
    ShopClickable shop;

    public static bool IsOpen => current != null && current.content != null && current.content.activeSelf;
    // ESC ที่เพิ่งใช้ปิดหน้าร้านในเฟรมนี้ ไม่ให้ PauseManager เอาไปเปิดหน้าหยุดเกมต่อ
    public static bool BlocksEscape => IsOpen || Time.frameCount == closedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { current = null; closedFrame = -1; }

    public static void Open(ShopWindow prefab, ShopClickable owner = null)
    {
        if (prefab == null) return;
        if (current == null) current = Instantiate(prefab);
        // รองรับการลาก prefab ร้านไปฉากอื่นที่ยังไม่มี EventSystem
        if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null)
            new GameObject("Shop EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        current.Show(owner);
    }

    void Awake()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            int index = i;
            slot.window = this;
            slot.index = index;
            if (slot.buyButton != null) slot.buyButton.onClick.AddListener(() => Buy(index));
        }
    }

    void Show(ShopClickable owner)
    {
        shop = owner;
        content.SetActive(true);
        Refresh();
        ShowHint();
    }

    public void Close()
    {
        content.SetActive(false);
        closedFrame = Time.frameCount;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (content.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    IReadOnlyList<ShopOffer> Stock => shop != null ? shop.Stock : null;
    LootTable Pool => shop != null ? shop.itemPool : null;

    static PlayerStats Player
    {
        get
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            return hero != null ? hero.GetComponent<PlayerStats>() : null;
        }
    }

    void Refresh()
    {
        var player = Player;
        int money = player != null ? player.currentCurrency : 0;
        if (currencyText != null) currencyText.text = money.ToString();

        var stock = Stock;
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var offer = stock != null && i < stock.Count ? stock[i] : null;
            slots[i].Show(offer, offer?.Icon(Pool), offer != null && money >= offer.price);
        }
    }

    public void ShowHint() => Say(Hint, HintColor);
    public void HoverEnded() { if (showDetailsOnHover) ShowHint(); } // ไม่ลบข้อความผลการซื้อตอนเลื่อนเมาส์ออก

    public void Describe(int index)
    {
        if (!showDetailsOnHover) return;
        var stock = Stock;
        if (stock == null || index >= stock.Count || stock[index] == null) { ShowHint(); return; }
        var offer = stock[index];
        Say($"<b>{offer.DisplayName}</b>  {offer.Describe(Pool)}", Color.white);
    }

    void Say(string text, Color color)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.color = color;
    }

    public void Buy(int index)
    {
        var stock = Stock;
        var player = Player;
        if (stock == null || player == null || index >= stock.Count) return;
        var offer = stock[index];
        if (offer == null || offer.sold) return;

        if (!player.TrySpendCurrency(offer.price))
        {
            Say($"เงินไม่พอ ต้องใช้ {offer.price} เหรียญ (มี {player.currentCurrency})", BadColor);
            return;
        }
        offer.sold = true;

        Transform parent = shop != null ? shop.transform.parent : null;
        Vector2 feet = player.transform.position;
        switch (offer.kind)
        {
            case ShopOffer.Kind.HpPotion:
            case ShopOffer.Kind.EnergyPotion:
                DropPotion(offer.kind, parent, feet);
                Say($"ซื้อ{offer.DisplayName}แล้ว", GoodColor);
                break;
            case ShopOffer.Kind.Weapon:
                var replaced = player.PickUpWeapon(offer.weapon);
                if (replaced != null)
                {
                    // วางอาวุธเดิมไว้ข้างตัว เปลี่ยนใจก็เดินไปกด F เก็บคืนได้
                    WeaponPickup.Create(replaced, parent, feet + Vector2.down * 0.8f);
                    Say($"ได้{offer.DisplayName}แล้ว · วาง{replaced.weaponName}ไว้ที่พื้น", GoodColor);
                }
                else Say($"ได้{offer.DisplayName}แล้ว · กด R สลับอาวุธ", GoodColor);
                break;
        }
        Refresh();
    }

    void DropPotion(ShopOffer.Kind kind, Transform parent, Vector2 at)
    {
        var pool = Pool;
        bool hp = kind == ShopOffer.Kind.HpPotion;
        var pickupKind = hp ? LootPickup.Kind.HpPotion : LootPickup.Kind.EnergyPotion;
        float amount = pool != null ? (hp ? pool.hpRestore : pool.energyRestore) : (hp ? 3f : 100f);
        Sprite sprite = pool != null ? (hp ? pool.hpPotionSprite : pool.energyPotionSprite) : null;
        float size = pool != null ? pool.potionSize : 0.5f;
        LootPickup.Create(pickupKind, amount, sprite, size, parent, at);
    }
}
