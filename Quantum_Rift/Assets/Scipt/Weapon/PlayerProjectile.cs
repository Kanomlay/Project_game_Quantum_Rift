using UnityEngine;

// กระสุน/ลูกธนูของผู้เล่น วิ่งเป็นเส้นตรงจนกว่าจะโดนศัตรู ชนกำแพง หรือหมดระยะ
//
// ไม่ใช้ collider ของตัวเอง แต่กวาด CircleCast ตามทางที่จะวิ่งในเฟรมนั้นแทน
// กระสุนเร็ว ๆ จะได้ไม่ทะลุมอนสเตอร์ตัวบางหรือกำแพงไปเฉย ๆ (แบบเดียวกับ MonsterAttackProjectile)
public sealed class PlayerProjectile : MonoBehaviour
{
    public float hitRadius = 0.12f;

    private Vector2 direction;
    private float speed;
    private float damage;
    private float remaining;
    private Transform owner;
    private bool spent;

    public void Launch(Transform shooter, Vector2 heading, float velocity, float lifetime, float power)
    {
        owner = shooter;
        direction = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.right;
        speed = velocity;
        remaining = lifetime;
        damage = power;

        // ภาพกระสุนทุกชิ้นวาดหันไปทางขวา หมุนให้หัวกระสุนชี้ทิศที่ยิง
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
                if (!monster.gameObject.activeInHierarchy) continue;
                monster.TakeDamage(damage);
                Expire();
                return;
            }

            // บอสแมพ 3 ใช้ระบบเลือดแยก (เหมือนที่ดาบเช็คใน WeaponController)
            var architect = other.GetComponentInParent<ArchitectBossHealth>();
            if (architect != null)
            {
                architect.TakeDamage(damage);
                Expire();
                return;
            }

            // ตัวตรวจจับห้อง พอร์ทัล พื้นอันตราย ฯลฯ เป็น trigger ไม่ควรกันกระสุน
            if (other.isTrigger) continue;
            if (other.GetComponentInParent<PlayerStats>() != null) continue;

            Expire(); // ที่เหลือถือเป็นกำแพง/ประตูห้อง/สิ่งกีดขวาง
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    private void Expire()
    {
        spent = true;
        Destroy(gameObject);
    }
}
