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
    protected MonsterNavigator navigator; // เดินอ้อมเสา/กำแพง (ตัวที่มี MonsterCombatActions ใช้ของตัวเอง)

    // ตัวคูณดาเมจที่รับ (บอสป่าช่วงร้อนเกิน/สตันรับแรงขึ้น เกราะรากรับน้อยลง)
    protected virtual float DamageTakenScale => 1f;
    // เลือดล็อกไม่ให้ต่ำกว่านี้ (บอสป่า: ตีแรงแค่ไหนก็ข้ามเฟสสุดท้ายไม่ได้ ต้องเข้าเฟสก่อน)
    protected virtual float HealthFloor => 0f;
    // แบบตัวเลขดาเมจที่เด้ง (ตีโดนเกราะ = เลขเทาเล็ก)
    protected virtual DamageNumbers.Kind HitKind => DamageNumbers.Kind.Enemy;

    // ฟื้นเลือด (ไม่เกินเต็ม) หลอดเลือดบอสขยับตาม
    protected void RestoreHealth(float amount)
    {
        if (!IsAlive || myData == null || amount <= 0f) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        if (bossHud != null) bossHud.RefreshHealth(currentHealth, MaxHealth);
    }

    // เลือดที่เหลือเทียบเต็ม (Zero Husk ใช้เช็คเกณฑ์ระเบิดตัวเอง)
    public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(currentHealth / MaxHealth) : 1f;

    // ตัวคูณเฉพาะตัว (มอนผิดเพี้ยนตั้งตอนเกิด ก่อน Start) MonsterData ใช้ร่วมกันทุกตัว แก้ตรงนั้นไม่ได้
    public float HealthScale { get; set; } = 1f;
    public float AttackScale { get; set; } = 1f;
    public float AttackCooldownScale { get; set; } = 1f;
    public float SpeedScale { get; set; } = 1f;
    public float ArmorScale { get; set; } = 1f;   // รับดาเมจตามสัดส่วนนี้ (มอนผิดเพี้ยนเกราะหนา = 0.5 จนกว่าจะเซ)
    public float MaxHealth => myData != null ? myData.maxHealth * HealthScale : 0f;

    // คอนโซลทดสอบ: หุ่นฝึกยืนนิ่งตลอด (Frozen) / หยุด AI มอนและบอสทุกตัว (DevCheats.FreezeMonsters) ยังโดนตี เซ ตายได้
    public bool Frozen { get; set; }
    protected bool IsHeld => Frozen || DevCheats.FreezeMonsters;
    private bool frozenNow;

    // ระเบิด/สลายตัวเอง นับเป็นตายปกติ (ประตูห้อง ตัวนับศัตรู พรเก็บเกี่ยวพลังงาน ทำงานเหมือนโดนฆ่า)
    public void SelfDestruct()
    {
        if (!IsAlive) return;
        currentHealth = 0f;
        Die();
    }

    // ยังสู้อยู่ (สกิลใช้เช็คก่อนทำดาเมจ/เล็งเป้า)
    public bool IsAlive => !isDying && currentHealth > 0f && gameObject.activeInHierarchy;

    // ติดสตัน (กับดักแม่เหล็กไฟฟ้า): หยุดเดิน/โจมตีชั่วคราว ตัวเป็นสีฟ้า
    public bool IsStunned => Time.time < stunnedUntil;
    private float stunnedUntil;
    private bool stunTinted;
    private static readonly Color StunTint = new Color(0.55f, 0.9f, 1f, 1f);

    // เพิ่งโผล่จากวงเตือน (RoomController): ยืนนิ่งหันหาผู้เล่นจนถึงเวลานี้ ก่อนเริ่มเดิน/โจมตี
    private float awakeAt;

    [Header("ตอนตาย")]
    public float deathLinger = 0.6f; // นอนค้างท่าตายให้เห็นก่อนค่อยจางหาย
    public float deathFade = 0.4f;
    // คงชุดโจมตีเฉพาะตัวจาก Boss_main พร้อมรองรับบอสที่สืบทอดจาก MainMenu
    private MonsterCombatActions combatActions;
    protected MonsterFx fx; // เงา กะพริบขาว หายใจ/เด้ง สลายตอนตาย (ใส่ให้เองตอนเริ่ม)
    private BossHealthHudLink bossHud;
    // เสียงร้องของตัวมอน: ตัวเดียวกันร้องถี่กว่านี้ไม่ได้ ไม่งั้นโดนตีรัว ๆ เสียงจะซ้อนกันเป็นพรืด (บอสเลือดเยอะ โดนตีนาน เว้นห่างกว่า)
    private const float HurtVoiceGap = 0.5f, BossHurtVoiceGap = 2f, AttackVoiceGap = 0.8f;
    private float nextHurtVoice, nextAttackVoice;
    [HideInInspector] public RoomController currentRoom;

    protected virtual void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 
        navigator = new MonsterNavigator(transform, this);

        if (myData != null) currentHealth = MaxHealth;

        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        if (hero != null) player = hero.transform;
        IgnorePlayerCollisions();
        fx = MonsterFx.Attach(this);
        combatActions = GetComponent<MonsterCombatActions>();
        if (combatActions != null) combatActions.Initialize(myData, player);
        // โผล่พร้อมกันหลายตัว: เหลื่อมจังหวะโจมตีแรกของแต่ละตัว ไม่ยิง/ตีพร้อมกันเป๊ะตอนตื่น
        if (awakeAt > 0f)
        {
            float firstAttack = awakeAt + Random.Range(0.1f, 0.5f);
            nextAttackTime = Mathf.Max(nextAttackTime, firstAttack);
            if (combatActions != null) combatActions.HoldAttacksUntil(firstAttack);
        }
        bossHud = GetComponent<BossHealthHudLink>();
        // เปิดหลอดเมื่อบอสถูกเสกในห้องต่อสู้ ไม่เปิดระหว่างดูตัวอย่างอนิเมชัน
        if (bossHud != null && myData != null && currentRoom != null)
            bossHud.BeginFight(currentHealth, MaxHealth, player != null ? player.GetComponent<PlayerStats>() : null);
    }

    protected virtual void Update()
    {
        if (isDying) return;
        if (UpdateStun()) return;
        if (Waking()) return;
        if (isKnockedBack) return;
        if (HoldStill()) return;

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
                Vector2 step = navigator.DirectionTo(player.position) * myData.moveSpeed * SpeedScale * Time.deltaTime
                               * BlessingManager.MonsterSpeedFactor(transform.position); // สนามชะลอระดับ 3
                transform.position += (Vector3)step;
            }
            else 
            {
                anim.SetBool("isWalking", false);

                if (Time.time >= nextAttackTime)
                {
                    anim.SetTrigger("Attack");
                    
                
                    HitPlayer(myData.attackDamage * AttackScale, 6f); 

                    nextAttackTime = Time.time + myData.attackCooldown * AttackCooldownScale;
                }
            }
        }
    }

    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (combatActions == null && myData != null && collision.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            HitPlayer(myData.attackDamage * AttackScale, 6f); 
        }
    }

    
    protected void HitPlayer(float damage, float knockbackForce)
    {
        if (player != null)
        {
            PlayerStats pStats = player.GetComponent<PlayerStats>();
            PlayerMovement pMove = player.GetComponent<PlayerMovement>();

            bool landed = pStats == null || pStats.CanTakeHit; // ติดอมตะอยู่ ไม่โดนผลักซ้ำ
            if (pStats != null) pStats.TakeDamage(damage); 
            
            if (landed && pMove != null) pMove.TakeKnockback(transform.position, knockbackForce);
        }
    }

    // เรียกทันทีหลังเสก (ก่อน Start) ผู้เล่นมีจังหวะตั้งตัวก่อนมอนเริ่มเดิน/โจมตี
    public void WakeUpAfter(float seconds)
    {
        awakeAt = Time.time + Mathf.Max(0f, seconds);
    }

    // ระหว่างตื่น: ยืนนิ่ง หันหน้าหาผู้เล่น (โดนตีได้ตามปกติ)
    protected bool Waking()
    {
        if (Time.time >= awakeAt) return false;
        if (anim != null) anim.SetBool("isWalking", false);
        if (player != null && sr != null) sr.flipX = player.position.x < transform.position.x;
        return true;
    }

    // ผลักกระเด็นออกจากจุด from (พรเกราะฉุกเฉินระดับ 3 ตอนเกราะแตก) บอสไม่กระเด็น
    public void Shove(Vector2 from, float speed)
    {
        if (!IsAlive || ResistsKnockback || rb == null || !gameObject.activeInHierarchy) return;
        StartCoroutine(ShoveRoutine(from, speed));
    }

    private IEnumerator ShoveRoutine(Vector2 from, float speed)
    {
        if (combatActions != null) combatActions.CancelAttack();
        isKnockedBack = true;
        Vector2 away = (Vector2)transform.position - from;
        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
        rb.linearVelocity = away.normalized * speed;
        yield return new WaitForSeconds(0.25f);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
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

    // poise = แรงกระแทกของการโดนครั้งนี้ (อาวุธส่งมาตามชนิด ดู WeaponData.PoiseDamage) สกิล/พรไม่มี
    public void TakeDamage(float damageAmount, float poise = 0f)
    {
        if (!gameObject.activeInHierarchy || currentHealth <= 0) return;
        // มอนรับดาเมจจากผู้เล่นเท่านั้น จึงสุ่มคริติคอลตรงนี้ได้ครบทุกอาวุธ/สกิล/พร
        bool crit = damageAmount > 0f && Random.value < PlayerStats.CritChance;
        if (crit)
        {
            damageAmount *= PlayerStats.CritMultiplier;
            ImpactSparks.Spawn(DamageNumbers.Above(sr, transform.position), new Color(1f, 0.85f, 0.3f), 8, Vector2.zero, 4.5f);
        }
        bool staggerHit = IsStaggered;
        if (staggerHit) damageAmount *= StaggerDamageScale;
        damageAmount *= DamageTakenScale * ArmorScale;
        currentHealth = Mathf.Max(currentHealth - damageAmount, Mathf.Min(HealthFloor, currentHealth)); // เลือดไม่ต่ำกว่าเพดานล็อก
        var kind = ArmorScale < 1f ? DamageNumbers.Kind.Armored : HitKind;
        if (damageAmount > 0f) Sfx.PlayAt(kind == DamageNumbers.Kind.Armored ? SfxId.HitArmor : SfxId.HitMonster, transform.position);
        if (damageAmount > 0f && currentHealth > 0f && Time.time >= nextHurtVoice)
        {
            nextHurtVoice = Time.time + (bossHud != null ? BossHurtVoiceGap : HurtVoiceGap);
            Sfx.PlayMonsterVoice(myData, SfxLibrary.Voice.Hurt, transform.position);
        }
        DamageNumbers.Spawn(DamageNumbers.Above(sr, transform.position), damageAmount, kind, crit: (crit || staggerHit) && kind == DamageNumbers.Kind.Enemy, side:
            player != null ? transform.position.x - player.position.x : 0f); // เลขกระเด็นไปทางเดียวกับมอน
        if (bossHud != null && myData != null) bossHud.RefreshHealth(currentHealth, MaxHealth);
        if (currentHealth > 0) ApplyPoise(poise);
        
    
        StartCoroutine(DamageEffectRoutine());

        if (currentHealth <= 0) Die();
    }

    
    // ---------- แรงกระแทก: อาวุธหนักทำให้มอนเซ ----------
    // ค่าทนแรงกระแทกที่มองไม่เห็น โดนตีลดตามชนิดอาวุธ (ค้อน/หอกมาก ดาบกลาง มีด/กรงเล็บ/ปืนน้อย) หมดแล้วเซ
    // เซ = สตัน + รับดาเมจแรงขึ้น (เลขเหลือง) มีดาวฟ้าวนเหนือหัว หลังเซกันเซซ้ำสักพัก (ค้อนตีรัวจนมอนขยับไม่ได้ไม่ได้)
    // ไม่โดนตีสักพักค่าทนฟื้นเต็ม ตัวใหญ่เลือดเยอะทนกว่า บอสไม่เซ (มีกลไกของตัวเอง)
    public const float StaggerTime = 1.2f;
    public const float StaggerDamageScale = 1.3f;
    const float PoiseRecoverDelay = 2.5f;
    const float StaggerImmunity = 3f;
    protected virtual bool CanStagger => bossHud == null;
    public float PoiseScale { get; set; } = 1f;   // มอนผิดเพี้ยนทนกว่า
    public bool IsStaggered => Time.time < staggeredUntil;
    public event System.Action<MonsterController> Staggered; // มอนผิดเพี้ยนเกราะหนา: เซแล้วเกราะแตก
    private float poise, lastPoiseHit = -99f, staggeredUntil, staggerImmuneUntil;
    private float PoiseMax => Mathf.Clamp(30f + MaxHealth * 0.45f, 40f, 100f) * PoiseScale;

    private void ApplyPoise(float amount)
    {
        if (amount <= 0f || !CanStagger || !IsAlive || Time.time < staggerImmuneUntil) return;
        if (Time.time - lastPoiseHit > PoiseRecoverDelay) poise = 0f;
        lastPoiseHit = Time.time;
        poise += amount;
        if (poise < PoiseMax) return;
        poise = 0f;
        staggeredUntil = Time.time + StaggerTime;
        staggerImmuneUntil = staggeredUntil + StaggerImmunity;
        Stun(StaggerTime);
        StaggerStars.Play(this, sr, StaggerTime);
        Staggered?.Invoke(this);
    }

    // บอสไม่กระเด็นตอนโดนตี (กันโดนตีรัว ๆ จนร่ายท่าไม่ออก) ยังกะพริบแดงให้รู้ว่าโดน
    // รากที่ฝังดิน (Rootlings) ก็ไม่ไถลไปตามแรงตี
    protected virtual bool ResistsKnockback => combatActions != null && combatActions.style == MonsterCombatActions.Style.Root;

    private IEnumerator DamageEffectRoutine()
    {
        // กะพริบขาว + ยุบตัว (ไม่มี shader เอฟเฟกต์ก็กลับไปย้อมแดงแบบเดิม)
        bool flashed = fx != null && fx.Active;
        if (flashed) fx.Hit();
        if (ResistsKnockback)
        {
            if (!flashed) sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (!flashed) sr.color = Color.white;
            yield break;
        }
        if (combatActions != null) combatActions.CancelAttack();
        isKnockedBack = true;
        if (!flashed) sr.color = Color.red;

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

    // บอสที่เขียน Update เองเรียกก่อนเลือกท่า: หยุด AI อยู่ = ยืนนิ่ง ไม่เริ่มท่าใหม่
    protected bool HoldStill()
    {
        bool hold = IsHeld;
        if (hold && !frozenNow)
        {
            if (combatActions != null) combatActions.CancelAttack();
            if (anim != null) anim.SetBool("isWalking", false);
        }
        frozenNow = hold;
        return hold;
    }

    // ท่าของบอสที่ร่ายอยู่ตอนกดหยุด AI ค้างไว้ที่จังหวะนั้น ปล่อยแล้วทำต่อ
    protected IEnumerator PausedWhileHeld(IEnumerator move) => DevCheats.Pausable(move, () => IsHeld);

    // คอนโซลทดสอบ: ตั้งเลือดเป็นสัดส่วนของเลือดเต็มทันที (บอสเช็คเฟสเองใน Update) เลือดล็อกของบอสยังมีผล
    public void SetHealthForTesting(float fraction)
    {
        if (!IsAlive || myData == null) return;
        float target = Mathf.Clamp(fraction, 0.01f, 1f) * MaxHealth;
        currentHealth = Mathf.Max(target, Mathf.Min(HealthFloor, currentHealth));
        if (bossHud != null) bossHud.RefreshHealth(currentHealth, MaxHealth);
    }

    // เสียงร้องตอนเริ่มท่าโจมตี (MonsterCombatActions เรียกพร้อมวาบเตือนก่อนโจมตี)
    public void AttackVoice()
    {
        if (Time.time < nextAttackVoice) return;
        nextAttackVoice = Time.time + AttackVoiceGap;
        Sfx.PlayMonsterVoice(myData, SfxLibrary.Voice.Attack, transform.position);
    }

    public static event System.Action<MonsterController> Died; // ทุกตัวที่ตาย รวมลูกน้องบอส (พรเก็บเกี่ยวพลังงานฟังอยู่)

    protected virtual void Die()
    {
        SummaryManager.enemiesDefeatedCount++;
        Died?.Invoke(this);
        if (bossHud == null) // บอสมีเสียงตายของตัวเอง
        {
            Sfx.PlayAt(SfxId.MonsterDeath, transform.position);
            Sfx.PlayMonsterVoice(myData, SfxLibrary.Voice.Death, transform.position);
        }
        // นับว่าตายทันที ประตูห้องจะได้เปิดตอนตัวสุดท้ายล้ม ไม่ต้องรอท่าตายจบ
        if (currentRoom != null) currentRoom.OnMonsterDied(this);
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

        var burst=GetComponent<ZeroHuskDeathBurst>();
        if(burst!=null)
        {
            if(anim!=null)anim.enabled=false;
            yield return burst.Play(player);
            gameObject.SetActive(false);yield break;
        }

        // วาบขาว ประกายแตกสีประจำแมพ ค้างท่าตายให้เห็น แล้วสลายเป็นเม็ดพิกเซล (ไม่มี shader เอฟเฟกต์ก็จางแบบเดิม)
        if (fx != null) fx.BeginDeath(SpawnTelegraph.ColorFor(this, SpawnTelegraph.Emphasis.Normal));
        yield return new WaitForSeconds(deathLinger);

        if (fx != null && fx.Active && deathFade > 0f)
        {
            float time = deathFade + 0.25f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                fx.Dissolve(t / time);
                yield return null;
            }
        }
        else if (sr != null && deathFade > 0f)
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

    // ผู้เล่นเดินชนแล้วผลักมอนสเตอร์ไม่ได้ และมอนก็ไม่ดันผู้เล่น: ปิดการชนทางฟิสิกส์ระหว่างกันเป็นคู่ ๆ
    // (ไม่ต้องตั้ง layer) ผู้เล่นยังเดินทะลุไม่ได้ เพราะ PlayerMovement เช็คแล้วหยุด/ไถลเลียบตัวมอนเอง
    // มอนสเตอร์ด้วยกันยังชนกันตามปกติ ไม่ยืนซ้อนกัน
    // บอสที่ปิด collider ชั่วคราว (วาร์ป) เรียกซ้ำหลังเปิดกลับ กันสถานะไม่ชนกันหาย
    protected void IgnorePlayerCollisions()
    {
        if (player == null) return;
        var own = GetComponentsInChildren<Collider2D>(true);
        foreach (var theirs in player.GetComponentsInChildren<Collider2D>(true))
            foreach (var mine in own)
                Physics2D.IgnoreCollision(mine, theirs, true);
    }

    // บอสบางตัวยังไม่มีท่าตาย (ไม่มีพารามิเตอร์ isDead) ถ้าสั่งไปจะมีคำเตือนเต็ม Console
    private static bool HasParameter(Animator animator, string name)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.name == name) return true;
        return false;
    }
}
