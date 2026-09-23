using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// นักรบ Q: พุ่งชนกระแทก — พุ่งไปทางเมาส์ อมตะระหว่างพุ่ง ศัตรูที่อยู่ในทางโดนดาเมจและกระเด็น
[CreateAssetMenu(fileName = "ImpactDash", menuName = "Game Data/Skills/Warrior/Impact Dash")]
public class ImpactDashSkill : SkillData
{
    [Header("พุ่งชน")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.25f; // ระยะพุ่ง = ความเร็ว × เวลา (ราว 5.5 หน่วย)
    public float damage = 6f;
    public float hitRadius = 0.9f;

    public override void ActivateSkill(GameObject player)
    {
        var movement = player.GetComponent<PlayerMovement>();
        var stats = player.GetComponent<PlayerStats>();
        if (movement == null || stats == null) return;

        Vector2 body = SkillCombat.BodyCenter(player);
        Vector2 direction = SkillCombat.AimFrom(body);
        movement.StartDash(direction, dashSpeed, dashDuration);
        stats.GrantInvincibility(dashDuration + 0.1f);

        // เอฟเฟกต์กรวยพลังงานหันตามทิศพุ่ง เล่นจบพอดีช่วงพุ่ง + แตกกระจายหลังชน
        float fps = effectFrames.Length / (dashDuration + 0.2f);
        SkillVfx.Spawn(effectFrames, body, effectScale, SkillCombat.Angle(direction), fps, effectOffset)
                .Follow(player.transform);

        stats.StartCoroutine(HitAlongDash(player));
    }

    private IEnumerator HitAlongDash(GameObject player)
    {
        var hit = new HashSet<Component>();
        for (float t = 0f; t < dashDuration; t += Time.deltaTime)
        {
            SkillCombat.DamageArea(SkillCombat.BodyCenter(player), hitRadius, damage, hit);
            yield return null;
        }
    }
}
