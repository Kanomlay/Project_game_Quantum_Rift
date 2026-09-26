using System;
using System.Collections.Generic;
using UnityEngine;

// พรควอนตัม: จบด่านแล้วเลือก 1 จาก 3 พรสุ่ม (ไม่ซ้ำกับที่มี) สะสมได้สูงสุด 4 พรต่อรอบ
// MapManager ขอให้เสนอพรก่อนโหลดด่านถัดไป ระหว่างเลือกเกมหยุด
// อยู่ในฉากเกม (BlessingBuilder ใส่ให้) เริ่มรอบใหม่ = โหลดฉากใหม่ พรที่มีจึงล้างเองทุกรอบ
//
// ผลของพรทุกแบบอยู่ในไฟล์นี้ ระบบอื่นแค่เรียกจุดเกี่ยวแบบ static (ไม่มีตัวจัดการในฉาก = ไม่มีผลอะไร):
// - อาวุธ: PayWeaponEnergy / HasWeaponEnergy (พลังงานสำรอง), OnAttackLanded (คลื่นสะสม), CleaveBullets (คมสลายมิติ)
//   และ SetupProjectile (กระสุนทะลุมิติ)
// - ผู้เล่น: AbsorbDamage (เกราะฉุกเฉิน), OnPlayerHurt (เกราะฉุกเฉินขึ้น / ก้าวพ้นรอยแยก)
// - ห้อง/มอนสเตอร์: ฟัง RoomController.CombatStarted/CombatCleared และ MonsterController.Died
public sealed class BlessingManager : MonoBehaviour
{
    public static BlessingManager Instance { get; private set; }

    [Header("พรทั้งหมด (BlessingBuilder ใส่ให้)")]
    public BlessingData[] all;
    [Min(1)] public int maxBlessings = 4;
    [Min(1)] public int choices = 3;

    [Header("UI (BlessingBuilder สร้างในฉาก)")]
    public BlessingWindow window;
    public BlessingHud hud;

    readonly List<BlessingData> owned = new List<BlessingData>();
    public IReadOnlyList<BlessingData> Owned => owned;
    public static bool IsChoosing => Instance != null && Instance.window != null && Instance.window.IsOpen;

    PlayerStats player;
    PlayerMovement mover;
    SpriteRenderer playerView;

    // สถานะของพรแต่ละแบบ
    int waveHits;                                              // คลื่นสะสม: โจมตีโดนไปกี่ครั้งแล้ว
    readonly Queue<object> countedAttacks = new Queue<object>(); // การโจมตีที่นับไปแล้ว (นับครั้งเดียวต่อการโจมตี)
    int reserveCount;                                          // พลังงานสำรอง: จ่ายพลังงานไปกี่ครั้งตั้งแต่ครั้งฟรีล่าสุด
    int harvestedThisRoom;                                     // เก็บเกี่ยวพลังงาน
    bool shieldUsedThisRoom;                                   // เกราะฉุกเฉิน
    float shieldLeft, shieldUntil;
    float riftStepReadyAt, riftStepUntil, nextGhost;           // ก้าวพ้นรอยแยก

    // ภาพ: สนามชะลอกระสุน (วงรอบตัว) และฟองเกราะ
    Transform field, fieldSpin, bubble;
    SpriteRenderer fieldRing, fieldGlow, bubbleRing, bubbleGlow;

    static readonly Color ShieldColor = new Color(0.35f, 0.85f, 1f);
    static readonly Color RiftColor = new Color(0.75f, 0.45f, 1f);
    static readonly Color HarvestColor = new Color(0.45f, 0.95f, 1f);
    static readonly Color HealColor = new Color(0.4f, 1f, 0.6f);
    static readonly Color ReserveColor = new Color(0.4f, 1f, 0.85f);

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        RoomController.CombatStarted += OnCombatStarted;
        RoomController.CombatCleared += OnCombatCleared;
        MonsterController.Died += OnMonsterDied;
    }

    void OnDisable()
    {
        RoomController.CombatStarted -= OnCombatStarted;
        RoomController.CombatCleared -= OnCombatCleared;
        MonsterController.Died -= OnMonsterDied;
        EnemyBullets.SetSlowField(false, Vector2.zero, 0f, 1f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (hud != null) hud.Refresh(this);
    }

    public BlessingData Get(BlessingType type)
    {
        foreach (var blessing in owned)
            if (blessing != null && blessing.type == type) return blessing;
        return null;
    }

    static BlessingData Active(BlessingType type) => Instance != null ? Instance.Get(type) : null;
    public static bool Has(BlessingType type) => Active(type) != null;

    bool FindPlayer()
    {
        if (player != null) return true;
        var hero = GameObject.FindGameObjectWithTag("Player");
        if (hero == null) return false;
        player = hero.GetComponent<PlayerStats>();
        mover = hero.GetComponent<PlayerMovement>();
        playerView = hero.GetComponent<SpriteRenderer>();
        return player != null;
    }

    // ---------- เสนอพร ----------

    // คืน false ถ้าไม่มีอะไรให้เลือก (ครบ 4 พรแล้ว / ยังไม่ได้ตั้ง UI) ให้คนเรียกไปต่อเองทันที
    public bool OfferChoice(Action afterChosen)
    {
        if (window == null || all == null || owned.Count >= maxBlessings) return false;

        var pool = new List<BlessingData>();
        foreach (var blessing in all)
            if (blessing != null && !owned.Contains(blessing) && !pool.Contains(blessing)) pool.Add(blessing);
        if (pool.Count == 0) return false;

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swap = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[swap]) = (pool[swap], pool[i]);
        }
        var options = pool.GetRange(0, Mathf.Min(choices, pool.Count));

        SetPaused(true);
        window.Open(options, owned, maxBlessings, chosen =>
        {
            if (chosen != null && !owned.Contains(chosen)) owned.Add(chosen);
            if (hud != null)
            {
                hud.Refresh(this);
                hud.Ping(owned.IndexOf(chosen));
            }
            SetPaused(false);
            afterChosen?.Invoke();
        });
        return true;
    }

    // ทดสอบใน Play Mode ไม่ต้องเล่นจนจบด่าน: คลิกขวาที่คอมโพเนนต์นี้ใน Inspector
    [ContextMenu("ทดสอบ: เปิดหน้าต่างเลือกพรตอนนี้")]
    void OfferNowForTesting()
    {
        if (!Application.isPlaying || IsChoosing) return;
        if (!OfferChoice(null)) Debug.Log($"ไม่มีพรให้เลือกแล้ว (มี {owned.Count}/{maxBlessings})");
    }

    static void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        PauseManager.isGamePaused = paused;
    }

    // ---------- ห้อง ----------

    void OnCombatStarted(RoomController room)
    {
        // ข้อจำกัดต่อห้อง: เกราะฉุกเฉินใช้ได้ใหม่ เก็บเกี่ยวพลังงานนับใหม่
        shieldUsedThisRoom = false;
        harvestedThisRoom = 0;
    }

    // ชีพจรฟื้นฟู: เคลียร์ห้องต่อสู้แล้วฟื้นเลือดตามสัดส่วนเลือดสูงสุด
    void OnCombatCleared(RoomController room)
    {
        var pulse = Get(BlessingType.HealingPulse);
        if (pulse == null || !FindPlayer() || player.isDead) return;
        player.Heal(player.maxHP * pulse.amount);
        Vector2 center = SkillCombat.BodyCenter(player.gameObject);
        ImpactSparks.Spawn(center, HealColor, 14, Vector2.zero, 3.5f);
        StartCoroutine(Pulse(center, HealColor, 2.2f, 0.45f));
        Ping(pulse);
    }

    // ---------- มอนสเตอร์ตาย ----------

    // เก็บเกี่ยวพลังงาน: กำจัดศัตรูแล้วได้พลังงาน นับเฉพาะที่ฟื้นได้จริง ไม่เกินเพดานต่อห้อง
    void OnMonsterDied(MonsterController monster)
    {
        var harvest = Get(BlessingType.EnergyHarvest);
        if (harvest == null || !FindPlayer() || player.isDead) return;
        int room = harvest.perRoomCap > 0 ? harvest.perRoomCap - harvestedThisRoom : int.MaxValue;
        int gain = Mathf.Min(Mathf.Max(0, harvest.count), room);
        if (gain <= 0) return;

        int before = player.currentEnergy;
        player.RestoreEnergy(gain);
        int restored = player.currentEnergy - before;
        if (restored <= 0) return; // พลังงานเต็มอยู่แล้ว ไม่นับ
        harvestedThisRoom += restored;

        if (monster != null) ImpactSparks.Spawn(monster.transform.position, HarvestColor, 6, Vector2.zero, 3f);
        ImpactSparks.Spawn(SkillCombat.BodyCenter(player.gameObject), HarvestColor, 4, Vector2.up, 2.5f, 40f);
        Ping(harvest);
    }

    // ---------- อาวุธ ----------

    // พลังงานสำรอง: การโจมตีที่เสียพลังงานครั้งที่ count ไม่เสียพลังงาน (โจมตีฟรีอยู่แล้วไม่นับ สกิลไม่ผ่านตรงนี้)
    public static bool PayWeaponEnergy(PlayerStats owner, int cost)
    {
        if (owner == null || cost <= 0) return true;
        var m = Instance;
        var reserve = m != null ? m.Get(BlessingType.EnergyReserve) : null;
        if (reserve == null) return owner.TrySpendEnergy(cost);

        if (m.ReserveReady(reserve))
        {
            m.reserveCount = 0;
            ImpactSparks.Spawn(SkillCombat.BodyCenter(owner.gameObject), ReserveColor, 6, Vector2.up, 3f, 50f);
            m.Ping(reserve);
            return true;
        }
        if (!owner.TrySpendEnergy(cost)) return false;
        m.reserveCount++;
        return true;
    }

    // เช็คก่อนง้าง (ธนู/หอก/ค้อนทอง): ครั้งถัดไปฟรีก็ง้างได้แม้พลังงานไม่พอ
    public static bool HasWeaponEnergy(PlayerStats owner, int cost)
    {
        if (owner == null || cost <= 0) return true;
        var m = Instance;
        var reserve = m != null ? m.Get(BlessingType.EnergyReserve) : null;
        return (reserve != null && m.ReserveReady(reserve)) || owner.HasEnergy(cost);
    }

    bool ReserveReady(BlessingData reserve) => reserveCount >= Mathf.Max(2, reserve.count) - 1;

    // คลื่นสะสม: การโจมตีหนึ่งครั้ง (ฟันหนึ่งที / ยิงหนึ่งนัดรวมทุกลูก) โดนศัตรูกี่ตัวก็นับครั้งเดียว ครบแล้วปล่อยคลื่น
    public static void OnAttackLanded(object attack)
    {
        var m = Instance;
        if (m == null || attack == null) return;
        var wave = m.Get(BlessingType.ChargedWave);
        if (wave == null || m.countedAttacks.Contains(attack)) return;

        m.countedAttacks.Enqueue(attack);
        while (m.countedAttacks.Count > 24) m.countedAttacks.Dequeue();

        if (++m.waveHits < Mathf.Max(1, wave.count)) return;
        m.waveHits = 0;
        m.ReleaseWave(wave);
    }

    void ReleaseWave(BlessingData wave)
    {
        if (!FindPlayer() || player.weaponController == null) return;
        var weapon = player.weaponController;
        var data = weapon.currentWeaponData;
        if (data == null) return;

        Vector2 direction = weapon.transform.right;
        Vector2 origin = (Vector2)weapon.transform.position + direction * 0.6f;
        float damage = data.attackDamage * wave.amount;
        float angle = SkillCombat.Angle(direction);

        var frames = wave.waveFrames;
        bool hasArt = frames != null && frames.Length > 0;
        Sprite[] fly = hasArt ? new[] { frames[Mathf.Min(1, frames.Length - 1)] } : new[] { ProceduralSprites.DashedRing };
        Sprite[] fade = hasArt && frames.Length >= 3 ? new[] { frames[2] } : null;
        if (hasArt) SkillVfx.Spawn(new[] { frames[0] }, origin, wave.waveScale * 1.2f, angle, 12f);

        float speed = Mathf.Max(0.1f, wave.waveSpeed);
        PiercingProjectile.Spawn(origin, direction, speed, wave.waveRange / speed, damage, wave.radius,
                                 fly, fade, wave.waveScale, default, true)
                          .WithTrail(0.04f, 0.18f)
                          .WithTint(wave.waveTint);
        CameraFollow.Shake(0.08f, 0.12f);
        Ping(wave);
    }

    // คมสลายมิติ: ท่าประชิดกวาดโดนกระสุนศัตรูในวงโจมตีแล้วกระสุนหาย ไม่เกิน count ลูกต่อการฟันหนึ่งครั้ง
    public static void CleaveBullets(Vector2 center, float radius, HashSet<Component> attack)
    {
        var cleave = Active(BlessingType.BulletCleave);
        if (cleave == null) return;
        int left = Mathf.Max(1, cleave.count) - EnemyBullets.CountIn(attack);
        if (left > 0 && EnemyBullets.CleaveAround(center, radius * 1.15f, left, attack) > 0) Instance.Ping(cleave);
    }

    // กระสุนทะลุมิติ: กระสุน/ลูกธนูทะลุศัตรูเพิ่ม (ไม่ใช้กับจรวด) + จำว่ามาจากการโจมตีครั้งไหน (คลื่นสะสม)
    public static void SetupProjectile(PlayerProjectile projectile, WeaponData data, object attack)
    {
        if (projectile == null) return;
        projectile.SetAttack(attack);
        var pierce = Active(BlessingType.PhasePiercing);
        if (pierce == null || (data != null && data.special == WeaponSpecial.Explosive)) return;
        projectile.SetPierce(Mathf.Max(1, pierce.count), pierce.amount > 0f ? pierce.amount : 0.6f);
    }

    // ---------- ผู้เล่นโดนตี ----------

    // เกราะฉุกเฉินรับดาเมจไว้ก่อน คืนดาเมจที่เหลือไปถึงเลือด
    public static float AbsorbDamage(PlayerStats target, float damage)
    {
        var m = Instance;
        if (m == null || m.shieldLeft <= 0f || damage <= 0f) return damage;
        if (Time.time >= m.shieldUntil)
        {
            m.EndShield(false);
            return damage;
        }

        float absorbed = Mathf.Min(m.shieldLeft, damage);
        m.shieldLeft -= absorbed;
        ImpactSparks.Spawn(SkillCombat.BodyCenter(target.gameObject), ShieldColor, 8, Vector2.zero, 4f);
        if (m.shieldLeft <= 0.001f) m.EndShield(true);
        return damage - absorbed;
    }

    // เรียกหลังเลือดลดจริง (ยังไม่ตาย)
    public static void OnPlayerHurt(PlayerStats target)
    {
        var m = Instance;
        if (m == null || target == null || target.isDead) return;
        m.FindPlayer();

        // เกราะฉุกเฉิน: เลือดลดถึงเกณฑ์ ได้เกราะตามสัดส่วนเลือดสูงสุด ห้องละครั้ง
        var shield = m.Get(BlessingType.EmergencyShield);
        if (shield != null && !m.shieldUsedThisRoom && target.currentHP <= target.maxHP * shield.threshold)
        {
            m.shieldUsedThisRoom = true;
            m.shieldLeft = target.maxHP * shield.amount;
            m.shieldUntil = Time.time + shield.duration;
            ImpactSparks.Spawn(SkillCombat.BodyCenter(target.gameObject), ShieldColor, 14, Vector2.zero, 4f);
            CameraFollow.Shake(0.08f, 0.15f);
            m.Ping(shield);
        }

        // ก้าวพ้นรอยแยก: หลังรับดาเมจวิ่งเร็วขึ้นชั่วครู่ มีคูลดาวน์
        var step = m.Get(BlessingType.RiftStep);
        if (step != null && Time.time >= m.riftStepReadyAt && m.mover != null)
        {
            m.riftStepReadyAt = Time.time + step.cooldown;
            m.riftStepUntil = Time.time + step.duration;
            m.mover.BlessingSpeedBoost(1f + step.amount, step.duration);
            ImpactSparks.Spawn(target.transform.position, RiftColor, 10, Vector2.zero, 3.5f);
            m.Ping(step);
        }
    }

    void EndShield(bool broken)
    {
        if (shieldLeft > 0f || broken)
        {
            if (FindPlayer())
                ImpactSparks.Spawn(SkillCombat.BodyCenter(player.gameObject), ShieldColor, broken ? 16 : 8, Vector2.zero, broken ? 5f : 2.5f);
            if (broken) CameraFollow.Shake(0.06f, 0.1f);
        }
        shieldLeft = 0f;
        shieldUntil = 0f;
    }

    // ---------- ทุกเฟรม ----------

    void Update()
    {
        bool alive = FindPlayer() && !player.isDead && player.gameObject.activeInHierarchy;
        Vector2 center = alive ? SkillCombat.BodyCenter(player.gameObject) : Vector2.zero;

        // สนามชะลอกระสุน
        var slow = alive ? Get(BlessingType.BulletSlowField) : null;
        EnemyBullets.SetSlowField(slow != null, center, slow != null ? slow.radius : 0f,
                                  slow != null ? Mathf.Clamp01(1f - slow.amount) : 1f);
        UpdateField(slow, center);

        // เกราะหมดเวลา
        if (shieldLeft > 0f && Time.time >= shieldUntil) EndShield(false);
        UpdateBubble(alive && shieldLeft > 0f, center);

        // ก้าวพ้นรอยแยก: เงาภาพค้างตามตัวระหว่างวิ่งเร็ว
        if (alive && Time.time < riftStepUntil && playerView != null && Time.time >= nextGhost)
        {
            nextGhost = Time.time + 0.07f;
            SpriteGhost.Spawn(playerView, 0.28f, 0.45f, RiftColor);
        }

        if (hud != null) hud.UpdateStates(this);
    }

    // สถานะบนไอคอน HUD: ตัวเลขมุมไอคอน, สัดส่วนที่มืด (คูลดาวน์/ใช้แล้ว), เรืองแสง (กำลังทำงาน/พร้อม)
    public void Describe(BlessingData blessing, out string badge, out float dim, out bool glow)
    {
        badge = "";
        dim = 0f;
        glow = false;
        if (blessing == null) return;
        float now = Time.time;
        switch (blessing.type)
        {
            case BlessingType.ChargedWave:
                badge = $"{waveHits}/{Mathf.Max(1, blessing.count)}";
                glow = waveHits >= Mathf.Max(1, blessing.count) - 1;
                break;
            case BlessingType.EmergencyShield:
                if (shieldLeft > 0f) { glow = true; badge = Mathf.CeilToInt(shieldUntil - now).ToString(); }
                else if (shieldUsedThisRoom) dim = 1f;
                break;
            case BlessingType.RiftStep:
                if (now < riftStepUntil) glow = true;
                else if (now < riftStepReadyAt)
                {
                    dim = Mathf.Clamp01((riftStepReadyAt - now) / Mathf.Max(0.01f, blessing.cooldown));
                    badge = Mathf.CeilToInt(riftStepReadyAt - now).ToString();
                }
                break;
            case BlessingType.BulletSlowField:
                glow = EnemyBullets.AnyInSlowField;
                break;
            case BlessingType.EnergyHarvest:
                if (blessing.perRoomCap > 0)
                {
                    badge = $"{harvestedThisRoom}/{blessing.perRoomCap}";
                    if (harvestedThisRoom >= blessing.perRoomCap) dim = 0.6f;
                }
                break;
            case BlessingType.EnergyReserve:
                bool ready = ReserveReady(blessing);
                badge = ready ? (LanguageSettings.IsThai ? "ฟรี" : "FREE") : $"{reserveCount}/{Mathf.Max(2, blessing.count) - 1}";
                glow = ready;
                break;
        }
    }

    void Ping(BlessingData blessing)
    {
        if (hud != null) hud.Ping(owned.IndexOf(blessing));
    }

    // ---------- ภาพ ----------

    // วงบาง ๆ รอบตัว = ขอบเขตสนาม สว่างขึ้นเมื่อมีกระสุนอยู่ในวง
    void UpdateField(BlessingData slow, Vector2 center)
    {
        if (slow == null)
        {
            if (field != null) field.gameObject.SetActive(false);
            return;
        }
        if (field == null)
        {
            field = new GameObject("BulletSlowField").transform;
            fieldGlow = Layer("Glow", field, ProceduralSprites.Glow, "bg2", 2);
            fieldSpin = new GameObject("Spin").transform;
            fieldSpin.SetParent(field, false);
            fieldRing = Layer("Ring", fieldSpin, ProceduralSprites.DashedRing, "bg2", 3);
        }
        field.gameObject.SetActive(true);
        field.position = center;
        field.localScale = Vector3.one * slow.radius * 2f;
        bool busy = EnemyBullets.AnyInSlowField;
        fieldSpin.Rotate(0f, 0f, (busy ? 70f : 18f) * Time.deltaTime);
        fieldRing.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, busy ? 0.45f : 0.16f);
        fieldGlow.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, busy ? 0.16f : 0.05f);
    }

    // ฟองเกราะครอบตัวผู้เล่น วาดทับตัว (ชั้น Effect) แบบโปร่ง กะพริบช่วงใกล้หมด
    void UpdateBubble(bool on, Vector2 center)
    {
        if (!on)
        {
            if (bubble != null) bubble.gameObject.SetActive(false);
            return;
        }
        if (bubble == null)
        {
            bubble = new GameObject("EmergencyShield").transform;
            bubbleGlow = Layer("Glow", bubble, ProceduralSprites.Glow, "Effect", 1);
            bubbleRing = Layer("Ring", bubble, ProceduralSprites.ThinRing, "Effect", 2);
        }
        bubble.gameObject.SetActive(true);
        bubble.position = center;
        float left = shieldUntil - Time.time;
        float blink = left < 1f ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 18f)) : 1f;
        float breath = 1f + 0.04f * Mathf.Sin(Time.time * 6f);
        bubble.localScale = Vector3.one * 1.9f * breath;
        bubbleRing.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, 0.85f * blink);
        bubbleGlow.color = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, 0.28f * blink);
    }

    static SpriteRenderer Layer(string name, Transform parent, Sprite sprite, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    // วงแสงขยายออกจากตัวแล้วจาง (ชีพจรฟื้นฟู)
    System.Collections.IEnumerator Pulse(Vector2 center, Color color, float size, float time)
    {
        var go = new GameObject("BlessingPulse");
        go.transform.position = center;
        var ring = Layer("Ring", go.transform, ProceduralSprites.ThinRing, "Effect", 2);
        var glow = Layer("Glow", go.transform, ProceduralSprites.Glow, "Effect", 1);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, size, 1f - (1f - k) * (1f - k));
            ring.color = new Color(color.r, color.g, color.b, 0.9f * (1f - k));
            glow.color = new Color(color.r, color.g, color.b, 0.35f * (1f - k));
            yield return null;
        }
        Destroy(go);
    }
}
