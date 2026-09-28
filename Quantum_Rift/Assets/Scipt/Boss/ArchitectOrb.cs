using System.Collections.Generic;
using UnityEngine;

// กระสุนของ Architect of Collapse: ลูกเวทมนตร์ (ภาพ BulletSpin) และคลื่นเคียว (ภาพ ScytheWave)
// บินตรง ภาพวนเฟรมเอง โดนผู้เล่นแล้วแตก (คลื่นเคียวทะลุตัว โดนได้ครั้งเดียว) ชนกำแพง/สิ่งกีดขวางแล้วแตก
// วัดระยะถึงกลางตัวผู้เล่นเองแทน trigger ฟิสิกส์ (บอสยิงทีละหลายสิบลูก ไม่ต้องมี Rigidbody ทุกลูก)
// ติดป้าย IEnemyBullet: พรคมสลายมิติฟันลบได้ สนามชะลอกระสุนทำให้ช้าลง
public sealed class ArchitectOrb : MonoBehaviour, IEnemyBullet
{
    const float Fps = 12f;
    const float GrowTime = 0.15f;
    const float FadeTime = 0.2f;
    const float WallGrace = 0.12f;  // เพิ่งออกจากตัวบอส ยังไม่เช็คกำแพง
    static readonly List<ArchitectOrb> live = new List<ArchitectOrb>();
    static readonly Collider2D[] probe = new Collider2D[8];
    static ContactFilter2D probeFilter = new ContactFilter2D { useTriggers = false };

    Sprite[] frames;
    int firstFrame, lastFrame;
    SpriteRenderer view;
    Collider2D target;
    Transform owner;
    Vector2 velocity;
    float damage, hitRadius, scale, life, age, knockback;
    bool pierce, hitDone, popped;
    Color burst;

    // frames first..last วนซ้ำ (คลื่นเคียววนเฉพาะช่วงที่ใหญ่สุด) pierce = ทะลุผู้เล่นแล้วบินต่อ หันตามทิศบิน
    public static ArchitectOrb Fire(Sprite[] frames, int first, int last, Vector2 at, Vector2 velocity, float damage,
                                    float hitRadius, float scale, float life, bool pierce, float knockback,
                                    Color burst, Collider2D target, Transform owner)
    {
        if (frames == null || frames.Length == 0) return null;
        var go = new GameObject(pierce ? "ArchitectWave" : "ArchitectOrb");
        go.transform.position = at;
        if (pierce) go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        var orb = go.AddComponent<ArchitectOrb>();
        orb.view = go.AddComponent<SpriteRenderer>();
        orb.view.sortingLayerName = "Effect";
        orb.view.sortingOrder = 5;
        orb.frames = frames;
        orb.firstFrame = Mathf.Clamp(first, 0, frames.Length - 1);
        orb.lastFrame = Mathf.Clamp(last, orb.firstFrame, frames.Length - 1);
        orb.velocity = velocity;
        orb.damage = damage;
        orb.hitRadius = hitRadius;
        orb.scale = scale;
        orb.life = life;
        orb.pierce = pierce;
        orb.knockback = knockback;
        orb.burst = burst;
        orb.target = target;
        orb.owner = owner;
        orb.Show();
        return orb;
    }

    // ล้างกระสุนทั้งหมด (บอสแปลงร่าง / คำราม / ตาย)
    public static void ClearAll()
    {
        for (int i = live.Count - 1; i >= 0; i--)
            if (live[i] != null) live[i].Pop(true);
        live.Clear();
    }

    void OnEnable()
    {
        live.Add(this);
        EnemyBullets.Register(this);
    }

    void OnDisable()
    {
        live.Remove(this);
        EnemyBullets.Unregister(this);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || popped) return;
        age += dt;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position = (Vector2)transform.position + velocity * EnemyBullets.SpeedFactor(transform.position) * dt;
        transform.position = position;
        Show();

        if (!hitDone && target != null && target.enabled
            && ((Vector2)target.bounds.center - position).sqrMagnitude <= hitRadius * hitRadius)
        {
            ArchitectBossAI.HurtPlayer(target.GetComponentInParent<PlayerStats>(), damage, position, knockback);
            if (!pierce)
            {
                Pop(true);
                return;
            }
            hitDone = true;
        }
        if (age > WallGrace && HitsWall(position)) Pop(true);
    }

    bool HitsWall(Vector2 position)
    {
        int count = Physics2D.OverlapPoint(position, probeFilter, probe);
        for (int i = 0; i < count; i++)
        {
            var col = probe[i];
            if (!LootPlacement.IsSolid(col)) continue;
            if (owner != null && col.transform.IsChildOf(owner)) continue;
            if (col.GetComponentInParent<PlayerStats>() != null || col.GetComponentInParent<MonsterController>() != null) continue;
            return true;
        }
        return false;
    }

    void Show()
    {
        int span = lastFrame - firstFrame + 1;
        view.sprite = frames[firstFrame + Mathf.FloorToInt(age * Fps) % span];
        float grow = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(age / GrowTime));
        transform.localScale = Vector3.one * scale * grow;
        float left = life - age;
        view.color = new Color(1f, 1f, 1f, left < FadeTime ? Mathf.Clamp01(left / FadeTime) : 1f);
    }

    void Pop(bool sparks)
    {
        if (popped) return;
        popped = true;
        if (sparks) ImpactSparks.Spawn(transform.position, burst, pierce ? 6 : 4, Vector2.zero, 3f);
        Destroy(gameObject);
    }
}
