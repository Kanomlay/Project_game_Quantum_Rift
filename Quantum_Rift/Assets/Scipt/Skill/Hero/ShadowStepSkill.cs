using UnityEngine;
using UnityEngine.Tilemaps;

// นักธนู E: ย่างก้าวเงา — หายตัวไปโผล่ทางเมาส์ในระยะสั้น อมตะช่วงสั้น ๆ
// ข้ามกำแพงภายในแมพได้ (เดินเงาทะลุผนังห้อง) แต่ห้ามไปโผล่นอกแมพหรือฝังในกำแพง:
// จุดโผล่ต้องมีพื้น (tilemap ชื่อ Floor ของแมพที่เล่นอยู่) และไม่ทับ collider ที่เป็นของแข็ง
// ถ้าจุดที่เล็งไว้ใช้ไม่ได้ จะถอยกลับหาจุดที่ใช้ได้ที่ใกล้เมาส์ที่สุดตามแนวเดิม
[CreateAssetMenu(fileName = "ShadowStep", menuName = "Game Data/Skills/Archer/Shadow Step")]
public class ShadowStepSkill : SkillData
{
    [Header("ย่างก้าว")]
    public float maxDistance = 4f;
    public float invincibleTime = 0.4f;
    public float bodyRadius = 0.3f;   // ขนาดตัวไว้เช็คว่าจุดโผล่ไม่ติดกำแพง/ของ
    public float searchStep = 0.2f;   // ระยะถอยทีละนิดตอนหาจุดโผล่ที่ใช้ได้

    public override void ActivateSkill(GameObject player)
    {
        Vector2 origin = player.transform.position; // เท้า: เงาวนอยู่บนพื้นตรงเท้า
        Vector2 toMouse = SkillCombat.MouseWorld() - SkillCombat.BodyCenter(player);
        Vector2 direction = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : Vector2.right;
        float distance = Mathf.Min(maxDistance, toMouse.magnitude);

        var floors = FloorTilemaps();
        Vector2 bodyOffset = SkillCombat.BodyCenter(player) - origin; // ตัวอยู่สูงกว่าเท้า เช็คชนที่ลำตัว
        Vector2 target = origin;
        for (float d = distance; d > 0.01f; d -= searchStep)
        {
            Vector2 candidate = origin + direction * d;
            if (CanStand(candidate, candidate + bodyOffset, player.transform, floors)) { target = candidate; break; }
        }

        SkillVfx.Spawn(effectFrames, origin, effectScale, 0f, 14f, effectOffset); // เงาหมุนวนตรงที่หายไป
        if (target == origin) return; // ไม่มีที่ให้โผล่เลย (เช่นยืนชิดขอบแมพแล้วเล็งออกนอกแมพ)

        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = target;
        player.transform.position = target;
        SkillVfx.Spawn(effectFrames, target, effectScale, 0f, 14f, effectOffset); // และตรงที่โผล่

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null) stats.GrantInvincibility(invincibleTime);
    }

    // พื้นของแมพที่กำลังเล่น (ผังที่ถูกปิดไม่นับ) ถ้าหาไม่เจอเลยจะไม่เช็คพื้น เช็คแค่ชนกำแพงอย่างเดียว
    private static Tilemap[] FloorTilemaps()
    {
        var map = MapManager.instance != null ? MapManager.instance.CurrentMapRoot : null;
        if (map == null) return new Tilemap[0];
        return System.Array.FindAll(map.GetComponentsInChildren<Tilemap>(false), t => t.name.Contains("Floor"));
    }

    private bool CanStand(Vector2 feet, Vector2 body, Transform player, Tilemap[] floors)
    {
        if (floors.Length > 0)
        {
            bool onFloor = false;
            foreach (var floor in floors)
                if (floor.HasTile(floor.WorldToCell(feet))) { onFloor = true; break; }
            if (!onFloor) return false; // นอกแมพ
        }

        foreach (var other in Physics2D.OverlapCircleAll(body, bodyRadius))
        {
            if (other.isTrigger || other.transform.IsChildOf(player)) continue;
            if (other.GetComponentInParent<MonsterController>() != null) continue; // ยืนทับศัตรูได้ เดี๋ยวก็ดันออกเอง
            return false; // กำแพง ประตูห้อง หรือของแข็ง
        }
        return true;
    }
}
