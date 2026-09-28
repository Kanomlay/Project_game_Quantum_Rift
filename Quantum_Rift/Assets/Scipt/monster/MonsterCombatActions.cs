using System.Collections;
using UnityEngine;

/// <summary>รูปแบบโจมตีเฉพาะตัว ไม่เปลี่ยน AI ของมอนสเตอร์ที่ไม่มีคอมโพเนนต์นี้</summary>
[RequireComponent(typeof(MonsterController), typeof(Animator), typeof(Rigidbody2D))]
public sealed class MonsterCombatActions : MonoBehaviour
{
    // Lunge ต่อท้ายเสมอ ค่าใน prefab เก็บเป็นตัวเลข (Wrench = ประชิดทั่วไป ใช้กับ Flux Jaw ด้วย)
    public enum Style { Rifle, Wrench, RockAndSlam, Lunge, Pounce }
    public bool closeRangeSlash;
    public Style style;
    public Sprite projectileSprite;
    public float rangedDistance = 7f;
    public float meleeDistance = 1.5f;
    public float projectileSpeed = 9f;
    public float projectileLifetime = 3f;
    public float projectileSize = .45f;
    public float lungeTriggerDistance = 3f; // Lunge: เริ่มพุ่งเมื่อผู้เล่นอยู่ในระยะนี้
    public float lungeSpeed = 6.5f;         // Lunge: ความเร็วพุ่งช่วงง้างท่า ก่อนเฟรมกระทบ
    public float lungeStopDistance = 0.9f;  // Lunge: หยุดพุ่งเมื่อถึงตัวผู้เล่น (ไม่ชนกันทางฟิสิกส์แล้ว ต้องหยุดเอง)
    [Header("Rifle แบบตั้งท่าเล็งค้าง (Phase Soldier)")]
    public bool holdAim;                  // ยกปืนค้างยิงต่อเนื่อง ไม่ลดปืนทุกนัด (ต้องมี isAiming/Fire ใน Animator)
    public float keepAwayDistance = 2.5f; // ผู้เล่นเข้าใกล้กว่านี้ ลดปืนแล้วถอยออกไปตั้งหลัก
    public float aimRaiseTime = .25f;     // เวลายกปืนก่อนยิงนัดแรก (ท่ายก 3 เฟรม)
    public float aimLowerTime = .17f;     // เวลาลดปืนก่อนเริ่มเดิน (ท่าลด 2 เฟรม)
    public float fireTime = .25f;         // ท่ายิง 3 เฟรม
    public Color projectileTint = Color.white; // ย้อมสีกระสุน ใช้ภาพกระสุนชุดเดียวกันได้หลายตัว (Woodmine ย้อมเขียว)
    [Header("ทุบกำแพงในห้องที่ขวางทาง (RoomBreakableWalls)")]
    public bool breakWalls = true;
    [Min(0f)] public float wallDamage = 12f; // ต่อครั้ง กำแพงช่องละ 30 = ทุบ 3 ครั้ง (ท่าทุบของ Heavy แรงเป็นสองเท่า)
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
    MonsterNavigator nav; // เดินอ้อมเสา/กำแพงแทนเดินตรงเข้าหาแล้วติด
    bool aiming;
    float aimReadyAt,moveBlockedUntil;
    MonsterFx fx; // วาบส้มตอนง้างโจมตี (MonsterController ใส่ให้ตอนเริ่ม)
    void Warn(){if(fx==null)fx=GetComponent<MonsterFx>();if(fx!=null)fx.Warn();}
    const float Duration = 7f / 12f;

    public void Initialize(MonsterData monsterData, Transform player)
    {
        data=monsterData;target=player;animator=GetComponent<Animator>();
        body=GetComponent<Rigidbody2D>();display=GetComponent<SpriteRenderer>();
        nav=new MonsterNavigator(transform,GetComponent<MonsterController>()){CanBreakWalls=breakWalls};
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
        if(style==Style.Rifle&&holdAim){TickHoldAim(delta,distance);return;}
        bool lunge=style==Style.Lunge || style==Style.Pounce;
        bool close=distance<=(lunge?lungeTriggerDistance:meleeDistance);
        bool ranged=style!=Style.Wrench && !lunge && !close && distance<=rangedDistance;
        if(style==Style.Rifle)ranged=distance<=rangedDistance;
        bool lineClear=ClearLine(transform.position,target.position);
        bool canAttack=(style==Style.Rifle?ranged:close||ranged)&&lineClear;
        if(canAttack&&Time.time>=nextAttack)
        {
            aim=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right;
            LastAttackWasRanged=ranged;
            nextAttack=Time.time+Mathf.Max(Duration+.15f,data.attackCooldown);
            attack=StartCoroutine(Attack(ranged));
            return;
        }
        // Heavy เดินเข้าหาหลังขว้างเสร็จ ระยะประชิดจะเปลี่ยนเป็นทุบแทน
        // ใกล้แต่มีเสา/กำแพงคั่น ต้องเดินอ้อมไปหาก่อน ไม่ใช่ยืนนิ่งอยู่หลังกำแพง
        bool shouldWalk=(!close||!lineClear) && !(style==Style.Rifle&&ranged&&canAttack);
        animator.SetBool("isWalking",shouldWalk);
        if(shouldWalk)move=nav.DirectionTo(target.position)*data.moveSpeed;
        else body.linearVelocity=Vector2.zero;
        if(shouldWalk&&nav.BlockingWall!=null)SmashOrWait(nav.BlockingWall);
    }

    // กำแพงในห้องขวางทาง (หรือเดินติดกำแพงอยู่): ยืนหันเข้าหากำแพงแล้วทุบเป็นจังหวะจนแตก
    void SmashOrWait(BreakableWallCell wall)
    {
        move=Vector2.zero;body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
        Vector2 toWall=wall.Center-(Vector2)transform.position;
        if(Mathf.Abs(toWall.x)>.01f)display.flipX=toWall.x<0;
        if(Time.time<nextAttack)return;
        nextAttack=Time.time+Mathf.Max(Duration+.1f,data.attackCooldown*.6f);
        attack=StartCoroutine(SmashWall(wall));
    }

    IEnumerator SmashWall(BreakableWallCell wall)
    {
        IsAttacking=true;move=Vector2.zero;body.linearVelocity=Vector2.zero;
        animator.SetBool("isWalking",false);
        // ใช้ท่าโจมตีประชิดของตัวเอง (Phase Soldier ใช้ท่าฟัน ปืนไรเฟิลที่ไม่มีท่าประชิดทุบโดยไม่เล่นท่า)
        string trigger=closeRangeSlash?"Melee":style==Style.Rifle?null:"Attack";
        if(trigger!=null){animator.ResetTrigger(trigger);animator.SetTrigger(trigger);}
        const float impact=3f/12f;
        yield return new WaitForSeconds(impact);
        if(wall!=null&&!wall.IsBroken)wall.Smash(wallDamage*(style==Style.RockAndSlam?2f:1f));
        yield return new WaitForSeconds(Duration-impact);
        IsAttacking=false;attack=null;
    }

    // ยืนเล็งค้างยิงเป็นชุดตราบที่ผู้เล่นอยู่ในระยะและไม่มีกำแพงบัง
    // ผู้เล่นเข้ามาใกล้เกิน → ลดปืนแล้วถอยหนี, หลุดระยะ/โดนบัง → ลดปืนแล้วเดินตาม แล้วค่อยกลับมายกปืนใหม่
    void TickHoldAim(Vector2 delta,float distance)
    {
        if(closeRangeSlash && distance<=keepAwayDistance && ClearLine(transform.position,target.position))
        {
            move=Vector2.zero;body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
            if(Time.time>=nextAttack)
            {
                aiming=false;animator.SetBool("isAiming",false);
                aim=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right;LastAttackWasRanged=false;
                nextAttack=Time.time+Mathf.Max(.85f,data.attackCooldown);attack=StartCoroutine(Attack(false));
            }
            return;
        }
        bool tooClose=distance<keepAwayDistance;
        bool canShoot=!tooClose&&distance<=rangedDistance&&ClearLine(transform.position,target.position);
        if(canShoot)
        {
            if(!aiming){aiming=true;aimReadyAt=Time.time+aimRaiseTime;animator.SetBool("isAiming",true);Warn();}
            animator.SetBool("isWalking",false);body.linearVelocity=Vector2.zero;
            if(Time.time>=aimReadyAt&&Time.time>=nextAttack)
            {
                aim=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right;
                LastAttackWasRanged=true;
                nextAttack=Time.time+Mathf.Max(fireTime,data.attackCooldown);
                attack=StartCoroutine(FireFromStance());
            }
            return;
        }
        if(aiming){aiming=false;animator.SetBool("isAiming",false);moveBlockedUntil=Time.time+aimLowerTime;}
        // ห้ามเดินจนกว่า Animator จะลดปืนจบจริง (ท่ายก/เล็ง/ยิง/ลด ติด tag Aim) ไม่ใช่แค่นับเวลา
        if(Time.time<moveBlockedUntil||GunUp()){animator.SetBool("isWalking",false);body.linearVelocity=Vector2.zero;return;}
        animator.SetBool("isWalking",true);
        move=(tooClose?-delta.normalized:nav.DirectionTo(target.position))*data.moveSpeed;
        if(!tooClose&&nav.BlockingWall!=null)SmashOrWait(nav.BlockingWall);
    }

    public const string AimTag="Aim";
    bool GunUp()=>animator.GetCurrentAnimatorStateInfo(0).IsTag(AimTag)||
        (animator.IsInTransition(0)&&animator.GetNextAnimatorStateInfo(0).IsTag(AimTag));

    // ท่ายิงเริ่มที่เฟรมไฟแลบ ปล่อยกระสุนทันที แล้วกลับไปท่าเล็งค้าง (Animator พากลับเอง)
    IEnumerator FireFromStance()
    {
        IsAttacking=true;AttacksStarted++;move=Vector2.zero;
        animator.SetTrigger("Fire");
        ReleaseProjectile();
        yield return new WaitForSeconds(fireTime);
        IsAttacking=false;attack=null;
    }

    void FixedUpdate()
    {
        if(body!=null&&move.sqrMagnitude>0&&!IsAttacking)
            body.MovePosition(body.position+move*Time.fixedDeltaTime);
    }

    IEnumerator Attack(bool ranged)
    {
        if(style==Style.Pounce){yield return Pounce();yield break;}
        IsAttacking=true;AttacksStarted++;move=Vector2.zero;Warn();
        body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
        animator.ResetTrigger("Attack");
        if(style==Style.RockAndSlam)animator.ResetTrigger("Throw");
        animator.SetTrigger(style==Style.Rifle && closeRangeSlash && !ranged?"Melee":style==Style.RockAndSlam&&ranged?"Throw":"Attack");
        float impact=style==Style.RockAndSlam&&ranged?5f/12f:3f/12f;
        if(style==Style.Lunge&&!ranged)
        {
            // Echo Stalker พุ่งเข้าหาช่วงง้างท่า แล้วค่อยฟันที่เฟรมกระทบ (ชนกำแพงก็หยุดเองเพราะใช้ความเร็ว)
            // ถึงตัวผู้เล่นแล้วหยุด ไม่พุ่งทะลุไปซ้อนทับ
            for(float t=0f;t<impact;t+=Time.deltaTime)
            {
                bool arrived=target!=null&&Vector2.Distance(transform.position,target.position)<=lungeStopDistance;
                body.linearVelocity=arrived?Vector2.zero:aim*lungeSpeed;
                yield return null;
            }
            body.linearVelocity=Vector2.zero;
        }
        else yield return new WaitForSeconds(impact);
        if(target!=null && data!=null)
        {
            if(ranged)ReleaseProjectile();
            else
            {
                MeleeImpacts++;
                Vector2 delta=target.position-transform.position;
                // ตรวจซ้ำที่เฟรมกระทบ: หลบออกจากระยะหรือไปหลังกำแพงแล้วไม่โดน
                if(delta.magnitude<=meleeDistance && Vector2.Dot(delta.normalized,aim)>.15f && ClearLine(transform.position,target.position))
                    DamagePlayer(target.GetComponent<PlayerStats>(),data.attackDamage,transform.position,style==Style.Rifle&&closeRangeSlash?10f:6f);
            }
        }
        yield return new WaitForSeconds(Duration-impact);
        IsAttacking=false;attack=null;
    }

    IEnumerator Pounce()
    {
        IsAttacking=true;AttacksStarted++;move=Vector2.zero;body.linearVelocity=Vector2.zero;Warn();
        animator.SetBool("isWalking",false);animator.SetTrigger("Attack");
        // ล็อกทิศตั้งแต่ง้าง ไม่เลี้ยวตามผู้เล่นกลางอากาศ จึงหลบด้านข้างได้
        yield return new WaitForSeconds(.25f);
        bool hit=false;
        for(float elapsed=0;elapsed<.32f;elapsed+=Time.deltaTime)
        {
            if(target==null)break;
            Vector2 delta=(Vector2)target.position-body.position;
            bool arrived=delta.magnitude<=lungeStopDistance;
            body.linearVelocity=arrived||!ClearLine(body.position,body.position+aim*.65f)?Vector2.zero:aim*lungeSpeed;
            if(!hit && delta.magnitude<=meleeDistance && ClearLine(body.position,target.position))
            {DamagePlayer(target.GetComponent<PlayerStats>(),data.attackDamage,transform.position);MeleeImpacts++;hit=true;}
            yield return null;
        }
        body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.13f);IsAttacking=false;attack=null;
    }

    void ReleaseProjectile()
    {
        if(projectileSprite==null)return;
        Vector2 origin=(Vector2)transform.position+aim*.55f;
        if(!ClearLine(transform.position,origin))return;
        var obj=new GameObject(style==Style.Rifle?"PhasePurpleBullet":"HeavyThrownRock");
        // ผูกกับห้อง/แมพ ไม่ผูกกับตัวที่กำลังเดิน และลบพร้อมแมพเมื่อเปลี่ยนด่าน
        obj.transform.SetParent(transform.parent,true);obj.transform.position=origin;
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=projectileSprite;renderer.color=projectileTint;
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

    public static void DamagePlayer(PlayerStats stats,float damage,Vector2 source,float knockback=6f)
    {
        if(stats==null||stats.isDead)return;
        float previous=stats.currentHP;stats.TakeDamage(damage);
        if(stats.currentHP<previous)
        {
            var movement=stats.GetComponent<PlayerMovement>();
            if(movement!=null)movement.TakeKnockback(source,knockback);
        }
    }

    // มอนที่เพิ่งโผล่: ห้ามโจมตีก่อนเวลานี้ (MonsterController ตั้งให้ตอนเกิดจากวงเตือน)
    public void HoldAttacksUntil(float time){nextAttack=Mathf.Max(nextAttack,time);}

    public void CancelAttack()
    {
        move=Vector2.zero;
        if(attack!=null)StopCoroutine(attack);
        attack=null;IsAttacking=false;
        if(body!=null)body.linearVelocity=Vector2.zero; // หยุดพุ่งถ้าโดนตีกลางท่า
        if(animator!=null)
        {
            animator.ResetTrigger("Attack");
            if(closeRangeSlash)animator.ResetTrigger("Melee");
            if(style==Style.RockAndSlam)animator.ResetTrigger("Throw");
            if(holdAim)animator.ResetTrigger("Fire");
            animator.SetBool("isWalking",false);
        }
    }
    void OnDisable(){CancelAttack();}
    void OnDrawGizmosSelected(){nav?.DrawGizmos();} // เส้นเหลือง = ทางที่กำลังเดินอ้อม
}
