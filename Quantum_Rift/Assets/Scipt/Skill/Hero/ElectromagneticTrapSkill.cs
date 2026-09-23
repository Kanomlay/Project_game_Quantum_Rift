using UnityEngine;

// นักประดิษฐ์ E: กับดักแม่เหล็กไฟฟ้า — วางกับดักตรงเมาส์ (ไม่เกินระยะวาง) ศัตรูเหยียบแล้วโดนดาเมจและสตัน
[CreateAssetMenu(fileName = "ElectromagneticTrap", menuName = "Game Data/Skills/Inventor/Electromagnetic Trap")]
public class ElectromagneticTrapSkill : SkillData
{
    [Header("กับดัก")]
    public float placeRange = 5f;
    public float armTime = 0.4f;     // เวลาเตรียมพร้อมหลังวาง
    public float lifetime = 12f;
    public float triggerRadius = 1.2f;
    public float blastRadius = 1.8f;
    public float damage = 5f;
    public float stunTime = 2f;

    public override void ActivateSkill(GameObject player)
    {
        Vector2 origin = player.transform.position;
        Vector2 toMouse = SkillCombat.MouseWorld() - origin;
        float distance = Mathf.Min(placeRange, toMouse.magnitude);
        Vector2 direction = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : Vector2.right;

        // ไม่วางทะลุกำแพงไปอีกฝั่ง
        foreach (var hit in Physics2D.RaycastAll(origin, direction, distance))
        {
            var other = hit.collider;
            if (other == null || other.isTrigger || other.transform.IsChildOf(player.transform)) continue;
            if (other.GetComponentInParent<MonsterController>() != null) continue;
            distance = Mathf.Max(0f, hit.distance - 0.3f);
            break;
        }

        ElectroTrap.Place(origin + direction * distance, effectFrames, effectScale,
                          armTime, lifetime, triggerRadius, blastRadius, damage, stunTime);
    }
}
