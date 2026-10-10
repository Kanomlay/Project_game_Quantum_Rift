using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// บอสประจำแมพ 2 ตามตาราง 1.8: ยักษ์ไม้โบราณ (ภาพชุด AncientEntborn: เดิน / ร่ายเลเซอร์ / กระทืบ + ลำแสง + รากแทง)
// แบ่ง 3 เฟสตามเลือด แต่ละเฟสเพิ่มท่าใหม่
//   เปิดตัว: ผุดจากดิน รากแทงรอบตัว จอสั่น
//   เฟส 1 (100–60%):
//     เลเซอร์ไม้ — ประกายฟ้าดูดเข้ามือ หินบนตัวเรือง เส้นบางกะพริบเตือน แล้วลำแสงเต็มหยุดที่กำแพง
//                  ยิงเสร็จ "ร้อนเกิน" ยืนนิ่งเรืองฟ้ามีไอพลังงาน รับดาเมจ +50% (จังหวะตีสวน)
//     กระทืบ     — คลื่นกระแทกรอบเท้า แล้วรากแทงหลายจุดรอบผู้เล่น (วงเตือนทุกจุด โดนแล้วติดพิษ)
//   เฟส 2 (60–30%): คำรามสั้น แล้วเพิ่ม
//     กระโดดทับ  — ย่อตัวลอยพ้นจอ เงาเป้าไล่ตามผู้เล่นแล้วล็อก ตกลงมาพร้อมคลื่นกระแทกและรากเป็นวงรอบจุดตก
//     กรงราก     — รากแทงเป็นวงล้อมผู้เล่นค้างเป็นรั้ว เว้นช่องหนี แล้วรากใหญ่แทงกลางวง
//     เรียก Rootlings โผล่รอบผู้เล่น
//     เลเซอร์ 3 สาย (พัด) และกระทืบแล้วต่อเลเซอร์ทันที
//   เฟส 3 (ต่ำกว่า 30%) "ป้อมรากโบราณ": คลั่ง ออร่าเขียว ท่าถี่ขึ้น เลเซอร์ 3 สายกวาดตามผู้เล่น รากไล่เป็นแนว กรงรากถี่ขึ้น
//     รากดูดพลัง — รากโผล่ 3 ต้นโยงสายพลังหาบอส ระหว่างนั้นบอสปักหลัก มีเกราะรับดาเมจแค่ 10% และฟื้นเลือด (ไม่เกิน 45%)
//                  ตีรากแตกครบ บอสสตันรับดาเมจ +50% ผ่านไป 12 วินาทีรากงอกใหม่ เกราะกลับมา
//     ยืนหยัดครั้งสุดท้าย — เลือดหมดครั้งแรกยังไม่ตาย อมตะ 6 วินาที เลเซอร์หมุนรอบตัว + รากแทงทั่วห้อง รอดครบบอสพังทลาย
//   ตาย: ตัวสั่น รากแตกออกรอบตัว ลูกน้องสลายตาม แล้วสลายเป็นพิกเซลแบบมอนทุกตัว (ชุดภาพไม่มีท่าตาย)
//
// สืบทอด MonsterController แบบเดียวกับ EchoCommanderBoss: RoomController เสกได้ ประตูปิด/เปิดตามระบบห้องเดิม
// ค่าเลือด/ดาเมจ/ความเร็วอ่านจาก MonsterData (ดาเมจ = เลเซอร์/กระโดดทับ รากแรงครึ่งหนึ่ง)
public class AncientEntbornBoss : MonsterController
{
    [Header("เลเซอร์ไม้")]
    public Sprite[] laserFrames;              // 7 เฟรม: 0–1 เส้นบาง (ใช้เป็นเส้นเตือน), 2–4 ลำแสงเต็ม, 5–6 สลาย
    [Min(1f)] public float laserRange = 11f;  // ผู้เล่นอยู่ในระยะนี้ถึงยิง
    [Min(1f)] public float laserMaxLength = 14f;
    [Min(0.1f)] public float laserWarning = 0.9f;
    [Min(0.1f)] public float laserActive = 0.55f;
    [Min(0.1f)] public float laserHitRadius = 0.45f; // ครึ่งความกว้างที่โดน (วัดถึงกลางตัวผู้เล่น)
    [Min(0.5f)] public float laserCooldown = 5f;
    [Range(0f, 90f)] public float laserAimClamp = 55f; // แขนชี้ได้แค่ซ้าย/ขวา ลำแสงเอียงได้ไม่เกินนี้
    public Vector2 muzzle = new Vector2(1.25f, 1.1f);  // ปลายมือในท่าชี้ (หันขวา วัดจากภาพ LaserCast เฟรม 3)
    [Min(0f)] public float overheatTime = 1.5f;        // ยืนนิ่งรับดาเมจแรงขึ้นหลังยิง
    [Min(0f)] public float rageOverheatTime = 0.8f;    // เฟส 3 ร้อนเกินสั้นลง
    [Min(1f)] public float overheatScale = 1.3f;
    [Min(1f)] public float stunnedScale = 1.5f;        // สตันจากรากแตกครบ
    [Range(0f, 60f)] public float fanAngle = 25f;      // เลเซอร์ 3 สาย (เฟส 2 ขึ้นไป) กาง ±องศานี้

    [Header("กระทืบ + รากแทง")]
    public Sprite[] rootFrames;               // 7 เฟรม: ผุด 0–3 (3 สูงสุด) หด 4–6
    public GameObject rootPrefab;             // สำรองถ้าไม่มีภาพรายเฟรม
    [Min(0.1f)] public float rootScale = 0.7f;
    [Min(1)] public int rootCount = 4;        // จุดแรกใต้เท้าผู้เล่น ที่เหลือกระจายรอบ ๆ
    [Min(0.5f)] public float rootSpread = 2.6f;
    [Min(0.1f)] public float rootWarning = 0.9f;
    [Min(0.1f)] public float rootRadius = 0.7f;
    [Min(0.5f)] public float stompCooldown = 6f;
    [Min(0.5f)] public float stompRange = 9f;
    [Min(0.5f)] public float stompRadius = 1.9f; // คลื่นกระแทกรอบเท้าตอนกระทืบ (ชิดตัวบอส)
    [Min(0f)] public float poisonSeconds = 5f;
    [Min(0f)] public float poisonDamage = 0.5f;

    [Header("เฟส 2: กระโดดทับ")]
    [Min(1f)] public float leapCooldown = 9f;
    [Min(0f)] public float leapMinDistance = 5f; // ใช้ไล่ผู้เล่นที่ยืนห่าง
    [Min(0.5f)] public float leapRadius = 2.3f;
    [Min(0.1f)] public float leapTrack = 1f;     // เงาเป้าไล่ตามผู้เล่น
    [Min(0.1f)] public float leapLock = 0.45f;   // เงาหยุดนิ่ง ให้เวลาหลบก่อนตก

    [Header("เฟส 2: กรงราก")]
    [Min(1f)] public float cageCooldown = 10f;
    [Min(1f)] public float cageRadius = 2.4f;
    [Min(4)] public int cageRoots = 12;
    [Min(1)] public int cageGap = 3;             // ช่องหนี (จำนวนรากที่เว้นติดกัน)
    [Min(0.1f)] public float cageWarning = 1f;
    [Min(0.1f)] public float cageHold = 2.2f;    // รั้วรากค้างขวางทาง
    [Min(0.1f)] public float cageFinale = 0.9f;  // หลังรั้วขึ้น รากใหญ่แทงกลางวง

    [Header("เรียก Rootlings (เฟส 2 ขึ้นไป)")]
    public MonsterData[] minions;
    [Min(1)] public int minionsPerSummon = 2;
    [Min(1)] public int maxAliveMinions = 3;
    [Min(1f)] public float summonCooldown = 12f;

    [Header("เฟส 3: คลั่ง + รากดูดพลัง")]
    [Range(0f, 1f)] public float phaseTwoAt = 0.6f;
    [Range(0f, 1f)] public float rageHealthThreshold = 0.3f;
    [Range(0.3f, 1f)] public float rageCooldownScale = 0.7f;
    [Min(0f)] public float sweepSpeed = 55f;  // องศา/วินาที ลำแสงหมุนตามผู้เล่นช่วงคลั่ง
    [Min(1)] public int rootNodes = 3;
    [Min(1f)] public float rootNodeHealth = 60f;
    [Min(0f)] public float healPerNode = 1.5f; // เลือด/วินาที ต่อรากหนึ่งต้น
    [Range(0f, 1f)] public float healCap = 0.45f;
    [Min(0f)] public float rootStun = 3f;
    [Range(0f, 1f)] public float rootArmor = 0.1f;   // รากยังอยู่ รับดาเมจแค่สัดส่วนนี้
    [Min(1f)] public float rootRegrow = 15f;         // ทลายรากครบแล้ว งอกใหม่หลังจากนี้
    [Range(0.2f, 1f)] public float rageCageScale = 0.8f; // เฟส 3 กรงรากถี่ขึ้นอีก
    [Header("ยืนหยัดครั้งสุดท้าย")]
    [Min(1f)] public float lastStandTime = 6f;
    [Min(10f)] public float spinSpeed = 50f;         // องศา/วินาที ลำแสงหมุนรอบตัว (4 สาย ห่างกันสายละ 90°)
    [Min(1)] public int spinBeams = 4;
    [Min(0.1f)] public float rootRainEvery = 1.1f;   // รากแทงใต้เท้าผู้เล่น (โดนแล้วเจ็บ)
    [Min(0.1f)] public float fenceEvery = 0.35f;     // รากคลุ้มคลั่งผุดมั่ว ค้างเป็นรั้วขวางทาง
    [Min(1)] public int fencePerBurst = 3;
    [Min(0.1f)] public float fenceHold = 2.5f;
    [Min(1)] public int maxFences = 18;

    [Min(0.5f)] public float preferredDistance = 4f; // เดินเข้าหาจนถึงระยะนี้แล้วยืนร่ายท่า
    [Min(0f)] public float actionGap = 1f;           // พักระหว่างท่า

    const float LaserPointTime = 3f / 8f;    // ท่าร่ายเลเซอร์ 8 fps ถึงเฟรมชี้แขน (เฟรม 3)
    const float StompImpact = 3f / 10f;      // ท่ากระทืบ 10 fps เท้ากระแทกพื้นที่เฟรม 3
    const float StompRaised = 2f / 10f / 0.7f; // เวลาในท่ากระทืบ (0–1) ตรงเฟรมยกเท้า ใช้เป็นท่าคำราม/ย่อก่อนกระโดด
    const float LaserFps = 12f;
    const float WoodBreakSeconds = 2f; // เสียงไม้แตก (ImpactWood05 ยาว 3.65 วิ) ใช้แค่ 2 วิแรก ช่วงหลังไม่เข้ากับท่า
    const float RootFps = 10f;
    const float LeapHeight = 6f;
    const float DeathShowTime = 1.4f;
    static readonly Color LaserColor = new Color(0.45f, 1f, 1f);
    static readonly Color RootColor = new Color(0.62f, 0.9f, 0.3f);
    static readonly Color Dirt = new Color(0.55f, 0.42f, 0.28f);
    static readonly Color RageGreen = new Color(0.45f, 1f, 0.55f);
    static readonly Color Leaves = new Color(1f, 0.55f, 0.2f);
    static readonly Color LeapColor = new Color(1f, 0.45f, 0.3f);
    static readonly Color Bark = new Color(0.72f, 0.64f, 0.55f);

    int phase = 1;
    float nextLaser, nextStomp, nextSummon, nextLeap, nextCage, nextAction, nextLeaf, nextHeal;
    float overheatUntil, regrowAt;
    bool isCasting, lastStand, lastStandDone, rageReady;
    float laserLength; // ความยาวภาพลำแสงตอนสเกล 1 (หน่วยในฉาก)
    SpriteRenderer aura, shell;
    readonly List<SpriteRenderer> beams = new List<SpriteRenderer>();
    PlayerStats playerStats;
    readonly List<GameObject> warnings = new List<GameObject>();   // วงเตือน/เงาเป้า ลบทิ้งเมื่อท่าถูกขัด
    readonly List<GameObject> roots = new List<GameObject>();      // รากที่แทงขึ้นอยู่ (รวมรั้ว)
    readonly List<GameObject> aliveMinions = new List<GameObject>();
    readonly List<EntbornRootNode> nodes = new List<EntbornRootNode>();

    bool Enraged => phase >= 3;
    bool Rooted => nodes.Exists(n => n != null && !n.IsBroken); // ปักหลักดูดพลัง ไม่เดิน/ไม่กระโดด
    protected override bool ResistsKnockback => true;
    protected override float DamageTakenScale =>
        lastStand ? 0f : IsStunned ? stunnedScale : Rooted ? rootArmor : Time.time < overheatUntil ? overheatScale : 1f;
    // เลือดล็อกที่เกณฑ์เฟส 3 จนกว่าจะเข้าเฟสเสร็จ (คำราม + รากดูดพลังขึ้นครบ) เบิร์สต์จังหวะเดียวข้ามเฟสไม่ได้
    protected override float HealthFloor => rageReady || myData == null ? 0f : myData.maxHealth * rageHealthThreshold;
    // ตัวบอสขยายได้ใน prefab ท่าที่อิงขนาดตัว (ลำแสง/คลื่นกระแทก) ขยายตาม
    float Size => Mathf.Max(0.1f, Mathf.Abs(transform.lossyScale.y));
    protected override DamageNumbers.Kind HitKind =>Rooted ? DamageNumbers.Kind.Armored : DamageNumbers.Kind.Enemy;
    float CooldownScale => phase >= 3 ? rageCooldownScale : phase == 2 ? 0.85f : 1f;
    float Side => sr != null && sr.flipX ? -1f : 1f;
    Vector2 Muzzle => (Vector2)transform.position + Vector2.Scale(new Vector2(muzzle.x * Side, muzzle.y), transform.lossyScale);
    public Vector2 Core => sr != null ? (Vector2)sr.bounds.center : (Vector2)transform.position + Vector2.up; // สายพลังของรากโยงมาที่นี่

    protected override void Start()
    {
        base.Start();
        laserLength = laserFrames != null && laserFrames.Length > 0 && laserFrames[0] != null ? laserFrames[0].bounds.size.x : 4.5f;
        nextLaser = Time.time + 2.5f;
        nextStomp = Time.time + 4.5f;
        nextAction = Time.time + 2f;
        if (currentRoom != null) StartCoroutine(Cast(Intro())); // เสกโดยห้องบอส (ไม่ใช่ตัวอย่างในฉากอนิเมชัน)
    }

    protected override void Update()
    {
        if (isDying) return;
        UpdateAura();
        UpdateShell();
        UpdateHeal();
        if (!lastStand && regrowAt > 0f && Time.time >= regrowAt) { regrowAt = 0f; RegrowNodes(); }
        if (UpdateStun()) return;
        if (Waking()) return;
        if (isCasting || isKnockedBack) return;
        if (HoldStill()) return; // คอนโซลทดสอบ: หยุด AI
        if (player == null || myData == null) return;
        if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null && playerStats.isDead) { if (anim != null) anim.SetBool("isWalking", false); return; }

        if (phase == 1 && HealthFraction <= phaseTwoAt) { phase = 2; StartCoroutine(Cast(PhaseTwo())); return; }
        if (phase == 2 && HealthFraction <= rageHealthThreshold) { phase = 3; StartCoroutine(Cast(EnterRage())); return; }

        float distance = Vector2.Distance(transform.position, player.position);
        if (Time.time >= nextAction)
        {
            if (phase >= 2 && Time.time >= nextSummon && CanSummon()) { StartCoroutine(Cast(Summon())); return; }
            // ผู้เล่นชิดตัว: กระทืบไล่ก่อน (คลื่นกระแทกรอบเท้า)
            if (distance <= stompRadius * Size + 0.6f && Time.time >= nextStomp) { StartCoroutine(Cast(Stomp())); return; }
            if (phase >= 2 && !Rooted && distance >= leapMinDistance && Time.time >= nextLeap) { StartCoroutine(Cast(Leap())); return; }
            if (phase >= 2 && distance <= stompRange && Time.time >= nextCage) { StartCoroutine(Cast(Cage())); return; }
            if (distance <= laserRange && Time.time >= nextLaser) { StartCoroutine(Cast(Laser())); return; }
            if (distance <= stompRange && Time.time >= nextStomp) { StartCoroutine(Cast(Stomp())); return; }
        }
        Walk(distance);
    }

    // เดินช้า ๆ เข้าหาจนถึงระยะร่ายท่า แล้วยืนหันหน้าหาผู้เล่น (ปักหลักดูดพลังอยู่ไม่เดิน)
    void Walk(float distance)
    {
        FacePlayer();
        bool walking = distance > preferredDistance && !Rooted;
        if (anim != null) anim.SetBool("isWalking", walking);
        if (!walking) return;
        Vector2 step = navigator.DirectionTo(player.position) * myData.moveSpeed * Time.deltaTime
                       * BlessingManager.MonsterSpeedFactor(transform.position);
        transform.position += (Vector3)step;
    }

    IEnumerator Cast(IEnumerator move)
    {
        isCasting = true;
        if (anim != null) anim.SetBool("isWalking", false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        yield return PausedWhileHeld(move);
        if (anim != null) anim.speed = 1f;
        isCasting = false;
        nextAction = Time.time + actionGap * CooldownScale;
    }

    // ---------- เปิดตัว ----------

    IEnumerator Intro()
    {
        SetBodyActive(false);
        SetAlpha(0f);
        Vector2 home = transform.position;
        CameraFollow.Shake(0.15f, 0.6f);
        for (int i = 0; i < 6; i++)
        {
            SpawnRoot(home + (Vector2)(Quaternion.Euler(0f, 0f, 60f * i + 30f) * Vector2.right) * 1.8f, rootScale * 0.8f, 0f, false);
            ImpactSparks.Spawn(home, Dirt, 6, Vector2.up, 4f, 120f);
            yield return new WaitForSeconds(0.08f);
        }
        EchoFx.Shockwave(home, RootColor, 3.5f, 0.5f);
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            SetAlpha(t / 0.7f);
            if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) ImpactSparks.Spawn(home + Random.insideUnitCircle * 0.8f, Dirt, 3, Vector2.up, 3f, 60f);
            yield return null;
        }
        SetAlpha(1f);
        SetBodyActive(true);
        yield return Roar(RootColor, 3f, 0.45f);
    }

    // ค้างท่ายกเท้า แสงวาบ คลื่นกระแทก ใบไม้แตก (เปิดตัว / ขึ้นเฟส)
    IEnumerator Roar(Color color, float size, float hold)
    {
        FacePlayer();
        if (anim != null)
        {
            anim.Play("Stomp", 0, StompRaised);
            anim.speed = 0f;
        }
        Vector2 center = Core;
        CameraFollow.Shake(0.22f, 0.35f);
        Sfx.Play(SfxId.EntbornRoar);
        EchoFx.Flash(center, color, size, 0.35f);
        EchoFx.Shockwave(transform.position, color, size + 1f, 0.5f);
        ImpactSparks.Spawn(center, color, 20, Vector2.zero, 5.5f);
        ImpactSparks.Spawn(center, Leaves, 12, Vector2.up, 4f, 180f);
        yield return new WaitForSeconds(hold);
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Idle", 0, 0f);
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator PhaseTwo()
    {
        nextSummon = Time.time + 2f;
        nextLeap = Time.time + 3f;
        nextCage = Time.time + 6f;
        yield return Roar(RootColor, 3f, 0.5f);
    }

    // ---------- เลเซอร์ไม้ ----------

    IEnumerator Laser()
    {
        FacePlayer();
        if (fx != null) fx.Warn(LaserColor);
        Sfx.Play(SfxId.EntbornWoodCreak);
        if (anim != null) anim.Play("LaserCast", 0, 0f);
        yield return new WaitForSeconds(LaserPointTime);
        if (anim != null) anim.speed = 0f; // ค้างท่าชี้แขนตลอดที่ยิง

        Vector2 aim = AimAtPlayer();
        int count = phase >= 2 ? 3 : 1;
        for (int i = 0; i < count; i++) beams.Add(MakeBeam());
        // ชาร์จ: ประกายฟ้าดูดเข้ามือ หินบนตัวเรืองขึ้นเรื่อย ๆ เส้นบางกะพริบเตือน (ช่วงคลั่งหันตามผู้เล่นด้วย)
        Sfx.PlayFor(SfxId.BossLaserCharge, laserWarning, fromEnd: true); // เสียงชาร์จจบพร้อมลำแสงออก
        float nextCharge = 0f;
        for (float t = 0f; t < laserWarning; t += Time.deltaTime)
        {
            float k = t / laserWarning;
            if (Enraged) aim = Turn(aim, AimAtPlayer(), sweepSpeed * Time.deltaTime);
            ShowWarning(Muzzle, aim, k, t);
            if (fx != null) fx.Glow(LaserColor, 0.6f * k);
            if (t >= nextCharge)
            {
                nextCharge = t + 0.05f;
                Vector2 from = Random.insideUnitCircle.normalized * Random.Range(0.9f, 1.5f);
                ImpactSparks.Spawn(Muzzle + from, LaserColor, 1, -from, 3.5f, 5f);
            }
            yield return null;
        }

        CameraFollow.Shake(0.12f, 0.2f);
        Sfx.PlayFor(SfxId.BossLaserBeam, laserActive + 2f / LaserFps);  // เสียงลำแสงดังเท่าที่ลำแสงอยู่บนจอ
        bool hit = false;
        for (float t = 0f; t < laserActive; t += Time.deltaTime)
        {
            if (Enraged) aim = Turn(aim, AimAtPlayer(), sweepSpeed * Time.deltaTime); // กวาดตามผู้เล่น
            int frame = 2 + Mathf.FloorToInt(t * LaserFps) % 3;
            if (t == 0f) ClearWarning();
            ShowBeams(Muzzle, aim, frame, 1f, 1.25f);
            if (!hit && AnyBeamHits(Muzzle, aim) && playerStats != null && playerStats.CanTakeHit)
            {
                hit = true; // ยิงหนึ่งครั้งโดนได้ครั้งเดียว (เกราะกันไว้ก็นับว่าโดนแล้ว)
                if (BeamHit()) { HitStop.Freeze(0.06f); CameraFollow.Shake(0.2f, 0.2f); }
            }
            if (Mathf.Repeat(t, 0.08f) < Time.deltaTime) BeamSparks(Muzzle, aim);
            yield return null;
        }
        for (int frame = 5; frame <= 6; frame++)
        {
            ShowBeams(Muzzle, aim, frame, 1f, 1.25f);
            yield return new WaitForSeconds(1f / LaserFps);
        }
        DestroyBeams();
        if (anim != null) anim.speed = 1f; // ลดแขนต่อจนจบ
        nextLaser = Time.time + laserCooldown * CooldownScale;
        yield return Overheat();
    }

    // ร้อนเกิน: ยืนนิ่ง หินบนตัวเรืองฟ้าเป็นจังหวะ มีไอพลังงานพุ่งขึ้น รับดาเมจแรงขึ้น (DamageTakenScale)
    IEnumerator Overheat()
    {
        float time = Enraged ? rageOverheatTime : overheatTime;
        overheatUntil = Time.time + time;
        Sfx.Play(SfxId.EntbornVoiceWeak);
        float nextSteam = 0f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (fx != null) fx.Glow(LaserColor, 0.25f + 0.35f * Mathf.Abs(Mathf.Sin(t * 9f)));
            if (t >= nextSteam && sr != null)
            {
                nextSteam = t + 0.07f;
                Bounds b = sr.bounds;
                var at = new Vector2(Random.Range(b.min.x + 0.3f, b.max.x - 0.3f), Random.Range(b.center.y, b.max.y));
                ImpactSparks.Spawn(at, Random.value < 0.5f ? Color.white : LaserColor, 1, Vector2.up, 2.2f, 25f);
            }
            yield return null;
        }
        overheatUntil = 0f;
    }

    Vector2 AimAtPlayer()
    {
        Vector2 facing = new Vector2(Side, 0f);
        Vector2 to = SkillCombat.BodyCenter(player.gameObject) - Muzzle;
        float tilt = to.sqrMagnitude > 0.001f ? Vector2.SignedAngle(facing, to) : 0f;
        return Quaternion.Euler(0f, 0f, Mathf.Clamp(tilt, -laserAimClamp, laserAimClamp)) * facing;
    }

    static Vector2 Turn(Vector2 from, Vector2 to, float maxDegrees)
    {
        float angle = Mathf.Clamp(Vector2.SignedAngle(from, to), -maxDegrees, maxDegrees);
        return Quaternion.Euler(0f, 0f, angle) * from;
    }

    SpriteRenderer MakeBeam()
    {
        var go = new GameObject("EntbornLaser");
        go.transform.SetParent(transform.parent, true);
        var view = go.AddComponent<SpriteRenderer>();
        view.sortingLayerName = "Effect";
        view.sortingOrder = 8;
        return view;
    }

    // ลำแสงโดนผู้เล่น คืน true ถ้าเลือดลดจริง (เกราะกันไว้ไม่หยุดภาพ)
    bool BeamHit()
    {
        float before = playerStats.currentHP;
        HitPlayer(myData.attackDamage, 6f);
        return playerStats.currentHP < before;
    }

    void DestroyBeams()
    {
        foreach (var beam in beams) if (beam != null) Destroy(beam.gameObject);
        beams.Clear();
        ClearWarning();
    }

    // เตือนก่อนยิง: แถบบนพื้นกว้างเท่าที่ลำแสงจะโดนจริง ค่อย ๆ อ้วนขึ้นจนเต็ม (k 0→1) กะพริบถี่ขึ้นใกล้ยิง
    // ทับด้วยเส้นลำแสงบางสว่าง ให้เห็นทั้งแนวและจังหวะ
    readonly List<SpriteRenderer> warnBars = new List<SpriteRenderer>();

    void ShowWarning(Vector2 origin, Vector2 aim, float k, float t)
    {
        while (warnBars.Count < beams.Count)
        {
            var go = new GameObject("LaserWarning");
            go.transform.SetParent(transform.parent, true);
            var bar = go.AddComponent<SpriteRenderer>();
            bar.sprite = ProceduralSprites.Bar;
            bar.sortingLayerName = "bg2";
            bar.sortingOrder = 4;
            warnBars.Add(bar);
        }
        float width = (laserHitRadius + 0.2f) * 2f * Size;
        float blink = Mathf.Abs(Mathf.Sin(t * Mathf.Lerp(8f, 22f, k)));
        for (int i = 0; i < warnBars.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            float length = RayLength(origin, dir);
            var bar = warnBars[i];
            bar.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            bar.transform.localScale = new Vector3(length, width * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(k)), 1f);
            bar.color = new Color(LaserColor.r, LaserColor.g, LaserColor.b, 0.3f + 0.25f * Mathf.Clamp01(k) + 0.3f * blink);
        }
        ShowBeams(origin, aim, 0, 0.6f + 0.4f * blink, 1.6f);
    }

    void ClearWarning()
    {
        foreach (var bar in warnBars) if (bar != null) Destroy(bar.gameObject);
        warnBars.Clear();
    }

    // ทิศของลำแสงลำที่ i: ลำเดียว = ตรง, สามลำ = พัดกาง ±fanAngle, ลำแสงหมุน (ยืนหยัด) = แบ่งรอบวงเท่า ๆ กัน
    Vector2 BeamDirection(Vector2 aim, int i)
    {
        int count = beams.Count;
        if (count <= 1) return aim;
        float angle = lastStand ? 360f / count * i : (i - (count - 1) / 2f) * fanAngle;
        return Quaternion.Euler(0f, 0f, angle) * aim;
    }

    void ShowBeams(Vector2 origin, Vector2 aim, int frame, float alpha, float thickness)
    {
        for (int i = 0; i < beams.Count; i++) ShowBeam(beams[i], origin, BeamDirection(aim, i), frame, alpha, thickness);
    }

    bool AnyBeamHits(Vector2 origin, Vector2 aim)
    {
        for (int i = 0; i < beams.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            if (InBeam(origin, dir, RayLength(origin, dir))) return true;
        }
        return false;
    }

    void BeamSparks(Vector2 origin, Vector2 aim)
    {
        for (int i = 0; i < beams.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            ImpactSparks.Spawn(origin + dir * RayLength(origin, dir), LaserColor, 3, -dir, 3f, 80f);
        }
    }

    // วางลำแสงจากจุดยิงไปถึงกำแพงแรก
    void ShowBeam(SpriteRenderer beam, Vector2 origin, Vector2 aim, int frame, float alpha, float thickness)
    {
        float length = RayLength(origin, aim);
        if (beam == null || laserFrames == null || laserFrames.Length == 0) return;
        beam.sprite = laserFrames[Mathf.Clamp(frame, 0, laserFrames.Length - 1)];
        beam.color = new Color(1f, 1f, 1f, alpha);
        beam.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg));
        beam.transform.localScale = new Vector3(length / Mathf.Max(0.1f, laserLength), thickness * Size, 1f);
    }

    float RayLength(Vector2 origin, Vector2 aim)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, aim, laserMaxLength))
        {
            var c = hit.collider;
            if (!LootPlacement.IsSolid(c)) continue;
            if (c.GetComponentInParent<MonsterController>() != null || c.GetComponentInParent<PlayerStats>() != null) continue;
            if (c.GetComponentInParent<EntbornRootNode>() != null) continue; // สายพลังของตัวเองไม่บังลำแสง
            return Mathf.Max(0.5f, hit.distance);
        }
        return laserMaxLength;
    }

    bool InBeam(Vector2 origin, Vector2 aim, float length)
    {
        if (player == null) return false;
        Vector2 offset = SkillCombat.BodyCenter(player.gameObject) - origin;
        float along = Vector2.Dot(offset, aim);
        if (along < -0.3f || along > length) return false;
        return Mathf.Abs(aim.x * offset.y - aim.y * offset.x) <= (laserHitRadius + 0.2f) * Size;
    }

    // ---------- กระทืบ + รากแทง ----------

    IEnumerator Stomp()
    {
        FacePlayer();
        if (fx != null) fx.Warn(RootColor);
        Sfx.Play(SfxId.EntbornVoiceAttack);
        Sfx.Play(SfxId.EntbornWoodCreak);
        Vector2 hero = player.position;
        foreach (var spot in PickRootSpots(hero)) StartCoroutine(RootAt(spot, rootWarning, rootRadius, rootScale, 0f));
        // ช่วงคลั่ง: รากไล่เป็นแนวจากตัวบอสไปหาผู้เล่น ทีละจุด
        if (Enraged)
        {
            Vector2 from = transform.position;
            Vector2 dir = (hero - from).normalized;
            float reach = Vector2.Distance(from, hero) + 1.5f;
            int step = 0;
            for (float d = 1.6f; d <= reach; d += 1.2f, step++)
            {
                Vector2 spot = from + dir * d;
                if (Blocked(spot)) break;
                StartCoroutine(RootAt(spot, rootWarning + 0.12f * step, rootRadius, rootScale, 0f));
            }
        }

        if (anim != null) anim.Play("Stomp", 0, 0f);
        yield return new WaitForSeconds(StompImpact);
        CameraFollow.Shake(0.18f, 0.25f);
        Sfx.Play(SfxId.EntbornStomp);
        EchoFx.Shockwave(transform.position, RootColor, stompRadius * Size, 0.35f);
        ImpactSparks.Spawn(transform.position, Dirt, 16, Vector2.up, 4.5f, 160f);
        // ท่ากระทืบใช้ดาเมจเต็มตามตาราง เช่นเดียวกับเลเซอร์และกระโดดทับ
        if (Vector2.Distance(player.position, transform.position) <= stompRadius * Size) HitPlayer(myData.attackDamage, 9f);
        yield return new WaitForSeconds(0.7f - StompImpact);
        nextStomp = Time.time + stompCooldown * CooldownScale;
        // เฟส 2 ขึ้นไป: ต่อเลเซอร์ทันที ผู้เล่นที่วิ่งหนีรากเจอลำแสงรออยู่
        if (phase >= 2 && player != null && Vector2.Distance(transform.position, player.position) <= laserRange)
            yield return Laser();
    }

    List<Vector2> PickRootSpots(Vector2 hero)
    {
        var spots = new List<Vector2> { hero };
        int wanted = rootCount + (Enraged ? 2 : 0);
        for (int attempt = 0; attempt < 40 && spots.Count < wanted; attempt++)
        {
            Vector2 spot = hero + Random.insideUnitCircle * rootSpread;
            if (Blocked(spot)) continue;
            bool apart = true;
            foreach (var other in spots) if (Vector2.Distance(other, spot) < 1.2f) { apart = false; break; }
            if (apart) spots.Add(spot);
        }
        return spots;
    }

    static bool Blocked(Vector2 spot)
    {
        foreach (var col in Physics2D.OverlapCircleAll(spot, 0.3f))
            if (LootPlacement.IsSolid(col) && col.GetComponentInParent<MonsterController>() == null && col.GetComponentInParent<PlayerStats>() == null)
                return true;
        return false;
    }

    // วงเตือนขยายจนเต็มแล้วรากแทงขึ้น ยืนในวงตอนนั้น = โดน + ติดพิษ (hold > 0 = รากค้างเป็นรั้วขวางทาง)
    IEnumerator RootAt(Vector2 spot, float warning, float radius, float scale, float hold)
    {
        var marker = new GameObject("RootWarning");
        marker.transform.SetParent(transform.parent, true);
        marker.transform.position = spot;
        warnings.Add(marker);
        var disc = FloorLayer(marker.transform, ProceduralSprites.Disc, 1);
        var ring = FloorLayer(marker.transform, ProceduralSprites.DashedRing, 2);
        for (float t = 0f; t < warning; t += Time.deltaTime)
        {
            float k = t / warning;
            disc.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.2f, 1f, k);
            ring.transform.localScale = Vector3.one * radius * 2f;
            disc.color = new Color(RootColor.r, RootColor.g, RootColor.b, 0.18f + 0.25f * k);
            ring.color = new Color(RootColor.r, RootColor.g, RootColor.b, 0.5f + 0.4f * Mathf.Abs(Mathf.Sin(t * 14f)));
            yield return null;
        }
        warnings.Remove(marker);
        Destroy(marker);
        SpawnRoot(spot, scale, hold, hold > 0f);
        Sfx.PlayAt(SfxId.EntbornRoot, spot);
        ImpactSparks.Spawn(spot, Dirt, 8, Vector2.up, 3.5f, 70f);
        if (player != null && Vector2.Distance(player.position, spot) <= radius)
        {
            bool landed = playerStats != null && playerStats.CanTakeHit;
            HitPlayer(myData.attackDamage, 4f);
            if (landed && poisonSeconds > 0f) playerStats.ApplyPoison(poisonSeconds, poisonDamage);
        }
    }

    // รากแทงขึ้น (ภาพรายเฟรม) ผุด → ค้างถ้ามี hold → หด แล้วหายไป รั้วมีตัวชนขวางทางตอนค้าง
    void SpawnRoot(Vector2 spot, float scale, float hold, bool fence)
    {
        if (rootFrames == null || rootFrames.Length < 7)
        {
            if (rootPrefab == null) return;
            var old = Instantiate(rootPrefab, spot, Quaternion.identity, transform.parent);
            old.transform.localScale = Vector3.one * scale;
            foreach (var view in old.GetComponentsInChildren<SpriteRenderer>()) view.sortingLayerName = "object";
            Destroy(old, 0.75f);
            return;
        }
        var go = new GameObject(fence ? "RootFence" : "Root");
        go.transform.SetParent(transform.parent, true);
        go.transform.position = spot;
        go.transform.localScale = Vector3.one * scale;
        var root = go.AddComponent<SpriteRenderer>();
        root.sortingLayerName = "object";
        roots.Add(go);
        StartCoroutine(PlayRoot(go, root, hold, fence, scale));
    }

    IEnumerator PlayRoot(GameObject go, SpriteRenderer view, float hold, bool fence, float scale)
    {
        for (int i = 0; i <= 3; i++) { if (go == null) yield break; view.sprite = rootFrames[i]; yield return new WaitForSeconds(1f / RootFps); }
        BoxCollider2D block = null;
        if (fence && go != null)
        {
            block = go.AddComponent<BoxCollider2D>();
            block.size = new Vector2(0.55f, 0.35f) / scale;
            block.offset = new Vector2(0f, 0.18f / scale);
        }
        if (hold > 0f) yield return new WaitForSeconds(hold);
        if (block != null) Destroy(block);
        for (int i = 4; i < rootFrames.Length; i++) { if (go == null) yield break; view.sprite = rootFrames[i]; yield return new WaitForSeconds(1f / RootFps); }
        roots.Remove(go);
        if (go != null) Destroy(go);
    }

    SpriteRenderer FloorLayer(Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject("Layer");
        go.transform.SetParent(parent, false);
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.sortingLayerName = "bg2";
        view.sortingOrder = order;
        return view;
    }

    // ---------- เฟส 2: กระโดดทับ ----------

    IEnumerator Leap()
    {
        FacePlayer();
        if (fx != null) fx.Warn(LeapColor);
        Sfx.Play(SfxId.EntbornVoiceAttack);
        Sfx.Play(SfxId.EntbornWoodCreak);
        // ย่อตัวรวมแรง
        if (anim != null)
        {
            anim.Play("Stomp", 0, StompRaised);
            anim.speed = 0f;
        }
        yield return new WaitForSeconds(0.35f);

        // ลอยพ้นจอ ระหว่างอยู่กลางอากาศตีไม่โดน
        SetBodyActive(false);
        Vector2 start = transform.position;
        ImpactSparks.Spawn(start, Dirt, 12, Vector2.up, 4f, 120f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            MoveTo(start + Vector2.up * LeapHeight * k * k);
            SetAlpha(1f - k);
            yield return null;
        }
        SetAlpha(0f);

        // เงาเป้าไล่ตามผู้เล่น แล้วหยุดนิ่งให้หลบ
        var marker = new GameObject("LeapTarget");
        marker.transform.SetParent(transform.parent, true);
        warnings.Add(marker);
        var disc = FloorLayer(marker.transform, ProceduralSprites.Disc, 1);
        var ring = FloorLayer(marker.transform, ProceduralSprites.DashedRing, 2);
        Vector2 target = player.position;
        for (float t = 0f; t < leapTrack + leapLock; t += Time.deltaTime)
        {
            bool locked = t >= leapTrack;
            if (!locked) target = Vector2.MoveTowards(target, player.position, 7f * Time.deltaTime);
            marker.transform.position = target;
            float k = Mathf.Clamp01(t / (leapTrack + leapLock));
            disc.transform.localScale = Vector3.one * leapRadius * 2f * Mathf.Lerp(0.3f, 1f, k);
            ring.transform.localScale = Vector3.one * leapRadius * 2f;
            float blink = locked ? Mathf.Abs(Mathf.Sin(t * 22f)) : 0.5f;
            disc.color = new Color(0f, 0f, 0f, 0.25f + 0.25f * k); // เงาบอสที่กำลังตกลงมา
            ring.color = new Color(LeapColor.r, LeapColor.g, LeapColor.b, 0.45f + 0.5f * blink);
            yield return null;
        }
        // จุดตกจมกำแพง: หาที่ว่างใกล้ ๆ
        if (Blocked(target) && SpawnPlacement.TryPickNear(currentRoom, gameObject, target, 2f, target, 0f, out Vector2 free)) target = free;

        // ตกลงมา
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Stomp", 0, StompRaised);
        }
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            float k = t / 0.18f;
            MoveTo(target + Vector2.up * LeapHeight * (1f - k) * (1f - k));
            SetAlpha(k);
            yield return null;
        }
        MoveTo(target);
        SetAlpha(1f);
        SetBodyActive(true);
        warnings.Remove(marker);
        Destroy(marker);

        CameraFollow.Shake(0.3f, 0.35f);
        Sfx.Play(SfxId.EntbornLeap);
        EchoFx.Shockwave(target, LeapColor, leapRadius + 0.5f, 0.45f);
        ImpactSparks.Spawn(target, Dirt, 24, Vector2.up, 5.5f, 170f);
        if (Vector2.Distance(player.position, target) <= leapRadius) HitPlayer(myData.attackDamage, 11f);
        // รากเป็นวงรอบจุดตก
        for (int i = 0; i < 8; i++)
        {
            Vector2 spot = target + (Vector2)(Quaternion.Euler(0f, 0f, 45f * i + 22.5f) * Vector2.right) * (leapRadius + 0.7f);
            if (!Blocked(spot)) StartCoroutine(RootAt(spot, 0.45f + 0.04f * i, rootRadius * 0.9f, rootScale * 0.85f, 0f));
        }
        yield return new WaitForSeconds(0.6f);
        nextLeap = Time.time + leapCooldown * CooldownScale;
    }

    // ---------- เฟส 2: กรงราก ----------

    IEnumerator Cage()
    {
        FacePlayer();
        if (fx != null) fx.Warn(RootColor);
        Vector2 center = player.position;
        int gapStart = Random.Range(0, cageRoots);
        for (int i = 0; i < cageRoots; i++)
        {
            int fromGap = (i - gapStart + cageRoots) % cageRoots;
            if (fromGap < cageGap) continue; // ช่องหนี
            Vector2 spot = center + (Vector2)(Quaternion.Euler(0f, 0f, 360f / cageRoots * i) * Vector2.right) * cageRadius;
            if (!Blocked(spot)) StartCoroutine(RootAt(spot, cageWarning, rootRadius * 0.8f, rootScale * 0.9f, cageHold));
        }
        // รากใหญ่กลางวง เตือนนานกว่ารั้ว ให้เวลาวิ่งออกทางช่อง
        StartCoroutine(RootAt(center, cageWarning + cageFinale, cageRadius - 0.35f, rootScale * 1.3f, 0f));

        if (anim != null) anim.Play("Stomp", 0, 0f);
        yield return new WaitForSeconds(StompImpact);
        CameraFollow.Shake(0.15f, 0.2f);
        EchoFx.Shockwave(transform.position, RootColor, 2f, 0.3f);
        yield return new WaitForSeconds(0.7f - StompImpact);
        nextCage = Time.time + cageCooldown * CooldownScale * (Enraged ? rageCageScale : 1f);
    }

    // ---------- เฟส 3: คลั่ง + รากดูดพลัง ----------

    IEnumerator EnterRage()
    {
        nextSummon = Mathf.Min(nextSummon, Time.time + 3f);
        if (sr != null && aura == null)
        {
            aura = EchoFx.Layer(transform, "RageAura", ProceduralSprites.Glow, sr.sortingLayerID, sr.sortingOrder - 1, Color.clear);
            aura.transform.localPosition = new Vector3(0f, 1f, 0f);
        }
        yield return Roar(RageGreen, 3.5f, 0.6f);
        SpawnRootNodes();
        rageReady = true;
    }

    // รากโผล่รอบห้องห่างกัน ไม่ติดผู้เล่น
    void SpawnRootNodes()
    {
        if (rootFrames == null || rootFrames.Length < 7) return;
        var placed = new List<Vector2>();
        Vector2 hero = player != null ? (Vector2)player.position : (Vector2)transform.position;
        for (int attempt = 0; attempt < 30 && placed.Count < rootNodes; attempt++)
        {
            if (!SpawnPlacement.TryPickNear(currentRoom, gameObject, transform.position, 7f, hero, 2.5f, out Vector2 spot)) continue;
            if (Vector2.Distance(spot, transform.position) < 2.5f) continue;
            bool apart = true;
            foreach (var other in placed) if (Vector2.Distance(other, spot) < 3f) { apart = false; break; }
            if (!apart) continue;
            placed.Add(spot);
            nodes.Add(EntbornRootNode.Create(this, transform.parent, spot, rootFrames, 0.75f, rootNodeHealth));
        }
        nextHeal = Time.time + 1f;
    }

    // รากที่ผุดเสร็จแล้วต้นละ healPerNode ต่อวินาที ไม่เกิน healCap
    void UpdateHeal()
    {
        if (nodes.Count == 0 || Time.time < nextHeal || myData == null) return;
        nextHeal = Time.time + 1f;
        int ready = 0;
        foreach (var node in nodes) if (node != null && node.IsReady) ready++;
        float room = myData.maxHealth * healCap - currentHealth;
        if (ready == 0 || room <= 0f || IsStunned) return;
        float amount = Mathf.Min(room, healPerNode * ready);
        RestoreHealth(amount);
        DamageNumbers.Spawn(Core + Vector2.up * 0.8f, amount, DamageNumbers.Kind.Heal);
        ImpactSparks.Spawn(Core, RageGreen, 6, Vector2.up, 2.5f, 90f);
    }

    public void OnRootNodeBroken(EntbornRootNode node)
    {
        if (isDying) return;
        foreach (var other in nodes) if (other != null && !other.IsBroken) return;
        nodes.Clear();
        regrowAt = Time.time + rootRegrow;
        // ตีรากแตกครบ: ท่าที่ร่ายอยู่หลุด เกราะแตก บอสทรุดสตัน (DamageTakenScale แรงขึ้นระหว่างสตัน)
        Interrupt();
        EchoFx.Shockwave(transform.position, Bark, 3f, 0.4f);
        ImpactSparks.Spawn(Core, Bark, 18, Vector2.zero, 5f);
        CameraFollow.Shake(0.25f, 0.35f);
        EchoFx.Flash(Core, Color.white, 3f, 0.3f);
        ImpactSparks.Spawn(Core, RageGreen, 20, Vector2.zero, 5f);
        Sfx.Play(SfxId.EntbornVoiceWeak);
        Sfx.PlayFor(SfxId.EntbornWoodBreak, WoodBreakSeconds);
        Stun(rootStun);
    }

    // รากงอกกลับมาครบ เกราะกลับมา (สั่นเตือน + ดินแตก)
    void RegrowNodes()
    {
        if (isDying) return;
        CameraFollow.Shake(0.15f, 0.3f);
        EchoFx.Shockwave(transform.position, RageGreen, 3.5f, 0.45f);
        SpawnRootNodes();
    }

    // เปลือกเกราะรอบตัวตอนรากยังอยู่: วงสีเปลือกไม้หมุนเต้น ตัวบอสหม่นลง
    void UpdateShell()
    {
        bool on = Rooted && !lastStand && !isDying;
        if (sr != null)
        {
            Color want = on ? Bark : Color.white;
            sr.color = new Color(Mathf.MoveTowards(sr.color.r, want.r, Time.deltaTime * 2f),
                                 Mathf.MoveTowards(sr.color.g, want.g, Time.deltaTime * 2f),
                                 Mathf.MoveTowards(sr.color.b, want.b, Time.deltaTime * 2f), sr.color.a);
        }
        if (!on)
        {
            if (shell != null) shell.gameObject.SetActive(false);
            return;
        }
        if (shell == null && sr != null)
        {
            shell = EchoFx.Layer(transform, "RootShell", ProceduralSprites.DashedRing, sr.sortingLayerID, sr.sortingOrder + 1, Color.clear);
            shell.transform.localPosition = new Vector3(0f, 1f, 0f);
        }
        if (shell == null) return;
        shell.gameObject.SetActive(true);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
        shell.transform.localScale = new Vector3(2.6f, 2.8f, 1f) * (1f + 0.04f * pulse);
        shell.transform.Rotate(0f, 0f, 25f * Time.deltaTime);
        shell.color = new Color(Bark.r, Bark.g, Bark.b, 0.45f + 0.25f * pulse);
    }

    // ---------- ยืนหยัดครั้งสุดท้าย ----------

    // เลือดหมดครั้งแรกในเฟส 3: ยังไม่ตาย อมตะ ลำแสงหมุนรอบตัว + รากแทงทั่วห้อง รอดครบเวลาบอสพังทลาย
    IEnumerator LastStand()
    {
        lastStand = true;
        Sfx.PlayFor(SfxId.EntbornWoodBreak, WoodBreakSeconds);
        foreach (var node in nodes) if (node != null) node.Withdraw();
        nodes.Clear();
        regrowAt = 0f;
        yield return Roar(Color.white, 4.5f, 0.7f);

        Vector2 aim = player != null ? (SkillCombat.BodyCenter(player.gameObject) - Core).normalized : Vector2.right;
        aim = Quaternion.Euler(0f, 0f, 90f) * aim; // เริ่มตั้งฉากกับผู้เล่น ไม่โดนทันที
        for (int i = 0; i < spinBeams; i++) beams.Add(MakeBeam());
        // เส้นเตือนทุกสายก่อนหมุน
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            ShowWarning(Core, aim, t, t);
            if (fx != null) fx.Glow(Color.white, 0.5f * t);
            yield return null;
        }

        ClearWarning();
        CameraFollow.Shake(0.15f, lastStandTime);
        float nextRain = 0f, nextFence = 0.3f, nextBeamHit = 0f;
        for (float t = 0f; t < lastStandTime; t += Time.deltaTime)
        {
            aim = Quaternion.Euler(0f, 0f, spinSpeed * Time.deltaTime) * aim;
            ShowBeams(Core, aim, 2 + Mathf.FloorToInt(t * LaserFps) % 3, 1f, 1.25f);
            if (fx != null) fx.Glow(Color.white, 0.3f + 0.3f * Mathf.Abs(Mathf.Sin(t * 8f)));
            // เว้นจังหวะระหว่างโดนแต่ละครั้ง: เกราะพร/เกราะสกิลกันดาเมจโดยไม่ติดอมตะ
            // ถ้าเช็คทุกเฟรมจะโดนรัว (ผลักติดรั้ว + หยุดภาพต่อกันจนเกมช้าค้าง)
            if (Time.time >= nextBeamHit && AnyBeamHits(Core, aim) && playerStats != null && playerStats.CanTakeHit)
            {
                nextBeamHit = Time.time + 0.6f;
                if (BeamHit()) HitStop.Freeze(0.05f);
            }
            if (Mathf.Repeat(t, 0.08f) < Time.deltaTime) BeamSparks(Core, aim);
            // รากแทงใต้เท้าผู้เล่นเป็นจังหวะ (ต้องขยับ)
            if (t >= nextRain && player != null)
            {
                nextRain = t + rootRainEvery;
                if (!Blocked(player.position)) StartCoroutine(RootAt(player.position, 0.75f, rootRadius, rootScale, 0f));
            }
            // รากคลุ้มคลั่ง: ผุดมั่วรอบผู้เล่น เน้นดักทางที่กำลังวิ่งไป ค้างเป็นรั้วให้เดินลำบาก
            if (t >= nextFence && player != null)
            {
                nextFence = t + fenceEvery;
                for (int i = 0; i < fencePerBurst && CountFences() < maxFences; i++)
                {
                    Vector2 spot = FenceSpot(i);
                    if (!Blocked(spot)) StartCoroutine(RootAt(spot, 0.55f, rootRadius * 0.8f, rootScale * 0.85f, fenceHold));
                }
            }
            yield return null;
        }
        for (int frame = 5; frame <= 6; frame++)
        {
            ShowBeams(Core, aim, frame, 1f, 1.25f);
            yield return new WaitForSeconds(1f / LaserFps);
        }
        DestroyBeams();
        lastStand = false;
        currentHealth = 0f;
        Die(); // รอดครบ: พังทลายจริง
    }

    // จุดรากคลุ้มคลั่ง: ครึ่งหนึ่งดักข้างหน้าทางที่ผู้เล่นวิ่ง ที่เหลือสุ่มรอบตัว ไม่ชิดตัวผู้เล่น (ขวาง ไม่ใช่ขัง)
    Vector2 FenceSpot(int index)
    {
        Vector2 hero = player.position;
        var body = player.GetComponent<Rigidbody2D>();
        Vector2 heading = body != null && body.linearVelocity.sqrMagnitude > 0.5f ? body.linearVelocity.normalized : Random.insideUnitCircle.normalized;
        if (index % 2 == 0)
        {
            Vector2 side = new Vector2(-heading.y, heading.x);
            return hero + heading * Random.Range(1.4f, 3.2f) + side * Random.Range(-1.6f, 1.6f);
        }
        return hero + Random.insideUnitCircle.normalized * Random.Range(1.3f, 4.5f);
    }

    int CountFences()
    {
        int count = 0;
        foreach (var root in roots) if (root != null && root.name == "RootFence") count++;
        return count;
    }

    // ---------- เรียก Rootlings ----------

    bool CanSummon()
    {
        if (minions == null || minions.Length == 0) return false;
        aliveMinions.RemoveAll(minion => minion == null || !minion.activeInHierarchy);
        return aliveMinions.Count + minionsPerSummon <= maxAliveMinions;
    }

    IEnumerator Summon()
    {
        FacePlayer();
        if (fx != null) fx.Warn(RageGreen);
        if (anim != null) anim.Play("Stomp", 0, 0f);
        yield return new WaitForSeconds(StompImpact);
        CameraFollow.Shake(0.15f, 0.2f);
        EchoFx.Shockwave(transform.position, RageGreen, 3f, 0.4f);
        Vector2 hero = player.position;
        for (int i = 0; i < minionsPerSummon; i++)
        {
            var data = minions[Random.Range(0, minions.Length)];
            if (data == null || data.monsterPrefab == null) continue;
            // โผล่รอบผู้เล่นแต่ไม่ประชิด (รากยิงจากระยะไกลอยู่แล้ว)
            if (!SpawnPlacement.TryPickNear(currentRoom, data.monsterPrefab, hero, 5f, hero, 3f, out Vector2 spot)) continue;
            var body = SpawnPlacement.Measure(data.monsterPrefab);
            SpawnTelegraph.Begin(transform.parent, spot, body, SpawnTelegraph.Emphasis.Normal,
                                 SpawnTelegraph.ColorFor(this, SpawnTelegraph.Emphasis.Normal), 0.8f, i * 0.15f,
                                 () => SpawnMinion(data, spot));
        }
        yield return new WaitForSeconds(0.7f - StompImpact);
        nextSummon = Time.time + summonCooldown;
    }

    GameObject SpawnMinion(MonsterData data, Vector2 spot)
    {
        if (isDying || !gameObject.activeInHierarchy) return null;
        var spawned = Instantiate(data.monsterPrefab, spot, Quaternion.identity, transform.parent);
        var controller = spawned.GetComponent<MonsterController>();
        controller.myData = data;
        // ไม่ผูกกับห้อง: ห้องจะได้ไม่นับว่าเคลียร์แล้วเปิดประตูทั้งที่บอสยังอยู่
        controller.currentRoom = null;
        controller.WakeUpAfter(0.3f);
        aliveMinions.Add(spawned);
        ImpactSparks.Spawn(spot, Dirt, 12, Vector2.up, 4f, 90f);
        return spawned;
    }

    // ---------- ออร่าคลั่ง ----------

    void UpdateAura()
    {
        if (aura == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        aura.transform.localScale = new Vector3(3f + 0.3f * pulse, 3.2f + 0.3f * pulse, 1f);
        float visible = sr != null ? sr.color.a : 1f;
        aura.color = new Color(RageGreen.r, RageGreen.g, RageGreen.b, (0.22f + 0.14f * pulse) * visible);
        if (Time.time >= nextLeaf && sr != null && visible > 0.9f)
        {
            nextLeaf = Time.time + 0.12f;
            Bounds b = sr.bounds;
            ImpactSparks.Spawn(new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.center.y, b.max.y)), RageGreen, 1, Vector2.up, 1.5f, 40f);
        }
    }

    // ---------- ตัวช่วย ----------

    void FacePlayer()
    {
        if (player == null || sr == null) return;
        sr.flipX = player.position.x < transform.position.x;
    }

    void MoveTo(Vector2 position)
    {
        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void SetAlpha(float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    void SetBodyActive(bool active)
    {
        foreach (var col in GetComponents<Collider2D>()) col.enabled = active;
        if (active) IgnorePlayerCollisions(); // เปิด collider กลับแล้วตั้งให้ไม่ชนผู้เล่นซ้ำ
    }

    // หยุดท่าที่ร่ายอยู่ทุกอย่าง ล้างวงเตือน/ลำแสง ตัวกลับมาอยู่บนพื้นเห็นชัด
    void Interrupt()
    {
        StopAllCoroutines();
        isCasting = false;
        overheatUntil = 0f;
        foreach (var marker in warnings) if (marker != null) Destroy(marker);
        warnings.Clear();
        DestroyBeams();
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Idle", 0, 0f);
        }
        SetAlpha(1f);
        SetBodyActive(true);
        // รากที่ค้างอยู่ (รวมรั้ว) หายไปพร้อมกัน coroutine ที่เล่นภาพถูกหยุดไปแล้ว
        foreach (var root in roots) if (root != null) Destroy(root);
        roots.Clear();
    }

    // ---------- ตาย ----------

    protected override void Die()
    {
        // เลือดหมดครั้งแรกยังไม่ตาย: ยืนหยัดครั้งสุดท้าย (เลือดค้างที่ 1 อมตะจนจบท่า)
        // ตีแรงจนข้ามเฟส 3 ไปเลยก็ยังได้เจอ
        if (!lastStandDone)
        {
            lastStandDone = true;
            phase = 3;
            currentHealth = 1f;
            Interrupt();
            StartCoroutine(Cast(LastStand()));
            return;
        }
        Interrupt();
        lastStand = false;
        if (shell != null) Destroy(shell.gameObject);
        foreach (var node in nodes) if (node != null) node.Withdraw();
        nodes.Clear();
        if (sr != null) sr.color = Color.white;
        if (anim != null) anim.speed = 0f; // ค้างท่าสุดท้ายไว้ระหว่างพังทลาย

        deathLinger = DeathShowTime + 0.1f;
        deathFade = 0.8f;
        Sfx.Play(SfxId.EntbornDeath);
        Sfx.Play(SfxId.EntbornVoiceDeath);
        Sfx.PlayFor(SfxId.EntbornWoodBreak, WoodBreakSeconds);
        base.Die();
        StartCoroutine(DeathShow());
    }

    // ตัวสั่น รากแตกออกรอบตัว ใบไม้/ดินกระจาย ลูกน้องสลายตามทีละตัว (จากนั้นระบบตายเดิมสลายตัวเป็นพิกเซล)
    IEnumerator DeathShow()
    {
        Vector2 home = transform.position;
        Vector2 center = Core;
        CameraFollow.Shake(0.2f, 0.4f);
        EchoFx.Flash(center, Color.white, 3f, 0.3f);
        var left = aliveMinions.ToArray();
        int next = 0, count = 0;
        float nextBurst = 0f;
        for (float t = 0f; t < DeathShowTime; t += Time.deltaTime)
        {
            float k = t / DeathShowTime;
            transform.position = home + Random.insideUnitCircle * 0.07f * (0.4f + k);
            if (aura != null) aura.color = new Color(RageGreen.r, RageGreen.g, RageGreen.b, 0.4f * (1f - k));
            if (t >= nextBurst)
            {
                nextBurst = t + 0.18f;
                float angle = count++ * 67f;
                SpawnRoot(home + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * Random.Range(1.2f, 2.4f), rootScale, 0f, false);
                ImpactSparks.Spawn(center + Random.insideUnitCircle * 0.7f, RootColor, 5, Vector2.zero, 3.5f);
                if (next < left.Length) DissolveMinion(left[next++]);
            }
            yield return null;
        }
        while (next < left.Length) DissolveMinion(left[next++]);
        transform.position = home;
        CameraFollow.Shake(0.35f, 0.35f);
        EchoFx.Shockwave(home, RootColor, 5f, 0.7f);
        ImpactSparks.Spawn(center, RootColor, 30, Vector2.zero, 7f);
        ImpactSparks.Spawn(center, Dirt, 20, Vector2.up, 5f, 160f);
        if (aura != null) Destroy(aura.gameObject);
    }

    void DissolveMinion(GameObject minion)
    {
        if (minion == null || !minion.activeInHierarchy) return;
        var controller = minion.GetComponent<MonsterController>();
        if (controller == null || !controller.IsAlive) return;
        ImpactSparks.Spawn((Vector2)minion.transform.position + Vector2.up * 0.5f, RootColor, 10, Vector2.zero, 4f);
        controller.SelfDestruct(); // ตายแบบปกติ (ท่าตาย/นับศัตรู) โดยไม่มีตัวเลขดาเมจยักษ์เด้ง
    }
}
