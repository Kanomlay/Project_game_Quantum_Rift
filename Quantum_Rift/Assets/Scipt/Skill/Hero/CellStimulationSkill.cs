using System.Collections;
using UnityEngine;

// มนุษย์กลายพันธุ์ E: กระตุ้นเซลล์ — ออร่าฟื้นฟูรอบตัว ค่อย ๆ ฟื้นพลังชีวิต และเร่งความเร็วเคลื่อนที่ชั่วคราว
[CreateAssetMenu(fileName = "CellStimulation", menuName = "Game Data/Skills/Mutant/Cell Stimulation")]
public class CellStimulationSkill : SkillData
{
    [Header("ฟื้นฟู")]
    public float healTotal = 3f;       // เท่าขวดยาฟื้นพลังชีวิต 1 ขวด (1.3.10)
    public float healDuration = 3f;
    public float speedMultiplier = 1.3f;
    public float speedDuration = 5f;

    public override void ActivateSkill(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.BoostSpeed(speedMultiplier, speedDuration);

        // เฟรม 1–3 วงก่อตัว, วนเฟรม 4–5 (วงเต็ม/แสงเซลล์) ตลอดช่วงฟื้นฟู, เฟรม 6–7 ลอยขึ้นจางหาย
        SkillVfx.Spawn(effectFrames, SkillCombat.BodyCenter(player), effectScale, 0f, 14f, effectOffset)
                .Loop(3, 4, healDuration)
                .Follow(player.transform);

        stats.StartCoroutine(Regenerate(stats));
    }

    private IEnumerator Regenerate(PlayerStats stats)
    {
        const float tick = 0.5f;
        int ticks = Mathf.Max(1, Mathf.RoundToInt(healDuration / tick));
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(tick);
            if (stats.isDead) yield break;
            stats.Heal(healTotal / ticks);
        }
    }
}
