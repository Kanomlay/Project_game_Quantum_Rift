using UnityEngine;
using System.Collections;

public class MonsterController : MonoBehaviour
{
    [Header("ข้อมูลมอนสเตอร์ (Data-Driven)")]
    public MonsterData myData;

    // เปิดให้บอสที่สืบทอดไปใช้ต่อได้ (ดู EchoCommanderBoss)
    protected float currentHealth;
    protected Transform player;
    protected Animator anim;
    protected SpriteRenderer sr; 
    protected Rigidbody2D rb; 
    protected float nextAttackTime = 0f;
    protected bool isKnockedBack = false;     
    protected bool isDying = false; // เลือดหมดแล้ว กำลังเล่นท่าตาย ห้ามเดิน/โจมตี

    // ยังสู้อยู่ (สกิลใช้เช็คก่อนทำดาเมจ/เล็งเป้า)
    public bool IsAlive => !isDying && currentHealth > 0f && gameObject.activeInHierarchy;

    // ติดสตัน (กับดักแม่เหล็กไฟฟ้า): หยุดเดิน/โจมตีชั่วคราว ตัวเป็นสีฟ้า
    public bool IsStunned => Time.time < stunnedUntil;
    private float stunnedUntil;
    private bool stunTinted;
    private static readonly Color StunTint = new Color(0.55f, 0.9f, 1f, 1f);

    [Header("ตอนตาย")]
    public float deathLinger = 0.6f; // นอนค้างท่าตายให้เห็นก่อนค่อยจางหาย
    public float deathFade = 0.4f;
    // คงชุดโจมตีเฉพาะตัวจาก Boss_main พร้อมรองรับบอสที่สืบทอดจาก MainMenu
    private MonsterCombatActions combatActions;
    private BossHealthHudLink bossHud;
    [HideInInspector] public RoomController currentRoom;

    protected virtual void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 

        if (myData != null) currentHealth = myData.maxHealth;

        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        if (hero != null) player = hero.transform;
        combatActions = GetComponent<MonsterCombatActions>();
        if (combatActions != null) combatActions.Initialize(myData, player);
        bossHud = GetComponent<BossHealthHudLink>();
        // เปิดหลอดเมื่อบอสถูกเสกในห้องต่อสู้ ไม่เปิดระหว่างดูตัวอย่างอนิเมชัน
        if (bossHud != null && myData != null && currentRoom != null)
            bossHud.BeginFight(currentHealth, myData.maxHealth, player != null ? player.GetComponent<PlayerStats>() : null);
    }

    protected virtual void Update()
    {
        if (isDying) return;
        if (UpdateStun()) return;
        if (isKnockedBack) return; 

        if (combatActions != null)
        {
            combatActions.Tick();
            return;
        }

        if (player != null && myData != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (player.position.x < transform.position.x) sr.flipX = true; 
            else if (player.position.x > transform.position.x) sr.flipX = false; 

            if (distance > myData.attackRange) 
            {
                anim.SetBool("isWalking", true);
                anim.ResetTrigger("Attack");
                transform.position = Vector2.MoveTowards(transform.position, player.position, myData.moveSpeed * Time.deltaTime);
            }
            else 
            {
                anim.SetBool("isWalking", false);

                if (Time.time >= nextAttackTime)
                {
                    anim.SetTrigger("Attack");
                    
                
                    HitPlayer(myData.attackDamage, 6f); 

                    nextAttackTime = Time.time + myData.attackCooldown;
                }
            }
        }
    }

    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (combatActions == null && myData != null && collision.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            HitPlayer(myData.attackDamage, 6f); 
        }
    }

    
    protected void HitPlayer(float damage, float knockbackForce)
    {
        if (player != null)
        {
            PlayerStats pStats = player.GetComponent<PlayerStats>();
            PlayerMovement pMove = player.GetComponent<PlayerMovement>();

            if (pStats != null) pStats.TakeDamage(damage); 
            
            if (pMove != null) pMove.TakeKnockback(transform.position, knockbackForce);
        }
    }

    public void Stun(float seconds)
    {
        if (!IsAlive || seconds <= 0f) return;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        if (combatActions != null) combatActions.CancelAttack();
        if (anim != null) anim.SetBool("isWalking", false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // คืน true ถ้ายังติดสตันอยู่ (ให้ Update หยุดตรงนั้น) และคุมสีตัวตอนติด/หลุดสตัน
    protected bool UpdateStun()
    {
        if (IsStunned)
        {
            if (!isKnockedBack && sr != null) sr.color = StunTint; // โดนตีกะพริบแดงก่อน แล้วค่อยกลับเป็นสีสตัน
            stunTinted = true;
            return true;
        }
        if (stunTinted)
        {
            stunTinted = false;
            if (sr != null) sr.color = Color.white;
        }
        return false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (!gameObject.activeInHierarchy || currentHealth <= 0) return;
        currentHealth -= damageAmount;
        if (bossHud != null && myData != null) bossHud.RefreshHealth(currentHealth, myData.maxHealth);
        
    
        StartCoroutine(DamageEffectRoutine());

        if (currentHealth <= 0) Die();
    }

    
    private IEnumerator DamageEffectRoutine()
    {
        if (combatActions != null) combatActions.CancelAttack();
        isKnockedBack = true; 
        sr.color = Color.red; 

        if (player != null && rb != null)
        {
            
            Vector2 knockbackDir = (transform.position - player.position).normalized;
            rb.linearVelocity = knockbackDir * 5f; 
        }

        yield return new WaitForSeconds(0.15f);

        sr.color = Color.white; 
        if (rb != null) rb.linearVelocity = Vector2.zero; 
        isKnockedBack = false; 
    }

    protected virtual void Die()
    {
        SummaryManager.enemiesDefeatedCount++;
        // นับว่าตายทันที ประตูห้องจะได้เปิดตอนตัวสุดท้ายล้ม ไม่ต้องรอท่าตายจบ
        if (currentRoom != null) currentRoom.OnMonsterDied(); 
        StartCoroutine(DeathRoutine());
    }

    // เล่นท่าตาย (Animator ของมอนสเตอร์มี isDead → state die) แล้วค่อยจางหายและปิดตัว
    // ปิด collider ก่อน ศพจะได้ไม่ขวางทางหรือรับดาเมจต่อ
    private IEnumerator DeathRoutine()
    {
        isDying = true;
        if (combatActions != null)
        {
            combatActions.CancelAttack();
            combatActions.enabled = false;
        }
        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (anim != null && HasParameter(anim, "isDead"))
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isDead", true);
        }

        yield return new WaitForSeconds(deathLinger);

        if (sr != null && deathFade > 0f)
        {
            Color start = Color.white;
            for (float t = 0f; t < deathFade; t += Time.deltaTime)
            {
                sr.color = new Color(start.r, start.g, start.b, 1f - t / deathFade);
                yield return null;
            }
        }
        gameObject.SetActive(false);
    }

    // บอสบางตัวยังไม่มีท่าตาย (ไม่มีพารามิเตอร์ isDead) ถ้าสั่งไปจะมีคำเตือนเต็ม Console
    private static bool HasParameter(Animator animator, string name)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.name == name) return true;
        return false;
    }
}
