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
    private float currentSwingAngle; // มุมดาบปัจจุบันเทียบแนวเล็ง (+ = ปลายชี้ขึ้น, - = ปลายชี้ลง)

    private float AttackInterval => 1f / Mathf.Max(0.01f, currentWeaponData.attackSpeed);
    private float ComboWindow => AttackInterval * 1.5f;

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

            // เริ่มที่ท่าพัก (ชูดาบขึ้นด้านบน) รอสวิงแรกที่เป็นบนลงล่าง
            currentSwingAngle = currentWeaponData.attackAngle / 2f;
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
        HashSet<MonsterController> hitEnemies = new HashSet<MonsterController>();

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

    // ใส่มุมสวิงให้ตัวดาบ (คนละตัวกับ WeaponHolder ที่เล็งตามเมาส์) ดาบจึงค้างมุมไว้ต่อสวิงถัดไปได้
    private void ApplySwingRotation()
    {
        if (currentWeaponObject == null) return;

        // WeaponHolder พลิกแกน Y ตอนเล็งไปทางซ้าย ต้องกลับเครื่องหมายมุมด้วย ไม่งั้นทิศฟันจะสลับบน-ล่าง
        float mirror = (transform.localScale.y < 0f) ? -1f : 1f;
        currentWeaponObject.transform.localEulerAngles = new Vector3(0f, 0f, currentSwingAngle * mirror);
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
        scale.y = downward ? Mathf.Abs(scale.y) : -Mathf.Abs(scale.y); // ฟันขึ้นให้พลิกคลื่นกลับด้านตามดาบ
        slash.transform.localScale = scale;
    }

    // พลิกดาบตามแกน Y เพื่อสลับด้านคม (ฟันลงคมชี้ลง / ฟันขึ้นคมชี้ขึ้น)
    private void SetBladeEdgeUp(bool edgeUp)
    {
        if (currentWeaponObject == null) return;

        Vector3 scale = currentWeaponObject.transform.localScale;
        scale.y = edgeUp ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        currentWeaponObject.transform.localScale = scale;
    }

    // คอมโบขาดแล้วให้ดาบค่อยๆ กลับไปท่าพัก (ชูขึ้นด้านบน) พร้อมคืนด้านคมดาบเป็นปกติ
    private void ReturnToRestPose()
    {
        if (currentWeaponData == null || currentWeaponObject == null) return;
        if (Time.time - lastAttackTime <= ComboWindow) return; // ยังอยู่ในช่วงคอมโบ ค้างท่าไว้รอกดต่อ

        float restAngle = currentWeaponData.attackAngle / 2f;
        float returnSpeed = currentWeaponData.attackAngle / Mathf.Max(0.01f, restReturnDuration);
        currentSwingAngle = Mathf.MoveTowards(currentSwingAngle, restAngle, returnSpeed * Time.deltaTime);

        if (Mathf.Approximately(currentSwingAngle, restAngle)) SetBladeEdgeUp(false);
    }

    private void CheckSwingHit(HashSet<MonsterController> alreadyHit)
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