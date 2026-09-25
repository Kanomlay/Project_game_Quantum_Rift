using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ถืออาวุธ เล็งตามเมาส์ และเล่นท่าตีตามชนิดอาวุธ
// ท่าตีทำด้วยการหมุน/เลื่อน/ยืดภาพอาวุธ (ไม่มี sprite ท่าตี) ค่าจังหวะและความรู้สึกตอนโดนอยู่ใน AttackMotion ของอาวุธ:
// - Swing (ดาบ มีดสั้น กระบอง): ง้าง → กวาดโค้งสลับบน/ล่างตามคอมโบ → เลยปลายวงนิดแล้วค่อยกลับ
// - Smash (ค้อน): ยกขึ้นเหนือหัว → ทุบลงเร่งแรง → หัวค้อนแบนตอนกระแทก ค้างที่พื้น → คืนท่า
// - Thrust (หอก): ดึงกลับ → แทงพุ่ง → ค้าง → ดึงกลับ
// - Strike (กรงเล็บ/มีดคู่): ตะปบสลับมือ (DualClawWeapon)
// - Shoot (ปืน/ธนู): เล่นเฟรมยิง + อาวุธถีบกลับในมือ
// ตีโดน: หยุดภาพชั่วขณะ (HitStop) กล้องสั่น ประกายกระเด็น และตัวละครก้าวตามแรงตี
public class WeaponController : MonoBehaviour
{
    [Header("ข้อมูลอาวุธปัจจุบัน")]
    public WeaponData currentWeaponData;
    private GameObject currentWeaponObject;
    private Animator currentWeaponAnim;
    [Header("ตั้งค่า Hitbox (การโจมตี)")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("ตั้งค่าท่าฟันดาบ")]
    public float restReturnDuration = 0.15f; // เวลาที่ดาบใช้ค่อยๆ กลับไปท่าพักหลังคอมโบขาด
    [Range(0f, 1.5f)]
    public float slashEffectDistance = 1f; // ตำแหน่งคลื่นดาบ คิดเป็นสัดส่วนของระยะมือถึงปลายดาบ (1 = ตรงปลายดาบพอดี)
    public float slashEffectScale = 3f;    // ตัวคูณขนาดคลื่นดาบ (ขนาดที่พอดีกับอาวุธยาวเท่า slashReferenceBladeLength)
    // คลื่นดาบย่อ/ขยายตามความยาวอาวุธ (มือถึง AttackPoint) อาวุธสั้นจะได้คลื่นเล็กลงเอง
    // 1.8 = ความยาวดาบสนิมภาพเก่าที่ slashEffectScale ถูกปรับไว้ให้พอดี
    public float slashReferenceBladeLength = 1.8f;

    [Header("หอกไอออน-X ง้างค้าง")]
    [Range(0f, 0.5f)] public float spearChargePullBack = 0.2f; // ถอยหอกกลับเท่านี้ (สัดส่วนความยาว)
    public Color spearChargedTint = new Color(0.55f, 0.95f, 1f); // ง้างครบเวลาแล้ว หอกเรืองแสง

    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool nextSwingDownward = true; // true = บนลงล่าง, false = ล่างขึ้นบน (สลับกันทุกครั้งที่คอมโบต่อติด)
    private float lastAttackTime = -999f;
    private float currentSwingAngle; // มุมอาวุธปัจจุบันเทียบแนวเล็ง (0 = ปลายชี้ตรงเมาส์, + = เชิดขึ้น, - = กดลง)
    private bool bladeEdgeUp;        // ฟันขึ้นต้องพลิกดาบเอาคมขึ้น

    private float AttackInterval => 1f / Mathf.Max(0.01f, currentWeaponData.attackSpeed);
    private float ComboWindow => AttackInterval * 1.5f;
    private const float RestAngle = 0f; // ท่าพัก = ปลายดาบชี้ตรงไปทางเมาส์

    // WeaponHolder พลิกแกน Y ตอนเล็งไปทางซ้าย ทั้งมุมสวิงและด้านคมดาบต้องกลับเครื่องหมายตาม
    // ไม่งั้นทิศฟันกับด้านคมจะสลับกันเวลาหันซ้าย
    private float AimMirror => (transform.localScale.y < 0f) ? -1f : 1f;

    // ตัวละครแต่ละตัวถูกย่อ/ขยายไม่เท่ากัน WeaponHolder จึงต้องมี scale ชดเชยของตัวเองไว้ให้ดาบขนาดเท่าเดิม
    // ต้องจำไว้ตั้งแต่แรก ไม่งั้นตอนพลิกซ้าย/ขวาจะถูกรีเซ็ตกลับเป็น 1
    private Vector3 baseScale = Vector3.one;
    private Vector3 weaponBaseScale = Vector3.one; // ขนาดของ prefab อาวุธ (ก่อนยืด/หดตอนฟาด)
    private Vector2 weaponStretch = Vector2.one;   // x = ตามแนวอาวุธ, y = ขวางแนวอาวุธ

    private PlayerStats owner;        // เจ้าของอาวุธ ใช้หักพลังงานตอนโจมตี
    private PlayerMovement mover;     // ก้าวตามแรงตี
    private Coroutine rangedRoutine;  // ท่ายิงที่กำลังเล่นอยู่ ต้องหยุดถ้าสลับอาวุธกลางท่า
    private Coroutine meleeRoutine;   // ท่าฟัน/ทุบ
    private Coroutine recoilRoutine;
    private DualClawWeapon currentClaws; // มีเมื่อถือกรงเล็บคู่ ใช้ท่าตะปบสลับมือแทนท่าฟันดาบ
    private Coroutine clawRoutine;
    private WeaponSpriteAnimator chargedFrames; // ไม่ใช่ null = กำลังง้างธนูค้างไว้
    private Coroutine spearRoutine;
    private float spearChargeStart = -1f;       // >= 0 = กำลังง้างหอกไอออน-X ค้างไว้
    private SpriteRenderer[] weaponRenderers;
    private int comboSwings;                    // ฟันต่อเนื่องในคอมโบนี้ไปกี่ครั้งแล้ว (ดาบผ่ามิติปล่อยคลื่นครั้งที่ 3)

    // พลังงานไม่พอแล้วยังกดค้าง ให้รอสักพักก่อนลองใหม่ ไม่งั้นแถบพลังงานจะกะพริบทุกเฟรม
    private const float OutOfEnergyRetryDelay = 0.3f;

    void Awake()
    {
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), transform.localScale.z);
        owner = GetComponentInParent<PlayerStats>();
        mover = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (PauseManager.isGamePaused) return;

        if (!isAttacking)
        {
            AimTowardsMouse();
            ReturnToRestPose();
        }

        // ธนู/หอกไอออน-X ง้างค้างอยู่ ปล่อยเมาส์เมื่อไหร่ค่อยยิง/แทง
        if (chargedFrames != null && !Input.GetMouseButton(0))
        {
            ReleaseChargedShot();
        }
        else if (spearChargeStart >= 0f && !Input.GetMouseButton(0))
        {
            ReleaseSpearCharge();
        }
        else if (Input.GetMouseButton(0))
        {
            AttemptAttack();
        }

        if (spearChargeStart >= 0f) ShowSpearCharge();
        ApplySwingRotation();
    }

    void AimTowardsMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 aimDirection = (mousePos - transform.position).normalized;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        transform.eulerAngles = new Vector3(0, 0, angle);

        Vector3 localScale = baseScale;
        if (angle > 90 || angle < -90)
        {
            localScale.y = -baseScale.y;
        }
        transform.localScale = localScale;
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        currentWeaponData = newWeaponData;

        StopRoutine(ref rangedRoutine);
        StopRoutine(ref clawRoutine);
        StopRoutine(ref spearRoutine);
        StopRoutine(ref meleeRoutine);
        StopRoutine(ref recoilRoutine);
        currentClaws = null;
        chargedFrames = null; // สลับอาวุธระหว่างง้าง = ยกเลิก ไม่ยิงและไม่เสียพลังงาน
        spearChargeStart = -1f;
        isAttacking = false;
        comboSwings = 0;
        weaponStretch = Vector2.one;

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        if (currentWeaponData != null && currentWeaponData.weaponPrefab != null)
        {

            currentWeaponObject = Instantiate(currentWeaponData.weaponPrefab, transform.position, transform.rotation, transform);
            currentWeaponAnim = currentWeaponObject.GetComponent<Animator>();
            Vector3 prefabScale = currentWeaponObject.transform.localScale;
            weaponBaseScale = new Vector3(Mathf.Abs(prefabScale.x), Mathf.Abs(prefabScale.y), prefabScale.z);

            Transform spawnAttackPoint = currentWeaponObject.transform.Find("AttackPoint");
            if (spawnAttackPoint != null)
            {
                attackPoint = spawnAttackPoint;
            }
            else
            {
                Debug.LogWarning("ระวัง! อาวุธ " + currentWeaponData.weaponName + " ยังไม่มี AttackPoint ใน Prefab นะ!");
            }

            // ต้องรู้ก่อน ApplySwingRotation ว่าเป็นกรงเล็บคู่ จะได้ไม่พลิกแบบดาบ
            currentClaws = currentWeaponObject.GetComponent<DualClawWeapon>();
            weaponRenderers = currentWeaponObject.GetComponentsInChildren<SpriteRenderer>(true);

            // ท่าพักให้ปลายดาบชี้ตรงแนวเล็ง รอสวิงแรกที่เป็นบนลงล่าง
            currentSwingAngle = RestAngle;
            nextSwingDownward = true;
            SetBladeEdgeUp(false);
            ApplySwingRotation();

            var frames = currentWeaponObject.GetComponent<WeaponSpriteAnimator>();
            if (frames != null) frames.ShowIdle();
        }
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null) return;
        StopCoroutine(routine);
        routine = null;
    }

   void AttemptAttack()
    {
        if (currentWeaponData == null || Time.time < nextAttackTime) return;

        bool ranged = currentWeaponData.IsRanged;
        if (!ranged && !IsMelee(currentWeaponData.weaponType)) return;

        // ธนู: กดค้าง = ง้างไว้ ยังไม่หักพลังงาน (หักตอนปล่อย) แค่เช็คว่าพอให้ง้างไหม
        if (ranged && chargedFrames == null)
        {
            var frames = currentWeaponObject != null ? currentWeaponObject.GetComponent<WeaponSpriteAnimator>() : null;
            if (frames != null && frames.holdToCharge)
            {
                if (owner != null && !owner.HasEnergy(currentWeaponData.energyCost))
                {
                    nextAttackTime = Time.time + OutOfEnergyRetryDelay;
                    return;
                }
                StopRoutine(ref rangedRoutine);
                if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");
                frames.ShowCharge();
                chargedFrames = frames;
                return;
            }
        }
        if (chargedFrames != null) return; // ง้างค้างอยู่ รอปล่อยเมาส์

        // หอกไอออน-X: กดค้าง = ง้าง (หักพลังงานตอนปล่อย) ปล่อยก่อนครบเวลา = แทงธรรมดา
        if (IsSpear && currentWeaponData.special == WeaponSpecial.ChargeWave)
        {
            if (spearChargeStart >= 0f || spearRoutine != null) return;
            if (owner != null && !owner.HasEnergy(currentWeaponData.energyCost))
            {
                nextAttackTime = Time.time + OutOfEnergyRetryDelay;
                return;
            }
            spearChargeStart = Time.time;
            return;
        }

        // 1.3.2 ค่าพลังงาน: พลังงานไม่พอ ใช้อาวุธนั้นไม่ได้ชั่วคราว (อาวุธที่ใช้ 0 ผ่านตลอด)
        if (owner != null && !owner.TrySpendEnergy(currentWeaponData.energyCost))
        {
            nextAttackTime = Time.time + OutOfEnergyRetryDelay;
            return;
        }

        if (ranged)
        {
            FireRanged();
            nextAttackTime = Time.time + AttackInterval;
            return;
        }

        if (IsSpear)
        {
            StartThrust(false);
            nextAttackTime = Time.time + AttackInterval;
            return;
        }

        if (currentClaws != null)
        {
            // กดรัวจนท่าก่อนหน้ายังหดกลับไม่สุด ให้ดึงกรงเล็บกลับที่แล้วตีข้างใหม่เลย
            if (clawRoutine != null) StopCoroutine(clawRoutine);
            clawRoutine = StartCoroutine(ClawStrikeRoutine(currentClaws));
            nextAttackTime = Time.time + AttackInterval;
            return;
        }

        // ถ้าทิ้งช่วงนานเกินคอมโบ (สวิงก่อนหน้าเล่นจบไปนานแล้ว) ให้เริ่มใหม่ด้วยท่าบนลงล่างเสมอ
        if (Time.time - lastAttackTime > ComboWindow)
        {
            nextSwingDownward = true;
            comboSwings = 0;
        }
        lastAttackTime = Time.time;
        comboSwings++;
        bool releaseWave = currentWeaponData.special == WeaponSpecial.ComboWave &&
                           comboSwings % Mathf.Max(1, currentWeaponData.comboCount) == 0;

        if (currentWeaponAnim != null)
        {
            currentWeaponAnim.SetTrigger("Attack");
        }

        StopRoutine(ref meleeRoutine);
        if (currentWeaponData.Motion.style == AttackMotion.Style.Smash)
        {
            meleeRoutine = StartCoroutine(SmashRoutine(releaseWave));
        }
        else
        {
            meleeRoutine = StartCoroutine(SwingRoutine(nextSwingDownward, releaseWave));
            nextSwingDownward = !nextSwingDownward;
        }

        nextAttackTime = Time.time + AttackInterval;
    }

    // ดาบ/กระบอง/ค้อน ฟันหรือทุบ, กรงเล็บ/มีดคู่ตะปบสลับมือ, หอกแทงตรง
    private static bool IsMelee(WeaponType type) =>
        type == WeaponType.Sword || type == WeaponType.Claw || type == WeaponType.Spear || type == WeaponType.Hammer;
    private bool IsSpear => currentWeaponData != null && currentWeaponData.weaponType == WeaponType.Spear;

    // ความยาวอาวุธ (มือถึง AttackPoint) ในพิกัดของ WeaponHolder
    private float WeaponLength => attackPoint != null ? transform.InverseTransformPoint(attackPoint.position).magnitude : 1f;

    // ---------- จังหวะท่า ----------

    // เวลาท่ารวมต้องไม่เกินช่วงห่างการโจมตี ไม่งั้นท่าถัดไปจะเริ่มทับ (อาวุธตีเร็ว ๆ ท่าจะสั้นลงตามสัดส่วน)
    private float FitTime(AttackMotion m, bool withRecover)
    {
        float total = m.windupTime + m.strikeTime + m.holdTime + (withRecover ? m.recoverTime : 0f);
        float budget = AttackInterval * 0.9f;
        return total > budget && total > 0f ? budget / total : 1f;
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInOut(float t) => t * t * (3f - 2f * t);

    // ฟาด: ยืดภาพตามแนวอาวุธ แรงสุดกลางท่า
    private void StretchDuringStrike(AttackMotion m, float progress)
    {
        float s = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * m.stretch;
        weaponStretch = new Vector2(1f + s, 1f - s * 0.5f);
    }

    private void Step(AttackMotion m, float duration)
    {
        if (mover != null) mover.AttackStep(transform.right, m.stepDistance, Mathf.Max(0.05f, duration));
    }

    // เงาภาพค้างตามทางที่อาวุธกวาดผ่าน
    private void Trail(AttackMotion m, ref float nextGhost)
    {
        if (m.trailInterval <= 0f || weaponRenderers == null || Time.time < nextGhost) return;
        nextGhost = Time.time + m.trailInterval;
        foreach (var renderer in weaponRenderers)
            if (renderer != null && renderer.enabled) SpriteGhost.Spawn(renderer, m.trailLife, m.trailAlpha);
    }

    // ---------- ฟัน (ดาบ มีดสั้น กระบอง) ----------

    // หมุนอาวุธกวาดผ่านมุม attackAngle รอบทิศที่เล็งอยู่ (บนลงล่าง / ล่างขึ้นบน สลับตามคอมโบ)
    // เริ่มต่อจากมุมที่ค้างอยู่จากท่าก่อนหน้า: ง้างเลยจุดเริ่มวง → ฟาดตามกราฟ → เลยปลายวงแล้วกลับมาค้างที่ปลายวง
    private IEnumerator SwingRoutine(bool downward, bool releaseWave)
    {
        WeaponData swungWith = currentWeaponData;
        AttackMotion m = swungWith.Motion;
        float fit = FitTime(m, false);
        isAttacking = true;
        var hitEnemies = new HashSet<Component>();
        SetBladeEdgeUp(!downward); // ฟันขึ้นต้องพลิกดาบเอาคมขึ้นด้วย

        float side = downward ? 1f : -1f; // บนลงล่าง: เริ่มฝั่ง + จบฝั่ง -
        float half = swungWith.attackAngle / 2f;
        float from = currentSwingAngle;
        float wound = side * (half + m.windupAngle);
        float through = -side * (half + m.followThrough);
        float settle = -side * half;

        float windup = m.windupTime * fit;
        for (float t = 0f; t < windup; t += Time.deltaTime)
        {
            currentSwingAngle = Mathf.Lerp(from, wound, EaseOut(t / windup));
            yield return null;
        }

        SpawnSlashEffect(downward);
        float strike = Mathf.Max(0.01f, m.strikeTime * fit);
        Step(m, strike);
        float nextGhost = 0f;
        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            float p = t / strike;
            currentSwingAngle = Mathf.LerpUnclamped(wound, through, m.strikeCurve.Evaluate(p));
            StretchDuringStrike(m, p);
            if (p >= m.hitFrom) CheckSwingHit(hitEnemies, m);
            Trail(m, ref nextGhost);
            yield return null;
        }
        currentSwingAngle = through;
        weaponStretch = Vector2.one;
        CheckSwingHit(hitEnemies, m);
        AfterImpact(swungWith, releaseWave);

        float hold = m.holdTime * fit;
        for (float t = 0f; t < hold; t += Time.deltaTime)
        {
            currentSwingAngle = Mathf.Lerp(through, settle, EaseOut(t / hold));
            yield return null;
        }
        currentSwingAngle = settle;
        isAttacking = false;
        meleeRoutine = null;
    }

    // ---------- ทุบ (ค้อน) ----------

    // ยกค้อนขึ้นถึง windupAngle → ทุบลงเร่งแรงถึง smashEndAngle (โดนเฉพาะช่วงท้าย) → กระแทก: กล้องสั่น ฝุ่นกระจาย
    // หัวค้อนแบนชั่วขณะ ค้างที่พื้น แล้วยกกลับท่าพัก ทุบทิศเดียวกันทุกครั้ง ไม่สลับบน/ล่างแบบดาบ
    private IEnumerator SmashRoutine(bool releaseWave)
    {
        WeaponData swungWith = currentWeaponData;
        AttackMotion m = swungWith.Motion;
        float fit = FitTime(m, true);
        isAttacking = true;
        var hitEnemies = new HashSet<Component>();
        SetBladeEdgeUp(false);

        float from = currentSwingAngle;
        float raised = m.windupAngle;
        float end = m.smashEndAngle;

        float windup = m.windupTime * fit;
        for (float t = 0f; t < windup; t += Time.deltaTime)
        {
            currentSwingAngle = Mathf.Lerp(from, raised, EaseOut(t / windup));
            yield return null;
        }

        float strike = Mathf.Max(0.01f, m.strikeTime * fit);
        Step(m, strike);
        float nextGhost = 0f;
        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            float p = t / strike;
            currentSwingAngle = Mathf.LerpUnclamped(raised, end, m.strikeCurve.Evaluate(p));
            StretchDuringStrike(m, p);
            if (p >= m.hitFrom) CheckSwingHit(hitEnemies, m);
            Trail(m, ref nextGhost);
            yield return null;
        }
        currentSwingAngle = end;
        CheckSwingHit(hitEnemies, m);

        // กระแทกพื้น: สั่นทุกครั้งแม้ไม่โดนใคร ฝุ่นพุ่งขึ้นรอบหัวค้อน
        CameraFollow.Shake(m.impactShake, 0.2f);
        if (attackPoint != null)
            ImpactSparks.Spawn(attackPoint.position, m.sparkColor, m.sparkCount, Vector2.zero, 4f);
        AfterImpact(swungWith, releaseWave);

        float hold = m.holdTime * fit;
        for (float t = 0f; t < hold; t += Time.deltaTime)
        {
            float squash = (1f - t / hold) * m.stretch;
            weaponStretch = new Vector2(1f - squash * 0.5f, 1f + squash * 0.6f);
            yield return null;
        }
        weaponStretch = Vector2.one;

        float recover = m.recoverTime * fit;
        for (float t = 0f; t < recover; t += Time.deltaTime)
        {
            currentSwingAngle = Mathf.Lerp(end, RestAngle, EaseInOut(t / recover));
            yield return null;
        }
        currentSwingAngle = RestAngle;
        isAttacking = false;
        meleeRoutine = null;
    }

    // ความสามารถตำนานหลังฟาด/ทุบถึงปลายท่า: ดาบผ่ามิติปล่อยคลื่นครั้งที่ 3, ค้อนควอนตัมเกิดวงพลังตรงหัวค้อน
    private void AfterImpact(WeaponData swungWith, bool releaseWave)
    {
        if (swungWith != currentWeaponData) return; // สลับอาวุธกลางท่า
        if (releaseWave) ReleaseWave(swungWith);
        if (swungWith.special == WeaponSpecial.GroundPulse && attackPoint != null)
            StartCoroutine(GroundPulseRoutine(attackPoint.position, swungWith));
    }

    // ---------- แทง (หอก) ----------

    private void StartThrust(bool releaseWave)
    {
        StopRoutine(ref spearRoutine);
        if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");
        spearRoutine = StartCoroutine(SpearThrustRoutine(releaseWave));
    }

    // ดึงหอกกลับ → แทงพุ่งตามแนวเล็ง (ตรวจโดนที่ปลายหอกตลอดทาง) → ค้าง → ดึงกลับ (ล็อกทิศระหว่างแทง)
    private IEnumerator SpearThrustRoutine(bool releaseWave)
    {
        WeaponData used = currentWeaponData;
        AttackMotion m = used.Motion;
        float fit = FitTime(m, true);
        isAttacking = true;
        var hitEnemies = new HashSet<Component>();
        Transform weapon = currentWeaponObject.transform;
        float length = WeaponLength;
        float from = weapon.localPosition.x;
        float back = -length * m.pullBack;
        float reach = length * m.thrustReach;

        // หอกไอออน-X ที่ง้างค้างมาแล้วถอยไกลกว่าท่าง้างปกติ ข้ามช่วงนี้ไปแทงเลย
        if (from > back)
        {
            float windup = m.windupTime * fit;
            for (float t = 0f; t < windup; t += Time.deltaTime)
            {
                weapon.localPosition = new Vector3(Mathf.Lerp(from, back, EaseOut(t / windup)), 0f, 0f);
                yield return null;
            }
        }
        else back = from;

        float strike = Mathf.Max(0.01f, m.strikeTime * fit);
        Step(m, strike);
        float nextGhost = 0f;
        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            float p = t / strike;
            weapon.localPosition = new Vector3(Mathf.LerpUnclamped(back, reach, m.strikeCurve.Evaluate(p)), 0f, 0f);
            StretchDuringStrike(m, p);
            if (p >= m.hitFrom) CheckSwingHit(hitEnemies, m);
            Trail(m, ref nextGhost);
            yield return null;
        }
        weapon.localPosition = new Vector3(reach, 0f, 0f);
        weaponStretch = Vector2.one;
        CheckSwingHit(hitEnemies, m);
        if (releaseWave && used == currentWeaponData) ReleaseWave(used);

        float hold = m.holdTime * fit;
        for (float t = 0f; t < hold; t += Time.deltaTime)
        {
            CheckSwingHit(hitEnemies, m); // ศัตรูเดินเข้าปลายหอกระหว่างค้างก็โดน
            yield return null;
        }

        float recover = m.recoverTime * fit;
        for (float t = 0f; t < recover; t += Time.deltaTime)
        {
            weapon.localPosition = new Vector3(Mathf.Lerp(reach, 0f, EaseInOut(t / recover)), 0f, 0f);
            yield return null;
        }
        weapon.localPosition = Vector3.zero;
        isAttacking = false;
        spearRoutine = null;
    }

    // ระหว่างง้างหอกไอออน-X: ถอยหอกกลับ พอครบเวลาหอกเรืองแสงกะพริบ บอกว่าปล่อยได้แล้ว
    private void ShowSpearCharge()
    {
        if (currentWeaponObject == null || currentWeaponData == null) return;
        float progress = Mathf.Clamp01((Time.time - spearChargeStart) / Mathf.Max(0.01f, currentWeaponData.chargeTime));
        currentWeaponObject.transform.localPosition = new Vector3(-WeaponLength * spearChargePullBack * progress, 0f, 0f);
        Color tint = progress >= 1f
            ? Color.Lerp(Color.white, spearChargedTint, 0.6f + 0.4f * Mathf.Sin(Time.time * 18f))
            : Color.Lerp(Color.white, spearChargedTint, progress * 0.5f);
        TintWeapon(tint);
    }

    private void ReleaseSpearCharge()
    {
        bool full = Time.time - spearChargeStart >= currentWeaponData.chargeTime;
        spearChargeStart = -1f;
        TintWeapon(Color.white);

        if (owner != null && !owner.TrySpendEnergy(currentWeaponData.energyCost))
        {
            if (currentWeaponObject != null) currentWeaponObject.transform.localPosition = Vector3.zero;
            nextAttackTime = Time.time + OutOfEnergyRetryDelay;
            return;
        }
        StartThrust(full);
        nextAttackTime = Time.time + AttackInterval;
    }

    private void TintWeapon(Color color)
    {
        if (weaponRenderers == null) return;
        foreach (var renderer in weaponRenderers)
            if (renderer != null) renderer.color = color;
    }

    // ---------- ความสามารถตำนาน ----------

    // คลื่นพลัง (ดาบผ่ามิติ / หอกไอออน-X): พุ่งออกจากปลายอาวุธตามแนวเล็ง ทะลุศัตรูทุกตัว หยุดที่กำแพง
    // ภาพ 3 เฟรม: เฟรม 2 = คลื่นเต็มตอนบิน, เฟรม 3 = สลายตอนหมดระยะ
    private void ReleaseWave(WeaponData data)
    {
        if (data == null || data.specialFrames == null || data.specialFrames.Length == 0) return;
        Vector2 direction = transform.right;
        float reach = attackPoint != null ? Vector2.Distance(transform.position, attackPoint.position) : 0.5f;
        Vector2 origin = (Vector2)transform.position + direction * reach;

        var frames = data.specialFrames;
        Sprite[] fly = { frames[Mathf.Min(1, frames.Length - 1)] };
        Sprite[] fade = frames.Length >= 3 ? new[] { frames[2] } : null;
        float lifetime = data.specialRange / Mathf.Max(0.1f, data.specialSpeed);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // แสงวาบตอนปล่อย (เฟรม 1 ขยายใหญ่) แล้วคลื่นพุ่งออกไปพร้อมเงาภาพค้างตามหลัง
        SkillVfx.Spawn(new[] { frames[0] }, origin, data.specialScale * 1.3f, angle, 12f);
        PiercingProjectile.Spawn(origin, direction, data.specialSpeed, lifetime, data.SpecialDamage,
                                 data.specialRadius, fly, fade, data.specialScale, default, true)
                          .WithTrail(0.035f, 0.22f);
        CameraFollow.Shake(0.12f, 0.18f);
    }

    // วงพลังค้อนควอนตัม: ตรงจุดที่ทุบ ทำดาเมจทุกตัวในวงเป็นจังหวะตลอด specialDuration วินาที
    private IEnumerator GroundPulseRoutine(Vector2 center, WeaponData data)
    {
        const int ticks = 3; // ตอนทุบ / กลางทาง / ท้าย
        float duration = Mathf.Max(0.05f, data.specialDuration);
        if (data.specialFrames != null && data.specialFrames.Length > 0)
            SkillVfx.Spawn(data.specialFrames, center, data.specialScale, 0f, data.specialFrames.Length / duration);
        CameraFollow.Shake(0.15f, 0.3f);

        for (int i = 0; i < ticks; i++)
        {
            SkillCombat.DamageArea(center, data.specialRadius, data.SpecialDamage);
            if (i < ticks - 1) yield return new WaitForSeconds(duration / (ticks - 1));
        }
    }

    // ---------- กรงเล็บ / มีดคู่ ----------

    // ข้างที่ถึงตาพุ่งตะปบ ตรวจโดนตลอดทางที่พุ่ง ปล่อยคลื่นฟันตอนสุดแขน แล้วหดกลับ
    // ไม่ล็อกการเล็งระหว่างตี (ต่างจากดาบ) ท่าสั้นมาก ถ้าล็อกจะรู้สึกหน่วง
    private IEnumerator ClawStrikeRoutine(DualClawWeapon claws)
    {
        AttackMotion m = currentWeaponData.Motion;
        bool upper = claws.NextHand();
        claws.ResetPose();
        if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");

        var hitEnemies = new HashSet<Component>();
        float strike = Mathf.Max(0.01f, Mathf.Min(claws.strikeTime, AttackInterval * 0.4f));
        float recover = Mathf.Max(0.01f, Mathf.Min(claws.recoverTime, AttackInterval * 0.5f));
        Transform tip = claws.TipOf(upper);
        Step(m, strike);

        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            claws.Pose(upper, t / strike);
            if (tip != null) HitAround(tip.position, claws.WorldHitRadius, hitEnemies, m);
            yield return null;
        }

        claws.Pose(upper, 1f);
        if (tip != null)
        {
            HitAround(tip.position, claws.WorldHitRadius, hitEnemies, m);
            SpawnClawSlash(tip, upper);
        }

        for (float t = 0f; t < recover; t += Time.deltaTime)
        {
            claws.Pose(upper, 1f - t / recover);
            yield return null;
        }

        claws.Pose(upper, 0f);
        clawRoutine = null;
    }

    // คลื่นฟันของกรงเล็บ เกิดที่ปลายเล็บข้างที่ตี มือบนกวาดลง มือล่างกวาดขึ้น
    // ไม่คูณ AimMirror แบบดาบ เพราะมือบน/ล่างพลิกไปพร้อม WeaponHolder อยู่แล้ว
    private void SpawnClawSlash(Transform tip, bool upper)
    {
        if (currentWeaponData.slashEffectPrefab == null) return;

        GameObject slash = Instantiate(currentWeaponData.slashEffectPrefab, transform);
        Vector3 local = transform.InverseTransformPoint(tip.position);
        slash.transform.localPosition = new Vector3(local.x, local.y, 0f);
        slash.transform.localRotation = Quaternion.identity;

        Vector3 scale = slash.transform.localScale * SlashSize();
        scale.y = (upper ? 1f : -1f) * Mathf.Abs(scale.y);
        slash.transform.localScale = scale;
    }

    // ---------- ยิง ----------

    // ปืน/ธนู: เล่นภาพง้าง/ไฟแลบ แล้วปล่อยกระสุนตรงเฟรมที่กำหนดไว้ใน WeaponSpriteAnimator
    // จำ WeaponData ของตอนกดยิงไว้ ถ้าสลับอาวุธระหว่างง้างจะได้ไม่ยิงกระสุนของอาวุธใหม่ออกไป
    private void FireRanged()
    {
        WeaponData firedWith = currentWeaponData;

        if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");

        var frames = currentWeaponObject != null ? currentWeaponObject.GetComponent<WeaponSpriteAnimator>() : null;
        if (frames == null)
        {
            SpawnProjectile(firedWith);
            return;
        }

        StopRoutine(ref rangedRoutine);
        rangedRoutine = StartCoroutine(frames.PlayAttack(AttackInterval, () => SpawnProjectile(firedWith)));
    }

    // ปล่อยเมาส์หลังง้าง: หักพลังงานตอนนี้ แล้วเล่นเฟรมปล่อยสายพร้อมยิง
    // คูลดาวน์ (attackSpeed) นับจากตอนปล่อย ง้างค้างนานแค่ไหนก็ยิงได้ทีละลูกตามอัตราเดิม
    private void ReleaseChargedShot()
    {
        var frames = chargedFrames;
        chargedFrames = null;
        if (frames == null || currentWeaponData == null) return;

        if (owner != null && !owner.TrySpendEnergy(currentWeaponData.energyCost))
        {
            frames.ShowIdle();
            return;
        }

        WeaponData firedWith = currentWeaponData;
        rangedRoutine = StartCoroutine(frames.PlayRelease(() => SpawnProjectile(firedWith)));
        nextAttackTime = Time.time + AttackInterval;
    }

    private void SpawnProjectile(WeaponData data)
    {
        if (data.projectilePrefab == null)
        {
            Debug.LogWarning($"อาวุธ {data.weaponName} เป็นอาวุธยิง แต่ยังไม่ได้ใส่ Projectile Prefab");
            return;
        }

        // ยิงไปทางที่ WeaponHolder เล็งอยู่ (แกน right ไม่โดนการพลิกซ้าย/ขวาของ scale)
        Vector3 muzzle = attackPoint != null ? attackPoint.position : transform.position;
        Vector2 direction = transform.right;

        GameObject shot = Instantiate(data.projectilePrefab, muzzle, Quaternion.identity);
        var projectile = shot.GetComponent<PlayerProjectile>();
        if (projectile == null) projectile = shot.AddComponent<PlayerProjectile>();

        Transform shooter = owner != null ? owner.transform : transform.root;
        projectile.Launch(shooter, direction, data.projectileSpeed, data.projectileLifetime, data.attackDamage);
        projectile.Configure(data);

        // ยิงหลายลูก (ธนูยิงกระจาย): ลูกที่เหลือเบนออกซ้าย/ขวาเป็นพัดรอบลูกกลาง
        int count = Mathf.Max(1, data.projectileCount);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * data.spreadAngle;
            if (Mathf.Approximately(offset, 0f)) continue; // ลูกกลางยิงไปแล้วข้างบน
            Vector2 spread = Quaternion.Euler(0f, 0f, offset) * direction;
            var extra = Instantiate(data.projectilePrefab, muzzle, Quaternion.identity).GetComponent<PlayerProjectile>();
            if (extra == null) continue;
            extra.Launch(shooter, spread, data.projectileSpeed, data.projectileLifetime, data.attackDamage);
            extra.Configure(data);
        }

        if (data == currentWeaponData) Kick(data.Motion);
    }

    // แรงถีบตอนยิง: อาวุธถอยกลับในมือแล้วดันคืน กล้องสั่นเบา ๆ ตัวละครถอยนิดหน่อย (ปืนหนัก)
    private void Kick(AttackMotion m)
    {
        CameraFollow.Shake(m.impactShake, 0.1f);
        Step(m, 0.08f);
        if (m.recoilDistance <= 0f || currentWeaponObject == null) return;
        StopRoutine(ref recoilRoutine);
        recoilRoutine = StartCoroutine(RecoilRoutine(m));
    }

    private IEnumerator RecoilRoutine(AttackMotion m)
    {
        Transform weapon = currentWeaponObject.transform;
        float time = Mathf.Max(0.01f, m.recoilTime);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            weapon.localPosition = new Vector3(-m.recoilDistance * (1f - EaseOut(t / time)), 0f, 0f);
            yield return null;
        }
        weapon.localPosition = Vector3.zero;
        recoilRoutine = null;
    }

    // ---------- ท่าทางอาวุธ ----------

    // ใส่มุมสวิง ด้านคม และการยืด/หดให้ตัวอาวุธ (คนละตัวกับ WeaponHolder ที่เล็งตามเมาส์) อาวุธจึงค้างมุมไว้ต่อสวิงถัดไปได้
    // ต้องทำทุกเฟรมเพราะทิศเล็งเปลี่ยนได้ตลอด ถ้าไปทำแค่ตอนเริ่มสวิงคมดาบจะค้างผิดด้านเมื่อเมาส์ข้ามไปอีกฝั่ง
    private void ApplySwingRotation()
    {
        if (currentWeaponObject == null) return;

        float mirror = AimMirror;
        Transform weapon = currentWeaponObject.transform;
        Vector3 scale = new Vector3(weaponBaseScale.x * weaponStretch.x, weaponBaseScale.y * weaponStretch.y, weaponBaseScale.z);

        // ปืน/ธนู/กรงเล็บคู่/หอกไม่มีท่าสวิงและไม่มีด้านคม ปล่อยให้ WeaponHolder พลิกอย่างเดียว
        // ถ้าพลิกซ้ำแบบดาบ พอเล็งไปทางซ้ายด้ามปืนจะหงายขึ้นฟ้า และมือบน/ล่างของกรงเล็บจะสลับกัน
        if (currentClaws != null || IsSpear || (currentWeaponData != null && currentWeaponData.IsRanged))
        {
            weapon.localEulerAngles = Vector3.zero;
            weapon.localScale = scale;
            return;
        }

        weapon.localEulerAngles = new Vector3(0f, 0f, currentSwingAngle * mirror);

        scale.y *= (bladeEdgeUp ? -1f : 1f) * mirror;
        weapon.localScale = scale;
    }

    // เสกคลื่นดาบไว้กลางวงสวิง หันตามทิศที่เล็ง และพลิกตามทิศฟัน
    private void SpawnSlashEffect(bool downward)
    {
        if (currentWeaponData.slashEffectPrefab == null) return;

        GameObject slash = Instantiate(currentWeaponData.slashEffectPrefab, transform);

        // วัดระยะมือถึงปลายดาบในพิกัดของ WeaponHolder (ไม่ใช่พิกัดโลก) ไม่งั้นตำแหน่งจะเพี้ยนตามสเกลของตัวละคร
        float bladeLength = (attackPoint != null) ? transform.InverseTransformPoint(attackPoint.position).magnitude : 1f;
        slash.transform.localPosition = new Vector3(bladeLength * slashEffectDistance, 0f, 0f);
        slash.transform.localRotation = Quaternion.identity;

        Vector3 scale = slash.transform.localScale * SlashSize();
        scale.y = (downward ? 1f : -1f) * AimMirror * Mathf.Abs(scale.y); // ฟันขึ้นให้พลิกคลื่นกลับด้านตามดาบ
        slash.transform.localScale = scale;
    }

    // ขนาดคลื่นฟันตามความยาวอาวุธ (มือถึง AttackPoint ของ prefab) เทียบกับความยาวอ้างอิง
    private float SlashSize()
    {
        float bladeLength = (attackPoint != null) ? transform.InverseTransformPoint(attackPoint.position).magnitude : 1f;
        return slashEffectScale * bladeLength / Mathf.Max(0.01f, slashReferenceBladeLength);
    }

    // สลับด้านคมดาบ (ฟันลงคมชี้ลง / ฟันขึ้นคมชี้ขึ้น) ตัวพลิกจริงอยู่ใน ApplySwingRotation ที่ทำทุกเฟรม
    private void SetBladeEdgeUp(bool edgeUp)
    {
        bladeEdgeUp = edgeUp;
    }

    // คอมโบขาดแล้วให้ดาบค่อยๆ กลับไปท่าพัก (ชูขึ้นด้านบน) พร้อมคืนด้านคมดาบเป็นปกติ
    private void ReturnToRestPose()
    {
        if (currentWeaponData == null || currentWeaponObject == null) return;
        if (Time.time - lastAttackTime <= ComboWindow) return; // ยังอยู่ในช่วงคอมโบ ค้างท่าไว้รอกดต่อ

        float returnSpeed = currentWeaponData.attackAngle / Mathf.Max(0.01f, restReturnDuration);
        currentSwingAngle = Mathf.MoveTowards(currentSwingAngle, RestAngle, returnSpeed * Time.deltaTime);

        if (Mathf.Approximately(currentSwingAngle, RestAngle)) SetBladeEdgeUp(false);
    }

    // ---------- ตีโดน ----------

    private void CheckSwingHit(HashSet<Component> alreadyHit, AttackMotion feel)
    {
        if (attackPoint == null) return;
        HitAround(attackPoint.position, currentWeaponData.attackRange, alreadyHit, feel);
    }

    // ตีโดนทุกตัวในรัศมีรอบจุดที่ให้มา ตัวละหนึ่งครั้งต่อการโจมตีหนึ่งท่า
    // ดาบใช้ attackRange ของ WeaponData ส่วนกรงเล็บคู่ใช้รัศมีที่ย่อ/ขยายตามขนาด prefab
    // ตัวแรกที่โดนในท่านี้: หยุดภาพชั่วขณะ + กล้องสั่น, ทุกตัวที่โดน: ประกายกระเด็นออกตามทิศที่ตี
    private void HitAround(Vector3 center, float radius, HashSet<Component> alreadyHit, AttackMotion feel)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);

        foreach (Collider2D enemy in hits)
        {
            Component target = null;
            MonsterController monster = enemy.GetComponent<MonsterController>();
            if (monster != null)
            {
                if (!alreadyHit.Contains(monster))
                {
                    alreadyHit.Add(monster);
                    monster.TakeDamage(currentWeaponData.attackDamage);
                    target = monster;
                }
            }
            else
            {
                // บอสใช้ระบบเลือดแยกจาก AI มอนสเตอร์เดิม และรับหนึ่งฮิตต่อการฟันหนึ่งครั้ง
                ArchitectBossHealth boss = enemy.GetComponentInParent<ArchitectBossHealth>();
                if (boss != null && alreadyHit.Add(boss))
                {
                    boss.TakeDamage(currentWeaponData.attackDamage);
                    target = boss;
                }
            }

            if (target != null) HitFeedback(enemy, center, alreadyHit.Count == 1, feel);
        }
    }

    private void HitFeedback(Collider2D enemy, Vector2 from, bool first, AttackMotion feel)
    {
        if (feel == null) return;
        Vector2 point = enemy.ClosestPoint(from);
        Vector2 direction = point - (Vector2)transform.position;
        ImpactSparks.Spawn(point, feel.sparkColor, feel.sparkCount, direction);
        if (!first) return;
        HitStop.Freeze(feel.hitStop);
        CameraFollow.Shake(feel.hitShake, 0.12f);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null || currentWeaponData == null) return;
        Gizmos.color = Color.red;

        // กรงเล็บคู่วาดวง hitbox ที่ปลายเล็บทั้งสองข้าง (ขนาดจริงที่ใช้ตี)
        if (currentClaws != null)
        {
            foreach (var tip in new[] { currentClaws.upperTip, currentClaws.lowerTip })
                if (tip != null) Gizmos.DrawWireSphere(tip.position, currentClaws.WorldHitRadius);
            return;
        }

        Gizmos.DrawWireSphere(attackPoint.position, currentWeaponData.attackRange);

        Vector3 weaponFacingDir = transform.right;
        Vector3 rightLimit = Quaternion.Euler(0, 0, currentWeaponData.attackAngle / 2f) * weaponFacingDir;
        Vector3 leftLimit = Quaternion.Euler(0, 0, -currentWeaponData.attackAngle / 2f) * weaponFacingDir;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, rightLimit * currentWeaponData.attackRange);
        Gizmos.DrawRay(transform.position, leftLimit * currentWeaponData.attackRange);
    }
}
