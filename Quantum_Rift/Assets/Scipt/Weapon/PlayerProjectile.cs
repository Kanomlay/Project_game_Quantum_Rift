using System.Collections.Generic;
using UnityEngine;

// กระสุน/ลูกธนูของผู้เล่น วิ่งเป็นเส้นตรงจนกว่าจะโดนศัตรู ชนกำแพง หรือหมดระยะ
//
// ไม่ใช้ collider ของตัวเอง แต่กวาด CircleCast ตามทางที่จะวิ่งในเฟรมนั้นแทน
// กระสุนเร็ว ๆ จะได้ไม่ทะลุมอนสเตอร์ตัวบางหรือกำแพงไปเฉย ๆ (แบบเดียวกับ MonsterAttackProjectile)
//
// ความสามารถของอาวุธตำนาน (ตั้งผ่าน Configure):
// - ชิ่งกำแพง (ธนูยิงกระจาย): ชนกำแพงแล้วสะท้อนออกตามมุม ได้ bounces ครั้ง พร้อมประกายตรงจุดชิ่ง
// - ระเบิด (เครื่องยิงจรวด): โดนอะไรหรือหมดระยะแล้วระเบิดเป็นวง ทำดาเมจทุกตัวในวง
public sealed class PlayerProjectile : MonoBehaviour
{
    public float hitRadius = 0.12f;

    private Vector2 direction;
    private float speed;
    private float damage;
    private float remaining;
    private Transform owner;
    private bool spent;

    private int bouncesLeft;
    private Sprite[] bounceFrames;
    private float bounceScale = 0.5f;
    private float explodeRadius;
    private Sprite[] explodeFrames;
    private float explodeScale = 1f;
    private static readonly Color HitSparkColor = new Color(1f, 0.95f, 0.8f);

    // พร (ตั้งผ่าน BlessingManager.SetupProjectile)
    private object attack;                // การยิงครั้งไหน (คลื่นสะสมนับครั้งเดียวต่อการยิง)
    private int pierceLeft;               // กระสุนทะลุมิติ: ทะลุได้อีกกี่ตัว
    private float pierceDamageScale = 1f; // ตัวที่ทะลุไปโดนต่อ รับดาเมจตามสัดส่วนนี้
    private HashSet<Component> pierced;   // ตัวที่ทะลุผ่านมาแล้ว ไม่โดนซ้ำ

    public void Launch(Transform shooter, Vector2 heading, float velocity, float lifetime, float power)
    {
        owner = shooter;
        speed = velocity;
        remaining = lifetime;
        damage = power;
        Face(heading);
    }

    // ต่อจาก Launch: ใส่ความสามารถพิเศษตามอาวุธที่ยิง
    public void Configure(WeaponData data)
    {
        if (data == null) return;
        if (data.special == WeaponSpecial.Ricochet)
        {
            bouncesLeft = Mathf.Max(0, data.bounces);
            bounceFrames = data.specialFrames;
            bounceScale = data.specialScale;
        }
        else if (data.special == WeaponSpecial.Explosive)
        {
            explodeRadius = data.specialRadius;
            explodeFrames = data.specialFrames;
            explodeScale = data.specialScale;
            damage = data.SpecialDamage;
        }
    }

    public void SetAttack(object key) => attack = key;

    public void SetPierce(int count, float nextDamageScale)
    {
        pierceLeft = Mathf.Max(0, count);
        pierceDamageScale = nextDamageScale;
    }

    // โดนศัตรูแล้ว: ยังทะลุได้ก็บินต่อ (ดาเมจลดลง) ไม่งั้นจบ คืน true ถ้าบินต่อ
    private bool PierceThrough(Component target)
    {
        BlessingManager.OnAttackLanded(attack);
        if (pierceLeft <= 0) return false;
        pierceLeft--;
        damage *= pierceDamageScale;
        if (pierced == null) pierced = new HashSet<Component>();
        pierced.Add(target);
        return true;
    }

    // ภาพกระสุนทุกชิ้นวาดหันไปทางขวา หมุนให้หัวกระสุนชี้ทิศที่ยิง
    private void Face(Vector2 heading)
    {
        direction = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.right;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void Update()
    {
        if (spent) return;
        if ((remaining -= Time.deltaTime) <= 0f) { Expire(); return; }

        float step = speed * Time.deltaTime;

        // ผลลัพธ์เรียงจากใกล้ไปไกล กำแพงที่ขวางอยู่ก่อนจะหยุดกระสุนก่อนถึงศัตรูที่อยู่หลังกำแพง
        foreach (var hit in Physics2D.CircleCastAll(transform.position, hitRadius, direction, step))
        {
            var other = hit.collider;
            if (other == null) continue;
            if (owner != null && other.transform.IsChildOf(owner)) continue; // ไม่ยิงโดนตัวเอง

            var breakable = other.GetComponentInParent<BreakableProp>();
            if (breakable != null)
            {
                breakable.TakeDamage(damage);
                transform.position = hit.centroid;
                Expire();
                return;
            }

            var monster = other.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                if (!monster.gameObject.activeInHierarchy || !monster.IsAlive) continue;
                if (pierced != null && pierced.Contains(monster)) continue;
                if (explodeRadius <= 0f)
                {
                    monster.TakeDamage(damage);
                    ImpactSparks.Spawn(hit.point, HitSparkColor, 4, direction);
                    if (PierceThrough(monster)) continue;
                }
                transform.position = hit.centroid;
                Expire();
                return;
            }

            // บอสแมพ 3 ใช้ระบบเลือดแยก (เหมือนที่ดาบเช็คใน WeaponController)
            var architect = other.GetComponentInParent<ArchitectBossHealth>();
            if (architect != null)
            {
                if (pierced != null && pierced.Contains(architect)) continue;
                if (explodeRadius <= 0f)
                {
                    architect.TakeDamage(damage);
                    ImpactSparks.Spawn(hit.point, HitSparkColor, 4, direction);
                    if (PierceThrough(architect)) continue;
                }
                transform.position = hit.centroid;
                Expire();
                return;
            }

            // ตัวตรวจจับห้อง พอร์ทัล พื้นอันตราย ฯลฯ เป็น trigger ไม่ควรกันกระสุน
            if (other.isTrigger) continue;
            if (other.GetComponentInParent<PlayerStats>() != null) continue;

            // กำแพง/ประตูห้อง/สิ่งกีดขวาง: ชิ่งออกถ้ายังเหลือจำนวนชิ่ง ไม่งั้นจบ
            if (bouncesLeft > 0 && hit.normal.sqrMagnitude > 0.01f)
            {
                bouncesLeft--;
                transform.position = hit.centroid + hit.normal * 0.02f;
                Face(Vector2.Reflect(direction, hit.normal));
                if (bounceFrames != null && bounceFrames.Length > 0)
                    SkillVfx.Spawn(bounceFrames, hit.point, bounceScale, 0f, 18f);
                return;
            }

            transform.position = hit.centroid;
            Expire();
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    private void Expire()
    {
        if (spent) return;
        spent = true;
        if (explodeRadius > 0f)
        {
            int hits = SkillCombat.DamageArea(transform.position, explodeRadius, damage);
            if (hits > 0) BlessingManager.OnAttackLanded(attack);
            if (explodeFrames != null && explodeFrames.Length > 0)
                SkillVfx.Spawn(explodeFrames, transform.position, explodeScale, 0f, 14f);
            // ระเบิด: กล้องสั่นแรง เศษไฟกระจายรอบทิศ โดนศัตรูแล้วหยุดภาพชั่วขณะ
            CameraFollow.Shake(0.22f, 0.25f);
            ImpactSparks.Spawn(transform.position, new Color(1f, 0.62f, 0.25f), 14, Vector2.zero, 6f);
            if (hits > 0) HitStop.Freeze(0.05f);
        }
        Destroy(gameObject);
    }
}
