using System.Collections.Generic;
using UnityEngine;

// ลูกศรทะลวงมิติ: บินตรงไม่หยุด ทำดาเมจศัตรูทุกตัวที่ผ่าน (ตัวละครละครั้ง) ทะลุกำแพงได้
// หมดระยะแล้วเล่นภาพสลายตรงจุดนั้น
public sealed class PiercingProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed, remaining, damage, radius, scale;
    private Vector2 contentOffset;
    private Sprite[] flyFrames, fadeFrames;
    private SpriteRenderer view;
    private readonly HashSet<Component> hit = new HashSet<Component>();
    private float animTime;

    public static PiercingProjectile Spawn(Vector2 position, Vector2 direction, float speed, float lifetime,
                                           float damage, float radius, Sprite[] flyFrames, Sprite[] fadeFrames, float scale,
                                           Vector2 contentOffset = default)
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
        if (flyFrames != null && flyFrames.Length > 0) projectile.view.sprite = flyFrames[0];
        return projectile;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        // กวาดตามทางทั้งช่วงที่บินในเฟรมนี้ ศัตรูตัวบาง ๆ จะไม่หลุด
        foreach (var cast in Physics2D.CircleCastAll(transform.position, radius, direction, step))
            SkillCombat.Damage(cast.collider, damage, hit);

        transform.position += (Vector3)(direction * step);

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
}
