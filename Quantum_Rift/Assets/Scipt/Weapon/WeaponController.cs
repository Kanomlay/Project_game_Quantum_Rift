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
    private float nextAttackTime = 0f; 

    void Update()
    {
        if (PauseManager.isGamePaused) return;
   
        AimTowardsMouse();

     
        if (Input.GetMouseButtonDown(0))
        {
            AttemptAttack();
        }
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
        }
    }

   void AttemptAttack()
    {
        if (currentWeaponData == null || Time.time < nextAttackTime) return;

        if (currentWeaponAnim != null)
        {
            currentWeaponAnim.SetTrigger("Attack"); 
        }
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, currentWeaponData.attackRange, enemyLayers);
        
        foreach(Collider2D enemy in hitEnemies)
        {
            Vector2 dirToEnemy = (enemy.transform.position - transform.position).normalized;

            Vector2 weaponFacingDir = transform.right;

            float angleToEnemy = Vector2.Angle(weaponFacingDir, dirToEnemy);

            if (angleToEnemy <= currentWeaponData.attackAngle / 2f)
            {
                MonsterController monster = enemy.GetComponent<MonsterController>();
                if (monster != null)
                {
                    monster.TakeDamage(currentWeaponData.attackDamage); 
                    Debug.Log("ฟาดโดนเข้าให้!: " + enemy.name + " โดนดาเมจไป " + currentWeaponData.attackDamage);
                }
            }
        }

        nextAttackTime = Time.time + (1f / currentWeaponData.attackSpeed);
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