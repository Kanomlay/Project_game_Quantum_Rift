using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// กล่องสมบัติ (ขอบเขต 1.3.8): โผล่ในห้องที่เพิ่งเคลียร์มอนสเตอร์ของแมพ 1 และ 2
// ผู้เล่นเดินมาชิดแล้วกล่องเปิดเอง ของกระเด็นออกรอบกล่อง: เหรียญ 5–10, ขวดยาเลือด 1, ขวดยาพลังงาน 1, อาวุธสุ่มระดับ
// root อยู่ที่พื้น (ก้นกล่อง) ภาพอยู่ในลูกชื่อ Visual ย่อให้กว้างเท่า width
// กล่องบอส (LootTable.bossChest): ใหญ่ขึ้น แสงทองฟุ้งบนพื้น + ลำแสงพุ่งขึ้น ประกายทองลอยรอบกล่อง
//   เปิดแล้ววาบทอง จอสั่น ลำแสงค่อย ๆ จางหาย ของกระเด็นไกลกว่าเดิม
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
    SpriteRenderer glow, beam;
    float nextSpark, openedAt;
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

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
        if (Loot != null && Loot.bossChest) MakeBossChest();
        StartCoroutine(Appear());
    }

    void MakeBossChest()
    {
        transform.localScale *= Loot.chestScale;
        tossDistance *= 1.4f;
        float scale = Mathf.Max(0.01f, Loot.chestScale);
        int order = display != null ? display.sortingOrder : 0;
        int layer = display != null ? display.sortingLayerID : SortingLayer.NameToID("object");
        glow = EchoFx.Layer(transform, "BossChestGlow", WeaponPickup.GlowSprite, layer, order - 2, Color.clear);
        glow.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        beam = EchoFx.Layer(transform, "BossChestBeam", WeaponPickup.BeamSprite, layer, order - 1, Color.clear);
        beam.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        beam.transform.localScale = new Vector3(3.2f / scale, 4.5f / scale, 1f);
        EchoFx.Flash(transform.position + Vector3.up * 0.5f, Gold, 3f, 0.4f);
        ImpactSparks.Spawn((Vector2)transform.position + Vector2.up * 0.5f, Gold, 16, Vector2.up, 5f, 70f);
    }

    // แสงกล่องบอส: ฟุ้งเป็นจังหวะ ลำแสงพุ่งขึ้น ประกายทองลอยขึ้นรอบกล่อง เปิดแล้วลำแสงจางหายใน 1.5 วินาที
    void AnimateBossChest()
    {
        if (glow == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f);
        float after = opened ? Mathf.Clamp01(1f - (Time.time - openedAt) / 1.5f) : 1f;
        float scale = Mathf.Max(0.01f, Loot.chestScale);
        float width = (2.6f + 0.25f * pulse) / scale;
        glow.transform.localScale = new Vector3(width, width * 0.55f, 1f);
        glow.color = new Color(Gold.r, Gold.g, Gold.b, (0.35f + 0.15f * pulse) * (0.4f + 0.6f * after));
        if (beam != null) beam.color = new Color(Gold.r, Gold.g, Gold.b, (0.25f + 0.15f * pulse) * after);
        if (after <= 0f || Time.time < nextSpark) return;
        nextSpark = Time.time + Random.Range(0.08f, 0.16f);
        Vector2 at = (Vector2)transform.position + new Vector2(Random.Range(-0.9f, 0.9f), Random.Range(0f, 0.6f)) * scale;
        ImpactSparks.Spawn(at, Gold, 1, Vector2.up, 3f, 15f);
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
        AnimateBossChest();
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
        openedAt = Time.time;
        ShowSprite(openSprite);
        if (Loot != null && Loot.bossChest)
        {
            EchoFx.Flash(transform.position + Vector3.up * 0.6f, Gold, 5f, 0.5f);
            EchoFx.Shockwave(transform.position, Gold, 3f, 0.6f);
            ImpactSparks.Spawn((Vector2)transform.position + Vector2.up * 0.6f, Gold, 24, Vector2.up, 6f, 80f);
            CameraFollow.Shake(0.2f, 0.25f);
        }
        if (Loot != null) StartCoroutine(Drop(Loot));
    }

    IEnumerator Drop(LootTable loot)
    {
        var items = new List<FloorItem>();
        Transform parent = transform.parent;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;

        foreach (var weapon in loot.RollWeapons()) items.Add(WeaponPickup.Create(weapon, parent, origin));
        for (int i = 0; i < loot.hpPotionCount; i++)
            items.Add(LootPickup.Create(LootPickup.Kind.HpPotion, loot.hpRestore, loot.hpPotionSprite, loot.potionSize, parent, origin));
        for (int i = 0; i < loot.energyPotionCount; i++)
            items.Add(LootPickup.Create(LootPickup.Kind.EnergyPotion, loot.energyRestore, loot.energyPotionSprite, loot.potionSize, parent, origin));
        int coins = loot.RollCurrency();
        if (loot.coinPileSprite != null && coins > 0) // กองเหรียญใหญ่ เก็บทีเดียวได้ทั้งหมด
            items.Add(LootPickup.Create(LootPickup.Kind.Coin, coins, loot.coinPileSprite, loot.coinPileSize, parent, origin));
        else
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
