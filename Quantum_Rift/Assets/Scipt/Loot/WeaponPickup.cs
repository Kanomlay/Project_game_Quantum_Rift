using System.Collections.Generic;
using TMPro;
using UnityEngine;

// อาวุธที่ตกอยู่บนพื้น เดินเข้าใกล้แล้วกด F เพื่อเก็บ
// ช่องอาวุธที่ 2 ยังว่าง = เก็บเข้าช่อง 2 แล้วถือเลย, เต็มแล้ว = สลับกับอาวุธในมือ อาวุธเดิมวางลงพื้นตรงนั้น
// ป้ายชื่อขึ้นเฉพาะชิ้นที่ใกล้ที่สุด สีตามระดับ (ธรรมดา ขาว / หายาก ฟ้า / ตำนาน ทอง)
//
// ภาพบนพื้นขยายใหญ่ให้เห็นชัด และมีแสงตามระดับ (ภาพแสงสร้างในโค้ด ไม่ใช้ไฟล์ภาพ):
// - ธรรมดา: แสงขาวจาง ๆ ใต้อาวุธ หายใจช้า ๆ
// - หายาก: แสงฟ้ากว้างขึ้น เต้นเร็วขึ้น มีประกายลอยขึ้นเป็นระยะ
// - ตำนาน: แสงทองกว้างสุด + ลำแสงพุ่งขึ้นฟ้า + ประกายลอยขึ้นถี่ ๆ
public sealed class WeaponPickup : FloorItem
{
    public const KeyCode PickKey = KeyCode.F;
    const float PickRadius = 1.2f;

    // ความยาวด้านยาวของช่องภาพ (192 px) ในฉาก ตัวอาวุธจริงกินราว 80% ของช่อง
    static readonly float[] SizeByTier = { 1.6f, 1.75f, 1.9f };
    static readonly float[] GlowWidth = { 1.5f, 1.9f, 2.4f };
    static readonly float[] GlowAlpha = { 0.22f, 0.4f, 0.5f };
    static readonly float[] PulseSpeed = { 2f, 3.5f, 4.5f };
    static readonly float[] SparkEvery = { 0f, 0.35f, 0.1f };

    static readonly List<WeaponPickup> all = new List<WeaponPickup>();
    static Sprite glowSprite, beamSprite;

    public WeaponData weapon;
    TextMeshPro label;
    float readyAt;

    SpriteRenderer glow, beam;
    Color tint;
    int tier;
    float phase, nextSpark;

    public static WeaponPickup Create(WeaponData weapon, Transform parent, Vector2 position)
    {
        int tier = Tier(weapon.rarity);
        var item = Create<WeaponPickup>("Weapon - " + weapon.weaponName, weapon.weaponIcon, SizeByTier[tier], parent, position);
        item.weapon = weapon;
        item.tier = tier;
        item.BuildAura();
        item.BuildLabel();
        return item;
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    // มีอาวุธบนพื้นอยู่ในระยะกด F ไหม (ร้านค้าใช้ปุ่มเดียวกัน ให้เก็บอาวุธก่อน)
    public static bool AnyInReach
    {
        get
        {
            var player = Player;
            if (player == null) return false;
            foreach (var item in all)
                if (item != null && item.Landed && item.DistanceTo(player) <= PickRadius) return true;
            return false;
        }
    }

    static int Tier(WeaponRarity rarity) =>
        rarity == WeaponRarity.Legendary ? 2 : rarity == WeaponRarity.Rare ? 1 : 0;

    void BuildLabel()
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        label = obj.AddComponent<TextMeshPro>();
        label.text = $"[{PickKey}] {weapon.weaponName}";
        label.fontSize = 2.4f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = RarityColor(weapon.rarity);
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(20, 12, 36, 255);
        label.rectTransform.sizeDelta = new Vector2(6f, 1f);
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

    // ---------- แสงตามระดับ ----------

    void BuildAura()
    {
        tint = RarityColor(weapon.rarity);
        phase = Random.value * Mathf.PI * 2f;
        // แสงเป็นวงรีแบนบนพื้น อยู่ใต้ตัวอาวุธ (ลำดับวาดต่ำกว่าอาวุธ -1)
        float width = GlowWidth[tier];
        glow = Layer("Glow", GlowSprite, new Vector3(width, width * 0.55f, 1f), -3);
        if (tier == 2) beam = Layer("Beam", BeamSprite, new Vector3(2.6f, 3.2f, 1f), -2);
        AnimateAura();
    }

    SpriteRenderer Layer(string name, Sprite sprite, Vector3 scale, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = scale;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "object";
        renderer.sortingOrder = order;
        return renderer;
    }

    protected override void Update()
    {
        base.Update();
        AnimateAura();
    }

    void AnimateAura()
    {
        if (glow == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed[tier] + phase);

        glow.color = new Color(tint.r, tint.g, tint.b, GlowAlpha[tier] * (0.7f + 0.3f * pulse));
        float breath = 1f + 0.08f * pulse;
        float width = GlowWidth[tier] * breath;
        glow.transform.localScale = new Vector3(width, width * 0.55f, 1f);

        if (beam != null)
        {
            beam.color = new Color(tint.r, tint.g, tint.b, 0.22f + 0.14f * pulse);
            beam.transform.localScale = new Vector3(2.6f * (0.85f + 0.3f * pulse), 3.2f, 1f);
        }

        // ประกายลอยขึ้นจากพื้นรอบอาวุธ
        if (SparkEvery[tier] > 0f && Landed && Time.time >= nextSpark)
        {
            nextSpark = Time.time + SparkEvery[tier] * Random.Range(0.7f, 1.3f);
            Vector2 at = (Vector2)transform.position + new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-0.1f, 0.3f));
            ImpactSparks.Spawn(at, tint, 1, Vector2.up, 3f, 15f);
        }
    }

    // วงกลมฟุ้ง: ทึบตรงกลางจางออกขอบ (1 หน่วย ย่อ/ขยายด้วย scale)
    static Sprite GlowSprite
    {
        get
        {
            if (glowSprite != null) return glowSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, (1f - d) * (1f - d));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return glowSprite;
        }
    }

    // ลำแสงแนวตั้ง: ฐานอยู่ที่พื้น (pivot ล่าง) จางออกด้านข้างและด้านบน (กว้าง 0.25 x สูง 1 หน่วยก่อน scale)
    static Sprite BeamSprite
    {
        get
        {
            if (beamSprite != null) return beamSprite;
            const int w = 16, h = 64;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float side = 1f - Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float up = (y + 0.5f) / h;
                    float bottom = Mathf.Clamp01(up * 8f); // ขอบล่างนุ่ม ไม่ตัดเป็นเส้น
                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Pow(side, 1.5f) * Mathf.Pow(1f - up, 1.3f) * bottom);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            beamSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
            return beamSprite;
        }
    }

    // ---------- เก็บ ----------

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
