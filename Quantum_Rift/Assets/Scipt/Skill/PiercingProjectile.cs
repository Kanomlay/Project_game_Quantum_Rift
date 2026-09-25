using System.Collections.Generic;
using UnityEngine;

// ลูกศรทะลวงมิติ: บินตรงไม่หยุด ทำดาเมจศัตรูทุกตัวที่ผ่าน (ตัวละครละครั้ง) ทะลุกำแพงได้
// หมดระยะแล้วเล่นภาพสลายตรงจุดนั้น
// คลื่นพลังของอาวุธตำนานใช้ตัวเดียวกัน แต่ส่ง blockedByWalls = true ให้หยุดที่กำแพง
public sealed class PiercingProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed, remaining, damage, radius, scale;
    private Vector2 contentOffset;
    private Sprite[] flyFrames, fadeFrames;
    private SpriteRenderer view;
    private readonly HashSet<Component> hit = new HashSet<Component>();
    private float animTime;
    private bool blockedByWalls;
    private const float WallProbeRadius = 0.2f; // กำแพงเช็คแค่แกนกลาง คลื่นใหญ่ ๆ จะได้ไม่ชนผนังข้างทางเดินแคบ
    private float trailInterval, trailLife, nextTrail;

    public static PiercingProjectile Spawn(Vector2 position, Vector2 direction, float speed, float lifetime,
                                           float damage, float radius, Sprite[] flyFrames, Sprite[] fadeFrames, float scale,
                                           Vector2 contentOffset = default, bool blockedByWalls = false)
    {
        var go = new GameObject("PiercingProjectile");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, SkillCombat.Angle(direction)));
        go.transform.localScale = new Vector3(scale, scale, 1f);

        // เลื่อนภาพให้กึ่งกลางลูกศรตรงจุดตรวจโดน (ภาพวาดไว้ต่ำกว่ากลางช่องนิดหน่อย)
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = -(Vector3)contentOffset;

        var projectile = go.AddComponent<PiercingProjectile>();
        projectile.contentOffset = contentOffset;
        projectile.view = sprite.AddComponent<SpriteRenderer>();
        projectile.view.sortingLayerName = "Effect";
        projectile.view.sortingOrder = 6;
        projectile.direction = direction.normalized;
        projectile.speed = speed;
        projectile.remaining = lifetime;
        projectile.damage = damage;
        projectile.radius = radius;
        projectile.flyFrames = flyFrames;
        projectile.fadeFrames = fadeFrames;
        projectile.scale = scale;
        projectile.blockedByWalls = blockedByWalls;
        if (flyFrames != null && flyFrames.Length > 0) projectile.view.sprite = flyFrames[0];
        return projectile;
    }

    // ทิ้งเงาภาพค้างไว้ตามทางที่บิน (คลื่นพลังอาวุธตำนาน)
    public PiercingProjectile WithTrail(float interval, float lifetime)
    {
        trailInterval = interval;
        trailLife = lifetime;
        return this;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        // หยุดที่กำแพง: หาระยะถึงกำแพงด้วยแกนกลางเส้นเล็ก ๆ ก่อน แล้วทำดาเมจเฉพาะช่วงก่อนถึงกำแพง
        if (blockedByWalls)
        {
            foreach (var probe in Physics2D.CircleCastAll(transform.position, WallProbeRadius, direction, step))
            {
                if (!IsWall(probe.collider)) continue;
                step = probe.distance;
                remaining = 0f;
                break;
            }
        }

        // กวาดตามทางทั้งช่วงที่บินในเฟรมนี้ ศัตรูตัวบาง ๆ จะไม่หลุด
        foreach (var cast in Physics2D.CircleCastAll(transform.position, radius, direction, step))
            SkillCombat.Damage(cast.collider, damage, hit);

        transform.position += (Vector3)(direction * step);

        if (trailInterval > 0f && Time.time >= nextTrail)
        {
            nextTrail = Time.time + trailInterval;
            SpriteGhost.Spawn(view, trailLife, 0.45f);
        }

        animTime += Time.deltaTime;
        if (flyFrames != null && flyFrames.Length > 0)
            view.sprite = flyFrames[(int)(animTime * 12f) % flyFrames.Length];

        if ((remaining -= Time.deltaTime) <= 0f)
        {
            if (fadeFrames != null && fadeFrames.Length > 0)
                SkillVfx.Spawn(fadeFrames, transform.position, scale, SkillCombat.Angle(direction), 14f, contentOffset);
            Destroy(gameObject);
        }
    }

    // กำแพง ประตูห้อง สิ่งกีดขวาง (ไม่ใช่ trigger และไม่ได้ติด Rigidbody ที่ขยับได้)
    private static bool IsWall(Collider2D other)
    {
        if (other == null || other.isTrigger) return false;
        var body = other.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }
}
