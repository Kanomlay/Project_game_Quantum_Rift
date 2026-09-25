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

            var monster = other.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                if (!monster.gameObject.activeInHierarchy || !monster.IsAlive) continue;
                if (explodeRadius <= 0f) monster.TakeDamage(damage);
                transform.position = hit.centroid;
                Expire();
                return;
            }

            // บอสแมพ 3 ใช้ระบบเลือดแยก (เหมือนที่ดาบเช็คใน WeaponController)
            var architect = other.GetComponentInParent<ArchitectBossHealth>();
            if (architect != null)
            {
                if (explodeRadius <= 0f) architect.TakeDamage(damage);
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
            SkillCombat.DamageArea(transform.position, explodeRadius, damage);
            if (explodeFrames != null && explodeFrames.Length > 0)
                SkillVfx.Spawn(explodeFrames, transform.position, explodeScale, 0f, 14f);
        }
        Destroy(gameObject);
    }
}
