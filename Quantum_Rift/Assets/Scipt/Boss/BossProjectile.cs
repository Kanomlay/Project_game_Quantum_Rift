using UnityEngine;

// กระสุนของบอส วิ่งเป็นเส้นตรงจนกว่าจะโดนผู้เล่น ชนกำแพง หรือหมดอายุ
// ยิงออกมาแล้วสั่ง Launch เพื่อกำหนดทิศ ความเร็ว และดาเมจ
[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    [Min(0.1f)] public float lifetime = 4f;
    public float knockbackForce = 4f;

    private Vector2 velocity;
    private float damage;
    private int shooterLayer = -1; // เลเยอร์ของคนยิง กระสุนจะทะลุตัวเองกับพวกเดียวกัน

    public void Launch(Vector2 direction, float speed, float damageAmount, int ownerLayer)
    {
        velocity = direction.normalized * speed;
        damage = damageAmount;
        shooterLayer = ownerLayer;

        // หันหัวกระสุนไปตามทิศที่ยิง
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ตัวตรวจจับของห้อง พอร์ทัล และ trigger อื่นๆ ไม่ควรหยุดกระสุน
        if (other.isTrigger) return;

        // ยิงทะลุตัวบอสเองและลูกน้องที่อยู่เลเยอร์เดียวกัน
        if (shooterLayer >= 0 && other.gameObject.layer == shooterLayer) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(damage);

            PlayerMovement movement = stats.GetComponent<PlayerMovement>();
            if (movement != null) movement.TakeKnockback(transform.position, knockbackForce);

            Destroy(gameObject);
            return;
        }

        Destroy(gameObject); // ที่เหลือถือว่าเป็นกำแพงหรือสิ่งกีดขวาง
    }
}
