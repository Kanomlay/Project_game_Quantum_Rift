using System.Collections.Generic;
using UnityEngine;

// ตัวช่วยที่สกิลทุกท่าใช้ร่วมกัน: ทิศเมาส์ และการสร้างดาเมจเป็นวงใส่ศัตรู
// ตรวจทุก layer แล้วดูจากคอมโพเนนต์ (แบบเดียวกับ PlayerProjectile) บอส Architect อยู่คนละ layer กับมอนสเตอร์
public static class SkillCombat
{
    public static Vector2 MouseWorld()
    {
        var cam = Camera.main;
        return cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
    }

    public static Vector2 AimFrom(Vector2 origin)
    {
        Vector2 delta = MouseWorld() - origin;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }

    // กลางลำตัวผู้เล่น: ตัวละครชุดนี้มีจุดอ้างอิง (pivot) อยู่ที่เท้า ถ้าเกิดเอฟเฟกต์ที่ transform จะจมลงไปอยู่ใต้ตัว
    // ใช้กลาง collider ตัว (ไม่ใช่ trigger) ซึ่งครอบลำตัวพอดี
    public static Vector2 BodyCenter(GameObject player)
    {
        foreach (var collider in player.GetComponents<Collider2D>())
            if (!collider.isTrigger && collider.enabled) return collider.bounds.center;
        return player.transform.position;
    }

    public static float Angle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    // ดาเมจทุกตัวในวง ตัวละครละครั้ง (ส่ง alreadyHit มาถ้าเรียกซ้ำหลายเฟรมในท่าเดียว) คืนจำนวนตัวที่โดน
    public static int DamageArea(Vector2 center, float radius, float damage, HashSet<Component> alreadyHit = null)
    {
        int count = 0;
        foreach (var other in Physics2D.OverlapCircleAll(center, radius))
            if (Damage(other, damage, alreadyHit)) count++;
        return count;
    }

    public static bool Damage(Collider2D other, float damage, HashSet<Component> alreadyHit = null)
    {
        if (other == null) return false;

        var monster = other.GetComponentInParent<MonsterController>();
        if (monster != null)
        {
            if (!monster.IsAlive) return false;
            if (alreadyHit != null && !alreadyHit.Add(monster)) return false;
            monster.TakeDamage(damage);
            return true;
        }

        var architect = other.GetComponentInParent<ArchitectBossHealth>();
        if (architect != null)
        {
            if (alreadyHit != null && !alreadyHit.Add(architect)) return false;
            architect.TakeDamage(damage);
            return true;
        }

        return false;
    }

    // มอนสเตอร์ที่ยังไม่ตายในวง (ใช้หาเป้าให้โดรน และสตันจากกับดัก)
    public static List<MonsterController> MonstersIn(Vector2 center, float radius)
    {
        var found = new List<MonsterController>();
        foreach (var other in Physics2D.OverlapCircleAll(center, radius))
        {
            var monster = other.GetComponentInParent<MonsterController>();
            if (monster != null && monster.IsAlive && !found.Contains(monster)) found.Add(monster);
        }
        return found;
    }
}
