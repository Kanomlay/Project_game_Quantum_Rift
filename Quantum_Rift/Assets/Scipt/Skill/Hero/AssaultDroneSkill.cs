using UnityEngine;

// นักประดิษฐ์ Q: โดรนจู่โจม — ปล่อยโดรนลอยตามตัว ยิงศัตรูใกล้สุดให้อัตโนมัติจนหมดเวลา
[CreateAssetMenu(fileName = "AssaultDrone", menuName = "Game Data/Skills/Inventor/Assault Drone")]
public class AssaultDroneSkill : SkillData
{
    [Header("โดรน")]
    public float duration = 10f;
    public float fireInterval = 0.6f;
    public float range = 6f;
    public float damage = 1.5f;
    public float bulletSpeed = 14f;
    public GameObject bulletPrefab; // ใช้กระสุนชุดเดียวกับปืน (ต้องมี PlayerProjectile)

    public override void ActivateSkill(GameObject player)
    {
        AssaultDrone.Deploy(player.transform, effectFrames, effectScale, bulletPrefab,
                            duration, fireInterval, range, damage, bulletSpeed);
    }
}
