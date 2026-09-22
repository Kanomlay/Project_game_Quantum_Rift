using System.Collections;
using UnityEngine;

/// <summary>รูปแบบโจมตีเฉพาะสามตัว ไม่เปลี่ยน AI ของมอนสเตอร์ที่ไม่มีคอมโพเนนต์นี้</summary>
[RequireComponent(typeof(MonsterController), typeof(Animator), typeof(Rigidbody2D))]
public sealed class MonsterCombatActions : MonoBehaviour
{
    public enum Style { Rifle, Wrench, RockAndSlam }
    public Style style;
    public Sprite projectileSprite;
    public float rangedDistance = 7f;
    public float meleeDistance = 1.5f;
    public float projectileSpeed = 9f;
    public float projectileLifetime = 3f;
    public float projectileSize = .45f;
    public bool IsAttacking { get; private set; }
    public int AttacksStarted { get; private set; }
    public int ShotsReleased { get; private set; }
    public int MeleeImpacts { get; private set; }
    public bool LastAttackWasRanged { get; private set; }

    MonsterData data;
    Transform target;
    Animator animator;
    Rigidbody2D body;
    SpriteRenderer display;
    Vector2 move, aim;
    float nextAttack;
    Coroutine attack;
    const float Duration = 7f / 12f;

    public void Initialize(MonsterData monsterData, Transform player)
    {
        data=monsterData;target=player;animator=GetComponent<Animator>();
        body=GetComponent<Rigidbody2D>();display=GetComponent<SpriteRenderer>();
    }

    public void Tick()
    {
        move=Vector2.zero;
        if(data==null||target==null||!target.gameObject.activeInHierarchy||IsAttacking)return;
        var stats=target.GetComponent<PlayerStats>();
        if(stats!=null&&stats.isDead){animator.SetBool("isWalking",false);body.linearVelocity=Vector2.zero;return;}
        Vector2 delta=target.position-transform.position;
        float distance=delta.magnitude;
        if(Mathf.Abs(delta.x)>.01f)display.flipX=delta.x<0;
        bool close=distance<=meleeDistance;
        bool ranged=style!=Style.Wrench && !close && distance<=rangedDistance;
        if(style==Style.Rifle)ranged=distance<=rangedDistance;
        bool canAttack=(style==Style.Rifle?ranged:close||ranged)&&ClearLine(transform.position,target.position);
        if(canAttack&&Time.time>=nextAttack)
        {
            aim=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right;
            LastAttackWasRanged=ranged;
            nextAttack=Time.time+Mathf.Max(Duration+.15f,data.attackCooldown);
            attack=StartCoroutine(Attack(ranged));
            return;
        }
        // Heavy เดินเข้าหาหลังขว้างเสร็จ ระยะประชิดจะเปลี่ยนเป็นทุบแทน
        bool shouldWalk=!close && !(style==Style.Rifle&&ranged&&canAttack);
        animator.SetBool("isWalking",shouldWalk);
        if(shouldWalk)move=delta.normalized*data.moveSpeed;
        else body.linearVelocity=Vector2.zero;
    }

    void FixedUpdate()
    {
        if(body!=null&&move.sqrMagnitude>0&&!IsAttacking)
            body.MovePosition(body.position+move*Time.fixedDeltaTime);
    }

    IEnumerator Attack(bool ranged)
    {
        IsAttacking=true;AttacksStarted++;move=Vector2.zero;
        body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
        animator.ResetTrigger("Attack");
        if(style==Style.RockAndSlam)animator.ResetTrigger("Throw");
        animator.SetTrigger(style==Style.RockAndSlam&&ranged?"Throw":"Attack");
        float impact=style==Style.RockAndSlam&&ranged?5f/12f:3f/12f;
        yield return new WaitForSeconds(impact);
        if(target!=null && data!=null)
        {
            if(ranged)ReleaseProjectile();
            else
            {
                MeleeImpacts++;
                Vector2 delta=target.position-transform.position;
                // ตรวจซ้ำที่เฟรมกระทบ: หลบออกจากระยะหรือไปหลังกำแพงแล้วไม่โดน
                if(delta.magnitude<=meleeDistance && Vector2.Dot(delta.normalized,aim)>.15f && ClearLine(transform.position,target.position))
                    DamagePlayer(target.GetComponent<PlayerStats>(),data.attackDamage,transform.position);
            }
        }
        yield return new WaitForSeconds(Duration-impact);
        IsAttacking=false;attack=null;
    }

    void ReleaseProjectile()
    {
        if(projectileSprite==null)return;
        Vector2 origin=(Vector2)transform.position+aim*.55f;
        if(!ClearLine(transform.position,origin))return;
        var obj=new GameObject(style==Style.Rifle?"PhasePurpleBullet":"HeavyThrownRock");
        // ผูกกับห้อง/แมพ ไม่ผูกกับตัวที่กำลังเดิน และลบพร้อมแมพเมื่อเปลี่ยนด่าน
        obj.transform.SetParent(transform.parent,true);obj.transform.position=origin;
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=projectileSprite;
        renderer.sortingLayerName="Effect";
        float scale=projectileSize/Mathf.Max(projectileSprite.bounds.size.x,projectileSprite.bounds.size.y);
        obj.transform.localScale=Vector3.one*scale;
        obj.AddComponent<MonsterAttackProjectile>().Launch(this,aim,projectileSpeed,projectileLifetime,data.attackDamage,style==Style.RockAndSlam);
        ShotsReleased++;
    }

    public bool ClearLine(Vector2 from,Vector2 to)
    {
        Vector2 delta=to-from;
        foreach(var hit in Physics2D.RaycastAll(from,delta.normalized,delta.magnitude))
        {
            var c=hit.collider;
            if(c==null||c.isTrigger||c.GetComponentInParent<MonsterController>()!=null||c.GetComponentInParent<PlayerStats>()!=null)continue;
            return false;
        }
        return true;
    }

    public static void DamagePlayer(PlayerStats stats,float damage,Vector2 source)
    {
        if(stats==null||stats.isDead)return;
        float previous=stats.currentHP;stats.TakeDamage(damage);
        if(stats.currentHP<previous)
        {
            var movement=stats.GetComponent<PlayerMovement>();
            if(movement!=null)movement.TakeKnockback(source,6f);
        }
    }

    public void CancelAttack()
    {
        move=Vector2.zero;
        if(attack!=null)StopCoroutine(attack);
        attack=null;IsAttacking=false;
        if(animator!=null)
        {
            animator.ResetTrigger("Attack");
            if(style==Style.RockAndSlam)animator.ResetTrigger("Throw");
            animator.SetBool("isWalking",false);
        }
    }
    void OnDisable(){CancelAttack();}
}
