using UnityEngine;
using System.Collections;

public class MonsterController : MonoBehaviour
{
    [Header("ข้อมูลมอนสเตอร์ (Data-Driven)")]
    public MonsterData myData;

    private float currentHealth;
    private Transform player;
    private Animator anim;
    private SpriteRenderer sr; 
    private Rigidbody2D rb; 
    private float nextAttackTime = 0f;
    private bool isKnockedBack = false;     
    [HideInInspector] public RoomController currentRoom;

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 

        if (myData != null) currentHealth = myData.maxHealth;

        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        if (hero != null) player = hero.transform;
    }

    void Update()
    {
        
        if (isKnockedBack) return; 

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
        if (collision.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            HitPlayer(myData.attackDamage, 6f); 
        }
    }

    
    private void HitPlayer(float damage, float knockbackForce)
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
        currentHealth -= damageAmount;
        
    
        StartCoroutine(DamageEffectRoutine());

        if (currentHealth <= 0) Die();
    }

    
    private IEnumerator DamageEffectRoutine()
    {
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

    void Die()
    {
        SummaryManager.enemiesDefeatedCount++;
        if (currentRoom != null) currentRoom.OnMonsterDied(); 
        gameObject.SetActive(false); 
    }
}