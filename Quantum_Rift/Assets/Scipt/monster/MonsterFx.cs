using UnityEngine;

// หน้าตามอนสเตอร์ให้ดูมีชีวิต (MonsterController ใส่ให้เองตอนเริ่ม ไม่ต้องตั้งใน prefab)
// - เงาวงรีใต้เท้า (GroundShadow) จางตามตัวตอนโผล่/ตาย
// - โดนตี: กะพริบขาว + ยุบตัว   ง้างโจมตี: วาบสีส้มอ่อน + ย่อตัวเตรียมพุ่ง
// - ยืนเฉย: หายใจยืดหดเบา ๆ   เดิน: ตัวเด้งตามก้าว (เงาหดตามนิดหน่อย)   พุ่ง/กระเด็นแล้วหยุด: ยุบตัวเหมือนลงพื้น
// - ตาย: วาบขาว ประกายแตก แล้วสลายเป็นเม็ดพิกเซล (MonsterController เรียก BeginDeath / Dissolve)
// ภาพเปลี่ยนผ่าน shader QuantumRift/SpriteFX (Resources/SpriteFX.mat) ไม่แตะ transform/collider
// หาไฟล์ material ไม่เจอ = ใช้ภาพเดิม (มีแค่เงา) ระบบเดิมยังกะพริบแดง/จางหายเหมือนก่อน
[DisallowMultipleComponent]
public sealed class MonsterFx : MonoBehaviour
{
    const float BobHeight = 0.06f;  // ตัวลอยขึ้นสูงสุดต่อก้าว (หน่วยในฉาก)
    const float StepRate = 11f;     // จังหวะก้าวที่ความเร็วเดินปกติ
    const float LandSpeed = 4.5f;   // เร็วกว่านี้แล้วหยุดทันที = ลงพื้นหลังพุ่ง/กระเด็น
    static readonly Color WarnColor = new Color(1f, 0.82f, 0.45f);
    static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    static readonly int SquashId = Shader.PropertyToID("_Squash");
    static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    static readonly int DissolveColorId = Shader.PropertyToID("_DissolveColor");
    static readonly int PixelGridId = Shader.PropertyToID("_PixelGrid");

    public static Material SharedMaterial => FxMaterial; // ผู้เล่นใช้ shader เดียวกัน (PlayerHitFx)

    static Material material;
    static bool materialLoaded;
    static Material FxMaterial
    {
        get
        {
            if (!materialLoaded)
            {
                materialLoaded = true;
                material = Resources.Load<Material>("SpriteFX");
                if (material == null) Debug.LogWarning("ไม่เจอ Resources/SpriteFX.mat มอนสเตอร์จะไม่มีเอฟเฟกต์กะพริบ/สลาย");
            }
            return material;
        }
    }

    SpriteRenderer view, shadow;
    MonsterController monster;
    MonsterCombatActions combat; // ค่าหน้าตาเฉพาะตัว (ลอย / ประกายรอบตัว)
    float hoverPhase, hoverLift, nextAmbient;
    MaterialPropertyBlock block;
    Vector3 shadowScale;
    float shadowAlpha;
    Vector2 lastPosition;
    float lastSpeed, stepPhase, breathPhase, hop;
    Vector2 pose = Vector2.one, kick = Vector2.one;
    float flash, flashTime = 0.1f, flashStrength;
    Color flashColor = Color.white;
    float dissolve, nextEmber;
    Color dissolveColor = Color.white;
    bool dying;

    public bool Active { get; private set; }

    public static MonsterFx Attach(MonsterController owner)
    {
        var fx = owner.GetComponent<MonsterFx>();
        if (fx == null) fx = owner.gameObject.AddComponent<MonsterFx>();
        fx.Setup(owner);
        return fx;
    }

    void Setup(MonsterController owner)
    {
        if (monster != null) return;
        monster = owner;
        view = GetComponent<SpriteRenderer>();
        combat = GetComponent<MonsterCombatActions>();
        hoverPhase = Random.value * Mathf.PI * 2f;
        block = new MaterialPropertyBlock();
        if (view != null && FxMaterial != null)
        {
            view.sharedMaterial = FxMaterial;
            Active = true;
        }
        shadow = GroundShadow.Attach(gameObject);
        shadowScale = shadow.transform.localScale;
        shadowAlpha = shadow.color.a;
        lastPosition = transform.position;
        breathPhase = Random.value * Mathf.PI * 2f; // ไม่หายใจพร้อมกันทั้งฝูง
    }

    public void Hit() => Flash(Color.white, 1f, 0.12f, new Vector2(1.16f, 0.84f));
    public void Warn() => Warn(WarnColor);
    public void Warn(Color color) => Flash(color, 0.6f, 0.25f, new Vector2(1.07f, 0.9f));

    // เรืองสีค้างไว้ (เรียกทุกเฟรมตราบที่ต้องการ หยุดเรียกแล้วจางเอง) เช่น Zero Husk ก่อนระเบิด
    public void Glow(Color color, float amount)
    {
        if (dying) return;
        flashColor = color;
        flashStrength = Mathf.Clamp01(amount);
        flashTime = 0.1f;
        flash = 1f;
    }

    void Flash(Color color, float strength, float time, Vector2 squash)
    {
        if (dying) return;
        flashColor = color;
        flashStrength = strength;
        flashTime = time;
        flash = 1f;
        kick = squash;
    }

    public void BeginDeath(Color color)
    {
        dying = true;
        dissolveColor = color;
        flashColor = Color.white;
        flashStrength = 1f;
        flashTime = 0.3f;
        flash = 1f;
        kick = new Vector2(1.2f, 0.8f);
        if (view != null) ImpactSparks.Spawn(view.bounds.center, color, 12, Vector2.zero, 4f);
    }

    public void Dissolve(float k) => dissolve = Mathf.Clamp01(k);

    void LateUpdate()
    {
        if (view == null) return;
        float dt = Time.deltaTime;
        if (dt > 0f) Animate(dt); // หยุดเกม/เลือกพร: ท่าค้างไว้

        if (Active)
        {
            view.GetPropertyBlock(block);
            float lift = (hop * BobHeight + hoverLift) / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            block.SetVector(SquashId, new Vector4(pose.x * kick.x, pose.y * kick.y, 0f, lift));
            block.SetColor(FlashColorId, flashColor);
            block.SetFloat(FlashAmountId, flashStrength * Mathf.Clamp01(flash * 1.5f)); // ขาวเต็มช่วงแรกแล้วค่อยจาง
            block.SetFloat(DissolveId, dissolve);
            block.SetColor(DissolveColorId, dissolveColor);
            Texture tex = view.sprite != null ? view.sprite.texture : null;
            if (tex != null) block.SetVector(PixelGridId, new Vector4(tex.width, tex.height, 0f, 0f)); // สลายทีละเม็ดพิกเซลของภาพจริง
            view.SetPropertyBlock(block);
        }
        if (shadow != null)
        {
            shadow.color = new Color(0f, 0f, 0f, shadowAlpha * view.color.a * (1f - dissolve));
            shadow.transform.localScale = shadowScale * (1f - 0.12f * hop - 0.8f * hoverLift); // ลอยสูง เงาเล็กลง
        }
    }

    void Animate(float dt)
    {
        Vector2 position = transform.position;
        float speed = (position - lastPosition).magnitude / dt;
        lastPosition = position;
        if (!dying && lastSpeed > LandSpeed && speed < LandSpeed * 0.3f) kick = new Vector2(1.18f, 0.82f);
        lastSpeed = speed;

        flash = Mathf.Max(0f, flash - dt / flashTime);
        kick = Vector2.Lerp(kick, Vector2.one, 1f - Mathf.Exp(-12f * dt));

        bool floating = combat != null && combat.floating;
        if (floating && !dying)
        {
            // วิญญาณลอย: ขึ้นลงช้า ๆ ตลอด ไม่เด้งเป็นก้าว
            hoverPhase += dt * 2.2f;
            hoverLift = 0.08f + 0.06f * Mathf.Sin(hoverPhase);
        }
        else hoverLift = Mathf.MoveTowards(hoverLift, 0f, dt * 0.5f);

        if (dying || (monster != null && monster.IsStunned))
        {
            pose = Vector2.one;
            hop = 0f;
        }
        else if (speed > 0.25f && !floating)
        {
            stepPhase += dt * StepRate * Mathf.Clamp(speed / 1.6f, 0.6f, 1.6f);
            hop = Mathf.Abs(Mathf.Sin(stepPhase));
            pose = new Vector2(1f - 0.03f * hop, 1f + 0.04f * hop);
        }
        else
        {
            breathPhase += dt * 2.4f;
            float breath = Mathf.Sin(breathPhase);
            hop = Mathf.MoveTowards(hop, 0f, dt * 4f);
            pose = new Vector2(1f - 0.012f * breath, 1f + 0.025f * breath);
        }

        // ประกายรอบตัวตลอดเวลา (ใบไม้ร่วงจาก Forest Wraith, เศษดินจาก Rootlings)
        if (!dying && combat != null && combat.ambientEvery > 0f && Time.time >= nextAmbient)
        {
            nextAmbient = Time.time + combat.ambientEvery * Random.Range(0.6f, 1.4f);
            Bounds b = view.bounds;
            float y = combat.ambientFromTop ? Random.Range(b.center.y, b.max.y) : Random.Range(b.min.y, b.center.y);
            ImpactSparks.Spawn(new Vector2(Random.Range(b.min.x, b.max.x), y), combat.ambientColor, 1, Vector2.down, 0.9f, 35f);
        }

        // ตอนสลายมีประกายลอยขึ้นจากตัว
        if (dying && dissolve > 0f && dissolve < 1f && Time.time >= nextEmber)
        {
            nextEmber = Time.time + 0.05f;
            Bounds b = view.bounds;
            ImpactSparks.Spawn(new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y)), dissolveColor, 1, Vector2.up, 2.5f, 20f);
        }
    }
}
