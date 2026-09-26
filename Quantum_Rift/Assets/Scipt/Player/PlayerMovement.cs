using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 5f; 
    public float runMultiplier = 2f;
    private float currentSpeed;
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private SpriteRenderer sr;
    private Animator anim; 

    private bool isDashing = false;
    private bool isKnockedBack = false;
    private PlayerStats stats;
    private float speedBoost = 1f;     // ตัวคูณความเร็วจากสกิล (กระตุ้นเซลล์)
    private float speedBoostUntil;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        foreach (var col in GetComponents<Collider2D>())
            if (!col.isTrigger) { bodyCollider = col; break; }

        if (GameManager.selectedCharacter != null)
        {
            baseSpeed = GameManager.selectedCharacter.moveSpeed * 0.05f; 
        }
        currentSpeed = baseSpeed;

    }

    void Update()
    {
        if (PauseManager.isGamePaused) return;

        // เปิดหน้าร้านอยู่ ยืนนิ่ง (คลิกซื้อของด้วยเมาส์ ไม่ให้ตัวละครเดินตามปุ่ม)
        if (ShopWindow.IsOpen)
        {
            movement = Vector2.zero;
            anim.SetBool("isWalking", false);
            return;
        }

        // ตายแล้วยืนนิ่งค้างท่าตาย ไม่งั้นศพจะเดินตามปุ่มได้
        if (stats != null && stats.isDead)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.x < 0) 
        {
            sr.flipX = true; 
        }
        else if (movement.x > 0) 
        {
            sr.flipX = false; 
        }
        if (movement.x != 0 || movement.y != 0)
        {
            anim.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed = baseSpeed * runMultiplier; 
        }
        else
        {
            currentSpeed = baseSpeed;
        }
        if (Time.time < speedBoostUntil) currentSpeed *= speedBoost;

    }

    void FixedUpdate()
    {
        if (isDashing) return;
        if (isKnockedBack) return;
        Vector2 velocity = movement.normalized * currentSpeed;
        if (Time.time < attackStepUntil) velocity += attackStepVelocity;
        rb.MovePosition(rb.position + AvoidMonsters(velocity * Time.fixedDeltaTime));
    }

    // มอนสเตอร์เป็นสิ่งกีดขวางที่ผลักไม่ได้ (แบบเกมทั่วไป): เดินชนแล้วหยุดตรงตัวมอน หรือไถลเลียบตัวมอนต่อ
    // ฟิสิกส์ระหว่างผู้เล่นกับมอนสเตอร์ถูกปิดไว้ (MonsterController) ต่างฝ่ายจึงดันกันไม่ได้ ต้องกันการเดินทับเองตรงนี้
    // ถ้าทับกันอยู่แล้ว (มอนพุ่งเข้ามาชิด) ยังเดินออกได้ แต่เดินลึกเข้าไปอีกไม่ได้
    private Collider2D bodyCollider;
    private readonly RaycastHit2D[] blockHits = new RaycastHit2D[8];
    private ContactFilter2D blockFilter = new ContactFilter2D { useTriggers = false };
    private const float BlockSkin = 0.02f;

    private Vector2 AvoidMonsters(Vector2 step)
    {
        if (bodyCollider == null || !bodyCollider.enabled || step.sqrMagnitude < 1e-8f) return step;
        Vector2 moved = ClampToMonsters(step, out Vector2 normal);
        if (normal == Vector2.zero) return step;

        // ส่วนที่ติดตัวมอน เปลี่ยนเป็นไถลตามผิวมอนแทน (ไม่ต้องหยุดนิ่งเวลาเดินเฉียง)
        Vector2 rest = step - moved;
        Vector2 slide = rest - Vector2.Dot(rest, normal) * normal;
        if (slide.sqrMagnitude < 1e-8f) return moved;
        return moved + ClampToMonsters(slide, out _);
    }

    private Vector2 ClampToMonsters(Vector2 step, out Vector2 normal)
    {
        normal = Vector2.zero;
        float distance = step.magnitude;
        if (distance < 1e-6f) return step;
        Vector2 direction = step / distance;
        float allowed = distance;

        int count = bodyCollider.Cast(direction, blockFilter, blockHits, distance + BlockSkin, true);
        for (int i = 0; i < count; i++)
        {
            var hit = blockHits[i];
            var monster = hit.collider.GetComponentInParent<MonsterController>();
            if (monster == null || !monster.IsAlive) continue; // กำแพงให้ฟิสิกส์จัดการตามปกติ

            Vector2 surface = hit.normal;
            if (hit.distance <= 0.0001f)
            {
                Vector2 away = (Vector2)bodyCollider.bounds.center - (Vector2)hit.collider.bounds.center;
                if (Vector2.Dot(direction, away) >= 0f) continue; // กำลังเดินออกจากตัวมอน
                surface = away.sqrMagnitude > 1e-6f ? away.normalized : -direction;
            }

            float free = Mathf.Max(0f, hit.distance - BlockSkin);
            if (free < allowed)
            {
                allowed = free;
                normal = surface;
            }
        }
        return direction * allowed;
    }

    // ก้าวตามแรงตีสั้น ๆ (ติดลบ = ถอยจากแรงถีบปืน) บวกกับการเดินปกติ ชนกำแพงก็หยุดเองตามฟิสิกส์
    private Vector2 attackStepVelocity;
    private float attackStepUntil;

    public void AttackStep(Vector2 direction, float distance, float duration)
    {
        if (Mathf.Approximately(distance, 0f) || duration <= 0f || (stats != null && stats.isDead)) return;
        attackStepVelocity = direction.normalized * (distance / duration);
        attackStepUntil = Time.time + duration;
    }

    public void BoostSpeed(float multiplier, float seconds)
    {
        speedBoost = multiplier;
        speedBoostUntil = Time.time + seconds;
    }

    public void StartDash(Vector2 direction, float dashSpeed, float dashDuration)
    {
        StartCoroutine(DashRoutine(direction, dashSpeed, dashDuration));
    }

    private IEnumerator DashRoutine(Vector2 dir, float dashSpeed, float dashDuration)
    {
        isDashing = true;
        rb.linearVelocity = dir * dashSpeed; 
        
        yield return new WaitForSeconds(dashDuration); 
        
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }

    public void TakeKnockback(Vector2 damageSource, float force)
    {
        StartCoroutine(KnockbackRoutine(damageSource, force));
    }

    private IEnumerator KnockbackRoutine(Vector2 damageSource, float force)
    {
        isKnockedBack = true;
        sr.color = Color.red;

        Vector2 knockbackDir = ((Vector2)transform.position - damageSource).normalized;
        rb.linearVelocity = knockbackDir * force; 

        yield return new WaitForSeconds(0.2f); 

        sr.color = Color.white; 
        rb.linearVelocity = Vector2.zero; 
        isKnockedBack = false; 
    }
}