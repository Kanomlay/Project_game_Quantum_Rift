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
    public float slashEffectScale = 3f;    // ตัวคูณขนาดคลื่นดาบ

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

    void Update()
    {
        if (PauseManager.isGamePaused) return;

        if (!isAttacking)
        {
            AimTowardsMouse();
            ReturnToRestPose();
        }

        if (Input.GetMouseButton(0))
        {
            AttemptAttack();
        }

        ApplySwingRotation();
    }

    void AimTowardsMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 aimDirection = (mousePos - transform.position).normalized;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        transform.eulerAngles = new Vector3(0, 0, angle);

        Vector3 localScale = Vector3.one;
        if (angle > 90 || angle < -90)
        {
            localScale.y = -1f;
        }
        else
        {
            localScale.y = 1f;
        }
        transform.localScale = localScale;
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        currentWeaponData = newWeaponData;

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

            // ท่าพักให้ปลายดาบชี้ตรงแนวเล็ง รอสวิงแรกที่เป็นบนลงล่าง
            currentSwingAngle = RestAngle;
            nextSwingDownward = true;
            SetBladeEdgeUp(false);
            ApplySwingRotation();
        }
    }

   void AttemptAttack()
    {
        if (currentWeaponData == null || Time.time < nextAttackTime) return;
        if (currentWeaponData.weaponType != WeaponType.Sword) return; // ปืน/ธนู ต้องใช้ระบบกระสุนแยก เดี๋ยวค่อยทำ

        // ถ้าทิ้งช่วงนานเกินคอมโบ (สวิงก่อนหน้าเล่นจบไปนานแล้ว) ให้เริ่มใหม่ด้วยท่าบนลงล่างเสมอ
        if (Time.time - lastAttackTime > ComboWindow)
        {
            nextSwingDownward = true;
        }
        lastAttackTime = Time.time;

        if (currentWeaponAnim != null)
        {
            currentWeaponAnim.SetTrigger("Attack");
        }

        StartCoroutine(SwordSwingRoutine(nextSwingDownward));
        nextSwingDownward = !nextSwingDownward;

        nextAttackTime = Time.time + AttackInterval;
    }

    // หมุน sprite ดาบกวาดผ่านมุม attackAngle รอบทิศที่เล็งอยู่ แทนการสลับเฟรมอนิเมชัน
    // downward = บนลงล่าง, !downward = ล่างขึ้นบน โดยเริ่มต่อจากมุมที่ดาบค้างอยู่จากสวิงก่อนหน้า
    private IEnumerator SwordSwingRoutine(bool downward)
    {
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
    }

    // ใส่มุมสวิงกับด้านคมให้ตัวดาบ (คนละตัวกับ WeaponHolder ที่เล็งตามเมาส์) ดาบจึงค้างมุมไว้ต่อสวิงถัดไปได้
    // ต้องทำทุกเฟรมเพราะทิศเล็งเปลี่ยนได้ตลอด ถ้าไปทำแค่ตอนเริ่มสวิงคมดาบจะค้างผิดด้านเมื่อเมาส์ข้ามไปอีกฝั่ง
    private void ApplySwingRotation()
    {
        if (currentWeaponObject == null) return;

        float mirror = AimMirror;
        Transform weapon = currentWeaponObject.transform;

        weapon.localEulerAngles = new Vector3(0f, 0f, currentSwingAngle * mirror);

        Vector3 scale = weapon.localScale;
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

        Vector3 scale = slash.transform.localScale * slashEffectScale;
        scale.y = (downward ? 1f : -1f) * AimMirror * Mathf.Abs(scale.y); // ฟันขึ้นให้พลิกคลื่นกลับด้านตามดาบ
        slash.transform.localScale = scale;
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, currentWeaponData.attackRange, enemyLayers);

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

        Gizmos.DrawWireSphere(attackPoint.position, currentWeaponData.attackRange);

        Vector3 weaponFacingDir = transform.right;
        Vector3 rightLimit = Quaternion.Euler(0, 0, currentWeaponData.attackAngle / 2f) * weaponFacingDir;
        Vector3 leftLimit = Quaternion.Euler(0, 0, -currentWeaponData.attackAngle / 2f) * weaponFacingDir;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, rightLimit * currentWeaponData.attackRange);
        Gizmos.DrawRay(transform.position, leftLimit * currentWeaponData.attackRange);
    }
}
