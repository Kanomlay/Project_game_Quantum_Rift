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
    SpriteRenderer glow;
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
        body.radius = radius;

        core.glow = EchoFx.Layer(go.transform, "CoreGlow", ProceduralSprites.Glow, SortingLayer.NameToID("Effect"), 3, Color.clear);
        core.glow.transform.localScale = Vector3.one * radius * 3f;
        EchoFx.Shockwave(at, Violet, radius * 3f, 0.5f);
        return core;
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
    }

    public void Close()
    {
        if (heart != null) heart.localScale = heartScale;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (heart != null) heart.localScale = heartScale;
    }
}
