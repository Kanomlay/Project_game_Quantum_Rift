using System.Collections;
using UnityEngine;

/// <summary>รูปแบบโจมตีเฉพาะตัว ไม่เปลี่ยน AI ของมอนสเตอร์ที่ไม่มีคอมโพเนนต์นี้</summary>
[RequireComponent(typeof(MonsterController), typeof(Animator), typeof(Rigidbody2D))]
public sealed class MonsterCombatActions : MonoBehaviour
{
    // แบบใหม่ต่อท้ายเสมอ ค่าใน prefab เก็บเป็นตัวเลข (Wrench = ประชิดทั่วไป ใช้กับ Flux Jaw ด้วย)
    // Root = อยู่กับที่ เรียกรากแทงขึ้นใต้เท้าผู้เล่น ผู้เล่นไกลนาน ๆ มุดดินไปโผล่ใกล้ ๆ (Rootlings)
    // Vine = ฟาดเถาวัลย์เป็นแนวตรงยาว มีแถบเตือนบนพื้นก่อนฟาด (Forest Wraith)
    public enum Style { Rifle, Wrench, RockAndSlam, Lunge, Pounce, Root, Vine }
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
    [Header("พิษ (มอนแมพป่า) 0 = ไม่ติดพิษ")]
    [Min(0f)] public float poisonSeconds;
    [Min(0f)] public float poisonDamage = 0.5f; // รวมทั้งช่วง ไม่ใช่ต่อวินาที
    [Header("Root: รากแทงขึ้นใต้เท้าผู้เล่น (Rootlings)")]
    public GameObject rootPrefab;          // ภาพรากแทงขึ้น 7 เฟรม (ชุดเดียวกับบอสป่า ย่อลง)
    public float rootScale = 0.4f;
    public float rootRange = 7f;           // ผู้เล่นอยู่ในระยะนี้ถึงเรียกราก (ใต้ดิน ไม่ต้องเห็นตัว)
    public float rootWarning = 0.8f;       // วงเตือนก่อนรากแทง
    public float rootRadius = 0.65f;       // ยืนในวงนี้ตอนรากแทง = โดน
    public float burrowAfter = 3f;         // ผู้เล่นอยู่นอกระยะนานเท่านี้ มุดดินไปโผล่ใกล้ผู้เล่น
    [Header("Vine: ฟาดเถาวัลย์เป็นแนวตรง (Forest Wraith)")]
    public float vineLength = 3.2f;
    public float vineWidth = 0.8f;
    public float vineWarning = 0.5f;       // แถบเตือนยืดออกไปจนสุดก่อนฟาด
    [Header("ระเบิดตัวเองตอนเลือดน้อย (Zero Husk) 0 = ไม่ระเบิด")]
    [Range(0f, 1f)] public float selfDestructAt;
    public float fuseTime = 1.2f;          // วิ่งเข้าหาเรืองฟ้าก่อนระเบิด (ภาพระเบิดเตือนต่ออีก 0.75 วินาที)
    public float fuseSpeed = 1.9f;         // คูณความเร็วเดินช่วงนี้
    [Header("หน้าตา")]
    public Color warnTint = new Color(1f, 0.82f, 0.45f); // วาบก่อนโจมตี (Woodmine เรืองเขียว)
    public bool floating;                  // วิญญาณลอยขึ้นลง ไม่เด้งเป็นก้าว (MonsterFx อ่าน)
    public Color ambientColor = Color.white;
    public float ambientEvery;             // ประกายรอบตัวทุกกี่วินาที (0 = ไม่มี) ใบไม้ร่วง / เศษดิน
    public bool ambientFromTop;            // ร่วงจากครึ่งบนของตัว (ใบไม้) หรือครึ่งล่าง (เศษดิน)
    public bool pounceTrail;               // เงาภาพค้างตามตัวตอนกระโจน (หมาป่า)
    public Color trailColor = new Color(0.45f, 0.95f, 1f);
    public bool projectileTrail;           // กระสุนมีหางตามลูก
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
    MonsterFx fx; // วาบตอนง้างโจมตี (MonsterController ใส่ให้ตอนเริ่ม)
    MonsterController owner;
    float fuseEnd=-1f,nextFuseSpark,nextTrail;
    MonsterFx Fx{get{if(fx==null)fx=GetComponent<MonsterFx>();return fx;}}
    void Warn(){if(Fx!=null)Fx.Warn(warnTint);if(owner!=null)owner.AttackVoice();}
    const float Duration = 7f / 12f;
    // ค่าจาก MonsterData คูณตัวคูณเฉพาะตัว (มอนผิดเพี้ยน)
    float Cooldown=>data.attackCooldown*(owner!=null?owner.AttackCooldownScale:1f);
    float AttackDamage=>data.attackDamage*(owner!=null?owner.AttackScale:1f);
    float MoveSpeed=>data.moveSpeed*(owner!=null?owner.SpeedScale:1f);

    public void Initialize(MonsterData monsterData, Transform player)
    {
        data=monsterData;target=player;animator=GetComponent<Animator>();
        body=GetComponent<Rigidbody2D>();display=GetComponent<SpriteRenderer>();
        restMass=body!=null?body.mass:1f;
        owner=GetComponent<MonsterController>();
        nav=new MonsterNavigator(transform,owner){CanBreakWalls=breakWalls};
    }

    public void Tick()
    {
        move=Vector2.zero;
        if(data==null||target==null||!target.gameObject.activeInHierarchy)return;
        // เลือดถึงเกณฑ์: ทิ้งท่าที่ทำอยู่ แล้ววิ่งเรืองฟ้าเข้าหาเพื่อระเบิดตัวเอง
        if(selfDestructAt>0f&&fuseEnd<0f&&owner!=null&&owner.HealthFraction<=selfDestructAt){CancelAttack();fuseEnd=Time.time+fuseTime;Sfx.PlayAt(SfxId.MonFuse,transform.position);Sfx.PlayAt(SfxId.VoiceHuskFuse,transform.position);}
        if(fuseEnd>=0f){TickFuse();return;}
        if(IsAttacking)return;
        var stats=target.GetComponent<PlayerStats>();
        if(stats!=null&&stats.isDead){animator.SetBool("isWalking",false);body.linearVelocity=Vector2.zero;return;}
        Vector2 delta=target.position-transform.position;
        float distance=delta.magnitude;
        if(Mathf.Abs(delta.x)>.01f)display.flipX=delta.x<0;
        if(style==Style.Rifle&&holdAim){TickHoldAim(delta,distance);return;}
        if(style==Style.Root){TickRoot(distance);return;}
        if(style==Style.Vine){TickVine(delta,distance);return;}
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
            nextAttack=Time.time+Mathf.Max(Duration+.15f,Cooldown);
            attack=StartCoroutine(Attack(ranged));
            return;
        }
        // Heavy เดินเข้าหาหลังขว้างเสร็จ ระยะประชิดจะเปลี่ยนเป็นทุบแทน
        // ใกล้แต่มีเสา/กำแพงคั่น ต้องเดินอ้อมไปหาก่อน ไม่ใช่ยืนนิ่งอยู่หลังกำแพง
        bool shouldWalk=(!close||!lineClear) && !(style==Style.Rifle&&ranged&&canAttack);
        animator.SetBool("isWalking",shouldWalk);
        if(shouldWalk)move=nav.DirectionTo(target.position)*MoveSpeed;
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
        nextAttack=Time.time+Mathf.Max(Duration+.1f,Cooldown*.6f);
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
                nextAttack=Time.time+Mathf.Max(.85f,Cooldown);attack=StartCoroutine(Attack(false));
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
                nextAttack=Time.time+Mathf.Max(fireTime,Cooldown);
                attack=StartCoroutine(FireFromStance());
            }
            return;
        }
        if(aiming){aiming=false;animator.SetBool("isAiming",false);moveBlockedUntil=Time.time+aimLowerTime;}
        // ห้ามเดินจนกว่า Animator จะลดปืนจบจริง (ท่ายก/เล็ง/ยิง/ลด ติด tag Aim) ไม่ใช่แค่นับเวลา
        if(Time.time<moveBlockedUntil||GunUp()){animator.SetBool("isWalking",false);body.linearVelocity=Vector2.zero;return;}
        animator.SetBool("isWalking",true);
        move=(tooClose?-delta.normalized:nav.DirectionTo(target.position))*MoveSpeed;
        if(!tooClose&&nav.BlockingWall!=null)SmashOrWait(nav.BlockingWall);
    }

    // ---------- ระเบิดตัวเอง (Zero Husk) ----------
    static readonly Color FuseColor=new Color(.45f,.9f,1f);

    void TickFuse()
    {
        Vector2 delta=target.position-transform.position;
        if(Mathf.Abs(delta.x)>.01f)display.flipX=delta.x<0;
        float k=1f-Mathf.Clamp01((fuseEnd-Time.time)/Mathf.Max(.01f,fuseTime));
        // กะพริบฟ้าถี่ขึ้นเรื่อย ๆ ใกล้ระเบิด
        if(Fx!=null)Fx.Glow(FuseColor,.3f+.5f*Mathf.Abs(Mathf.Sin(Time.time*(7f+20f*k))));
        if(Time.time>=nextFuseSpark)
        {
            nextFuseSpark=Time.time+Mathf.Lerp(.12f,.04f,k);
            ImpactSparks.Spawn((Vector2)display.bounds.center+Random.insideUnitCircle*.35f,FuseColor,2,Vector2.zero,3f);
        }
        if(Time.time>=fuseEnd||delta.magnitude<=1f)
        {
            body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
            owner.SelfDestruct(); // ตายแล้วเล่นภาพระเบิดของ ZeroHuskDeathBurst (ระเบิดจริงหลังเตือน 0.75 วินาที)
            return;
        }
        animator.SetBool("isWalking",true);
        move=nav.DirectionTo(target.position)*MoveSpeed*fuseSpeed;
    }

    // ---------- Vine (Forest Wraith) ----------
    static readonly Color VineColor=new Color(.55f,1f,.4f);
    static readonly Color LeafColor=new Color(1f,.55f,.2f);

    void TickVine(Vector2 delta,float distance)
    {
        bool lineClear=ClearLine(transform.position,target.position);
        if(distance<=vineLength*.85f&&lineClear&&Time.time>=nextAttack)
        {
            aim=delta.sqrMagnitude>.001f?delta.normalized:Vector2.right;
            nextAttack=Time.time+Mathf.Max(Duration+vineWarning+.2f,Cooldown);
            attack=StartCoroutine(VineLash());
            return;
        }
        // เข้าไปยืนระยะฟาดสบาย ๆ แล้วรอจังหวะ ไม่ต้องชิดตัว
        bool shouldWalk=distance>1.6f||!lineClear;
        animator.SetBool("isWalking",shouldWalk);
        if(shouldWalk)move=nav.DirectionTo(target.position)*MoveSpeed;
        else body.linearVelocity=Vector2.zero;
        if(shouldWalk&&nav.BlockingWall!=null)SmashOrWait(nav.BlockingWall);
    }

    // แถบเตือนยืดจากตัวไปตามทิศที่ล็อกไว้ (หลบด้านข้างทัน) สุดแถบแล้วฟาด ยืนในแถบ = โดน
    IEnumerator VineLash()
    {
        IsAttacking=true;AttacksStarted++;move=Vector2.zero;body.linearVelocity=Vector2.zero;Warn();
        animator.SetBool("isWalking",false);
        display.flipX=aim.x<0;
        Vector2 origin=(Vector2)transform.position+aim*.3f;
        var strip=warningMarker=new GameObject("VineWarning");
        strip.transform.SetParent(transform.parent,true);
        strip.transform.SetPositionAndRotation(origin,Quaternion.Euler(0f,0f,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg));
        var bar=Layer(strip.transform,ProceduralSprites.Bar,1);
        for(float t=0f;t<vineWarning;t+=Time.deltaTime)
        {
            float k=t/vineWarning;
            bar.transform.localScale=new Vector3(vineLength*Mathf.Lerp(.15f,1f,1f-(1f-k)*(1f-k)),vineWidth,1f);
            bar.color=new Color(VineColor.r,VineColor.g,VineColor.b,.35f+.4f*Mathf.Abs(Mathf.Sin(t*16f)));
            yield return null;
        }
        animator.ResetTrigger("Attack");animator.SetTrigger("Attack");
        const float impact=3f/12f; // เฟรมที่เถาวัลย์ยืดสุด
        yield return new WaitForSeconds(impact);
        Destroy(strip);warningMarker=null;
        Sfx.PlayAt(SfxId.MonVine,origin);
        for(int i=1;i<=4;i++)ImpactSparks.Spawn(origin+aim*vineLength*i/4f,LeafColor,3,aim,2.5f,90f);
        if(target!=null)
        {
            Vector2 offset=(Vector2)target.position-origin;
            float along=Vector2.Dot(offset,aim);
            float side=Mathf.Abs(aim.x*offset.y-aim.y*offset.x);
            if(along>=-.3f&&along<=vineLength&&side<=vineWidth*.5f+.3f)
            {Hit(target.GetComponent<PlayerStats>(),AttackDamage,origin,8f);MeleeImpacts++;}
        }
        yield return new WaitForSeconds(Duration-impact);
        IsAttacking=false;attack=null;
    }

    // ---------- Root (Rootlings) ----------
    static readonly Color RootWarnColor=new Color(.62f,.9f,.3f);
    static readonly Color DirtColor=new Color(.55f,.4f,.25f);
    float outOfRangeSince=-1f;
    bool burrowing;
    GameObject warningMarker; // วงเตือนที่กำลังขึ้นอยู่ ตาย/โดนขัดกลางท่าต้องลบทิ้ง ไม่งั้นค้างอยู่บนพื้น

    void TickRoot(float distance)
    {
        move=Vector2.zero;body.linearVelocity=Vector2.zero;animator.SetBool("isWalking",false);
        if(distance<=rootRange)
        {
            outOfRangeSince=-1f;
            if(Time.time<nextAttack)return;
            nextAttack=Time.time+Mathf.Max(1.5f,Cooldown);
            attack=StartCoroutine(RootStrike(target.position));
            return;
        }
        if(outOfRangeSince<0f)outOfRangeSince=Time.time;
        else if(Time.time-outOfRangeSince>=burrowAfter)attack=StartCoroutine(Burrow());
    }

    // วงเตือนตรงเท้าผู้เล่น (ตำแหน่งตอนเริ่มร่าย เดินหนีทัน) แล้วรากแทงขึ้น โดน = ดาเมจ + พิษ
    IEnumerator RootStrike(Vector2 spot)
    {
        IsAttacking=true;AttacksStarted++;Warn();
        animator.ResetTrigger("Attack");animator.SetTrigger("Attack");
        var warn=warningMarker=new GameObject("RootWarning");
        warn.transform.SetParent(transform.parent,true);warn.transform.position=spot;
        var disc=Layer(warn.transform,ProceduralSprites.Disc,1);
        var ring=Layer(warn.transform,ProceduralSprites.DashedRing,2);
        for(float t=0f;t<rootWarning;t+=Time.deltaTime)
        {
            float k=t/rootWarning;
            disc.transform.localScale=Vector3.one*rootRadius*2f*Mathf.Lerp(.2f,1f,k); // วงทึบขยายเต็ม = รากขึ้น
            ring.transform.localScale=Vector3.one*rootRadius*2f;
            disc.color=new Color(RootWarnColor.r,RootWarnColor.g,RootWarnColor.b,.18f+.22f*k);
            ring.color=new Color(RootWarnColor.r,RootWarnColor.g,RootWarnColor.b,.5f+.4f*Mathf.Abs(Mathf.Sin(t*14f)));
            yield return null;
        }
        Destroy(warn);warningMarker=null;
        Sfx.PlayAt(SfxId.MonRoot,spot);
        if(rootPrefab!=null)
        {
            var root=Instantiate(rootPrefab,spot,Quaternion.identity,transform.parent);
            root.transform.localScale=Vector3.one*rootScale;
            foreach(var view in root.GetComponentsInChildren<SpriteRenderer>())view.sortingLayerName="object";
            Destroy(root,.75f);
        }
        ImpactSparks.Spawn(spot,DirtColor,8,Vector2.up,3.5f,70f);
        if(target!=null&&Vector2.Distance(target.position,spot)<=rootRadius)
        {Hit(target.GetComponent<PlayerStats>(),AttackDamage,spot,4f);MeleeImpacts++;}
        yield return new WaitForSeconds(.25f);
        IsAttacking=false;attack=null;
    }

    // มุดดิน: จมหาย ย้ายไปโผล่ในห้องเดิมใกล้ผู้เล่น (ไม่ประชิดเกิน) ระหว่างอยู่ใต้ดินตีไม่โดน
    IEnumerator Burrow()
    {
        var owner=GetComponent<MonsterController>();
        Vector2 spot;
        if(owner==null||!SpawnPlacement.TryPickNear(owner.currentRoom,gameObject,target.position,4.5f,target.position,2.5f,out spot))
        {outOfRangeSince=Time.time;yield break;}
        IsAttacking=true;burrowing=true;outOfRangeSince=-1f;
        Sfx.PlayAt(SfxId.MonBurrow,transform.position);
        animator.SetBool("isWalking",true); // ท่าเดินของ Rootlings คือกองดินที่หดหัวลง
        SetBodyActive(false);
        ImpactSparks.Spawn(transform.position,DirtColor,10,Vector2.up,3f,80f);
        yield return Fade(1f,0f,.35f);
        body.position=spot;transform.position=spot;
        ImpactSparks.Spawn(spot,DirtColor,10,Vector2.up,3f,80f);
        yield return Fade(0f,1f,.35f);
        EndBurrow();
        nextAttack=Mathf.Max(nextAttack,Time.time+.8f); // โผล่แล้วให้ผู้เล่นตั้งตัวก่อนรากแรก
        IsAttacking=false;attack=null;
    }

    IEnumerator Fade(float from,float to,float time)
    {
        for(float t=0f;t<time;t+=Time.deltaTime)
        {
            var c=display.color;display.color=new Color(c.r,c.g,c.b,Mathf.Lerp(from,to,t/time));
            yield return null;
        }
        var end=display.color;display.color=new Color(end.r,end.g,end.b,to);
    }

    void SetBodyActive(bool on){foreach(var c in GetComponents<Collider2D>())if(!c.isTrigger)c.enabled=on;}

    // จบมุดดิน (หรือโดนขัดกลางทาง): ตัวกลับมาเห็น/โดนตีได้ ไม่ค้างหายใต้ดิน
    void EndBurrow()
    {
        if(!burrowing)return;
        burrowing=false;SetBodyActive(true);
        if(display!=null){var c=display.color;display.color=new Color(c.r,c.g,c.b,1f);}
        if(animator!=null)animator.SetBool("isWalking",false);
    }

    SpriteRenderer Layer(Transform parent,Sprite sprite,int order)
    {
        var go=new GameObject("Layer");go.transform.SetParent(parent,false);
        var view=go.AddComponent<SpriteRenderer>();view.sprite=sprite;view.sortingLayerName="bg2";view.sortingOrder=order;
        return view;
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

    // มอนเป็นตัวฟิสิกส์ที่ชนกันเอง (ไม่ยืนซ้อนกัน) ตัวที่พุ่งด้วยความเร็วจึงเคยชนเพื่อนกระเด็นไถลไปไม่หยุด แก้สองชั้น:
    // 1) ระหว่างพุ่ง ตัวพุ่งเบาลงมาก ชนเพื่อนแล้วโดนกั้นเหมือนชนกำแพง ส่งแรงให้เพื่อนแทบไม่ได้
    // 2) มอนที่ไม่ได้พุ่งและไม่ได้โดนกระแทกจากผู้เล่น ไม่เก็บความเร็วที่ได้จากการถูกชนไว้ (เดินด้วย MovePosition ไม่ใช้ความเร็วอยู่แล้ว)
    bool dashing;float restMass=1f;
    void Dash(bool on){dashing=on;if(body!=null)body.mass=on?Mathf.Max(.0001f,restMass*.001f):restMass;}
    void FixedUpdate()
    {
        if(body!=null&&!dashing&&(owner==null||!owner.IsKnockedBack))body.linearVelocity=Vector2.zero;
        if(body!=null&&move.sqrMagnitude>0&&!IsAttacking)
            body.MovePosition(body.position+move*BlessingManager.MonsterSpeedFactor(body.position)*Time.fixedDeltaTime); // สนามชะลอระดับ 3
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
            Sfx.PlayAt(SfxId.MonLunge,transform.position);
            // Echo Stalker พุ่งเข้าหาช่วงง้างท่า แล้วค่อยฟันที่เฟรมกระทบ (ชนกำแพงก็หยุดเองเพราะใช้ความเร็ว)
            // ถึงตัวผู้เล่นแล้วหยุด ไม่พุ่งทะลุไปซ้อนทับ
            Dash(true);
            for(float t=0f;t<impact;t+=Time.deltaTime)
            {
                bool arrived=target!=null&&Vector2.Distance(transform.position,target.position)<=lungeStopDistance;
                body.linearVelocity=arrived?Vector2.zero:aim*lungeSpeed;
                yield return null;
            }
            Dash(false);
            body.linearVelocity=Vector2.zero;
        }
        else yield return new WaitForSeconds(impact);
        if(target!=null && data!=null)
        {
            if(ranged)ReleaseProjectile();
            else
            {
                MeleeImpacts++;
                if(style!=Style.Lunge)Sfx.PlayAt(style==Style.RockAndSlam?SfxId.MonSlam:SfxId.MonMelee,transform.position);
                Vector2 delta=target.position-transform.position;
                // ตรวจซ้ำที่เฟรมกระทบ: หลบออกจากระยะหรือไปหลังกำแพงแล้วไม่โดน
                if(delta.magnitude<=meleeDistance && Vector2.Dot(delta.normalized,aim)>.15f && ClearLine(transform.position,target.position))
                    Hit(target.GetComponent<PlayerStats>(),AttackDamage,transform.position,style==Style.Rifle&&closeRangeSlash?10f:6f);
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
        Sfx.PlayAt(SfxId.MonPounce,transform.position);
        bool hit=false;
        Dash(true);
        for(float elapsed=0;elapsed<.32f;elapsed+=Time.deltaTime)
        {
            if(target==null)break;
            Vector2 delta=(Vector2)target.position-body.position;
            bool arrived=delta.magnitude<=lungeStopDistance;
            body.linearVelocity=arrived||!ClearLine(body.position,body.position+aim*.65f)?Vector2.zero:aim*lungeSpeed;
            if(pounceTrail&&!arrived&&Time.time>=nextTrail){nextTrail=Time.time+.05f;SpriteGhost.Spawn(display,.22f,.5f,trailColor);}
            if(!hit && delta.magnitude<=meleeDistance && ClearLine(body.position,target.position))
            {Hit(target.GetComponent<PlayerStats>(),AttackDamage,transform.position);MeleeImpacts++;hit=true;Sfx.PlayAt(SfxId.MonBite,transform.position);}
            yield return null;
        }
        Dash(false);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.13f);IsAttacking=false;attack=null;
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
        obj.AddComponent<MonsterAttackProjectile>().Launch(this,aim,projectileSpeed,projectileLifetime,AttackDamage,style==Style.RockAndSlam);
        ShotsReleased++;
        if(style==Style.RockAndSlam)Sfx.PlayAt(SfxId.MonRockThrow,origin);else Sfx.PlayMonsterShot(data,origin);
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

    // คืน true ถ้าโดนจริง (เลือดลด) ใช้ต่อพิษ/ผลพิเศษเฉพาะตอนโดน
    public static bool DamagePlayer(PlayerStats stats,float damage,Vector2 source,float knockback=6f)
    {
        if(stats==null||stats.isDead)return false;
        float previous=stats.currentHP;stats.TakeDamage(damage);
        if(stats.currentHP>=previous)return false;
        var movement=stats.GetComponent<PlayerMovement>();
        if(movement!=null)movement.TakeKnockback(source,knockback);
        return true;
    }

    // โดนตี/โดนกระสุนของตัวนี้: ดาเมจ + ผลักกระเด็น + พิษ (ถ้าตั้งไว้)
    public void Hit(PlayerStats stats,float damage,Vector2 source,float knockback=6f)
    {
        if(DamagePlayer(stats,damage,source,knockback)&&poisonSeconds>0f)stats.ApplyPoison(poisonSeconds,poisonDamage);
    }

    // มอนที่เพิ่งโผล่: ห้ามโจมตีก่อนเวลานี้ (MonsterController ตั้งให้ตอนเกิดจากวงเตือน)
    public void HoldAttacksUntil(float time){nextAttack=Mathf.Max(nextAttack,time);}

    public void CancelAttack()
    {
        move=Vector2.zero;
        if(attack!=null)StopCoroutine(attack);
        attack=null;IsAttacking=false;
        EndBurrow();
        if(warningMarker!=null){Destroy(warningMarker);warningMarker=null;}
        Dash(false);
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
