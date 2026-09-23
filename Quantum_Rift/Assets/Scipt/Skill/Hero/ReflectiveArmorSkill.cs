using System.Collections;
using UnityEngine;

// นักรบ E: เกราะสะท้อนกลับ — กางวงเกราะรอบตัว กันดาเมจทั้งหมดชั่วคราว
// ทุกครั้งที่กันได้ เกราะสะท้อนแรงออกเป็นวงทำดาเมจศัตรูรอบตัว
[CreateAssetMenu(fileName = "ReflectiveArmor", menuName = "Game Data/Skills/Warrior/Reflective Armor")]
public class ReflectiveArmorSkill : SkillData
{
    [Header("เกราะ")]
    public float duration = 4f;
    public float reflectDamage = 4f;
    public float reflectRadius = 2.5f;
    public float reflectInterval = 0.25f; // กันไม่ให้โดนรัวหลายนัดติดแล้วสะท้อนถี่เกิน

    public override void ActivateSkill(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.RaiseShield(duration);

        // เฟรม 1–4 วงเกราะก่อตัว ค้างเฟรม 4 (วงเต็ม) ตลอดเวลาที่กาง แล้วเฟรม 6–7 แตกจางหาย
        var vfx = SkillVfx.Spawn(effectFrames, SkillCombat.BodyCenter(player), effectScale, 0f, 14f, effectOffset)
                          .Loop(3, 3, duration)
                          .Follow(player.transform);

        stats.StartCoroutine(Reflect(stats, vfx));
    }

    private IEnumerator Reflect(PlayerStats stats, SkillVfx vfx)
    {
        float nextReflect = 0f;
        void OnBlocked(float blocked)
        {
            if (Time.time < nextReflect) return;
            nextReflect = Time.time + reflectInterval;
            SkillCombat.DamageArea(SkillCombat.BodyCenter(stats.gameObject), reflectRadius, reflectDamage);
            if (vfx != null) vfx.Pulse(4, 0.15f); // เฟรม 5 = แสงสะท้อนวาบออก
        }

        stats.DamageBlocked += OnBlocked;
        yield return new WaitForSeconds(duration);
        stats.DamageBlocked -= OnBlocked;
    }
}
