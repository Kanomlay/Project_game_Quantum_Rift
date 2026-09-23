using System.Collections;
using UnityEngine;

// โดรนจู่โจมของนักประดิษฐ์: ลอยตามข้างตัวผู้เล่น ยิงศัตรูที่ใกล้ที่สุดในระยะเป็นระยะ ๆ จนหมดเวลา
// ภาพ 7 เฟรม: 1–2 ลอยนิ่ง (วนระหว่างรอ) → 3 เล็ง → 4 ไฟแลบ (ปล่อยกระสุน) → 5–6 ถีบกลับ/ทรงตัว → 7 กลับท่าเดิม
public sealed class AssaultDrone : MonoBehaviour
{
    private static AssaultDrone active; // มีได้ทีละตัว กดซ้ำระหว่างยังอยู่ = ต่อเวลา

    private Transform owner;
    private Sprite[] frames;
    private GameObject bulletPrefab;
    private float until, fireInterval, range, damage, bulletSpeed;
    private float nextShot;
    private bool firing;
    private SpriteRenderer view;
    private Vector3 velocity;

    // ลอยอยู่เยื้องหลังบนของผู้เล่น ไม่บังตัวละครและไม่บังศัตรูตรงหน้า
    private static readonly Vector3 HoverOffset = new Vector3(-0.8f, 0.9f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { active = null; }

    public static void Deploy(Transform owner, Sprite[] frames, float scale, GameObject bulletPrefab,
                              float duration, float fireInterval, float range, float damage, float bulletSpeed)
    {
        if (active != null)
        {
            active.until = Time.time + duration;
            return;
        }

        var go = new GameObject("AssaultDrone");
        go.transform.position = owner.position + HoverOffset;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var drone = go.AddComponent<AssaultDrone>();
        drone.view = go.AddComponent<SpriteRenderer>();
        drone.view.sortingLayerName = "Effect";
        drone.view.sortingOrder = 4;
        drone.owner = owner;
        drone.frames = frames;
        drone.bulletPrefab = bulletPrefab;
        drone.until = Time.time + duration;
        drone.fireInterval = fireInterval;
        drone.range = range;
        drone.damage = damage;
        drone.bulletSpeed = bulletSpeed;
        drone.nextShot = Time.time + 0.4f;
        active = drone;
    }

    void Update()
    {
        if (owner == null || !owner.gameObject.activeInHierarchy || Time.time >= until)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, owner.position + HoverOffset, ref velocity, 0.15f);

        if (!firing)
        {
            // ลอยขึ้นลงเบา ๆ ระหว่างรอ (เฟรม 1–2)
            view.sprite = frames[(int)(Time.time * 6f) % 2];

            var target = FindTarget();
            if (target != null)
            {
                view.flipX = target.transform.position.x < transform.position.x;
                if (Time.time >= nextShot) StartCoroutine(Fire(target));
            }
        }
    }

    private MonsterController FindTarget()
    {
        MonsterController best = null;
        float bestDistance = range;
        foreach (var monster in SkillCombat.MonstersIn(transform.position, range))
        {
            float distance = Vector2.Distance(monster.transform.position, transform.position);
            if (distance < bestDistance) { bestDistance = distance; best = monster; }
        }
        return best;
    }

    private IEnumerator Fire(MonsterController target)
    {
        firing = true;
        nextShot = Time.time + fireInterval;
        const float frameTime = 1f / 14f;

        for (int i = 2; i < frames.Length; i++)
        {
            view.sprite = frames[i];
            if (i == 3 && target != null && target.IsAlive) Shoot(target.transform.position);
            yield return new WaitForSeconds(frameTime);
        }
        firing = false;
    }

    private void Shoot(Vector2 at)
    {
        if (bulletPrefab == null) return;
        Vector2 direction = (at - (Vector2)transform.position).normalized;
        var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        var projectile = bullet.GetComponent<PlayerProjectile>();
        if (projectile == null) projectile = bullet.AddComponent<PlayerProjectile>();
        projectile.Launch(owner, direction, bulletSpeed, range / Mathf.Max(0.01f, bulletSpeed) + 0.2f, damage);
    }

    void OnDestroy()
    {
        if (active == this) active = null;
    }
}
