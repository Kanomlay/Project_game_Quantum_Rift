using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float swingDuration = 0.12f;      // เวลาที่ใช้ฟัน 1 ครั้ง (ความเร็วท่าฟัน คนละเรื่องกับ attackSpeed ที่เป็นคูลดาวน์)
    public float restReturnDuration = 0.15f; // เวลาที่ดาบใช้ค่อยๆ กลับไปท่าพักหลังคอมโบขาด
    [Range(0f, 1.5f)]
    public float slashEffectDistance = 1f; // ตำแหน่งคลื่นดาบ คิดเป็นสัดส่วนของระยะมือถึงปลายดาบ (1 = ตรงปลายดาบพอดี)
    public float slashEffectScale = 3f;    // ตัวคูณขนาดคลื่นดาบ (ขนาดที่พอดีกับอาวุธยาวเท่า slashReferenceBladeLength)
    // คลื่นดาบย่อ/ขยายตามความยาวอาวุธ (มือถึง AttackPoint) อาวุธสั้นจะได้คลื่นเล็กลงเอง
    // 1.8 = ความยาวดาบสนิมภาพเก่าที่ slashEffectScale ถูกปรับไว้ให้พอดี
    public float slashReferenceBladeLength = 1.8f;

    [Header("ท่าแทงหอก")]
    [Range(0f, 1f)] public float spearThrustReach = 0.5f; // แทงพุ่งออกไปไกลเท่านี้ (สัดส่วนความยาวหอก)
    public float spearThrustTime = 0.09f;
    public float spearRecoverTime = 0.16f;
    [Range(0f, 0.5f)] public float spearChargePullBack = 0.2f; // ง้างหอกไอออน-X ถอยหอกกลับเท่านี้ (สัดส่วนความยาว)
    public Color spearChargedTint = new Color(0.55f, 0.95f, 1f); // ง้างครบเวลาแล้ว หอกเรืองแสง

    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool nextSwingDownward = true; // true = บนลงล่าง, false = ล่างขึ้นบน (สลับกันทุกครั้งที่คอมโบต่อติด)
    private float lastAttackTime = -999f;
    private float currentSwingAngle; // มุมดาบปัจจุบันเทียบแนวเล็ง (0 = ปลายดาบชี้ตรงเมาส์, + = เชิดขึ้น, - = กดลง)
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

    private PlayerStats owner;        // เจ้าของอาวุธ ใช้หักพลังงานตอนโจมตี
    private Coroutine rangedRoutine;  // ท่ายิงที่กำลังเล่นอยู่ ต้องหยุดถ้าสลับอาวุธกลางท่า
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

        if (rangedRoutine != null)
        {
            StopCoroutine(rangedRoutine);
            rangedRoutine = null;
        }
        if (clawRoutine != null)
        {
            StopCoroutine(clawRoutine);
            clawRoutine = null;
        }
        if (spearRoutine != null)
        {
            StopCoroutine(spearRoutine);
            spearRoutine = null;
        }
        currentClaws = null;
        chargedFrames = null; // สลับอาวุธระหว่างง้าง = ยกเลิก ไม่ยิงและไม่เสียพลังงาน
        spearChargeStart = -1f;
        isAttacking = false;
        comboSwings = 0;

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        if (currentWeaponData != null && currentWeaponData.weaponPrefab != null)
        {
            
            currentWeaponObject = Instantiate(currentWeaponData.weaponPrefab, transform.position, transform.rotation, transform);
            currentWeaponAnim = currentWeaponObject.GetComponent<Animator>();

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
                if (rangedRoutine != null) { StopCoroutine(rangedRoutine); rangedRoutine = null; }
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

        StartCoroutine(SwordSwingRoutine(nextSwingDownward, releaseWave));
        nextSwingDownward = !nextSwingDownward;

        nextAttackTime = Time.time + AttackInterval;
    }

    // ดาบ/กระบอง/ค้อน ใช้ท่าฟันชุดเดียวกัน (ค้อนรอแยกท่าทุบ), กรงเล็บ/มีดคู่ตะปบสลับมือ, หอกแทงตรง
    private static bool IsMelee(WeaponType type) =>
        type == WeaponType.Sword || type == WeaponType.Claw || type == WeaponType.Spear || type == WeaponType.Hammer;
    private bool IsSpear => currentWeaponData != null && currentWeaponData.weaponType == WeaponType.Spear;

    // ความยาวอาวุธ (มือถึง AttackPoint) ในพิกัดของ WeaponHolder
    private float WeaponLength => attackPoint != null ? transform.InverseTransformPoint(attackPoint.position).magnitude : 1f;

    private void StartThrust(bool releaseWave)
    {
        if (spearRoutine != null) StopCoroutine(spearRoutine);
        if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");
        spearRoutine = StartCoroutine(SpearThrustRoutine(releaseWave));
    }

    // แทงหอก: พุ่งไปข้างหน้าตามแนวเล็ง ตรวจโดนที่ปลายหอกตลอดทางที่พุ่ง แล้วดึงกลับ (ล็อกทิศระหว่างแทง)
    private IEnumerator SpearThrustRoutine(bool releaseWave)
    {
        isAttacking = true;
        var hitEnemies = new HashSet<Component>();
        Transform weapon = currentWeaponObject.transform;
        float length = WeaponLength;
        float from = weapon.localPosition.x; // ต่อจากท่าง้างถอยหลัง (ถ้ามี)
        float reach = length * spearThrustReach;
        float strike = Mathf.Max(0.01f, Mathf.Min(spearThrustTime, AttackInterval * 0.35f));
        float recover = Mathf.Max(0.01f, Mathf.Min(spearRecoverTime, AttackInterval * 0.45f));

        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            float k = t / strike;
            weapon.localPosition = new Vector3(Mathf.Lerp(from, reach, 1f - (1f - k) * (1f - k)), 0f, 0f);
            CheckSwingHit(hitEnemies);
            yield return null;
        }
        weapon.localPosition = new Vector3(reach, 0f, 0f);
        CheckSwingHit(hitEnemies);
        if (releaseWave) ReleaseWave(currentWeaponData);

        for (float t = 0f; t < recover; t += Time.deltaTime)
        {
            weapon.localPosition = new Vector3(Mathf.Lerp(reach, 0f, t / recover), 0f, 0f);
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
    }

    // วงพลังค้อนควอนตัม: ตรงจุดที่ทุบ ทำดาเมจทุกตัวในวงเป็นจังหวะตลอด specialDuration วินาที
    private IEnumerator GroundPulseRoutine(Vector2 center, WeaponData data)
    {
        const int ticks = 3; // ตอนทุบ / กลางทาง / ท้าย
        float duration = Mathf.Max(0.05f, data.specialDuration);
        if (data.specialFrames != null && data.specialFrames.Length > 0)
            SkillVfx.Spawn(data.specialFrames, center, data.specialScale, 0f, data.specialFrames.Length / duration);

        for (int i = 0; i < ticks; i++)
        {
            SkillCombat.DamageArea(center, data.specialRadius, data.SpecialDamage);
            if (i < ticks - 1) yield return new WaitForSeconds(duration / (ticks - 1));
        }
    }

    // กรงเล็บคู่: ข้างที่ถึงตาพุ่งตะปบ ตรวจโดนตลอดทางที่พุ่ง ปล่อยคลื่นฟันตอนสุดแขน แล้วหดกลับ
    // ไม่ล็อกการเล็งระหว่างตี (ต่างจากดาบ) ท่าสั้นมาก ถ้าล็อกจะรู้สึกหน่วง
    private IEnumerator ClawStrikeRoutine(DualClawWeapon claws)
    {
        bool upper = claws.NextHand();
        claws.ResetPose();
        if (currentWeaponAnim != null) currentWeaponAnim.SetTrigger("Attack");

        var hitEnemies = new HashSet<Component>();
        float strike = Mathf.Max(0.01f, Mathf.Min(claws.strikeTime, AttackInterval * 0.4f));
        float recover = Mathf.Max(0.01f, Mathf.Min(claws.recoverTime, AttackInterval * 0.5f));
        Transform tip = claws.TipOf(upper);

        for (float t = 0f; t < strike; t += Time.deltaTime)
        {
            claws.Pose(upper, t / strike);
            if (tip != null) HitAround(tip.position, claws.WorldHitRadius, hitEnemies);
            yield return null;
        }

        claws.Pose(upper, 1f);
        if (tip != null)
        {
            HitAround(tip.position, claws.WorldHitRadius, hitEnemies);
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

        if (rangedRoutine != null) StopCoroutine(rangedRoutine);
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
    }

    // หมุน sprite ดาบกวาดผ่านมุม attackAngle รอบทิศที่เล็งอยู่ แทนการสลับเฟรมอนิเมชัน
    // downward = บนลงล่าง, !downward = ล่างขึ้นบน โดยเริ่มต่อจากมุมที่ดาบค้างอยู่จากสวิงก่อนหน้า
    private IEnumerator SwordSwingRoutine(bool downward, bool releaseWave = false)
    {
        WeaponData swungWith = currentWeaponData;
        isAttacking = true;
        HashSet<Component> hitEnemies = new HashSet<Component>();

        SetBladeEdgeUp(!downward); // ฟันขึ้นต้องพลิกดาบเอาคมขึ้นด้วย
        SpawnSlashEffect(downward);

        float half = currentWeaponData.attackAngle / 2f;
        float startAngle = currentSwingAngle;
        float endAngle = downward ? -half : half;
        // กันไม่ให้ท่าฟันยาวเกินคูลดาวน์จนสวิงถัดไปเริ่มทับกัน
        float swingTime = Mathf.Max(0.01f, Mathf.Min(swingDuration, AttackInterval));

        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            currentSwingAngle = Mathf.Lerp(startAngle, endAngle, Mathf.Clamp01(t / swingTime));

            CheckSwingHit(hitEnemies);

            yield return null;
        }

        currentSwingAngle = endAngle;
        isAttacking = false;

        // ความสามารถตำนาน: ดาบผ่ามิติปล่อยคลื่นครั้งที่ 3, ค้อนควอนตัมเกิดวงพลังตรงหัวค้อน
        if (swungWith != currentWeaponData) yield break; // สลับอาวุธกลางท่า
        if (releaseWave) ReleaseWave(swungWith);
        if (swungWith.special == WeaponSpecial.GroundPulse && attackPoint != null)
            StartCoroutine(GroundPulseRoutine(attackPoint.position, swungWith));
    }

    // ใส่มุมสวิงกับด้านคมให้ตัวดาบ (คนละตัวกับ WeaponHolder ที่เล็งตามเมาส์) ดาบจึงค้างมุมไว้ต่อสวิงถัดไปได้
    // ต้องทำทุกเฟรมเพราะทิศเล็งเปลี่ยนได้ตลอด ถ้าไปทำแค่ตอนเริ่มสวิงคมดาบจะค้างผิดด้านเมื่อเมาส์ข้ามไปอีกฝั่ง
    private void ApplySwingRotation()
    {
        if (currentWeaponObject == null) return;

        float mirror = AimMirror;
        Transform weapon = currentWeaponObject.transform;
        Vector3 scale = weapon.localScale;

        // ปืน/ธนู/กรงเล็บคู่/หอกไม่มีท่าสวิงและไม่มีด้านคม ปล่อยให้ WeaponHolder พลิกอย่างเดียว
        // ถ้าพลิกซ้ำแบบดาบ พอเล็งไปทางซ้ายด้ามปืนจะหงายขึ้นฟ้า และมือบน/ล่างของกรงเล็บจะสลับกัน
        if (currentClaws != null || IsSpear || (currentWeaponData != null && currentWeaponData.IsRanged))
        {
            weapon.localEulerAngles = Vector3.zero;
            scale.y = Mathf.Abs(scale.y);
            weapon.localScale = scale;
            return;
        }

        weapon.localEulerAngles = new Vector3(0f, 0f, currentSwingAngle * mirror);

        scale.y = (bladeEdgeUp ? -1f : 1f) * mirror * Mathf.Abs(scale.y);
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

    private void CheckSwingHit(HashSet<Component> alreadyHit)
    {
        if (attackPoint == null) return;
        HitAround(attackPoint.position, currentWeaponData.attackRange, alreadyHit);
    }

    // ตีโดนทุกตัวในรัศมีรอบจุดที่ให้มา ตัวละหนึ่งครั้งต่อการโจมตีหนึ่งท่า
    // ดาบใช้ attackRange ของ WeaponData ส่วนกรงเล็บคู่ใช้รัศมีที่ย่อ/ขยายตามขนาด prefab
    private void HitAround(Vector3 center, float radius, HashSet<Component> alreadyHit)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);

        foreach (Collider2D enemy in hits)
        {
            MonsterController monster = enemy.GetComponent<MonsterController>();
            if (monster != null && !alreadyHit.Contains(monster))
            {
                alreadyHit.Add(monster);
                monster.TakeDamage(currentWeaponData.attackDamage);
                Debug.Log("ฟาดโดนเข้าให้!: " + enemy.name + " โดนดาเมจไป " + currentWeaponData.attackDamage);
            }
            else if (monster == null)
            {
                // บอสใช้ระบบเลือดแยกจาก AI มอนสเตอร์เดิม และรับหนึ่งฮิตต่อการฟันหนึ่งครั้ง
                ArchitectBossHealth boss = enemy.GetComponentInParent<ArchitectBossHealth>();
                if (boss != null && alreadyHit.Add(boss)) boss.TakeDamage(currentWeaponData.attackDamage);
            }
        }
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
