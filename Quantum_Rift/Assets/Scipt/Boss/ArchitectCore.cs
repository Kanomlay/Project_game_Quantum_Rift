using UnityEngine;

// แกนหัวใจรังมิติ (ฉากจบของ Architect): เลือดบอสหมดแล้วร่างสลายเข้าหัวใจกลางห้อง หัวใจเปิดเปลือกให้ตี
// ผู้เล่นตีแตกได้ผ่านระบบเดียวกับกล่อง/กำแพง (IBreakable) ตีแตกทันเวลา = บอสตายจริง
// ภาพ: หัวใจเดิมของสนาม (EnergyHeart) เต้นแรงขึ้น + แสงเรืองซ้อน ไม่แตะสีของหัวใจ (สนามปรับสีเองทุกเฟรม)
// ตัวชนเป็น trigger: อาวุธ/กระสุนผู้เล่นหาเจอ แต่ไม่ขวางทางเดินและกระสุนบอสที่ยิงออกจากหัวใจ
public sealed class ArchitectCore : MonoBehaviour, IBreakable
{
    static readonly Color Violet = new Color(0.78f, 0.42f, 1f);

    public bool IsBroken { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }

    Transform heart;
    Vector3 heartScale;
    SpriteRenderer glow, pointer;
    float flash, age;

    public static ArchitectCore Open(Vector2 at, float radius, float health, float maxHealth, Transform heart)
    {
        var go = new GameObject("ArchitectCore");
        go.transform.position = at;
        var core = go.AddComponent<ArchitectCore>();
        core.Health = health;
        core.MaxHealth = Mathf.Max(1f, maxHealth);
        core.heart = heart;
        if (heart != null) core.heartScale = heart.localScale;

        var body = go.AddComponent<CircleCollider2D>();
        body.isTrigger = true;
        // ครอบทั้งบ่อและร่างเงาบอสที่ลอยอยู่เหนือบ่อ (ผู้เล่นมักตีที่ตัวบอส) ตัวชนเดียว การโจมตีครั้งเดียวจึงไม่นับซ้ำ
        body.radius = radius + 0.8f;
        body.offset = Vector2.up * 0.8f;

        core.glow = EchoFx.Layer(go.transform, "CoreGlow", ProceduralSprites.Glow, SortingLayer.NameToID("Effect"), 3, Color.clear);
        core.glow.transform.localScale = Vector3.one * radius * 3f;
        core.pointer = EchoFx.Layer(null, "CorePointer", ProceduralSprites.Sector(24f), SortingLayer.NameToID("Effect"), 60, Color.clear);
        EchoFx.Shockwave(at, Violet, radius * 3f, 0.5f);
        return core;
    }

    // คอนโซลทดสอบ: ตั้งเลือดแกน (เหลือ 1 = ตีอีกครั้งเดียวแตก)
    public void SetHealthForTesting(float value)
    {
        if (!IsBroken) Health = Mathf.Clamp(value, 1f, MaxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken || amount <= 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        flash = 1f;
        DamageNumbers.Spawn((Vector2)transform.position + Vector2.up * 1.2f, amount, DamageNumbers.Kind.Enemy);
        ImpactSparks.Spawn(transform.position, Color.white, 5, Vector2.zero, 4f);
        if (Health > 0f) return;
        IsBroken = true;
        CameraFollow.Shake(0.5f, 0.5f);
        HitStop.Freeze(0.15f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        flash = Mathf.Max(0f, flash - dt * 6f);
        // ยิ่งเลือดแกนน้อย ยิ่งเต้นถี่
        float left = Health / MaxHealth;
        float beat = Mathf.Abs(Mathf.Sin(age * Mathf.Lerp(9f, 4f, left)));
        if (heart != null) heart.localScale = heartScale * (1.1f + 0.12f * beat + 0.15f * flash);
        if (glow != null)
        {
            Color c = Color.Lerp(Violet, Color.white, flash);
            glow.color = new Color(c.r, c.g, c.b, 0.35f + 0.3f * beat + 0.3f * flash);
        }
        PointAt(pointer, transform.position, beat, Violet);
    }

    // เป้าอยู่นอกจอ: ลิ่มที่ขอบจอชี้ไปหา (ห้องบอสกว้างกว่าจอ) เข้ามาในจอแล้วหายไป ประตูออกหลังชนะ (CoreRiftPortal) ใช้ด้วย
    public static void PointAt(SpriteRenderer pointer, Vector2 target, float beat, Color color)
    {
        var cam = Camera.main;
        if (pointer == null || cam == null || !cam.orthographic) return;
        const float Margin = 1.3f;
        Vector2 center = cam.transform.position, to = target - center;
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        if (Mathf.Abs(to.x) < halfW && Mathf.Abs(to.y) < halfH)
        {
            pointer.color = Color.clear;
            return;
        }
        float fit = Mathf.Min((halfW - Margin) / Mathf.Max(0.001f, Mathf.Abs(to.x)), (halfH - Margin) / Mathf.Max(0.001f, Mathf.Abs(to.y)));
        var t = pointer.transform;
        t.position = center + to * fit;
        // ลิ่มของ Sector บานไปทาง +X จึงหมุนกลับหลังให้ปลายแหลมชี้ไปหาเป้า
        t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg + 180f);
        t.localScale = Vector3.one * (1.5f + 0.3f * beat);
        pointer.color = new Color(color.r, color.g, color.b, 0.75f + 0.25f * beat);
    }

    public void Close()
    {
        if (heart != null) heart.localScale = heartScale;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (pointer != null) Destroy(pointer.gameObject);
        if (heart != null) heart.localScale = heartScale;
    }
}
