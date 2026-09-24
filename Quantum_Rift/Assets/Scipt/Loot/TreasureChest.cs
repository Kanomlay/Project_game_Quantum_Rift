using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// กล่องสมบัติ (ขอบเขต 1.3.8): โผล่ในห้องที่เพิ่งเคลียร์มอนสเตอร์ของแมพ 1 และ 2
// ผู้เล่นเดินมาชิดแล้วกล่องเปิดเอง ของกระเด็นออกรอบกล่อง: เหรียญ 5–10, ขวดยาเลือด 1, ขวดยาพลังงาน 1, อาวุธสุ่มระดับ
// root อยู่ที่พื้น (ก้นกล่อง) ภาพอยู่ในลูกชื่อ Visual ย่อให้กว้างเท่า width
public sealed class TreasureChest : MonoBehaviour
{
    public Sprite closedSprite;
    public Sprite openSprite;
    public float width = 1.3f;
    public float openRadius = 1.1f;     // ผู้เล่นเข้ามาใกล้แค่นี้กล่องเปิดเอง
    public float appearTime = 0.3f;     // กล่องเด้งโผล่ขึ้นมา
    public float tossDistance = 1.5f;   // ของกระเด็นออกไปไกลสุดเท่านี้
    public float tossInterval = 0.04f;  // ของออกมาทีละชิ้นติด ๆ กัน

    public LootTable Loot { get; private set; }
    public bool IsOpen => opened;

    SpriteRenderer display;
    bool opened;
    float readyAt;

    public static TreasureChest Spawn(LootTable loot, RoomController room)
    {
        if (loot == null || loot.chestPrefab == null || room == null) return null;
        var hero = GameObject.FindGameObjectWithTag("Player");
        Vector2 playerPosition = hero != null ? (Vector2)hero.transform.position : Vector2.one * 9999f;
        Vector2 spot = LootPlacement.ChestSpot(room, playerPosition);
        // เป็นลูกของห้อง เปลี่ยนด่านแล้วหายไปพร้อมแมพ
        var chest = Instantiate(loot.chestPrefab, spot, Quaternion.identity, room.transform);
        chest.Loot = loot;
        return chest;
    }

    void Awake()
    {
        display = GetComponentInChildren<SpriteRenderer>();
        ShowSprite(closedSprite);
    }

    void Start()
    {
        readyAt = Time.time + appearTime + 0.2f;
        StartCoroutine(Appear());
    }

    IEnumerator Appear()
    {
        var visual = display.transform;
        Vector3 full = visual.localScale;
        for (float t = 0f; t < appearTime; t += Time.deltaTime)
        {
            float k = t / appearTime;
            float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f; // เด้งเกินนิดนึงแล้วยุบกลับ
            visual.localScale = full * (k * overshoot);
            yield return null;
        }
        visual.localScale = full;
    }

    void ShowSprite(Sprite sprite)
    {
        if (display == null || sprite == null) return;
        display.sprite = sprite;
        float scale = width / Mathf.Max(0.01f, sprite.bounds.size.x);
        display.transform.localScale = Vector3.one * scale;
        // ภาพกล่องปิด/เปิดสูงไม่เท่ากัน วางก้นภาพไว้ที่ root เสมอ ฝาเปิดจะได้งอกขึ้นด้านบน
        display.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y * scale, 0f);
    }

    void Update()
    {
        if (opened || Time.time < readyAt || PauseManager.isGamePaused) return;
        var hero = GameObject.FindGameObjectWithTag("Player");
        if (hero == null) return;
        var stats = hero.GetComponent<PlayerStats>();
        if (stats != null && stats.isDead) return;
        if (Vector2.Distance(transform.position, hero.transform.position) <= openRadius + width * 0.5f) Open();
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        ShowSprite(openSprite);
        if (Loot != null) StartCoroutine(Drop(Loot));
    }

    IEnumerator Drop(LootTable loot)
    {
        var items = new List<FloorItem>();
        Transform parent = transform.parent;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;

        var weapon = loot.RollWeapon();
        if (weapon != null) items.Add(WeaponPickup.Create(weapon, parent, origin));
        for (int i = 0; i < loot.hpPotionCount; i++)
            items.Add(LootPickup.Create(LootPickup.Kind.HpPotion, loot.hpRestore, loot.hpPotionSprite, loot.potionSize, parent, origin));
        for (int i = 0; i < loot.energyPotionCount; i++)
            items.Add(LootPickup.Create(LootPickup.Kind.EnergyPotion, loot.energyRestore, loot.energyPotionSprite, loot.potionSize, parent, origin));
        int coins = loot.RollCurrency();
        for (int i = 0; i < coins; i++)
            items.Add(LootPickup.Create(LootPickup.Kind.Coin, 1f, loot.coinSprite, loot.coinSize, parent, origin));

        // กระจายรอบกล่องเท่า ๆ กัน เริ่มจากด้านล่าง (ฝั่งที่ผู้เล่นมักยืน) ชิ้นใหญ่ออกก่อน
        float start = -90f + Random.Range(-20f, 20f);
        for (int i = 0; i < items.Count; i++)
        {
            items[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;
            float angle = (start + i * 360f / items.Count + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = tossDistance * Random.Range(0.6f, 1f);
            Vector2 landing = LootPlacement.Landing(origin, direction, distance, transform);
            items[i].gameObject.SetActive(true);
            items[i].Toss(origin, landing);
            yield return new WaitForSeconds(tossInterval);
        }
    }
}
