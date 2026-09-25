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

        if (GameManager.selectedCharacter != null)
        {
            baseSpeed = GameManager.selectedCharacter.moveSpeed * 0.05f; 
        }
        currentSpeed = baseSpeed;

    }

    void Update()
    {
        if (PauseManager.isGamePaused) return;

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
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
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