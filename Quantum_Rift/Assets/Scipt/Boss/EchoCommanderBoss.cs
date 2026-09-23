using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// บอสประจำแมพ 1 ตามตาราง 1.8: เรียกลูกน้องออกมาช่วยสู้ และยิงกระสุนกระจายเป็นทรงพัด
//
// สืบทอดจาก MonsterController เพื่อให้ RoomController เสกได้เหมือนมอนสเตอร์ปกติ
// จะได้ประตูปิดตอนเริ่มสู้ เปิดตอนบอสตาย และพอร์ทัลออกทำงานตามระบบห้องเดิม
// ค่าเลือด/ดาเมจ/ความเร็ว/หน่วงโจมตีประชิด อ่านจาก MonsterData เหมือนตัวอื่น
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

    [Header("ท่าเรียกลูกน้อง")]
    public MonsterData[] minions;
    [Min(1)] public int minionsPerSummon = 2;
    [Min(1f)] public float summonCooldown = 12f;
    [Min(1)] public int maxAliveMinions = 4;
    [Min(0f)] public float summonWindup = 0.5f;
    [Min(0.5f)] public float summonRadius = 3f; // ใช้ตอนห้องไม่มีจุดเกิดให้

    private float nextShootTime;
    private float nextSummonTime;
    private bool isCasting; // ระหว่างร่ายท่าไม่ให้เดินหรือฟันซ้อน
    private readonly List<GameObject> aliveMinions = new List<GameObject>();

    protected override void Start()
    {
        base.Start();

        // เว้นจังหวะตอนเข้าห้องใหม่ๆ ผู้เล่นจะได้ไม่โดนรัวตั้งแต่วินาทีแรก
        nextShootTime = Time.time + 2f;
        nextSummonTime = Time.time + 5f;
    }

    protected override void Update()
    {
        if (isDying) return;
        if (isCasting) return;
        if (isKnockedBack) return;

        if (player != null && myData != null)
        {
            if (Time.time >= nextSummonTime && CanSummon())
            {
                StartCoroutine(SummonRoutine());
                return;
            }

            float distance = Vector2.Distance(transform.position, player.position);
            if (Time.time >= nextShootTime && distance <= shootRange && distance > myData.attackRange)
            {
                StartCoroutine(ShootRoutine());
                return;
            }
        }

        // นอกจากท่าพิเศษ ใช้การเดินไล่และฟันประชิดของมอนสเตอร์ปกติ
        base.Update();
    }

    private IEnumerator ShootRoutine()
    {
        isCasting = true;
        FacePlayer();
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("Shoot");
        }

        yield return new WaitForSeconds(shootWindup);

        if (!isKnockedBack) FireFan();

        yield return new WaitForSeconds(0.25f);

        nextShootTime = Time.time + shootCooldown;
        isCasting = false;
    }

    // กระจายกระสุนเป็นพัดรอบทิศที่เล็งผู้เล่น นัดกลางพุ่งตรง
    private void FireFan()
    {
        if (projectilePrefab == null || player == null) return;

        Vector2 aim = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float step = projectilesPerShot > 1 ? fanSpreadDegrees / (projectilesPerShot - 1) : 0f;
        float start = -fanSpreadDegrees / 2f;

        for (int i = 0; i < projectilesPerShot; i++)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, start + step * i) * aim;
            var shot = Instantiate(projectilePrefab, transform.position + (Vector3)(direction * 0.8f), Quaternion.identity);

            var projectile = shot.GetComponent<BossProjectile>();
            if (projectile != null) projectile.Launch(direction, projectileSpeed, projectileDamage, gameObject.layer);
            else Debug.LogWarning("prefab กระสุนของบอสยังไม่มีสคริปต์ BossProjectile กระสุนจะลอยนิ่ง");
        }
    }

    private bool CanSummon()
    {
        if (minions == null || minions.Length == 0) return false;

        aliveMinions.RemoveAll(minion => minion == null || !minion.activeInHierarchy);
        return aliveMinions.Count + minionsPerSummon <= maxAliveMinions;
    }

    private IEnumerator SummonRoutine()
    {
        isCasting = true;
        FacePlayer();
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("Summon");
        }

        yield return new WaitForSeconds(summonWindup);

        for (int i = 0; i < minionsPerSummon; i++) SpawnMinion(i);

        yield return new WaitForSeconds(0.25f);

        nextSummonTime = Time.time + summonCooldown;
        isCasting = false;
    }

    private void SpawnMinion(int index)
    {
        var data = minions[Random.Range(0, minions.Length)];
        if (data == null || data.monsterPrefab == null) return;
        if (data.monsterPrefab.GetComponent<MonsterController>() == null) return;

        var spawned = Instantiate(data.monsterPrefab, PickSummonPosition(index), Quaternion.identity, transform.parent);

        var controller = spawned.GetComponent<MonsterController>();
        controller.myData = data;
        // ตั้งใจไม่ผูกกับห้อง: ถ้าให้ลูกน้องรายงานการตายด้วย ห้องจะนับว่าเคลียร์แล้วเปิดประตูทั้งที่บอสยังอยู่
        controller.currentRoom = null;

        aliveMinions.Add(spawned);
    }

    // ใช้จุดเกิดมอนสเตอร์ของห้องถ้ามี ไม่งั้นวางเป็นวงรอบตัวบอส
    private Vector3 PickSummonPosition(int index)
    {
        var points = currentRoom != null ? currentRoom.monsterSpawnPoints : null;
        if (points != null && points.Length > 0)
        {
            var point = points[Random.Range(0, points.Length)];
            if (point != null) return point.position;
        }

        float angle = (360f / Mathf.Max(1, minionsPerSummon)) * index + Random.Range(-20f, 20f);
        Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.right * summonRadius;
        return transform.position + offset;
    }

    private void FacePlayer()
    {
        if (player == null || sr == null) return;
        sr.flipX = player.position.x < transform.position.x;
    }

    protected override void Die()
    {
        StopAllCoroutines();
        isCasting = false;
        base.Die();
    }
}
