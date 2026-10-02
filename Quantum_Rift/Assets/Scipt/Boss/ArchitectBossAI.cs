using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// บอสสุดท้าย (แมพ 3 รังมิติ) ตามตาราง 1.8: The Architect of Collapse
// ความสามารถตามเอกสาร: 1. วาร์ปพุ่งชนผู้เล่นด้วยความเร็วสูง  2. ยิงกระสุนเวทมนตร์หมุนวนต่อเนื่อง
// เลือด 500/500 = หลอดเดียว 1000 แปลงร่างที่ 50% ความเร็ว 100/150 ระยะหน่วง 5/2 ดาเมจ 2
//
//   เปิดตัว: คำราม คลื่นกระแทก (อมตะสั้น ๆ)
//   ร่าง 1 "แกนเนื้อ" (100–50%) ลอยช้า ๆ รักษาระยะห่าง
//     กระสุนหมุนวน — กางหนวด ยิงลูกเวทมนตร์เป็นเกลียว 3 แขนหมุนรอบตัว (หน่วง 5 วินาที ตามเอกสาร)
//     เลเซอร์     — แถบเตือนบนพื้นไล่ตามผู้เล่นแล้วล็อก ลำแสงพุ่งจากประตูมิติหน้าตัว (ต่ำกว่า 75% = 3 สายกางพัด)
//     วงแหวน      — ยิงเป็นวงมีช่องหลบ 2–3 ระลอก ปิดท้ายยิงตรงใส่ผู้เล่น 3 นัด
//   แปลงร่าง (50%): เลือดล็อกที่ 50% จนแปลงเสร็จ ตีไม่เข้า กระสุนสลาย ผลักผู้เล่นออก
//   ร่าง 2 "สถาปนิก" (50–0%) เดินเร็วขึ้น เข้าใกล้
//     วาร์ปพุ่งชน  — กลายเป็นลูกแก้วออร่า เส้นเตือนชี้ใส่ผู้เล่น แล้วพุ่งทะลุ ปลายทางเคียวฟันเป็นวง (หน่วง 2 วินาที)
//     คลื่นเคียว  — ฟันเคียวส่งคลื่นพระจันทร์เสี้ยว 3 ลูกกางพัด ทะลุตัวผู้เล่น
//     เลเซอร์     — ลำแสงกวาดตามผู้เล่นช้า ๆ
//     พายุหมุนวน  — ลอยกลับกลางห้อง ยิงเกลียว 4 แขน ระหว่างนั้นเหวี่ยงคลื่นเคียวใส่เป็นระยะ
//     ร่างแยกหลอก  — ลูกแก้วแยกหลายลูกไปล้อมผู้เล่นแล้วพุ่งพร้อมกัน มีลูกเดียวที่จริง (ลูกหลอกใสกว่า เส้นเตือนจางกว่า)
//     ตารางเลเซอร์ — เปิดประตูมิติ (ภาพของ Echo Commander) ที่ขอบห้อง ยิงลำแสงข้ามห้องเป็นแนว เว้นช่องให้ยืน สลับแนวนอน/ตั้ง
//     หลังวาร์ปพุ่ง/ร่างแยกทุกครั้ง บอสเซ 1 วินาที ดาวฟ้าวนเหนือหัว รับดาเมจ ×1.5 (จังหวะตีสวนของคนที่หลบได้)
//   คลั่ง (ต่ำกว่า 25% สนามเข้าเฟส ENRAGED): คำราม ท่าถี่ขึ้น วาร์ปพุ่ง 3 ครั้งติด ลำแสง 3 สาย คลื่นเคียว 5 ลูก 2 ชุด
//     เกลียว 6 แขนกลับทิศกลางทาง และหัวใจรังมิติกลางห้องปล่อยกระสุนหมุนวนเป็นระยะ
//     ห้องพังทลาย — ขอบห้องกลายเป็นพื้นว่างเปล่าทีละชั้น (ภาพพื้นอันตรายของสนาม) เตือนก่อนแล้วยืนนอกกรอบโดนดาเมจ
//   ฉากจบ "แกนกลางถล่ม": เลือดหมดแล้วยังไม่ตาย ร่างสลายไหลเข้าหัวใจกลางห้อง หัวใจเปิดให้ตี 10 วินาที ยิงกระสุนหนัก
//     ตีแตกทัน = ตายจริง ไม่ทัน = ร่างกลับมาพร้อมเลือด 10% (ดาเมจที่หัวใจค้างไว้ รอบหน้าตีต่อจากเดิม)
//   ตาย: สลายเป็นเม็ดพิกเซล (ชุดภาพไม่มีท่าตาย) จากนั้นเปิดประตูออก
//
// ตัวเลือด/เฟส/สนามเป็นของเดิม (ArchitectBossHealth + LivingBossArena) สคริปต์นี้ทำแค่ท่าโจมตีกับหน้าตา
// ตัวบอสเดินด้วย transform ในกรอบสนาม (ArenaEntryTrigger หดเข้ามา) ผู้เล่นเดินทะลุตัวบอสไม่ได้ (PlayerMovement)
[RequireComponent(typeof(ArchitectBossHealth))]
public sealed class ArchitectBossAI : MonoBehaviour
{
    [Header("ตาราง 1.8 (ร่าง 1 / ร่าง 2)")]
    [Min(0f)] public float damage = 2f;
    [Min(0f)] public float speedOne = 1.5f;       // ความเร็ว 100 (สเกลเดียวกับมอนตัวอื่น ×1.5/100)
    [Min(0f)] public float speedTwo = 2.25f;      // ความเร็ว 150
    [Min(0.1f)] public float spiralCooldown = 5f; // ระยะหน่วง 5: กระสุนหมุนวน (ร่าง 1)
    [Min(0.1f)] public float dashCooldown = 2f;   // ระยะหน่วง 2: วาร์ปพุ่งชน (ร่าง 2)

    [Header("ภาพ (ตัวสร้างใส่ให้)")]
    public Sprite[] orbFrames;      // BulletSpin 7 เฟรม
    public Sprite[] laserOneFrames; // 0–1 เส้นบาง 2–4 ลำแสงเต็ม 5–6 สลาย
    public Sprite[] laserTwoFrames;
    public Sprite[] waveFrames;     // ScytheWave 7 เฟรม (3 ใหญ่สุด)

    [Header("การเดิน / จังหวะ")]
    [Min(1f)] public float keepDistanceOne = 6.5f;
    [Min(1f)] public float keepDistanceTwo = 4f;
    [Min(0.1f)] public float actionGapOne = 1.2f;
    [Min(0.1f)] public float actionGapTwo = 0.7f;
    [Min(0.1f)] public float actionGapRage = 0.4f;

    [Header("กระสุนหมุนวน")]
    [Min(0.1f)] public float orbSpeed = 4.2f;
    [Min(0.05f)] public float orbHitRadius = 0.6f;  // ถึงกลางตัวผู้เล่น
    [Min(0.1f)] public float orbScale = 1.1f;
    [Min(0.5f)] public float orbLife = 6f;
    [Min(0.03f)] public float spiralInterval = 0.13f;
    [Min(0f)] public float spiralSpin = 75f;         // องศา/วินาที
    [Min(0.5f)] public float spiralTime = 3f;

    [Header("เลเซอร์")]
    [Min(0.1f)] public float laserWarning = 1.1f;
    [Min(0.1f)] public float laserActive = 0.6f;
    [Min(0.1f)] public float laserHitRadius = 0.5f;
    [Min(0.1f)] public float laserThickness = 1.5f;
    [Min(1f)] public float laserMaxLength = 30f;
    [Min(0f)] public float laserSweep = 35f;         // องศา/วินาที ร่าง 2 กวาดตามผู้เล่น
    [Range(0f, 60f)] public float fanAngle = 28f;

    [Header("ร่าง 2: วาร์ปพุ่งชน")]
    [Min(0.1f)] public float dashWarning = 0.75f;
    [Min(1f)] public float dashSpeed = 24f;
    [Min(0f)] public float dashOvershoot = 3f;       // พุ่งเลยตัวผู้เล่นไปอีก
    [Min(0.1f)] public float dashHitRadius = 1.3f;
    [Min(0.5f)] public float slashRadius = 2.8f;

    [Header("ร่าง 2: คลื่นเคียว")]
    [Min(0.1f)] public float waveSpeed = 8f;
    [Min(0.1f)] public float waveScale = 1.7f;
    [Min(0.1f)] public float waveHitRadius = 1f;
    [Range(0f, 45f)] public float waveSpread = 20f;

    [Header("คลั่ง (สนามเฟส ENRAGED)")]
    [Range(0.3f, 1f)] public float rageCooldownScale = 0.7f;
    [Min(0.05f)] public float heartInterval = 0.6f;
    [Min(0.1f)] public float heartOrbSpeed = 3f;

    [Header("ร่าง 2: เซหลังฟัน (จังหวะตีสวน)")]
    [Min(0f)] public float staggerTime = 1f;
    [Min(1f)] public float staggerScale = 1.5f;

    [Header("ร่าง 2: ร่างแยกหลอก")]
    [Min(2)] public int splitCount = 3;
    [Min(2f)] public float splitRadius = 6f;           // ร่างแยกไปยืนรอบผู้เล่นห่างเท่านี้
    [Range(0.1f, 1f)] public float fakeAlpha = 0.55f;  // ร่างหลอกใสกว่า (จุดสังเกต)

    [Header("ร่าง 2: ตารางเลเซอร์จากประตูมิติ")]
    public Texture2D echoAtlas;                        // ภาพท่าของ Echo Commander (ตัดรูปประตูมิติ ตัวสร้างใส่ให้)
    [Min(1)] public int gridLines = 3;
    [Min(0.1f)] public float gridWarning = 1.2f;

    [Header("คลั่ง: ห้องพังทลาย")]
    [Min(1)] public int collapseSteps = 3;
    [Min(1f)] public float collapseEvery = 8f;
    public Vector2 collapseShrink = new Vector2(2.5f, 1.5f); // ขอบหดเข้าต่อครั้ง (ต่อด้าน)
    [Min(0.1f)] public float collapseWarning = 2f;
    [Min(0f)] public float collapseDamage = 1f;

    [Header("ฉากจบ: แกนกลางถล่ม")]
    [Min(1f)] public float coreHealth = 60f;
    [Min(1f)] public float coreTime = 10f;
    [Min(0.3f)] public float coreRadius = 1.3f;
    [Range(0.01f, 0.5f)] public float reviveFraction = 0.1f;

    enum Move { Spiral, Laser, Burst, Dash, Waves, Storm, Split, Grid }
    static readonly Move[] FormOneMoves = { Move.Spiral, Move.Laser, Move.Burst };
    static readonly Move[] FormTwoMoves = { Move.Dash, Move.Waves, Move.Laser, Move.Storm, Move.Split, Move.Grid };

    const float LaserFps = 12f;
    const float MuzzleOne = 2.3f;    // ประตูมิติของเลเซอร์ห่างจากกลางตัว (ร่าง 1 ตัวกว้าง)
    const float MuzzleTwo = 1.6f;
    const float BodyRadius = 1.6f;   // พุ่งหยุดก่อนชนกำแพงเท่านี้
    const float Knockback = 7f;
    static readonly Color Violet = new Color(0.78f, 0.42f, 1f);
    static readonly Color Cyan = new Color(0.45f, 0.95f, 1f);
    static readonly Color RageRed = new Color(1f, 0.25f, 0.4f);
    static readonly Color SlashRed = new Color(1f, 0.3f, 0.55f); // สีเดียวกับพัดเตือนของ Echo Commander
    const float SlashAt = 3f / 12f + 4f / 12f; // คลายลูกแก้ว: เฟรมท้าย AuraDash 3 เฟรม + ReturnSlash ถึงเฟรมฟัน
    static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    static readonly int DissolveColorId = Shader.PropertyToID("_DissolveColor");
    static readonly int PixelGridId = Shader.PropertyToID("_PixelGrid");

    ArchitectBossHealth health;
    LivingBossArena arena;
    SpriteRenderer viewOne, viewTwo;
    Animator animOne, animTwo;
    ArchitectPhase1Animation formOne;
    ArchitectPhase2Animation formTwo;
    CircleCollider2D circleHitbox;
    PlayerStats player;
    Collider2D playerBody;
    Bounds area;
    Vector2 home;

    // ลำแสงหนึ่งสาย = หัว (ภาพเต็มขนาดจริง มีประกายตรงต้น) + ตัว (แถบกลางของภาพเดียวกัน ยืดยาวถึงกำแพง)
    // ยืดทั้งภาพตรง ๆ ประกายต้นลำแสงจะเละ
    sealed class LaserArt
    {
        public Sprite[] frames, strips;
        public float length;      // ความยาวลำแสงในภาพ (หน่วยในฉาก ที่สเกล 1)
        public float bodyStart;   // ตัวลำแสงเริ่มทับหัวตรงนี้
        public float stripWidth;
    }

    sealed class Beam
    {
        public SpriteRenderer head, body;
    }

    Coroutine brain;
    bool introDone, rageShown, transforming, dead;
    float graceUntil, nextHeart, heartAngle, strafeSign = 1f, nextStrafeFlip;
    Move last = Move.Burst;
    Move? forceNext; // เปิดร่าง 2 / ช่วงคลั่งด้วยวาร์ปพุ่งชนเสมอ
    readonly Dictionary<Move, float> readyAt = new Dictionary<Move, float>();
    LaserArt laserOne, laserTwo;
    readonly List<Beam> beams = new List<Beam>();
    readonly List<SpriteRenderer> ghosts = new List<SpriteRenderer>();    // ร่างแยกหลอก
    readonly List<EchoPortal> portals = new List<EchoPortal>();
    readonly List<SpriteRenderer> voidTiles = new List<SpriteRenderer>(); // พื้นที่พังแล้ว
    Rect arenaBox;       // กรอบสนาม (ArenaEntryTrigger)
    Rect safe;           // ห้องพังทลาย: ในกรอบนี้ยังยืนได้
    bool collapsing;
    Coroutine collapse;
    float nextVoidHit;
    ArchitectCore core;
    readonly List<EchoSlashTelegraph> slashMarks = new List<EchoSlashTelegraph>(); // วงแดงจุดฟันปลายทางวาร์ป
    float coreLeft;
    bool coreRunning, coreBroken;

    // เส้นพุ่งของวาร์ป (ตัวจริงหรือร่างหลอก)
    sealed class DashLine
    {
        public Vector2 from, dir;
        public float length, travelled, alpha = 1f;
        public SpriteRenderer bar;
    }
    readonly List<SpriteRenderer> marks = new List<SpriteRenderer>(); // แถบ/วงเตือนบนพื้น ลบทิ้งเมื่อท่าถูกขัด

    MaterialPropertyBlock block;
    float flashLeft, flashTime = 0.1f, flashPower, glowAmount, dissolve;
    Color flashColor = Color.white, glowColor = Color.white;

    int Form => health.phaseTwo != null && health.phaseTwo.activeSelf ? 2 : 1;
    bool Enraged => Form == 2 && arena != null && arena.CurrentPhase >= 3;
    float HealthFraction => health.CurrentHealth / Mathf.Max(1f, health.maxHealth);
    float Speed => Form == 2 ? speedTwo : speedOne;
    SpriteRenderer View => Form == 2 ? viewTwo : viewOne;
    Animator Anim => Form == 2 ? animTwo : animOne;
    // กลางลำตัว: ร่าง 1 ภาพ pivot กลาง ร่าง 2 pivot ที่เท้า ตัวสูง ~2.9 หน่วยภาพ
    Vector2 Core => Form == 2
        ? (viewTwo != null ? (Vector2)viewTwo.transform.position + Vector2.up * 1.3f * viewTwo.transform.lossyScale.y : (Vector2)transform.position)
        : (viewOne != null ? (Vector2)viewOne.transform.position : (Vector2)transform.position);
    Vector2 PlayerCenter => playerBody != null ? (Vector2)playerBody.bounds.center
        : player != null ? (Vector2)player.transform.position : Core + Vector2.down * 5f;
    Vector2 HeartSpot => arena != null && arena.core != null ? (Vector2)arena.core.transform.position : home;

    void Awake()
    {
        health = GetComponent<ArchitectBossHealth>();
        arena = health.arena;
        if (health.phaseOne != null)
        {
            viewOne = health.phaseOne.GetComponentInChildren<SpriteRenderer>(true);
            animOne = health.phaseOne.GetComponentInChildren<Animator>(true);
            formOne = health.phaseOne.GetComponentInChildren<ArchitectPhase1Animation>(true);
        }
        if (health.phaseTwo != null)
        {
            viewTwo = health.phaseTwo.GetComponentInChildren<SpriteRenderer>(true);
            animTwo = health.phaseTwo.GetComponentInChildren<Animator>(true);
            formTwo = health.phaseTwo.GetComponentInChildren<ArchitectPhase2Animation>(true);
        }
        circleHitbox = health.hitbox as CircleCollider2D;
        health.Damaged += OnDamaged;
        health.Defeated += OnDefeated;
        health.InterceptDeath = InterceptDeath;
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Defeated -= OnDefeated;
    }

    void Start()
    {
        block = new MaterialPropertyBlock();
        var fx = MonsterFx.SharedMaterial; // shader เดียวกับมอน: กะพริบขาว / เรืองสี / สลายเป็นพิกเซล
        if (fx != null)
        {
            if (viewOne != null) viewOne.sharedMaterial = fx;
            if (viewTwo != null) viewTwo.sharedMaterial = fx;
        }
        laserOne = BuildLaserArt(laserOneFrames);
        laserTwo = BuildLaserArt(laserTwoFrames);

        home = transform.position;
        var entry = transform.root.GetComponentInChildren<BossArenaEntry>(true);
        var box = entry != null ? entry.GetComponent<Collider2D>() : null;
        Vector2 size = box != null ? (Vector2)box.bounds.size - new Vector2(5f, 4f) : new Vector2(28f, 18f);
        Vector2 center = box != null ? (Vector2)box.bounds.center : home;
        area = new Bounds(new Vector3(center.x, center.y, 0f), new Vector3(Mathf.Max(4f, size.x), Mathf.Max(4f, size.y), 100f));
        arenaBox = box != null ? new Rect(box.bounds.min, box.bounds.size) : new Rect(home - new Vector2(19f, 13f), new Vector2(38f, 26f));
        coreLeft = coreHealth;
        FitHitbox();
        FindPlayer();
    }

    // ---------- ผู้เล่น ----------

    void FindPlayer()
    {
        if (player != null) return;
        player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;
        playerBody = null;
        foreach (var col in player.GetComponentsInChildren<Collider2D>())
        {
            if (!col.isTrigger && playerBody == null) playerBody = col;
            // ผู้เล่นกับตัวบอสไม่ดันกันทางฟิสิกส์ (บอสเดิน/พุ่งด้วย transform) PlayerMovement กันเดินทะลุเอง
            if (health.hitbox != null) Physics2D.IgnoreCollision(col, health.hitbox, true);
        }
    }

    // ทำดาเมจผู้เล่น (ใช้ร่วมกับกระสุน) คืน true ถ้าเลือดลดจริง (เกราะ/อมตะกันไว้ไม่หยุดภาพ)
    public static bool HurtPlayer(PlayerStats stats, float amount, Vector2 from, float knockback)
    {
        if (stats == null || stats.isDead) return false;
        bool landed = stats.CanTakeHit;
        float before = stats.currentHP;
        stats.TakeDamage(amount);
        var movement = stats.GetComponent<PlayerMovement>();
        if (landed && movement != null && knockback > 0f) movement.TakeKnockback(from, knockback);
        if (stats.currentHP >= before) return false;
        HitStop.Freeze(0.05f);
        CameraFollow.Shake(0.18f, 0.18f);
        return true;
    }

    // ---------- ลำดับการต่อสู้ ----------

    void Update()
    {
        if (health == null || dead) return;
        FindPlayer();
        bool fighting = arena != null && arena.IsFighting && !health.IsDefeated;
        if (!fighting)
        {
            if (brain != null || coreRunning) Interrupt();
            return;
        }
        VoidTick();
        if (coreRunning) return; // ฉากแกนกลางถล่มคุมเอง

        if (health.IsTransforming)
        {
            if (!transforming) BeginTransform();
            TransformTick();
            return;
        }
        if (transforming) EndTransform();

        if (Enraged && !rageShown && brain != null) Interrupt(); // เข้าคลั่งทันที ไม่รอท่าเดิมจบ
        if (Enraged && rageShown && !DevCheats.FreezeMonsters) HeartTick();
        if (brain == null) brain = StartCoroutine(Brain());
    }

    IEnumerator Brain()
    {
        if (!introDone)
        {
            introDone = true;
            yield return Held(Intro());
        }
        if (Enraged && !rageShown)
        {
            rageShown = true;
            yield return Held(Rage());
        }
        while (true)
        {
            float gap = Form == 1 ? actionGapOne : Enraged ? actionGapRage : actionGapTwo;
            float until = Mathf.Max(Time.time + gap, graceUntil);
            do
            {
                if (!DevCheats.FreezeMonsters) Drift();
                yield return null;
            } while (Time.time < until || DevCheats.FreezeMonsters); // คอนโซลทดสอบ: หยุด AI = ลอยนิ่ง ไม่เริ่มท่าใหม่
            yield return NextMove();
        }
    }

    // สุ่มท่าที่พร้อม ไม่ซ้ำท่าเดิมติดกันถ้ามีท่าอื่นพร้อม
    IEnumerator NextMove()
    {
        var pool = Form == 1 ? FormOneMoves : FormTwoMoves;
        var ready = new List<Move>();
        foreach (var move in pool)
            if (Time.time >= ReadyAt(move) && move != last) ready.Add(move);
        if (ready.Count == 0 && Time.time >= ReadyAt(last) && System.Array.IndexOf(pool, last) >= 0) ready.Add(last);
        Move? forced = forceNext.HasValue && System.Array.IndexOf(pool, forceNext.Value) >= 0 ? forceNext : null;
        forceNext = null;
        if (ready.Count == 0 && !forced.HasValue) yield break;
        // ท่าหลักตามเอกสาร (หมุนวน / วาร์ป) พร้อมเมื่อไหร่ได้ก่อน
        Move pick = forced.HasValue ? forced.Value
                  : ready.Contains(Move.Spiral) ? Move.Spiral
                  : ready.Contains(Move.Dash) && Random.value < 0.6f ? Move.Dash
                  : ready[Random.Range(0, ready.Count)];
        last = pick;
        int form = Form;
        yield return Held(Perform(pick));
        readyAt[pick] = Time.time + Cooldown(pick, form) * (Enraged ? rageCooldownScale : 1f);
    }

    // ท่าที่ร่ายอยู่ตอนกดหยุด AI (คอนโซลทดสอบ) ค้างไว้ที่จังหวะนั้น ปล่อยแล้วทำต่อ
    static IEnumerator Held(IEnumerator move) => DevCheats.Pausable(move, () => DevCheats.FreezeMonsters);

    float ReadyAt(Move move) => readyAt.TryGetValue(move, out float at) ? at : 0f;

    float Cooldown(Move move, int form)
    {
        switch (move)
        {
            case Move.Spiral: return spiralCooldown;
            case Move.Dash: return dashCooldown;
            case Move.Burst: return 3.5f;
            case Move.Waves: return 3.5f;
            case Move.Storm: return 9f;
            case Move.Split: return 7f;
            case Move.Grid: return 11f;
            default: return form == 1 ? 4.5f : 5f; // เลเซอร์
        }
    }

    IEnumerator Perform(Move move)
    {
        switch (move)
        {
            case Move.Spiral: return Spiral(3, spiralTime, spiralSpin, false);
            case Move.Laser: return Laser();
            case Move.Burst: return Burst();
            case Move.Dash: return Dash();
            case Move.Waves: return Waves();
            case Move.Split: return SplitDash();
            case Move.Grid: return Grid();
            default: return Storm();
        }
    }

    // ขัดท่าที่ทำอยู่ (แปลงร่าง / คลั่ง / ผู้เล่นตาย) ลบของค้างทั้งหมด คืนท่าทาง
    void Interrupt()
    {
        if (brain != null) StopCoroutine(brain);
        brain = null;
        DestroyBeams();
        ClearMarks();
        if (animOne != null) animOne.speed = 1f;
        if (animTwo != null) animTwo.speed = 1f;
        if (formTwo != null && health.phaseTwo.activeInHierarchy) formTwo.SetFloating(false);
        ClearGhosts();
        ClearSlashMarks();
        ClosePortals();
        health.Invulnerable = false;
        health.DamageScale = 1f;
        if (coreRunning) EndCore();
    }

    IEnumerator Intro()
    {
        health.Invulnerable = true;
        Warn(Violet);
        if (formOne != null) formOne.PlayMixedPose();
        CameraFollow.Shake(0.3f, 0.6f);
        EchoFx.Flash(Core, Violet, 7f, 0.5f);
        EchoFx.Shockwave(Core, Violet, 7f, 0.8f);
        ImpactSparks.Spawn(Core, Violet, 16, Vector2.zero, 5f);
        yield return Wait(1f);
        health.Invulnerable = false;
        graceUntil = Time.time + 0.4f;
    }

    // ---------- แปลงร่าง (เวลาและการสลับร่างเป็นของ ArchitectBossHealth) ----------

    void BeginTransform()
    {
        Interrupt();
        transforming = true;
        ArchitectOrb.ClearAll();
        CameraFollow.Shake(0.25f, 0.5f);
        EchoFx.Shockwave(Core, Violet, 4f, 0.6f);
        Sfx.Play(SfxId.ArchitectTransform);
    }

    // ชาร์จ: ประกายม่วง/ฟ้าดูดเข้าตัว ตัวเรืองขึ้นเรื่อย ๆ จอสั่นเบา ๆ
    void TransformTick()
    {
        float k = Mathf.Clamp01(Mathf.PingPong(Time.time * 3f, 1f));
        Glow(Violet, 0.35f + 0.4f * k);
        if (Random.value < 0.5f)
        {
            Vector2 from = Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
            ImpactSparks.Spawn(Core + from, Random.value < 0.5f ? Violet : Cyan, 1, -from, 5f, 5f);
        }
        if (Random.value < 0.1f) CameraFollow.Shake(0.12f, 0.1f);
    }

    void EndTransform()
    {
        transforming = false;
        FitHitbox();
        Flash(Color.white, 1f, 0.3f);
        CameraFollow.Shake(0.5f, 0.45f);
        EchoFx.Flash(Core, Violet, 9f, 0.6f);
        EchoFx.Shockwave(Core, Violet, 9f, 0.9f);
        ImpactSparks.Spawn(Core, Cyan, 24, Vector2.zero, 7f);
        PushPlayer(6f, 11f);
        graceUntil = Time.time + 1f;
        forceNext = Move.Dash; // ร่างใหม่เปิดด้วยวาร์ปพุ่งชน
        readyAt[Move.Split] = Time.time + 8f;
        readyAt[Move.Grid] = Time.time + 12f;
    }

    IEnumerator Rage()
    {
        health.Invulnerable = true;
        ArchitectOrb.ClearAll();
        if (formTwo != null) formTwo.SetFloating(true);
        CameraFollow.Shake(0.45f, 0.8f);
        EchoFx.Flash(Core, RageRed, 8f, 0.6f);
        EchoFx.Shockwave(Core, RageRed, 8f, 0.8f);
        PushPlayer(5f, 9f);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            Glow(RageRed, 0.4f + 0.3f * Mathf.Abs(Mathf.Sin(t * 12f)));
            if (Random.value < 0.4f) ImpactSparks.Spawn(Core + Random.insideUnitCircle * 1.5f, RageRed, 1, Vector2.up, 3f, 30f);
            yield return null;
        }
        if (formTwo != null) formTwo.SetFloating(false);
        health.Invulnerable = false;
        forceNext = Move.Dash; // เปิดช่วงคลั่งด้วยวาร์ปพุ่ง 3 ครั้ง
        if (collapse == null) collapse = StartCoroutine(Collapse());
        graceUntil = Time.time + 0.3f;
    }

    // หัวใจรังมิติกลางห้องปล่อยกระสุนหมุนวนตลอดช่วงคลั่ง (หยุดตอนคำราม/ตาย)
    void HeartTick()
    {
        if (health.Invulnerable || Time.time < nextHeart) return;
        nextHeart = Time.time + heartInterval;
        heartAngle += 17f;
        for (int i = 0; i < 2; i++)
            FireOrb(HeartSpot, heartAngle + 180f * i, heartOrbSpeed, 0.9f);
    }

    // ---------- เดิน ----------

    // รักษาระยะจากผู้เล่น ใกล้พอแล้วเดินวนรอบ ๆ อยู่ในกรอบสนาม
    void Drift()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        FacePlayer();
        if (Time.time >= nextStrafeFlip)
        {
            nextStrafeFlip = Time.time + Random.Range(2f, 4f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }
        Vector2 to = PlayerCenter - Core;
        float distance = to.magnitude;
        float keep = Form == 2 ? keepDistanceTwo : keepDistanceOne;
        Vector2 dir = Vector2.zero;
        if (player != null && distance > 0.01f)
        {
            if (distance > keep + 1f) dir = to / distance;
            else if (distance < keep - 1.5f) dir = -to / distance;
            else dir = new Vector2(-to.y, to.x) / distance * strafeSign * 0.6f;
        }
        MoveWithin((Vector2)transform.position + dir * Speed * dt, Speed * dt);
    }

    // ขยับไปจุดหมาย แต่ไม่ออกนอกกรอบสนาม (อยู่นอกกรอบแล้ว เช่นหลังวาร์ป = เดินกลับเข้ามา)
    void MoveWithin(Vector2 next, float step)
    {
        Vector2 root = transform.position;
        if (!area.Contains(new Vector3(next.x, next.y, 0f)))
            next = Vector2.MoveTowards(root, area.ClosestPoint(new Vector3(root.x, root.y, 0f)), Mathf.Max(step, 0.001f));
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    void FacePlayer()
    {
        if (Form == 2 && formTwo != null) formTwo.FaceLeft(PlayerCenter.x < Core.x);
    }

    // ---------- กระสุนหมุนวน (ท่าหลักตามเอกสาร) ----------

    IEnumerator Spiral(int arms, float time, float spin, bool storm)
    {
        Warn(Violet);
        Sfx.Play(storm ? SfxId.ArchitectStorm : SfxId.BossBullets);
        if (Form == 1)
        {
            if (formOne != null) formOne.PlayBulletPose();
            yield return HoldPose(animOne, 3f / 10f); // กางหนวดสุด ค้างไว้ตลอดที่ยิง
        }
        else if (formTwo != null) formTwo.SetFloating(true);

        float angle = Random.Range(0f, 360f), dir = Random.value < 0.5f ? -1f : 1f, next = 0f, nextWave = 1.1f;
        bool flipped = false;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (storm)
            {
                Vector2 root = transform.position;
                Vector2 goal = home;
                MoveWithin(Vector2.MoveTowards(root, goal, Speed * 1.5f * Time.deltaTime), Speed * 1.5f * Time.deltaTime);
                if (t >= nextWave)
                {
                    nextWave = t + (Enraged ? 0.8f : 1.1f);
                    if (formTwo != null) formTwo.PlayScytheSlash();
                    ThrowWaves(1, Aim());
                }
            }
            if (!flipped && Enraged && t > time * 0.5f)
            {
                flipped = true; // คลั่ง: เกลียวกลับทิศกลางทาง
                dir = -dir;
                Warn(RageRed);
            }
            angle += spin * dir * Time.deltaTime;
            if (t >= next)
            {
                next += spiralInterval;
                for (int i = 0; i < arms; i++) FireFromBody(angle + 360f / arms * i, orbSpeed);
            }
            Glow(Violet, 0.15f);
            yield return null;
        }
        ReleasePose(animOne);
        if (Form == 2 && formTwo != null) formTwo.SetFloating(false);
    }

    // ---------- ร่าง 1: วงแหวนมีช่องหลบ ----------

    IEnumerator Burst()
    {
        Warn(Violet);
        Sfx.Play(SfxId.BossBullets);
        if (formOne != null) formOne.PlayMixedPose();
        yield return HoldPose(animOne, 3f / 10f);
        int rings = HealthFraction <= 0.75f ? 3 : 2;
        const int count = 20;
        for (int r = 0; r < rings; r++)
        {
            int gap = Random.Range(0, count); // ช่องหลบ 3 ลูก สุ่มตำแหน่งทุกระลอก
            float offset = r * 180f / count;
            for (int i = 0; i < count; i++)
            {
                int fromGap = (i - gap + count) % count;
                if (fromGap < 3) continue;
                FireFromBody(offset + 360f / count * i, orbSpeed * 0.85f);
            }
            EchoFx.Shockwave(Core, Violet, 3f, 0.4f);
            CameraFollow.Shake(0.1f, 0.12f);
            yield return Wait(0.5f);
        }
        float aim = Angle(Aim());
        for (int k = -1; k <= 1; k++) FireFromBody(aim + k * 12f, orbSpeed * 1.6f);
        yield return Wait(0.3f);
        ReleasePose(animOne);
    }

    // ---------- เลเซอร์ ----------

    IEnumerator Laser()
    {
        bool two = Form == 2;
        LaserArt art = two ? laserTwo : laserOne;
        if (art == null) yield break;
        int count = two ? (Enraged ? 3 : 1) : (HealthFraction <= 0.75f ? 3 : 1);
        Warn(Cyan);
        FacePlayer();
        if (two) { if (formTwo != null) formTwo.PlayLaserPose(); }
        else if (formOne != null) formOne.PlayLaserPose();
        yield return HoldPose(Anim, 3f / 8f);

        Vector2 aim = Aim();
        for (int i = 0; i < count; i++) beams.Add(MakeBeam());
        Sfx.Play(SfxId.BossLaserCharge);
        // เตือน: ไล่ตามผู้เล่นช่วงแรก แล้วล็อกทิศให้เวลาหลบ ประกายดูดเข้าประตูมิติ
        float nextCharge = 0f;
        for (float t = 0f; t < laserWarning; t += Time.deltaTime)
        {
            float k = t / laserWarning;
            if (k < 0.6f) aim = Turn(aim, Aim(), 140f * Time.deltaTime);
            ShowWarning(aim, k, t, art);
            Glow(Cyan, 0.5f * k);
            if (t >= nextCharge)
            {
                nextCharge = t + 0.05f;
                for (int i = 0; i < beams.Count; i++)
                {
                    Vector2 from = Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.4f);
                    ImpactSparks.Spawn(Muzzle(BeamDirection(aim, i)) + from, Cyan, 1, -from, 3.5f, 5f);
                }
            }
            yield return null;
        }
        ClearMarks();

        CameraFollow.Shake(0.15f, 0.25f);
        Sfx.Play(SfxId.BossLaserBeam);
        bool hit = false;
        for (float t = 0f; t < laserActive; t += Time.deltaTime)
        {
            if (two) aim = Turn(aim, Aim(), laserSweep * (Enraged ? 1.5f : 1f) * Time.deltaTime);
            int frame = 2 + Mathf.FloorToInt(t * LaserFps) % 3;
            ShowBeams(aim, frame, 1f, art, laserThickness);
            if (!hit && AnyBeamHits(aim))
            {
                hit = true; // ยิงครั้งหนึ่งโดนได้ครั้งเดียว
                HurtPlayer(player, damage, Core, Knockback);
            }
            if (Mathf.Repeat(t, 0.08f) < Time.deltaTime)
                for (int i = 0; i < beams.Count; i++)
                {
                    Vector2 dir = BeamDirection(aim, i);
                    ImpactSparks.Spawn(Muzzle(dir) + dir * RayLength(Muzzle(dir), dir), Cyan, 3, -dir, 3f, 80f);
                }
            yield return null;
        }
        for (int frame = 5; frame <= 6; frame++)
        {
            ShowBeams(aim, frame, 1f, art, laserThickness);
            yield return Wait(1f / LaserFps);
        }
        DestroyBeams();
        ReleasePose(Anim);
    }

    // แถบกลางของแต่ละเฟรม (ช่วง 40–75% ของความยาวลำแสง) ใช้ยืดเป็นตัวลำแสง
    static LaserArt BuildLaserArt(Sprite[] frames)
    {
        if (frames == null || frames.Length < 7) return null;
        foreach (var frame in frames) if (frame == null) return null;
        var art = new LaserArt { frames = frames, strips = new Sprite[frames.Length] };
        Sprite full = frames[3];
        art.length = full.bounds.max.x * 0.95f; // ปลายภาพมีขอบว่างนิดหน่อย
        float ppu = full.pixelsPerUnit;
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite s = frames[i];
            Rect r = s.rect;
            float from = s.pivot.x + art.length * 0.4f * ppu, to = s.pivot.x + art.length * 0.75f * ppu;
            art.strips[i] = Sprite.Create(s.texture, new Rect(r.x + from, r.y, to - from, r.height),
                                          new Vector2(0f, s.pivot.y / r.height), ppu, 0, SpriteMeshType.FullRect);
        }
        art.bodyStart = art.length * 0.55f;
        art.stripWidth = art.length * 0.35f;
        return art;
    }

    Vector2 Muzzle(Vector2 dir) => Core + dir * (Form == 2 ? MuzzleTwo : MuzzleOne);

    Vector2 BeamDirection(Vector2 aim, int i)
    {
        int count = beams.Count;
        if (count <= 1) return aim;
        return Quaternion.Euler(0f, 0f, (i - (count - 1) / 2f) * fanAngle) * aim;
    }

    Beam MakeBeam()
    {
        int layer = SortingLayer.NameToID("Effect");
        return new Beam
        {
            body = EchoFx.Layer(null, "ArchitectLaserBody", null, layer, 7, Color.white),
            head = EchoFx.Layer(null, "ArchitectLaser", null, layer, 8, Color.white),
        };
    }

    // เตือนก่อนยิง: แถบบนพื้นกว้างเท่าที่โดนจริง ค่อย ๆ อ้วนขึ้น กะพริบถี่ขึ้นใกล้ยิง ทับด้วยเส้นลำแสงบาง
    void ShowWarning(Vector2 aim, float k, float t, LaserArt art)
    {
        while (marks.Count < beams.Count) FloorMark(ProceduralSprites.Bar, 4);
        float width = (laserHitRadius + 0.25f) * 2f;
        float blink = Mathf.Abs(Mathf.Sin(t * Mathf.Lerp(8f, 22f, k)));
        for (int i = 0; i < beams.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            Vector2 origin = Muzzle(dir);
            var bar = marks[i];
            bar.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Angle(dir)));
            bar.transform.localScale = new Vector3(RayLength(origin, dir), width * Mathf.Lerp(0.35f, 1f, k), 1f);
            bar.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.3f + 0.25f * k + 0.3f * blink);
        }
        ShowBeams(aim, 0, 0.6f + 0.4f * blink, art, laserThickness * 0.8f);
    }

    void ShowBeams(Vector2 aim, int frame, float alpha, LaserArt art, float thickness)
    {
        for (int i = 0; i < beams.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            DrawBeam(beams[i], Muzzle(dir), dir, frame, alpha, art, thickness);
        }
    }

    void DrawBeam(Beam beam, Vector2 origin, Vector2 dir, int frame, float alpha, LaserArt art, float thickness)
    {
        if (beam.head == null || beam.body == null) return;
        frame = Mathf.Clamp(frame, 0, art.frames.Length - 1);
        var tint = new Color(1f, 1f, 1f, alpha);
        float length = RayLength(origin, dir);
        var rotation = Quaternion.Euler(0f, 0f, Angle(dir));

        beam.head.sprite = art.frames[frame];
        beam.head.color = tint;
        beam.head.transform.SetPositionAndRotation(origin, rotation);
        beam.head.transform.localScale = new Vector3(Mathf.Min(1f, length / art.length), thickness, 1f);

        bool longer = length > art.length;
        beam.body.enabled = longer;
        if (!longer) return;
        beam.body.sprite = art.strips[frame];
        beam.body.color = tint;
        beam.body.transform.SetPositionAndRotation(origin + dir * art.bodyStart, rotation);
        beam.body.transform.localScale = new Vector3((length - art.bodyStart) / art.stripWidth, thickness, 1f);
    }

    bool AnyBeamHits(Vector2 aim)
    {
        for (int i = 0; i < beams.Count; i++)
        {
            Vector2 dir = BeamDirection(aim, i);
            if (InBeam(Muzzle(dir), dir)) return true;
        }
        return false;
    }

    bool InBeam(Vector2 origin, Vector2 dir)
    {
        if (player == null) return false;
        Vector2 offset = PlayerCenter - origin;
        float along = Vector2.Dot(offset, dir);
        if (along < -0.5f || along > RayLength(origin, dir)) return false;
        return Mathf.Abs(dir.x * offset.y - dir.y * offset.x) <= laserHitRadius + 0.25f;
    }

    void DestroyBeams()
    {
        foreach (var beam in beams)
        {
            if (beam.head != null) Destroy(beam.head.gameObject);
            if (beam.body != null) Destroy(beam.body.gameObject);
        }
        beams.Clear();
    }

    // ---------- ร่าง 2: วาร์ปพุ่งชน (ท่าหลักตามเอกสาร) ----------

    IEnumerator Dash()
    {
        if (formTwo == null || animTwo == null) yield break;
        int count = Enraged ? 3 : HealthFraction <= 0.38f ? 2 : 1;
        for (int n = 0; n < count; n++)
        {
            formTwo.SetFloating(false);
            FacePlayer();
            formTwo.PlayAuraDash();
            yield return HoldPose(animTwo, 2f / 12f); // ห่อตัวเป็นลูกแก้วออร่า
            Warn(Violet);

            float warn = Enraged ? dashWarning * 0.75f : dashWarning;
            var line = new DashLine { dir = Aim(), bar = FloorMark(ProceduralSprites.Bar, 4) };
            Vector2 target = PlayerCenter;
            for (float t = 0f; t < warn; t += Time.deltaTime)
            {
                float k = t / warn;
                if (k < 0.6f) target = PlayerCenter; // ไล่ตามช่วงแรก แล้วล็อกให้เวลาหลบ
                line.from = Core;
                AimDash(line, target, k, t);
                Glow(Violet, 0.3f + 0.3f * k);
                if (Random.value < 0.4f) ImpactSparks.Spawn(Core + Random.insideUnitCircle * 1.4f, Violet, 1, -line.dir, 3f, 40f);
                yield return null;
            }
            ClearMarks();

            // พุ่ง: ค้างภาพลูกแก้วมีหางออร่า ทิ้งเงาตามทาง ชนผู้เล่นได้ครั้งเดียว
            // วงแดงขึ้นที่จุดตกตั้งแต่เริ่มพุ่ง เต็มวงพอดีจังหวะเคียวฟัน
            MarkSlash(line);
            BeginDashPose(line.dir);
            bool hit = false;
            float nextGhost = 0f;
            while (line.travelled < line.length)
            {
                Vector2 before = Core;
                float step = Mathf.Min(dashSpeed * Time.deltaTime, line.length - line.travelled);
                transform.position += (Vector3)(line.dir * step);
                line.travelled += step;
                hit |= DashHit(hit, before);
                if (line.travelled >= nextGhost)
                {
                    nextGhost = line.travelled + 0.8f;
                    EchoFx.DriftGhost(viewTwo, -line.dir * 2f, 0.3f, 0.55f, Violet, 0.1f);
                }
                yield return null;
            }
            yield return ReturnSlash(n);
            if (n < count - 1) yield return Wait(0.15f);
        }
        yield return Stagger();
    }

    // เส้นเตือนก่อนพุ่ง จาก line.from ไปทางเป้า เลยไปอีกนิด หยุดก่อนชนกำแพง
    void AimDash(DashLine line, Vector2 target, float k, float t)
    {
        Vector2 to = target - line.from;
        if (to.sqrMagnitude > 0.01f) line.dir = to.normalized;
        line.length = Mathf.Max(0f, Mathf.Min(to.magnitude + dashOvershoot, RayLength(line.from, line.dir) - BodyRadius));
        if (line.bar == null) return;
        line.bar.transform.SetPositionAndRotation(line.from, Quaternion.Euler(0f, 0f, Angle(line.dir)));
        line.bar.transform.localScale = new Vector3(line.length, dashHitRadius * 2f * Mathf.Lerp(0.4f, 1f, k), 1f);
        float blink = Mathf.Abs(Mathf.Sin(t * Mathf.Lerp(8f, 24f, k)));
        line.bar.color = new Color(Violet.r, Violet.g, Violet.b, (0.3f + 0.25f * k + 0.3f * blink) * line.alpha);
    }

    // วงแดงแบบ Echo Commander ที่จุดตกของเส้นพุ่ง: ขอบขึ้นทันที ไส้เต็มจากกลางจนถึงจังหวะฟัน ช่วงท้ายกะพริบ
    void MarkSlash(DashLine line)
    {
        Vector2 end = line.from + line.dir * line.length;
        float warning = line.length / Mathf.Max(1f, dashSpeed) + SlashAt;
        slashMarks.Add(EchoSlashTelegraph.Show(end, Vector2.right, slashRadius, 360f, warning, SlashRed));
    }

    void ClearSlashMarks()
    {
        foreach (var mark in slashMarks) if (mark != null) Destroy(mark.gameObject);
        slashMarks.Clear();
    }

    void BeginDashPose(Vector2 dir)
    {
        Sfx.Play(SfxId.BossTeleport);
        formTwo.FaceLeft(dir.x < 0f);
        animTwo.Play("AuraDash", 0, 3f / 7f);
        animTwo.speed = 0f;
        CameraFollow.Shake(0.15f, 0.2f);
    }

    // ตัวจริงพุ่งผ่านผู้เล่น (วัดทั้งช่วงที่ขยับในเฟรมนี้ พุ่งเร็วไม่ทะลุข้าม)
    bool DashHit(bool already, Vector2 before)
    {
        if (already || player == null || SegmentDistance(PlayerCenter, before, Core) > dashHitRadius) return false;
        HurtPlayer(player, damage, before, Knockback * 1.4f);
        return true;
    }

    // ปลายทาง: คลายลูกแก้ว เคียวฟันกลับเป็นวง (วงเตือนเต็มพอดีจังหวะฟัน)
    IEnumerator ReturnSlash(int n)
    {
        animTwo.speed = 1f;
        animTwo.Play("AuraDash", 0, 4f / 7f);
        Vector2 center = Core;
        var mark = slashMarks.Count > 0 ? slashMarks[0] : null;
        if (mark == null) mark = EchoSlashTelegraph.Show(center, Vector2.right, slashRadius, 360f, SlashAt, SlashRed);
        else mark.transform.position = center; // จุดตกจริงอาจสั้นกว่าที่คาด (เฟรมสุดท้ายของการพุ่ง)
        slashMarks.Clear();
        yield return Wait(SlashAt);
        Sfx.Play(SfxId.ArchitectSlash);
        if (mark != null) mark.Strike();
        if (player != null && (PlayerCenter - center).sqrMagnitude <= slashRadius * slashRadius)
            HurtPlayer(player, damage, center, Knockback);
        EchoFx.Shockwave(center, Violet, slashRadius, 0.35f);
        ImpactSparks.Spawn(center, Violet, 12, Vector2.zero, 6f);
        CameraFollow.Shake(0.2f, 0.15f);
        if (Enraged)
            for (int i = 0; i < 10; i++) FireOrb(center, 36f * i + 18f * n, orbSpeed * 0.9f);
    }

    // เซหลังฟัน: ค้างท่าฟันจบ ดาวฟ้าวนเหนือหัว รับดาเมจแรงขึ้น (จังหวะตีสวนของคนที่หลบได้)
    IEnumerator Stagger()
    {
        yield return Wait(1f / 12f);
        if (animTwo != null) animTwo.speed = 0f;
        float time = Enraged ? staggerTime * 0.7f : staggerTime;
        if (time <= 0f) yield break;
        health.DamageScale = staggerScale;
        Vector2 head = viewTwo != null
            ? (Vector2)viewTwo.transform.position + Vector2.up * 2.6f * viewTwo.transform.lossyScale.y
            : Core + Vector2.up * 2.5f;
        EchoFx.Flash(head, Cyan, 2f, 0.3f);
        float spin = 0f, nextStar = 0f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            Glow(Cyan, 0.2f + 0.15f * Mathf.Abs(Mathf.Sin(t * 10f)));
            spin += Time.deltaTime * 540f;
            if (t >= nextStar)
            {
                nextStar = t + 0.05f;
                for (int k = 0; k < 3; k++)
                {
                    float a = (spin + 120f * k) * Mathf.Deg2Rad;
                    ImpactSparks.Spawn(head + new Vector2(Mathf.Cos(a) * 1.1f, Mathf.Sin(a) * 0.4f), k == 0 ? Color.white : Cyan, 1, Vector2.zero, 0.3f);
                }
            }
            yield return null;
        }
        health.DamageScale = 1f;
        if (animTwo != null) animTwo.speed = 1f;
    }

    // ---------- ร่าง 2: ร่างแยกหลอก ----------

    // ลูกแก้วออร่าแยกไปยืนล้อมผู้เล่น ทุกลูกขึ้นเส้นเตือนแล้วพุ่งพร้อมกัน มีลูกเดียวที่จริง
    // จุดสังเกต: ลูกหลอกใสและสั่นไหว เส้นเตือนจางกว่า ไม่ทิ้งเงาตามทาง พุ่งจบแล้วแตกสลาย ไม่ทำดาเมจ
    IEnumerator SplitDash()
    {
        if (formTwo == null || animTwo == null || viewTwo == null) yield break;
        formTwo.SetFloating(false);
        FacePlayer();
        formTwo.PlayAuraDash();
        yield return HoldPose(animTwo, 2f / 12f);
        Warn(Violet);

        int count = Mathf.Max(2, splitCount);
        int real = Random.Range(0, count);
        Vector2 coreOffset = Core - (Vector2)transform.position;
        Vector2 viewOffset = (Vector2)viewTwo.transform.position - (Vector2)transform.position;
        var lines = new DashLine[count];
        var starts = new Vector2[count];
        float baseAngle = Random.Range(0f, 360f);
        Vector2 center = PlayerCenter;
        ClearGhosts();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, baseAngle + 360f / count * i) * Vector2.right;
            Vector2 root = center + dir * splitRadius - coreOffset;
            root = area.ClosestPoint(new Vector3(root.x, root.y, 0f));
            lines[i] = new DashLine { from = root + coreOffset, alpha = i == real ? 1f : 0.6f };
            starts[i] = Core;
            if (i != real) ghosts.Add(MakeGhost());
        }

        // แยกตัวออกไปยืนรอบผู้เล่น
        const float spreadTime = 0.35f;
        for (float t = 0f; t <= spreadTime; t += Time.deltaTime)
        {
            float k = 1f - (1f - t / spreadTime) * (1f - t / spreadTime);
            for (int i = 0; i < count; i++) PlaceSplit(i, real, Vector2.Lerp(starts[i], lines[i].from, k), coreOffset, viewOffset);
            yield return null;
        }
        for (int i = 0; i < count; i++)
        {
            PlaceSplit(i, real, lines[i].from, coreOffset, viewOffset);
            lines[i].bar = FloorMark(ProceduralSprites.Bar, 4);
        }

        float warn = Enraged ? dashWarning * 0.85f : dashWarning;
        Vector2 target = PlayerCenter;
        for (float t = 0f; t < warn; t += Time.deltaTime)
        {
            float k = t / warn;
            if (k < 0.6f) target = PlayerCenter;
            for (int i = 0; i < count; i++) AimDash(lines[i], target, k, t);
            FlickerGhosts();
            Glow(Violet, 0.3f + 0.3f * k);
            yield return null;
        }
        ClearMarks();

        // พุ่งพร้อมกัน (วงแดงขึ้นทุกลูก ไม่งั้นวงจะบอกว่าลูกไหนจริง)
        foreach (var line in lines) MarkSlash(line);
        BeginDashPose(lines[real].dir);
        for (int i = 0, g = 0; i < count; i++)
            if (i != real && g < ghosts.Count) ghosts[g++].flipX = lines[i].dir.x < 0f;
        bool hit = false, moving = true;
        float nextGhost = 0f;
        while (moving)
        {
            moving = false;
            float step = dashSpeed * Time.deltaTime;
            for (int i = 0, g = 0; i < count; i++)
            {
                var line = lines[i];
                float d = Mathf.Min(step, line.length - line.travelled);
                if (i != real) g++;
                if (d <= 0f) continue;
                moving = true;
                line.travelled += d;
                Vector2 at = line.from + line.dir * line.travelled;
                if (i == real)
                {
                    Vector2 before = Core;
                    transform.position = (Vector3)(at - coreOffset);
                    hit |= DashHit(hit, before);
                    if (line.travelled >= nextGhost)
                    {
                        nextGhost = line.travelled + 0.8f;
                        EchoFx.DriftGhost(viewTwo, -line.dir * 2f, 0.3f, 0.55f, Violet, 0.1f);
                    }
                }
                else if (g - 1 < ghosts.Count && ghosts[g - 1] != null)
                {
                    ghosts[g - 1].transform.position = at - coreOffset + viewOffset;
                    ghosts[g - 1].sprite = viewTwo.sprite;
                }
            }
            FlickerGhosts();
            yield return null;
        }
        foreach (var ghost in ghosts)
            if (ghost != null) ImpactSparks.Spawn((Vector2)ghost.transform.position - viewOffset + coreOffset, Violet, 10, Vector2.zero, 4f);
        ClearGhosts();
        // วงของร่างหลอกวาบว่างเปล่า เหลือวงของตัวจริงให้ ReturnSlash ฟันลง
        for (int i = slashMarks.Count - 1; i >= 0; i--)
        {
            if (i == real) continue;
            if (slashMarks[i] != null) slashMarks[i].Strike();
            slashMarks.RemoveAt(i);
        }
        yield return ReturnSlash(0);
        yield return Stagger();
    }

    void PlaceSplit(int i, int real, Vector2 core, Vector2 coreOffset, Vector2 viewOffset)
    {
        if (i == real)
        {
            transform.position = (Vector3)(core - coreOffset);
            return;
        }
        int g = i < real ? i : i - 1;
        if (g >= ghosts.Count || ghosts[g] == null) return;
        ghosts[g].transform.position = core - coreOffset + viewOffset;
        ghosts[g].sprite = viewTwo.sprite;
    }

    SpriteRenderer MakeGhost()
    {
        var ghost = EchoFx.Layer(null, "ArchitectSplit", viewTwo.sprite, viewTwo.sortingLayerID, viewTwo.sortingOrder - 1,
                                 new Color(0.85f, 0.8f, 1f, fakeAlpha));
        ghost.transform.localScale = viewTwo.transform.lossyScale;
        ghost.flipX = viewTwo.flipX;
        return ghost;
    }

    void FlickerGhosts()
    {
        foreach (var ghost in ghosts)
            if (ghost != null) ghost.color = new Color(0.85f, 0.8f, 1f, fakeAlpha * (0.8f + 0.2f * Mathf.Sin(Time.time * 35f)));
    }

    void ClearGhosts()
    {
        foreach (var ghost in ghosts) if (ghost != null) Destroy(ghost.gameObject);
        ghosts.Clear();
    }

    // ---------- ร่าง 2: ตารางเลเซอร์จากประตูมิติ ----------

    // เปิดประตูมิติติดกำแพง ยิงลำแสงข้ามห้องเป็นแนวขนาน เว้นช่องให้ยืน 2–3 ระลอก สลับแนวนอน/แนวตั้ง
    IEnumerator Grid()
    {
        if (laserTwo == null) yield break;
        FacePlayer();
        Warn(Cyan);
        if (formTwo != null) formTwo.PlayLaserPose();
        yield return HoldPose(animTwo, 3f / 8f);

        int rounds = Enraged ? 3 : 2;
        bool horizontal = Random.value < 0.5f;
        for (int r = 0; r < rounds; r++, horizontal = !horizontal)
        {
            ClearMarks();
            Rect box = Playable();
            int count = gridLines + (Enraged ? 1 : 0);
            Vector2 dir = horizontal ? (Random.value < 0.5f ? Vector2.right : Vector2.left)
                                     : (Random.value < 0.5f ? Vector2.down : Vector2.up);
            float span = horizontal ? box.height : box.width;
            float spacing = span / count;
            float jitter = Random.Range(-0.3f, 0.3f) * spacing;
            var origins = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float c = (horizontal ? box.yMin : box.xMin) + spacing * (i + 0.5f) + jitter;
                Vector2 inside = horizontal ? new Vector2(box.center.x, c) : new Vector2(c, box.center.y);
                // ประตูมิติติดกำแพงฝั่งตรงข้ามทิศยิง
                origins[i] = inside - dir * Mathf.Max(0f, RayLength(inside, -dir) - 0.8f);
                beams.Add(MakeBeam());
                if (echoAtlas != null)
                {
                    var portal = EchoPortal.Open(null, origins[i], origins[i], 2.4f, echoAtlas, SortingLayer.NameToID("Effect"), 6, Violet);
                    if (!horizontal) portal.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    portals.Add(portal);
                }
                FloorMark(ProceduralSprites.Bar, 4);
            }

            float warn = Enraged ? gridWarning * 0.85f : gridWarning;
            Sfx.Play(SfxId.BossLaserCharge);
            float width = (laserHitRadius + 0.25f) * 2f;
            for (float t = 0f; t < warn; t += Time.deltaTime)
            {
                float k = t / warn;
                float blink = Mathf.Abs(Mathf.Sin(t * Mathf.Lerp(8f, 22f, k)));
                for (int i = 0; i < count; i++)
                {
                    var bar = marks[i];
                    bar.transform.SetPositionAndRotation(origins[i], Quaternion.Euler(0f, 0f, Angle(dir)));
                    bar.transform.localScale = new Vector3(RayLength(origins[i], dir), width * Mathf.Lerp(0.35f, 1f, k), 1f);
                    bar.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.3f + 0.25f * k + 0.3f * blink);
                    DrawBeam(beams[i], origins[i], dir, 0, 0.5f + 0.4f * blink, laserTwo, laserThickness * 0.7f);
                }
                yield return null;
            }
            ClearMarks();
            foreach (var portal in portals) if (portal != null) portal.Flare(0.25f);
            Sfx.Play(SfxId.BossLaserBeam);
            CameraFollow.Shake(0.2f, 0.25f);

            bool hit = false;
            for (float t = 0f; t < laserActive; t += Time.deltaTime)
            {
                int frame = 2 + Mathf.FloorToInt(t * LaserFps) % 3;
                for (int i = 0; i < count; i++)
                {
                    DrawBeam(beams[i], origins[i], dir, frame, 1f, laserTwo, laserThickness);
                    if (hit || !InBeam(origins[i], dir)) continue;
                    hit = true;
                    // ผลักออกด้านข้างของลำแสง (ไม่ใช่ไปตามลำแสง)
                    Vector2 offset = PlayerCenter - origins[i];
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    if (Vector2.Dot(offset, side) < 0f) side = -side;
                    HurtPlayer(player, damage, PlayerCenter - side, Knockback);
                }
                yield return null;
            }
            for (int frame = 5; frame <= 6; frame++)
            {
                for (int i = 0; i < count; i++) DrawBeam(beams[i], origins[i], dir, frame, 1f, laserTwo, laserThickness);
                yield return Wait(1f / LaserFps);
            }
            DestroyBeams();
            ClosePortals();
            yield return Wait(0.35f);
        }
        ReleasePose(animTwo);
    }

    void ClosePortals()
    {
        foreach (var portal in portals) if (portal != null) portal.Close();
        portals.Clear();
    }

    // พื้นที่ที่ยังยืนได้ (กรอบสนาม หักส่วนที่พังแล้ว)
    Rect Playable() => collapsing ? safe : arenaBox;

    // ---------- คลั่ง: ห้องพังทลาย ----------

    // ขอบห้องพังเป็นชั้น ๆ: พื้นอันตรายกะพริบเตือนก่อน แล้วกลายเป็นพื้นว่างเปล่าค้างไว้ ยืนนอกกรอบโดนดาเมจและถูกผลักเข้าใน
    IEnumerator Collapse()
    {
        var zone = arena != null && arena.zones != null && arena.zones.Length > 0 ? arena.zones[0] : null;
        var zoneView = zone != null ? zone.GetComponent<SpriteRenderer>() : null;
        Sprite warnSprite = zone != null && zone.warningSprite != null ? zone.warningSprite : ProceduralSprites.Disc;
        Sprite voidSprite = zone != null && zone.activeSprite != null ? zone.activeSprite : ProceduralSprites.Disc;
        float scale = zone != null ? Mathf.Abs(zone.transform.lossyScale.x) : 1f;
        float cell = Mathf.Clamp(voidSprite.bounds.size.x * scale * 0.7f, 1.2f, 4f);
        int layer = zoneView != null ? zoneView.sortingLayerID : SortingLayer.NameToID("bg2");
        int order = zoneView != null ? zoneView.sortingOrder : 1;
        Rect outer = new Rect(arenaBox.xMin - 8f, arenaBox.yMin - 8f, arenaBox.width + 16f, arenaBox.height + 16f);
        Rect current = outer;

        yield return Wait(2f);
        for (int step = 0; step < collapseSteps; step++)
        {
            Rect next = Shrink(step == 0 ? arenaBox : current, step == 0 ? collapseShrink * 0.4f : collapseShrink);
            if (next.width < 6f || next.height < 6f) yield break;
            // ชั้นที่จะพัง: ช่องที่อยู่ในกรอบเดิมแต่นอกกรอบใหม่ (ข้ามช่องที่เป็นกำแพง)
            var band = new List<SpriteRenderer>();
            for (float x = outer.xMin + cell * 0.5f; x < outer.xMax; x += cell)
                for (float y = outer.yMin + cell * 0.5f; y < outer.yMax; y += cell)
                {
                    var at = new Vector2(x, y);
                    if (next.Contains(at) || !current.Contains(at) || InsideWall(at) || !InsideArena(at)) continue;
                    var tile = EchoFx.Layer(null, "CollapseTile", warnSprite, layer, order, Color.clear);
                    tile.transform.position = at + Random.insideUnitCircle * cell * 0.15f;
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, 90f * Random.Range(0, 4));
                    tile.transform.localScale = Vector3.one * scale;
                    band.Add(tile);
                }
            Warn(RageRed);
            CameraFollow.Shake(0.15f, 0.3f);
            for (float t = 0f; t < collapseWarning; t += Time.deltaTime)
            {
                float blink = 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * Mathf.Lerp(6f, 16f, t / collapseWarning)));
                foreach (var tile in band) if (tile != null) tile.color = new Color(1f, 0.55f, 0.6f, blink);
                yield return null;
            }
            foreach (var tile in band)
            {
                if (tile == null) continue;
                tile.sprite = voidSprite;
                voidTiles.Add(tile);
            }
            if (!collapsing)
            {
                collapsing = true;
                nextVoidHit = Time.time + 0.3f;
            }
            safe = next;
            current = next;
            ShrinkArea(next);
            CameraFollow.Shake(0.3f, 0.35f);
            yield return Wait(collapseEvery);
        }
    }

    static Rect Shrink(Rect r, Vector2 by) => new Rect(r.xMin + by.x, r.yMin + by.y, r.width - by.x * 2f, r.height - by.y * 2f);

    // ตัวบอสเดินในกรอบที่ยังไม่พัง (เว้นขอบให้ตัวใหญ่ไม่ล้ำเข้าไปในพื้นที่พัง)
    void ShrinkArea(Rect standable)
    {
        Sfx.Play(SfxId.ArchitectCollapse);
        Rect inner = Shrink(standable, new Vector2(2.5f, 2f));
        float xMin = Mathf.Max(area.min.x, inner.xMin), xMax = Mathf.Min(area.max.x, inner.xMax);
        float yMin = Mathf.Max(area.min.y, inner.yMin), yMax = Mathf.Min(area.max.y, inner.yMax);
        if (xMax - xMin < 2f || yMax - yMin < 2f) return;
        area = new Bounds(new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f), new Vector3(xMax - xMin, yMax - yMin, 100f));
    }

    static readonly Collider2D[] wallProbe = new Collider2D[8];
    static ContactFilter2D wallFilter = new ContactFilter2D { useTriggers = false };

    bool InsideWall(Vector2 at)
    {
        int count = Physics2D.OverlapPoint(at, wallFilter, wallProbe);
        for (int i = 0; i < count; i++)
            if (LootPlacement.IsSolid(wallProbe[i]) && !wallProbe[i].transform.IsChildOf(transform)) return true;
        return false;
    }

    // อยู่ในห้อง (มองจากกลางสนามไปถึงได้โดยไม่ติดกำแพงก่อน) นอกกำแพงไม่ต้องวางพื้นพัง
    bool InsideArena(Vector2 at)
    {
        Vector2 to = at - home;
        float distance = to.magnitude;
        return distance < 0.01f || RayLength(home, to / distance) >= distance;
    }

    // ยืนบนพื้นที่พัง: ดาเมจทุก 1 วินาที ถูกผลักเข้ากลางห้อง พื้นว่างเปล่าเรืองเป็นจังหวะ
    void VoidTick()
    {
        if (!collapsing) return;
        float pulse = 0.55f + 0.2f * Mathf.Sin(Time.time * 3f);
        foreach (var tile in voidTiles) if (tile != null) tile.color = new Color(0.55f, 0.3f, 0.7f, pulse);
        if (player == null || player.isDead || Time.time < nextVoidHit) return;
        Vector2 at = PlayerCenter;
        if (safe.Contains(at)) return;
        nextVoidHit = Time.time + 1f;
        Vector2 inward = (safe.center - at).normalized;
        ImpactSparks.Spawn(at, Violet, 6, inward, 3f);
        HurtPlayer(player, collapseDamage, at - inward, 6f);
    }

    void StopCollapse()
    {
        if (collapse != null) StopCoroutine(collapse);
        collapse = null;
        collapsing = false;
        foreach (var tile in voidTiles) if (tile != null) Destroy(tile.gameObject);
        voidTiles.Clear();
    }

    // ---------- ฉากจบ: แกนกลางถล่ม ----------

    bool InterceptDeath()
    {
        if (coreBroken || dead) return false;
        Interrupt();
        ArchitectOrb.ClearAll();
        coreRunning = true;
        health.Invulnerable = true;
        brain = StartCoroutine(CoreCollapse());
        return true;
    }

    // เลือดหมด: ร่างสั่น สลายเป็นประกายไหลเข้าหัวใจกลางห้อง หัวใจเปิดให้ตีภายในเวลา (วงนับเวลาหดเข้าหาหัวใจ)
    // ระหว่างนั้นหัวใจยิงเกลียว 3 แขนกับวงแหวนเป็นระยะ ตีแตกทัน = ตายจริง ไม่ทัน = ร่างกลับมาจากหัวใจพร้อมเลือด 10%
    IEnumerator CoreCollapse()
    {
        if (health.hitbox != null) health.hitbox.enabled = false;
        var anim = Anim;
        if (anim != null) anim.speed = 0f;
        var view = View;
        Vector3 rest = view != null ? view.transform.localPosition : Vector3.zero;
        Flash(Color.white, 1f, 0.3f);
        HitStop.Freeze(0.1f);
        CameraFollow.Shake(0.35f, 0.8f);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            if (view != null) view.transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * 0.06f);
            Glow(Color.white, 0.3f + 0.4f * Mathf.Abs(Mathf.Sin(t * 14f)));
            yield return null;
        }
        if (view != null) view.transform.localPosition = rest;

        Vector2 heart = HeartSpot;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            dissolve = t / 0.8f;
            if (Random.value < 0.7f)
            {
                Vector2 from = Core + Random.insideUnitCircle * 2f;
                ImpactSparks.Spawn(from, Random.value < 0.5f ? Violet : Cyan, 1, heart - from, 9f, 8f);
            }
            yield return null;
        }
        dissolve = 1f;

        if (coreLeft <= 0f) coreLeft = coreHealth;
        core = ArchitectCore.Open(heart, coreRadius, coreLeft, coreHealth, arena != null && arena.core != null ? arena.core.transform : null);
        EchoFx.Flash(heart, Violet, 6f, 0.5f);
        CameraFollow.Shake(0.3f, 0.3f);
        var timer = FloorMark(ProceduralSprites.ThinRing, 3);
        timer.transform.position = heart;
        float angle = Random.Range(0f, 360f), next = 0.4f, nextRing = 1.2f;
        for (float t = 0f; t < coreTime && core != null && !core.IsBroken; t += Time.deltaTime)
        {
            float k = t / coreTime;
            float d = Mathf.Lerp(12f, coreRadius * 2f, k);
            timer.transform.localScale = new Vector3(d, d * 0.6f, 1f);
            Color c = Color.Lerp(Cyan, RageRed, k);
            timer.color = new Color(c.r, c.g, c.b, 0.85f);
            angle += 95f * Time.deltaTime;
            if (t >= next)
            {
                next += 0.16f;
                for (int i = 0; i < 3; i++) FireAround(heart, angle + 120f * i, orbSpeed * 0.9f);
            }
            if (t >= nextRing)
            {
                nextRing += 1.8f;
                for (int i = 0; i < 14; i++) FireAround(heart, 360f / 14f * i + angle * 0.5f, orbSpeed * 0.6f);
                EchoFx.Shockwave(heart, Violet, 3f, 0.4f);
            }
            yield return null;
        }
        ClearMarks();
        coreLeft = core != null ? core.Health : 0f;
        bool broken = core != null && core.IsBroken;
        if (core != null) core.Close();
        core = null;
        ArchitectOrb.ClearAll();

        if (broken)
        {
            coreBroken = true;
            coreRunning = false;
            EchoFx.Flash(heart, Color.white, 12f, 0.8f);
            EchoFx.Shockwave(heart, Violet, 12f, 1.2f);
            if (orbFrames != null && orbFrames.Length > 3) EchoFx.Shards(orbFrames[3], heart, 16, 7f, 1f);
            ImpactSparks.Spawn(heart, Cyan, 30, Vector2.zero, 8f);
            health.Kill(); // → OnDefeated → เปิดประตูออก
            yield break;
        }

        // ไม่ทัน: หัวใจปิด ร่างกลับมาจากหัวใจ
        transform.position = (Vector3)(heart - (Core - (Vector2)transform.position));
        if (anim != null) anim.speed = 1f;
        EchoFx.Flash(heart, RageRed, 8f, 0.6f);
        EchoFx.Shockwave(heart, RageRed, 8f, 0.8f);
        CameraFollow.Shake(0.4f, 0.5f);
        PushPlayer(6f, 11f);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            dissolve = 1f - t / 0.8f;
            yield return null;
        }
        dissolve = 0f;
        health.Revive(health.maxHealth * reviveFraction);
        coreRunning = false;
        graceUntil = Time.time + 0.6f;
        forceNext = Move.Split;
        brain = null;
    }

    // หยุดฉากแกนกลางกลางคัน (ผู้เล่นตาย)
    void EndCore()
    {
        coreRunning = false;
        if (core != null) core.Close();
        core = null;
    }

    void FireAround(Vector2 center, float angle, float speed)
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
        FireOrb(center + dir * coreRadius, angle, speed);
    }

    static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(point, a + ab * t);
    }

    // ---------- ร่าง 2: คลื่นเคียว / พายุหมุนวน ----------

    IEnumerator Waves()
    {
        int volleys = Enraged ? 2 : 1;
        for (int v = 0; v < volleys; v++)
        {
            FacePlayer();
            Warn(Violet);
            if (formTwo != null) formTwo.PlayScytheSlash();
            yield return Wait(3f / 12f); // เฟรมเคียวฟันลง
            ThrowWaves(Enraged ? 5 : 3, Aim());
            CameraFollow.Shake(0.12f, 0.15f);
            yield return Wait(0.45f);
        }
    }

    void ThrowWaves(int count, Vector2 aim)
    {
        if (waveFrames == null || waveFrames.Length == 0) return;
        float baseAngle = Angle(aim);
        Sfx.Play(SfxId.ArchitectWave);
        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (i - (count - 1) / 2f) * waveSpread;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            ArchitectOrb.Fire(waveFrames, 2, 4, Core + dir * 1.2f, dir * waveSpeed, damage, waveHitRadius, waveScale,
                              4f, true, Knockback, Violet, playerBody, transform);
        }
        ImpactSparks.Spawn(Core + aim * 1.2f, Violet, 6, aim, 4f, 40f);
    }

    // ลอยกลับกลางห้อง ยิงเกลียวรอบตัว ระหว่างนั้นเหวี่ยงคลื่นเคียวใส่เป็นระยะ
    IEnumerator Storm()
    {
        if (formTwo != null) formTwo.SetFloating(true);
        Warn(Violet);
        for (float t = 0f; t < 1.2f && ((Vector2)transform.position - home).sqrMagnitude > 0.25f; t += Time.deltaTime)
        {
            float step = Speed * 3f * Time.deltaTime;
            MoveWithin(Vector2.MoveTowards(transform.position, home, step), step);
            yield return null;
        }
        yield return Spiral(Enraged ? 6 : 4, spiralTime + 0.5f, spiralSpin * 0.85f, true);
    }

    // ---------- ตาย ----------

    void OnDefeated()
    {
        Interrupt();
        Sfx.Play(SfxId.ArchitectDeath);
        ArchitectOrb.ClearAll();
        dead = true;
        StartCoroutine(DeathShow());
    }

    IEnumerator DeathShow()
    {
        if (dissolve >= 1f) // สลายเข้าหัวใจไปแล้ว (แกนแตก)
        {
            yield return Wait(0.8f);
            StopCollapse();
            health.FinishDeath();
            yield break;
        }
        var view = View;
        var anim = Anim;
        if (anim != null) anim.speed = 0f; // ค้างท่าสุดท้าย
        Vector3 rest = view != null ? view.transform.localPosition : Vector3.zero;
        Flash(Color.white, 1f, 0.3f);
        HitStop.Freeze(0.12f);
        CameraFollow.Shake(0.4f, 1.3f);
        for (float t = 0f; t < 1.3f; t += Time.deltaTime)
        {
            if (view != null) view.transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * 0.06f);
            Glow(Color.white, 0.2f + 0.4f * Mathf.Abs(Mathf.Sin(t * 14f)));
            if (Random.value < 0.5f)
                ImpactSparks.Spawn(Core + Random.insideUnitCircle * 2f, Random.value < 0.5f ? Violet : Cyan, 2, Vector2.zero, 4f);
            yield return null;
        }
        if (view != null) view.transform.localPosition = rest;
        EchoFx.Flash(Core, Color.white, 9f, 0.6f);
        EchoFx.Shockwave(Core, Violet, 10f, 1f);
        if (orbFrames != null && orbFrames.Length > 3) EchoFx.Shards(orbFrames[3], Core, 12, 6f, 0.9f);
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            dissolve = t;
            yield return null;
        }
        dissolve = 1f;
        StopCollapse();
        health.FinishDeath();
    }

    // ---------- หน้าตา ----------

    void OnDamaged(float amount) => Flash(Color.white, 0.85f, 0.1f);

    void Flash(Color color, float power, float time)
    {
        flashColor = color;
        flashPower = power;
        flashTime = Mathf.Max(0.01f, time);
        flashLeft = flashTime;
    }

    void Warn(Color color) => Flash(color, 0.6f, 0.25f);

    // เรืองสีเฉพาะเฟรมนี้ (เรียกทุกเฟรมตราบที่ต้องการ)
    void Glow(Color color, float amount)
    {
        if (amount < glowAmount) return;
        glowColor = color;
        glowAmount = Mathf.Clamp01(amount);
    }

    void LateUpdate()
    {
        if (block == null) return;
        float flash = flashPower * Mathf.Clamp01(flashLeft / flashTime * 1.5f);
        flashLeft = Mathf.Max(0f, flashLeft - Time.deltaTime);
        if (Enraged && !dead && !health.IsDefeated) Glow(RageRed, 0.12f + 0.1f * Mathf.Sin(Time.time * 6f));
        Color color = flash >= glowAmount ? flashColor : glowColor;
        float amount = Mathf.Max(flash, glowAmount);
        glowAmount = 0f;
        Apply(viewOne, color, amount);
        Apply(viewTwo, color, amount);
    }

    void Apply(SpriteRenderer view, Color color, float amount)
    {
        if (view == null || !view.gameObject.activeInHierarchy || view.sharedMaterial != MonsterFx.SharedMaterial) return;
        view.GetPropertyBlock(block);
        block.SetColor(FlashColorId, color);
        block.SetFloat(FlashAmountId, amount);
        block.SetFloat(DissolveId, dissolve);
        block.SetColor(DissolveColorId, Violet);
        Texture tex = view.sprite != null ? view.sprite.texture : null;
        if (tex != null) block.SetVector(PixelGridId, new Vector4(tex.width, tex.height, 0f, 0f));
        view.SetPropertyBlock(block);
    }

    // ตัวชนตามร่าง: ร่าง 1 ก้อนกลมใหญ่ ร่าง 2 คนยืน (ครอบลำตัวช่วงล่างถึงอก)
    void FitHitbox()
    {
        if (circleHitbox == null) return;
        Vector2 local = (Vector2)transform.InverseTransformPoint(Core);
        circleHitbox.offset = Form == 2 ? local + Vector2.down * 0.4f : local;
        circleHitbox.radius = Form == 2 ? 1.9f : 2.5f;
    }

    // ---------- ตัวช่วย ----------

    // ยิงจากขอบตัว (ภาพบอสใหญ่ ยิงจากกลางตัวกระสุนจะจมอยู่ใต้ภาพช่วงแรก)
    void FireFromBody(float angle, float speed)
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
        FireOrb(Core + dir * (Form == 2 ? 1f : 1.8f), angle, speed);
    }

    void FireOrb(Vector2 at, float angle, float speed, float scale = 1f)
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
        ArchitectOrb.Fire(orbFrames, 0, 6, at, dir * speed, damage, orbHitRadius * scale, orbScale * scale, orbLife, false,
                          Knockback * 0.6f, Violet, playerBody, transform);
    }

    void PushPlayer(float radius, float force)
    {
        if (player == null || player.isDead || (PlayerCenter - Core).sqrMagnitude > radius * radius) return;
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.TakeKnockback(Core, force);
    }

    Vector2 Aim()
    {
        Vector2 to = PlayerCenter - Core;
        return to.sqrMagnitude > 0.001f ? to.normalized : Vector2.down;
    }

    static float Angle(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    static Vector2 Turn(Vector2 from, Vector2 to, float maxDegrees)
    {
        float angle = Mathf.Clamp(Vector2.SignedAngle(from, to), -maxDegrees, maxDegrees);
        return Quaternion.Euler(0f, 0f, angle) * from;
    }

    // ระยะถึงกำแพง/สิ่งกีดขวางแรก (ไม่นับตัวบอส ผู้เล่น มอน)
    float RayLength(Vector2 origin, Vector2 dir)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, dir, laserMaxLength))
        {
            var c = hit.collider;
            if (!LootPlacement.IsSolid(c) || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<PlayerStats>() != null || c.GetComponentInParent<MonsterController>() != null) continue;
            return Mathf.Max(0.5f, hit.distance);
        }
        return laserMaxLength;
    }

    SpriteRenderer FloorMark(Sprite sprite, int order)
    {
        var view = EchoFx.Layer(null, "ArchitectWarning", sprite, SortingLayer.NameToID("bg2"), order, Color.clear);
        marks.Add(view);
        return view;
    }

    void ClearMarks()
    {
        foreach (var mark in marks) if (mark != null) Destroy(mark.gameObject);
        marks.Clear();
    }

    // เล่นท่าไปถึงเฟรมที่ต้องการแล้วค้างไว้ (Animator ไม่มี curve ขยับ transform ค้างได้ปลอดภัย)
    static IEnumerator HoldPose(Animator anim, float seconds)
    {
        yield return Wait(seconds);
        if (anim != null) anim.speed = 0f;
    }

    static void ReleasePose(Animator anim)
    {
        if (anim != null) anim.speed = 1f;
    }

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
    }

    void OnDisable()
    {
        brain = null;
        collapse = null;
        DestroyBeams();
        ClearMarks();
        ClearGhosts();
        ClearSlashMarks();
        ClosePortals();
        foreach (var tile in voidTiles) if (tile != null) Destroy(tile.gameObject);
        voidTiles.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.6f);
        Gizmos.DrawWireCube(area.center, new Vector3(area.size.x, area.size.y, 0f));
    }
#endif
}
