using UnityEngine;

// นักธนู Q: ลูกศรทะลวงมิติ — ยิงลูกศรพลังงานไปทางเมาส์ ทะลุศัตรูทุกตัวและทะลุกำแพง (ทะลวงมิติ)
[CreateAssetMenu(fileName = "DimensionalArrow", menuName = "Game Data/Skills/Archer/Dimensional Arrow")]
public class DimensionalArrowSkill : SkillData
{
    [Header("ลูกศร")]
    public float damage = 6f;
    public float speed = 16f;
    public float range = 14f;
    public float hitRadius = 0.35f;

    public override void ActivateSkill(GameObject player)
    {
        Vector2 origin = SkillCombat.BodyCenter(player);
        Vector2 direction = SkillCombat.AimFrom(origin);

        // เฟรม 1–3 ชาร์จที่ตัวผู้เล่น, เฟรม 4–5 ลูกศรเต็มตัววนระหว่างบิน, เฟรม 6–7 สลายตอนสุดระยะ
        SkillVfx.Spawn(Frames(0, 2), origin + direction * 0.6f, effectScale, SkillCombat.Angle(direction), 20f, effectOffset);
        PiercingProjectile.Spawn(origin + direction * 0.6f, direction, speed, range / Mathf.Max(0.01f, speed),
                                 damage, hitRadius, Frames(3, 4), Frames(5, 6), effectScale, effectOffset);
    }

    private Sprite[] Frames(int from, int to)
    {
        var picked = new Sprite[to - from + 1];
        for (int i = from; i <= to; i++) picked[i - from] = effectFrames[i];
        return picked;
    }
}
