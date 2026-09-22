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
        if (currentRoom != null) currentRoom.OnMonsterDied(); 
        gameObject.SetActive(false); 
    }
}
