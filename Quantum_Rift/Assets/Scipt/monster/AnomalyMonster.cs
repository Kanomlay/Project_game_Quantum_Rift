using UnityEngine;

// มอนผิดเพี้ยน (Anomaly): มอนธรรมดาที่โดนพลังรอยแยกทำให้ผิดเพี้ยน สุ่มตอนเกิดในห้อง (RoomController)
// คนละอย่างกับมอนระดับหัวหน้าหน่วย (anomaly ในเอกสารขอบเขต 1.3.5.2) ที่เป็นมอนอีกชนิดเลย
// วงเตือนตอนเกิดเป็นแบบหัวหน้า (ใหญ่ สั่น) สีตามแบบ
// หน้าตา: ขอบเรืองสีประจำแบบรอบตัวแบบพิกเซล (ภาพตัวมอนเองย้อมสีทึบ ซ้อนหลังตัว 8 ทิศ ตามเฟรม/หายใจ/เด้งของตัวจริง)
//         วงเส้นประหมุนช้า ๆ ที่เท้า + แสงสีจาง ๆ บนพื้น ประกายเล็ก ๆ ลอยขึ้น
// ฆ่าได้ดรอปเหรียญ 3–5 เหรียญ (ภาพเหรียญของกล่องสมบัติแมพนั้น)
//   ร่างยักษ์ (ทอง)   ตัวใหญ่ 1.35 เท่า เลือด ×2.5 ตีแรง ×1.5 ทนแรงกระแทก ×1.8 เดินช้าลงนิด
//   โหมกระหน่ำ (แดง) โจมตีไม่มีหน่วง: ประชิดตีต่อทันทีที่ท่าจบ / ยิงไกลหน่วงเหลือ 35% (กันกระสุนเป็นสายยิงไม่หยุด) เดินเร็วขึ้น
//   เกราะหนา (ฟ้าเหล็ก) รับดาเมจครึ่งเดียว (เลขเทา) จนกว่าจะเซครั้งแรก เกราะแตกแล้วรับดาเมจปกติ ใช้อาวุธหนักแก้ทาง
// ค่าที่ปรับตัวมอนเป็นตัวคูณเฉพาะตัวใน MonsterController (MonsterData ใช้ร่วมกันทุกตัว แก้ตรงนั้นไม่ได้)
public sealed class AnomalyMonster : MonoBehaviour
{
    public enum Kind { Colossal, Frenzied, Armored } // ร่างยักษ์ / โหมกระหน่ำ / เกราะหนา

    public const float Chance = 0.15f; // โอกาสต่อมอนหนึ่งตัวที่เกิดในห้อง
    public const int MaxPerRoom = 2;
    const int CoinMin = 3, CoinMax = 5;
    const float ColossalSize = 1.35f;

    public Kind kind;
    MonsterController monster;
    const float RingSpin = 40f;       // องศา/วินาที
    const float Flatness = 0.42f;     // พื้นมองเฉียง วงเป็นวงรี (เท่าเงาใต้เท้า)
    static readonly Vector2[] OutlineDirections =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f),
    };
    static readonly int SquashId = Shader.PropertyToID("_Squash");
    static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    SpriteRenderer view, ring, floorGlow;
    SpriteRenderer[] outline;
    MaterialPropertyBlock block;
    float phase, nextSpark, fade = 1f;

    public static bool Roll(MonsterData data, int anomaliesSoFar) =>
        data != null && data.monsterPrefab != null && data.monsterPrefab.GetComponent<BossHealthHudLink>() == null
        && anomaliesSoFar < MaxPerRoom && Random.value < Chance;

    public static Kind RandomKind() => (Kind)Random.Range(0, 3);

    public static Color ColorOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.Colossal: return new Color(1f, 0.8f, 0.25f);
            case Kind.Frenzied: return new Color(1f, 0.25f, 0.25f);
            default: return new Color(0.6f, 0.8f, 1f);
        }
    }

    // คำอธิบายสั้น ๆ (การ์ดตัวอย่างในคอนโซลทดสอบ) ตัวเลขตรงกับ Make ข้างล่าง
    public static string Describe(Kind kind)
    {
        switch (kind)
        {
            case Kind.Colossal: return "ตัวใหญ่ 1.35 เท่า เลือด x2.5 ตีแรง x1.5 ทนแรงกระแทก x1.8 เดินช้าลง";
            case Kind.Frenzied: return "โจมตีไม่มีหน่วง (ยิงไกลหน่วงเหลือ 35%) เดินเร็วขึ้น x1.25";
            default: return "รับดาเมจครึ่งเดียวจนกว่าจะเซครั้งแรก ทนแรงกระแทก x1.3";
        }
    }

    // เรียกทันทีหลังเสก (ก่อน Start ของมอน) ตัวคูณจะมีผลตั้งแต่เลือดเริ่มต้น
    public static AnomalyMonster Make(MonsterController monster, Kind kind)
    {
        var anomaly = monster.gameObject.AddComponent<AnomalyMonster>();
        anomaly.monster = monster;
        anomaly.kind = kind;
        switch (kind)
        {
            case Kind.Colossal:
                monster.transform.localScale *= ColossalSize;
                monster.HealthScale = 2.5f;
                monster.AttackScale = 1.5f;
                monster.PoiseScale = 1.8f;
                monster.SpeedScale = 0.85f;
                break;
            case Kind.Frenzied:
                var combat = monster.GetComponent<MonsterCombatActions>();
                bool ranged = combat != null && (combat.style == MonsterCombatActions.Style.Rifle
                              || combat.style == MonsterCombatActions.Style.RockAndSlam
                              || combat.style == MonsterCombatActions.Style.Root);
                monster.AttackCooldownScale = ranged ? 0.35f : 0f;
                monster.SpeedScale = 1.25f;
                break;
            default:
                monster.ArmorScale = 0.5f;
                monster.PoiseScale = 1.3f;
                monster.Staggered += anomaly.BreakArmor;
                break;
        }
        return anomaly;
    }

    void Start()
    {
        if (monster == null) monster = GetComponent<MonsterController>();
        view = GetComponent<SpriteRenderer>();
        Color color = ColorOf(kind);
        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(lossy.x)), sy = Mathf.Max(0.01f, Mathf.Abs(lossy.y));

        if (view != null) BuildOutline(sx, sy);

        // พื้นใต้เท้า: ขอบล่างของตัวชน (ที่เดียวกับเงา) วงเส้นประหมุนอยู่ในตัวแม่ที่บีบแบนไว้ หมุนแล้วยังเป็นวงรีบนพื้น
        Collider2D body = null;
        foreach (var col in GetComponents<Collider2D>()) if (!col.isTrigger) { body = col; break; }
        Vector2 feet = body != null ? new Vector2(body.bounds.center.x, body.bounds.min.y) : (Vector2)transform.position;
        float width = Mathf.Clamp(body != null ? body.bounds.size.x * 1.6f : 1.2f, 0.9f, 3.2f);
        var floor = new GameObject("AnomalyFloor").transform;
        floor.SetParent(transform, false);
        floor.position = feet + Vector2.up * 0.02f;
        floor.localScale = new Vector3(1f / sx, Flatness / sy, 1f);
        int floorLayer = SortingLayer.NameToID("bg2");
        floorGlow = EchoFx.Layer(floor, "AnomalyGlow", ProceduralSprites.Glow, floorLayer, 1, Color.clear);
        floorGlow.transform.localScale = Vector3.one * width * 1.5f;
        ring = EchoFx.Layer(floor, "AnomalyRing", ProceduralSprites.DashedRing, floorLayer, 3, Color.clear);
        ring.transform.localScale = Vector3.one * width;

        ImpactSparks.Spawn(view != null ? (Vector2)view.bounds.center : (Vector2)transform.position, color, 12, Vector2.zero, 4f);
    }

    // ขอบเรืองแบบพิกเซล: สำเนาภาพตัวมอน 8 ชิ้นเยื้องออกรอบตัวนิดเดียว ย้อมสีทึบด้วย shader เดียวกับตอนกะพริบ อยู่หลังตัวจริง
    // ความหนา ~1.5 เม็ดพิกเซลของภาพ (อย่างน้อย 0.03 หน่วย ภาพความละเอียดสูงจะได้ยังเห็นขอบ)
    void BuildOutline(float sx, float sy)
    {
        var fx = MonsterFx.SharedMaterial;
        if (fx == null || view.sprite == null) return;
        float texel = sx / view.sprite.pixelsPerUnit;
        float thickness = Mathf.Max(texel * 1.5f, 0.03f);
        block = new MaterialPropertyBlock();
        outline = new SpriteRenderer[OutlineDirections.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            var copy = EchoFx.Layer(transform, "AnomalyOutline", view.sprite, view.sortingLayerID, view.sortingOrder - 1, Color.white);
            copy.sharedMaterial = fx;
            Vector2 offset = OutlineDirections[i] * thickness;
            copy.transform.localPosition = new Vector3(offset.x / sx, offset.y / sy, 0f);
            outline[i] = copy;
        }
    }

    void OnEnable() => MonsterController.Died += OnDied;
    void OnDisable() => MonsterController.Died -= OnDied;

    void Update()
    {
        if (monster == null) return;
        if (!monster.IsAlive) fade = Mathf.Max(0f, fade - Time.deltaTime * 4f);
        phase += Time.deltaTime;
        Color color = ColorOf(kind);
        bool armorGone = kind == Kind.Armored && monster.ArmorScale >= 1f;
        float strength = (armorGone ? 0.3f : 1f) * fade;
        float pulse = 0.5f + 0.5f * Mathf.Sin(phase * (kind == Kind.Frenzied ? 9f : 4f));
        if (ring != null)
        {
            ring.color = new Color(color.r, color.g, color.b, (0.55f + 0.3f * pulse) * strength);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, -phase * RingSpin);
        }
        if (floorGlow != null) floorGlow.color = new Color(color.r, color.g, color.b, (0.18f + 0.1f * pulse) * strength);

        // ประกายเล็ก ๆ ลอยขึ้นจากตัวเป็นระยะ (คลั่ง = ถ่านแดงถี่กว่า)
        if (fade < 1f || view == null || Time.time < nextSpark) return;
        nextSpark = Time.time + (kind == Kind.Frenzied ? 0.15f : 0.4f) * Random.Range(0.7f, 1.3f);
        Bounds b = view.bounds;
        var at = new Vector2(Mathf.Lerp(b.min.x, b.max.x, Random.Range(0.2f, 0.8f)), Mathf.Lerp(b.min.y, b.center.y, Random.value));
        ImpactSparks.Spawn(at, Color.Lerp(color, Color.white, 0.35f), 1, Vector2.up, kind == Kind.Frenzied ? 2.2f : 1.4f, 20f);
    }

    // ขอบตามตัวจริงทุกเฟรม: เฟรมภาพ พลิกซ้ายขวา
    // ไม่ส่งท่าหายใจ/เด้ง (_Squash) ให้สำเนา: สำเนา 8 ชิ้นค่าเหมือนกันถูกรวมวาดเป็นก้อนเดียวในพิกัดโลก
    // ค่ายืดจะไปยืดรอบจุดกลางฉากแทนเท้ามอน ขอบทั้งชุดเลื่อนหลุดตัว (ต่างจากตัวจริงแค่ไม่ถึงเม็ดพิกเซล)
    static readonly Vector4 NoSquash = new Vector4(1f, 1f, 0f, 0f);

    void LateUpdate()
    {
        if (outline == null || view == null) return;
        Color color = ColorOf(kind);
        bool armorGone = kind == Kind.Armored && monster != null && monster.ArmorScale >= 1f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(phase * (kind == Kind.Frenzied ? 9f : 4f));
        float alpha = (armorGone ? 0.25f : 0.65f + 0.35f * pulse) * fade * view.color.a;
        bool show = view.enabled && alpha > 0.01f;
        foreach (var copy in outline)
        {
            if (copy == null) continue;
            copy.enabled = show;
            if (!show) continue;
            copy.sprite = view.sprite;
            copy.flipX = view.flipX;
            copy.color = new Color(1f, 1f, 1f, alpha);
            // อ่านค่าเดิมของสำเนาก่อน (มีภาพของสไปรต์อยู่ในนั้น) ใช้ชุดเปล่าทับ ภาพจะหาย เหลือแผ่นสีทึบทรงกรอบภาพ
            copy.GetPropertyBlock(block);
            block.SetVector(SquashId, NoSquash);
            block.SetColor(FlashColorId, color);
            block.SetFloat(FlashAmountId, 1f);
            copy.SetPropertyBlock(block);
        }
    }

    // เกราะแตกตอนเซครั้งแรก: เศษเกราะกระจาย รับดาเมจปกติจากนี้
    void BreakArmor(MonsterController target)
    {
        if (target != monster || monster.ArmorScale >= 1f) return;
        monster.ArmorScale = 1f;
        monster.Staggered -= BreakArmor;
        Vector2 at = view != null ? (Vector2)view.bounds.center : (Vector2)transform.position;
        ImpactSparks.Spawn(at, new Color(0.75f, 0.85f, 1f), 18, Vector2.zero, 5f);
        ImpactSparks.Spawn(at, Color.white, 8, Vector2.zero, 3f);
        CameraFollow.Shake(0.15f, 0.15f);
    }

    void OnDied(MonsterController dead)
    {
        if (dead != monster) return;
        var map = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
        var loot = map != null ? map.chestLoot : null;
        if (loot == null || loot.coinSprite == null) return;
        Transform parent = monster.currentRoom != null ? monster.currentRoom.transform : transform.parent;
        Vector2 origin = view != null ? (Vector2)view.bounds.center : (Vector2)transform.position;
        int coins = Random.Range(CoinMin, CoinMax + 1);
        float start = Random.Range(0f, 360f);
        for (int i = 0; i < coins; i++)
        {
            var coin = LootPickup.Create(LootPickup.Kind.Coin, 1f, loot.coinSprite, loot.coinSize, parent, origin);
            float angle = (start + 360f / coins * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            coin.Toss(origin, LootPlacement.Landing(origin, dir, Random.Range(0.7f, 1.3f), transform));
        }
        ImpactSparks.Spawn(origin, new Color(1f, 0.85f, 0.3f), 10, Vector2.up, 4f);
    }
}
