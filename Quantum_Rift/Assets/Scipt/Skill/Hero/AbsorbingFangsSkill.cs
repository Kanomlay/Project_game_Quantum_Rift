using System.Collections;
using UnityEngine;

// มนุษย์กลายพันธุ์ Q: คมเขี้ยวดูดกลืน — ขากรรไกรพลังงานงับตรงหน้าทางเมาส์
// ทำดาเมจศัตรูทุกตัวในวง แล้วดูดพลังชีวิตกลับมาตามจำนวนตัวที่โดน
[CreateAssetMenu(fileName = "AbsorbingFangs", menuName = "Game Data/Skills/Mutant/Absorbing Fangs")]
public class AbsorbingFangsSkill : SkillData
{
    [Header("งับ")]
    public float reach = 1.3f;      // ระยะจากตัวผู้เล่นถึงกลางวงงับ
    public float biteRadius = 1f;
    public float damage = 8f;
    public float healPerHit = 1f;   // พลังชีวิตที่ได้คืนต่อศัตรูหนึ่งตัวที่โดน
    public float maxHeal = 2f;      // ต่อการงับหนึ่งครั้ง
    public int impactFrame = 3;     // เฟรม 4 ขากรรไกรหุบ = จังหวะโดน
    public float fps = 16f;

    public override void ActivateSkill(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        Vector2 origin = SkillCombat.BodyCenter(player);
        Vector2 direction = SkillCombat.AimFrom(origin);
        Vector2 center = origin + direction * reach;

        SkillVfx.Spawn(effectFrames, center, effectScale, SkillCombat.Angle(direction), fps, effectOffset);
        stats.StartCoroutine(Bite(stats, center));
    }

    private IEnumerator Bite(PlayerStats stats, Vector2 center)
    {
        yield return new WaitForSeconds(impactFrame / fps);
        int hits = SkillCombat.DamageArea(center, biteRadius, damage);
        if (hits > 0) stats.Heal(Mathf.Min(maxHeal, hits * healPerHit)); // เฟรม 5 เส้นพลังชีวิตไหลกลับเข้าตัว
    }
}
