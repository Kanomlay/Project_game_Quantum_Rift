using UnityEngine;

// เม็ดพลังงานสีฟ้า: กระเด็นออกจากจุดที่แตก ลอยค้างแป๊บหนึ่ง แล้วพุ่งเข้าหาผู้เล่นเอง ถึงตัวแล้วฟื้นพลังงานเม็ดละ energy หน่วย
// (สร้างภาพเองในโค้ด ไม่ต้องมีไฟล์ภาพ ไม่ต้องเดินไปเก็บ)
public sealed class ManaMotes : MonoBehaviour
{
    static readonly Color GlowColor = new Color(0.3f, 0.75f, 1f, 0.75f);
    static readonly Color CoreColor = new Color(0.8f, 0.95f, 1f);
    const float BurstTime = 0.4f;   // ช่วงกระเด็นออกก่อนเริ่มบินเข้าหาผู้เล่น
    const float MaxSpeed = 14f;
    const float Acceleration = 30f;
    const float AbsorbDistance = 0.3f;
    const float MaxLifetime = 4f;   // บินนานเกินนี้ (ติดมุม/ผู้เล่นวาร์ป) ก็ถือว่าเก็บได้เลย

    Transform player;
    PlayerStats stats;
    Vector2 velocity;
    float age, homeAt, size;
    int energy;
    SpriteRenderer glow;

    public static void Spawn(Vector2 at, int count, int energyEach = 1)
    {
        if (count <= 0) return;
        var hero = GameObject.FindGameObjectWithTag("Player");
        if (hero == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("ManaMote");
            go.transform.position = at + Random.insideUnitCircle * 0.15f;
            var mote = go.AddComponent<ManaMotes>();
            mote.player = hero.transform;
            mote.stats = hero.GetComponent<PlayerStats>();
            mote.energy = energyEach;
            mote.velocity = Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
            mote.homeAt = BurstTime + Random.Range(0f, 0.2f); // ออกบินไม่พร้อมกัน ดูเป็นสาย
            mote.size = Random.Range(0.85f, 1.1f);

            mote.glow = Layer(go.transform, ProceduralSprites.Glow, GlowColor, 0.45f * mote.size, 6);
            Layer(go.transform, ProceduralSprites.Disc, CoreColor, 0.12f * mote.size, 7);
        }
    }

    static SpriteRenderer Layer(Transform parent, Sprite sprite, Color color, float scale, int order)
    {
        var go = new GameObject("Layer");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale / Mathf.Max(0.01f, sprite.bounds.size.x);
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.color = color;
        view.sortingLayerName = "Effect";
        view.sortingOrder = order;
        return view;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // หยุดเกม/เลือกพร เม็ดค้างอยู่กับที่
        age += dt;

        if (player == null || stats == null || stats.isDead)
        {
            Destroy(gameObject);
            return;
        }

        // ขอบเรืองกระพริบเบา ๆ
        float pulse = 1f + 0.12f * Mathf.Sin(age * 18f);
        glow.transform.localScale = Vector3.one * 0.45f * size * pulse / Mathf.Max(0.01f, glow.sprite.bounds.size.x);

        Vector2 at = transform.position;
        if (age < homeAt)
        {
            velocity *= Mathf.Max(0f, 1f - 6f * dt); // กระเด็นแล้วชะลอลอยค้าง
        }
        else
        {
            Vector2 target = SkillCombat.BodyCenter(player.gameObject);
            Vector2 to = target - at;
            if (to.magnitude <= AbsorbDistance || age >= MaxLifetime)
            {
                Absorb(target);
                return;
            }
            // เลี้ยวเข้าหาผู้เล่นและเร่งขึ้นเรื่อย ๆ
            float speed = Mathf.Min(MaxSpeed, velocity.magnitude + Acceleration * dt);
            Vector2 heading = Vector2.Lerp(velocity.normalized, to.normalized, 12f * dt).normalized;
            velocity = heading * Mathf.Max(speed, 3f);
            if (velocity.magnitude * dt >= to.magnitude)
            {
                Absorb(target);
                return;
            }
        }
        transform.position = at + velocity * dt;
    }

    void Absorb(Vector2 target)
    {
        stats.RestoreEnergy(energy);
        Sfx.Play(SfxId.PickupMana);
        ImpactSparks.Spawn(target, CoreColor, 2, Vector2.zero, 2f);
        Destroy(gameObject);
    }
}
