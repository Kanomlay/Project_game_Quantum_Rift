using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// บอสประจำแมพ 1 ตามตาราง 1.8: ผู้บัญชาการเสียงสะท้อน ใช้ภาพชุดเดิมทั้งหมด (ท่าเดิน/เรียก/ยิง/ฟัน + กระสุนดาวกระจาย)
//   ฟัน: ง้างท่าพร้อมพัดเตือนบนพื้นด้านหน้า แล้วโดนเฉพาะในพัด ถอยออกหรืออ้อมไปข้างหลังก็หลบได้
//   ยิง: ดาวกระจายทรงพัดเล็งผู้เล่น
//   เรียกลูกน้อง: ประตูมิติ (ตัดจากท่าเรียก) เปิดตรงจุดเกิด ลูกน้องก้าวออกมา
//                 ปกติเรียกระดับธรรมดา ช่วงคลั่งเรียกหัวหน้าหน่วย
//   วาร์ป: ผู้เล่นยืนห่างนาน ๆ บอสเดินเข้าประตูข้างตัว แล้วโผล่จากอีกบานข้างผู้เล่นพร้อมฟัน (บานปลายทางเปิดให้เห็นก่อน)
//   ช่วงคลั่ง (เลือดต่ำกว่า 30%): ออร่าม่วงกับเงาตามตัว ยิงถี่และกว้างขึ้น
//                 และร่างเสียงสะท้อนโผล่รอบผู้เล่นมายิงพร้อมบอส
//   ตาย: ตัวสั่นกะพริบ เงาร่างแตกออก ดาวกระจายแตกรอบตัว ลูกน้องที่เหลือสลายตาม
//
// สืบทอดจาก MonsterController เพื่อให้ RoomController เสกได้เหมือนมอนสเตอร์ปกติ
// จะได้ประตูปิดตอนเริ่มสู้ เปิดตอนบอสตาย และพอร์ทัลออกทำงานตามระบบห้องเดิม
// ค่าเลือด/ดาเมจ/ความเร็ว/ระยะและคูลดาวน์ท่าฟัน อ่านจาก MonsterData เหมือนตัวอื่น
public class EchoCommanderBoss : MonsterController
{
    [Header("ท่ายิงกระสุนทรงพัด")]
    public GameObject projectilePrefab;
    [Min(1)] public int projectilesPerShot = 5;
    [Min(0f)] public float fanSpreadDegrees = 60f;
    [Min(0.1f)] public float projectileSpeed = 7f;
    [Min(0f)] public float projectileDamage = 2f;
    [Min(0.5f)] public float shootCooldown = 4f;
    [Min(1f)] public float shootRange = 14f;   // ไกลกว่านี้ไม่ยิง เดินเข้าหาก่อน
    [Min(0f)] public float shootWindup = 0.35f; // รอให้อนิเมชันเงื้อก่อนกระสุนออก

    [Header("ท่าฟัน (เริ่มฟันเมื่อผู้เล่นอยู่ในระยะโจมตีของ MonsterData)")]
    [Min(0.5f)] public float slashRadius = 2.6f;  // รัศมีพัดที่โดน
    [Range(30f, 240f)] public float slashArc = 130f;
    [Min(0.1f)] public float slashWindup = 0.45f; // เวลาง้างให้ผู้เล่นเห็นพัดเตือนก่อนฟันลง
    [Min(0f)] public float slashKnockback = 8f;

    [Header("ท่าเรียกลูกน้อง")]
    public MonsterData[] minions;             // ก่อนเข้าช่วงคลั่ง: เรียกมอนระดับธรรมดา
    [Min(1)] public int minionsPerSummon = 2;
    public MonsterData[] eliteMinions;        // ช่วงคลั่ง: เรียกระดับหัวหน้าหน่วย (ไม่ใส่ = เรียกระดับธรรมดาต่อ)
    [Min(1)] public int eliteMinionsPerSummon = 1; // ตัวใหญ่เลือดเยอะ เรียกทีละน้อยกว่า
    [Min(1f)] public float summonCooldown = 12f;
    [Min(1)] public int maxAliveMinions = 4;
    [Min(0f)] public float summonWindup = 0.5f;
    [Min(0.5f)] public float summonRadius = 3f;

    [Header("ท่าวาร์ปประตูมิติ")]
    [Min(1f)] public float riftStepDistance = 6f;    // ผู้เล่นอยู่ไกลกว่านี้...
    [Min(0f)] public float riftStepDelay = 2.5f;     // ...ติดต่อกันนานเท่านี้ บอสวาร์ปไปหา
    [Min(1f)] public float riftStepCooldown = 10f;
    [Min(0.5f)] public float riftStepWarning = 1.3f; // ประตูปลายทางเปิดให้เห็นนานเท่านี้ก่อนบอสโผล่
    [Min(0.1f)] public float riftStepSlashWindup = 0.35f;

    [Header("ช่วงคลั่ง (เลือดต่ำกว่าเกณฑ์)")]
    [Range(0f, 1f)] public float rageHealthThreshold = 0.3f; // คลั่งและเริ่มเรียกหัวหน้าหน่วย
    [Min(0)] public int rageExtraProjectiles = 2;
    [Range(0.3f, 1f)] public float rageCooldownScale = 0.75f; // คูณคูลดาวน์ยิง/ฟัน/วาร์ป
    [Min(0)] public int echoCount = 2;         // ร่างเสียงสะท้อนที่โผล่มายิงพร้อมบอส
    [Min(1)] public int echoProjectiles = 3;
    [Min(1f)] public float echoCooldown = 9f;

    private const float MinionWarning = 0.7f;  // ประตูเปิด (0.3 วิ) แล้วค้างให้เห็นก่อนลูกน้องก้าวออก
    private const float MinionWakeUp = 0.3f;
    private const float MuzzleHeight = 0.8f;   // กระสุนออกจากมือ ไม่ใช่จากเท้า
    private const float SlashAimClamp = 40f;   // ภาพฟันหันได้แค่ซ้าย/ขวา พัดเอียงตามผู้เล่นได้ไม่เกินนี้
    private const float SlashLead = 0.12f;     // ปล่อยท่าต่อจากเฟรมง้าง ถึงเฟรมคลื่นฟัน (เฟรม 3)
    private const float MeleeWindupFrame = 0.214f; // เวลาในท่าฟัน (0–1) ตรงเฟรมยกมือ (เฟรม 1 จาก 7)
    private const float RageFrame = 0.24f;     // ท่าเรียกเฟรม 2 (ยกมือมีพลังรวม) ใช้เป็นท่าคำราม
    private const float SummonPortalOpen = 0.75f; // ท่าเรียกถึงเฟรมประตูเปิดเต็ม (เฟรม 6)
    private const float DeathShowTime = 1.3f;
    // ประตูในเฟรม 6 ของท่าเรียก อยู่ห่างจากเท้าบอสเท่านี้ (วัดจากภาพ) ประตูจริงวางทับตรงนี้พอดี
    private static readonly Vector2 PortalBeside = new Vector2(1.04f, 0.775f);
    private static readonly Color SlashColor = new Color(1f, 0.3f, 0.55f);

    private float nextShootTime, nextSummonTime, nextRiftStepTime, nextEchoTime, farSince = -1f;
    private float nextAfterimage;
    private bool isCasting; // ระหว่างร่ายท่าไม่ให้เดินหรือฟันซ้อน
    private bool enraged;
    private Vector2 bodyOffset = new Vector2(0f, 0.92f);
    private SpriteRenderer aura;
    private EchoSlashTelegraph slashTelegraph;
    private readonly List<GameObject> aliveMinions = new List<GameObject>();
    private readonly List<EchoPortal> portals = new List<EchoPortal>();
    private readonly List<EchoCommanderEcho> echoes = new List<EchoCommanderEcho>();

    protected override bool ResistsKnockback => true;

    protected override void Start()
    {
        base.Start();

        foreach (var col in GetComponents<Collider2D>())
            if (!col.isTrigger) { bodyOffset = col.offset; break; }

        // เว้นจังหวะตอนเข้าห้องใหม่ๆ ผู้เล่นจะได้ไม่โดนรัวตั้งแต่วินาทีแรก
        nextShootTime = Time.time + 2f;
        nextSummonTime = Time.time + 5f;
        nextRiftStepTime = Time.time + 8f;
    }

    protected override void Update()
    {
        if (isDying) return;
        UpdateAura();
        if (UpdateStun()) return;
        if (Waking()) return;
        if (isCasting) return;
        if (isKnockedBack) return;
        if (HoldStill()) return; // คอนโซลทดสอบ: หยุด AI

        if (player == null || myData == null)
        {
            base.Update();
            return;
        }

        // เลือดหล่นต่ำกว่าเกณฑ์ครั้งแรก: คำราม เข้าช่วงคลั่ง
        if (!enraged && currentHealth <= myData.maxHealth * rageHealthThreshold)
        {
            StartCoroutine(Cast(EnterRage()));
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= riftStepDistance) farSince = -1f;
        else if (farSince < 0f) farSince = Time.time;

        if (Time.time >= nextSummonTime && CanSummon())
        {
            StartCoroutine(Cast(SummonRoutine()));
            return;
        }

        if (farSince >= 0f && Time.time - farSince >= riftStepDelay && Time.time >= nextRiftStepTime)
        {
            StartCoroutine(Cast(RiftStepRoutine()));
            return;
        }

        if (distance <= myData.attackRange)
        {
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(Cast(SlashRoutine(slashWindup)));
                return;
            }
            // ยืนรอจังหวะฟันครั้งถัดไป (ไม่ใช้ท่าฟันโดนทันทีของมอนปกติ)
            if (anim != null) anim.SetBool("isWalking", false);
            FacePlayer();
            return;
        }

        if (Time.time >= nextShootTime && distance <= shootRange)
        {
            StartCoroutine(Cast(ShootRoutine()));
            return;
        }

        // นอกจากท่าพิเศษ เดินไล่ผู้เล่นแบบมอนสเตอร์ปกติ
        base.Update();
    }

    private float CooldownScale => enraged ? rageCooldownScale : 1f;
    private Vector2 BodyCenter => (Vector2)transform.position + bodyOffset;
    private Texture2D Atlas => sr != null && sr.sprite != null ? sr.sprite.texture : null; // ภาพบอสทั้งชุดอยู่ในภาพเดียว

    private IEnumerator Cast(IEnumerator move)
    {
        isCasting = true;
        if (anim != null) anim.SetBool("isWalking", false);
        yield return PausedWhileHeld(move);
        if (anim != null) anim.speed = 1f;
        isCasting = false;
    }

    // ---------- ท่าฟัน ----------

    private IEnumerator SlashRoutine(float windup)
    {
        FacePlayer();
        Vector2 origin = BodyCenter;
        Vector2 facing = sr != null && sr.flipX ? Vector2.left : Vector2.right;
        Vector2 toPlayer = SkillCombat.BodyCenter(player.gameObject) - origin;
        float tilt = toPlayer.sqrMagnitude > 0.001f ? Vector2.SignedAngle(facing, toPlayer) : 0f;
        Vector2 aim = Quaternion.Euler(0f, 0f, Mathf.Clamp(tilt, -SlashAimClamp, SlashAimClamp)) * facing;

        slashTelegraph = EchoSlashTelegraph.Show(origin, aim, slashRadius, slashArc, windup + SlashLead, SlashColor);

        // ง้าง: ค้างเฟรมยกมือ (มีประกายที่มือ) ตลอดช่วงเตือน แล้วปล่อยท่าต่อจนคลื่นฟันออก
        if (anim != null)
        {
            anim.Play("Melee", 0, MeleeWindupFrame);
            anim.speed = 0f;
        }
        yield return new WaitForSeconds(windup);
        if (anim != null) anim.speed = 1f;
        yield return new WaitForSeconds(SlashLead);

        if (slashTelegraph != null) slashTelegraph.Strike();
        slashTelegraph = null;
        if (InSlash(origin, aim)) HitPlayer(myData.attackDamage, slashKnockback);
        ImpactSparks.Spawn(origin + aim * slashRadius * 0.6f, SlashColor, 10, aim, 5f, 55f);
        CameraFollow.Shake(0.07f, 0.1f);

        yield return new WaitForSeconds(0.3f);
        nextAttackTime = Time.time + myData.attackCooldown * CooldownScale;
    }

    // โดนเมื่อตัวผู้เล่นอยู่ในพัด (เผื่อขนาดตัวนิดหน่อย) ชิดตัวบอสมาก ๆ โดนเสมอ
    private bool InSlash(Vector2 origin, Vector2 aim)
    {
        if (player == null) return false;
        Vector2 to = SkillCombat.BodyCenter(player.gameObject) - origin;
        float distance = to.magnitude;
        if (distance > slashRadius + 0.3f) return false;
        if (distance < 0.6f) return true;
        return Vector2.Angle(aim, to) <= slashArc * 0.5f + 8f;
    }

    // ---------- ท่ายิง ----------

    private IEnumerator ShootRoutine()
    {
        FacePlayer();
        bool withEchoes = enraged && echoCount > 0 && Time.time >= nextEchoTime;
        if (withEchoes)
        {
            SpawnEchoes();
            yield return new WaitForSeconds(EchoCommanderEcho.AppearTime);
            FacePlayer();
        }

        if (anim != null) anim.Play("Shoot", 0, 0f);
        foreach (var echo in echoes) if (echo != null) echo.Shoot(player.position);

        yield return new WaitForSeconds(shootWindup);

        int count = projectilesPerShot + (enraged ? rageExtraProjectiles : 0);
        float spread = fanSpreadDegrees + (enraged ? 20f : 0f);
        FireFan((Vector2)transform.position + Vector2.up * MuzzleHeight, count, spread);
        foreach (var echo in echoes)
        {
            if (echo == null) continue;
            FireFan(echo.BodyCenter + Vector2.up * (MuzzleHeight - bodyOffset.y), echoProjectiles, 40f);
            echo.Vanish();
        }
        echoes.Clear();
        if (withEchoes) nextEchoTime = Time.time + echoCooldown;

        yield return new WaitForSeconds(0.25f);
        nextShootTime = Time.time + shootCooldown * CooldownScale;
    }

    // กระจายกระสุนเป็นพัดรอบทิศที่เล็งผู้เล่น นัดกลางพุ่งตรง
    private void FireFan(Vector2 origin, int count, float spread)
    {
        if (projectilePrefab == null || player == null) return;

        Vector2 aim = (SkillCombat.BodyCenter(player.gameObject) - origin).normalized;
        if (aim.sqrMagnitude < 0.001f) aim = Vector2.right;
        float step = count > 1 ? spread / (count - 1) : 0f;
        float start = count > 1 ? -spread / 2f : 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, start + step * i) * aim;
            var shot = Instantiate(projectilePrefab, origin + direction * 0.5f, Quaternion.identity);

            var projectile = shot.GetComponent<BossProjectile>();
            if (projectile != null) projectile.Launch(direction, projectileSpeed, projectileDamage, gameObject.layer);
            else Debug.LogWarning("prefab กระสุนของบอสยังไม่มีสคริปต์ BossProjectile กระสุนจะลอยนิ่ง");
        }
    }

    // ร่างเสียงสะท้อนโผล่รอบผู้เล่น คนละฝั่งกับบอส ผู้เล่นโดนกระสุนบีบจากหลายทิศ
    private void SpawnEchoes()
    {
        if (sr == null || anim == null) return;
        Vector2 hero = player.position;
        Vector2 fromHero = (Vector2)transform.position - hero;
        float baseAngle = fromHero.sqrMagnitude > 0.01f ? Mathf.Atan2(fromHero.y, fromHero.x) * Mathf.Rad2Deg : 0f;
        for (int i = 0; i < echoCount; i++)
        {
            float angle = baseAngle + 360f / (echoCount + 1) * (i + 1);
            Vector2 want = hero + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * 4.5f;
            if (!SpawnPlacement.TryPickNear(currentRoom, gameObject, want, 1.5f, hero, 3f, out Vector2 spot) &&
                !SpawnPlacement.TryPickNear(currentRoom, gameObject, hero, 6f, hero, 3.5f, out spot))
                continue;
            echoes.Add(EchoCommanderEcho.Create(transform.parent, spot, bodyOffset, sr, anim.runtimeAnimatorController, hero.x < spot.x));
        }
    }

    // ---------- ท่าเรียกลูกน้อง ----------

    private bool HasElites => eliteMinions != null && eliteMinions.Length > 0;
    private bool SummonsElites => enraged && HasElites;
    private MonsterData[] SummonPool => SummonsElites ? eliteMinions : minions;
    private int SummonCount => SummonsElites ? eliteMinionsPerSummon : minionsPerSummon;

    private bool CanSummon()
    {
        var pool = SummonPool;
        if (pool == null || pool.Length == 0) return false;

        aliveMinions.RemoveAll(minion => minion == null || !minion.activeInHierarchy);
        return aliveMinions.Count + SummonCount <= maxAliveMinions;
    }

    private IEnumerator SummonRoutine()
    {
        FacePlayer();
        if (anim != null) anim.Play("Summon", 0, 0f);

        yield return new WaitForSeconds(summonWindup);

        var pool = SummonPool;
        int count = SummonCount;
        bool elite = SummonsElites;
        for (int i = 0; i < count; i++) StartCoroutine(SummonThroughPortal(pool, elite, count, i));

        yield return new WaitForSeconds(0.5f);
        nextSummonTime = Time.time + summonCooldown;
    }

    // ประตูมิติเปิดตรงจุดเกิด ค้างให้เห็นแป๊บหนึ่ง แล้วลูกน้องก้าวออกมา ประตูวาบแล้วปิด
    private IEnumerator SummonThroughPortal(MonsterData[] pool, bool elite, int count, int index)
    {
        var data = pool[Random.Range(0, pool.Length)];
        if (data == null || data.monsterPrefab == null) yield break;
        if (data.monsterPrefab.GetComponent<MonsterController>() == null) yield break;

        Vector2 spot = PickSummonPosition(index, count, data.monsterPrefab);
        var body = SpawnPlacement.Measure(data.monsterPrefab);
        float height = elite ? 2.3f : 1.7f;
        Vector2 center = spot + body.Feet + Vector2.up * (height * 0.5f - 0.1f);
        var look = data.monsterPrefab.GetComponentInChildren<SpriteRenderer>();
        int layer = look != null ? look.sortingLayerID : sr.sortingLayerID;
        int order = (look != null ? look.sortingOrder : sr.sortingOrder) - 1; // อยู่หลังตัวมอนที่ก้าวออกมา
        // หัวหน้าหน่วยแสงรอบประตูสีส้ม แบบเดียวกับวงเตือนหัวหน้าในห้องปกติ
        Color accent = elite ? SpawnTelegraph.ColorFor(this, SpawnTelegraph.Emphasis.Leader) : EchoFx.Purple;
        var portal = EchoPortal.Open(transform.parent, center, spot, height, Atlas, layer, order, accent);
        portals.Add(portal);

        yield return new WaitForSeconds(MinionWarning + index * 0.15f);
        if (isDying || !gameObject.activeInHierarchy) { portal.Close(); yield break; }

        var spawned = Instantiate(data.monsterPrefab, spot, Quaternion.identity, transform.parent);
        var controller = spawned.GetComponent<MonsterController>();
        controller.myData = data;
        // ตั้งใจไม่ผูกกับห้อง: ถ้าให้ลูกน้องรายงานการตายด้วย ห้องจะนับว่าเคลียร์แล้วเปิดประตูทั้งที่บอสยังอยู่
        controller.currentRoom = null;
        controller.WakeUpAfter(MinionWakeUp);
        aliveMinions.Add(spawned);

        EchoFx.Emerge(spawned, 0.25f);
        portal.Flare();
        if (elite) CameraFollow.Shake(0.12f, 0.2f);
        portal.Close(0.4f);
        portals.Remove(portal);
    }

    // โผล่รอบตัวบอสตรงที่ว่างในห้อง (ไม่จมกำแพง ไม่ทับผู้เล่น) หาไม่ได้ใช้จุดเกิดของห้อง ไม่งั้นวางเป็นวงรอบตัวบอส
    private Vector2 PickSummonPosition(int index, int count, GameObject prefab)
    {
        Vector2 hero = player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.one * 99f;
        if (SpawnPlacement.TryPickNear(currentRoom, prefab, transform.position, summonRadius, hero, 2.5f, out Vector2 near))
            return near;

        var points = currentRoom != null ? currentRoom.monsterSpawnPoints : null;
        if (points != null && points.Length > 0)
        {
            var point = points[Random.Range(0, points.Length)];
            if (point != null) return point.position;
        }

        float angle = (360f / Mathf.Max(1, count)) * index + Random.Range(-20f, 20f);
        Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.right * summonRadius;
        return transform.position + offset;
    }

    // ---------- ท่าวาร์ปประตูมิติ ----------

    private IEnumerator RiftStepRoutine()
    {
        farSince = -1f;
        Vector2 hero = player.position;
        if (!SpawnPlacement.TryPickNear(currentRoom, gameObject, hero, 2.2f, hero, 1.4f, out Vector2 destination))
        {
            nextRiftStepTime = Time.time + 3f; // ข้างผู้เล่นไม่มีที่ว่าง ลองใหม่ทีหลัง
            yield break;
        }

        // ประตูปลายทางเปิดก่อน ผู้เล่นเห็นว่าบอสจะโผล่ตรงไหน
        var exit = EchoPortal.Open(transform.parent, destination + bodyOffset, destination, EchoPortal.NaturalHeight,
                                   Atlas, sr.sortingLayerID, sr.sortingOrder - 1, EchoFx.Purple);
        portals.Add(exit);
        float arriveAt = Time.time + riftStepWarning;

        // ท่าเรียก: ยกมือแล้วประตูเปิดข้างตัว วางประตูจริงไว้หลังภาพประตูในท่า (ตัวบอสจางไป ประตูยังอยู่)
        FacePlayer();
        float side = sr.flipX ? -1f : 1f;
        if (anim != null) anim.Play("Summon", 0, 0f);
        yield return new WaitForSeconds(SummonPortalOpen - 0.3f);
        Vector2 besideAt = (Vector2)transform.position + new Vector2(PortalBeside.x * side, PortalBeside.y);
        var entry = EchoPortal.Open(transform.parent, besideAt, besideAt, EchoPortal.NaturalHeight,
                                    Atlas, sr.sortingLayerID, sr.sortingOrder - 1, EchoFx.Purple);
        portals.Add(entry);
        yield return new WaitForSeconds(0.3f);
        if (anim != null) anim.speed = 0f; // ค้างเฟรมประตูเปิดเต็ม

        // เดินเข้าประตูพร้อมจางหาย ระหว่างนี้ตีไม่โดน
        SetBodyActive(false);
        Vector2 start = transform.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            MoveTo(start + Vector2.right * side * 0.5f * k);
            SetAlpha(1f - k);
            yield return null;
        }
        SetAlpha(0f);
        entry.Flare();
        entry.Close(0.2f);
        portals.Remove(entry);

        while (Time.time < arriveAt) yield return null;

        // โผล่จากประตูปลายทาง
        MoveTo(destination);
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Idle", 0, 0f);
        }
        FacePlayer();
        exit.Flare();
        EchoFx.Shockwave(destination, EchoFx.Purple, 1.6f, 0.35f);
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            SetAlpha(t / 0.15f);
            yield return null;
        }
        SetAlpha(1f);
        SetBodyActive(true);
        exit.Close(0.25f);
        portals.Remove(exit);

        nextRiftStepTime = Time.time + riftStepCooldown * CooldownScale;
        yield return SlashRoutine(riftStepSlashWindup);
    }

    private void MoveTo(Vector2 position)
    {
        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private void SetBodyActive(bool active)
    {
        foreach (var col in GetComponents<Collider2D>()) col.enabled = active;
        if (active) IgnorePlayerCollisions(); // เปิด collider กลับแล้วตั้งให้ไม่ชนผู้เล่นซ้ำ
    }

    // ---------- ช่วงคลั่ง ----------

    private IEnumerator EnterRage()
    {
        enraged = true;
        nextSummonTime = Mathf.Min(nextSummonTime, Time.time + 1.5f); // เรียกหัวหน้าหน่วยทันทีหลังคำราม
        nextEchoTime = Time.time + 2.5f;

        // คำราม: ค้างท่ายกมือรวมพลัง แสงวาบ คลื่นกระแทก เงาร่างแตกออกสามทิศ
        FacePlayer();
        if (anim != null)
        {
            anim.Play("Summon", 0, RageFrame);
            anim.speed = 0f;
        }
        Vector2 center = BodyCenter;
        CameraFollow.Shake(0.25f, 0.35f);
        EchoFx.Flash(center, EchoFx.Purple, 3.5f, 0.35f);
        EchoFx.Shockwave(transform.position, EchoFx.Purple, 4.5f, 0.55f);
        ImpactSparks.Spawn(center, EchoFx.Purple, 24, Vector2.zero, 6f);
        EchoFx.DriftGhost(sr, Vector2.left * 2.5f, 0.5f, 0.5f, EchoFx.Purple);
        EchoFx.DriftGhost(sr, Vector2.right * 2.5f, 0.5f, 0.5f, EchoFx.Purple);
        EchoFx.DriftGhost(sr, Vector2.up * 2f, 0.5f, 0.5f, EchoFx.Purple);
        if (sr != null && aura == null)
        {
            aura = EchoFx.Layer(transform, "RageAura", ProceduralSprites.Glow, sr.sortingLayerID, sr.sortingOrder - 1, Color.clear);
            aura.transform.localPosition = bodyOffset;
        }

        yield return new WaitForSeconds(0.7f);
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Idle", 0, 0f);
        }
    }

    // ออร่าม่วงเต้นเป็นจังหวะ และเงาตามตัวทิ้งไว้เป็นระยะ
    private void UpdateAura()
    {
        if (aura == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        aura.transform.localScale = new Vector3(2.4f + 0.25f * pulse, 3f + 0.3f * pulse, 1f);
        Color p = EchoFx.Purple;
        float visible = sr != null ? sr.color.a : 1f;
        aura.color = new Color(p.r, p.g, p.b, (0.26f + 0.14f * pulse) * visible);
        if (Time.time >= nextAfterimage && visible > 0.9f)
        {
            nextAfterimage = Time.time + 0.14f;
            SpriteGhost.Spawn(sr, 0.35f, 0.3f, p);
        }
    }

    // ---------- ตาย ----------

    private void FacePlayer()
    {
        if (player == null || sr == null) return;
        sr.flipX = player.position.x < transform.position.x;
    }

    protected override void Die()
    {
        StopAllCoroutines();
        isCasting = false;
        if (slashTelegraph != null) Destroy(slashTelegraph.gameObject);
        foreach (var echo in echoes) if (echo != null) echo.Vanish();
        echoes.Clear();
        foreach (var portal in portals) if (portal != null) portal.Close();
        portals.Clear();
        if (sr != null) sr.color = Color.white;
        if (anim != null) anim.speed = 0f; // ค้างท่าสุดท้ายไว้ระหว่างแตกสลาย

        deathLinger = DeathShowTime + 0.1f;
        deathFade = 0.6f;
        base.Die();
        StartCoroutine(DeathShow());
    }

    // ตัวสั่นกะพริบ เงาร่างหลุดออกทีละชั้น ลูกน้องสลายตามทีละตัว แล้วระเบิดเป็นดาวกระจาย (จากนั้นระบบตายเดิมค่อยจางตัวหาย)
    private IEnumerator DeathShow()
    {
        Vector2 home = transform.position;
        Vector2 center = BodyCenter;
        Color purple = EchoFx.Purple;
        CameraFollow.Shake(0.2f, 0.4f);
        EchoFx.Flash(center, Color.white, 3f, 0.3f);
        ImpactSparks.Spawn(center, purple, 20, Vector2.zero, 5f);

        var left = aliveMinions.ToArray();
        int next = 0;
        float nextBurst = 0f;
        for (float t = 0f; t < DeathShowTime; t += Time.deltaTime)
        {
            float k = t / DeathShowTime;
            transform.position = home + Random.insideUnitCircle * 0.07f * (0.4f + k);
            sr.color = Mathf.Repeat(t, 0.16f) < 0.08f ? Color.white : new Color(0.55f, 0.35f, 0.9f);
            if (aura != null) aura.color = new Color(purple.r, purple.g, purple.b, 0.45f * (1f - k));
            if (t >= nextBurst)
            {
                nextBurst = t + 0.16f;
                ImpactSparks.Spawn(center + Random.insideUnitCircle * 0.6f, purple, 5, Vector2.zero, 3.5f);
                EchoFx.DriftGhost(sr, Random.insideUnitCircle.normalized * 1.8f, 0.6f, 0.45f, purple, 0.25f);
                if (next < left.Length) DissolveMinion(left[next++]);
            }
            yield return null;
        }
        while (next < left.Length) DissolveMinion(left[next++]);

        transform.position = home;
        sr.color = Color.white;
        CameraFollow.Shake(0.35f, 0.35f);
        EchoFx.Flash(center, EchoFx.PaleViolet, 5f, 0.45f);
        EchoFx.Shockwave(home, purple, 5f, 0.7f);
        ImpactSparks.Spawn(center, purple, 36, Vector2.zero, 7f);
        var shard = projectilePrefab != null ? projectilePrefab.GetComponentInChildren<SpriteRenderer>() : null;
        EchoFx.Shards(shard != null ? shard.sprite : null, center, 8, 6f, 0.7f);
        for (int i = 0; i < 6; i++)
            EchoFx.DriftGhost(sr, Quaternion.Euler(0f, 0f, 60f * i + 30f) * Vector2.right * 3f, 0.7f, 0.5f, purple, 0.4f);
        if (aura != null) Destroy(aura.gameObject);
    }

    // ลูกน้องที่ยังเหลือสลายตามบอส (ตายตามระบบปกติ มีท่าตาย/นับเป็นศัตรูที่กำจัด)
    private void DissolveMinion(GameObject minion)
    {
        if (minion == null || !minion.activeInHierarchy) return;
        var controller = minion.GetComponent<MonsterController>();
        if (controller == null || !controller.IsAlive) return;
        ImpactSparks.Spawn((Vector2)minion.transform.position + Vector2.up * 0.5f, EchoFx.Purple, 10, Vector2.zero, 4f);
        controller.SelfDestruct(); // ตายแบบปกติโดยไม่มีตัวเลขดาเมจยักษ์เด้ง
    }
}
