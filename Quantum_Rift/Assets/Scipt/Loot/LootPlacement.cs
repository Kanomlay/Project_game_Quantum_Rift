using UnityEngine;

// หาที่วางกล่อง/จุดตกของที่ไม่จมกำแพง ไม่ลอยนอกห้อง
// ของแข็ง = collider ที่ไม่ใช่ trigger และไม่ได้ติด Rigidbody ที่ขยับได้ (กำแพง เสา ประตูห้อง กล่องอื่น)
public static class LootPlacement
{
    const float PortalClearance = 2.5f; // กล่องห่างประตูมิติ (ประตูอยู่กลางห้องสุดท้าย) จะได้ไม่บังกัน
    const float PlayerClearance = 1.4f; // ไม่เสกกล่องทับตัวผู้เล่น
    static readonly Vector2 ChestFootprint = new Vector2(1.5f, 1.2f);
    const float ChestFootprintLift = 0.5f; // root กล่องอยู่ที่พื้น ตัวกล่องสูงขึ้นไปราวหนึ่งหน่วย

    public static bool IsSolid(Collider2D col)
    {
        if (col == null || col.isTrigger) return false;
        var body = col.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    static bool Ignored(Collider2D col, Transform ignore) => ignore != null && col.transform.IsChildOf(ignore);

    static bool Blocked(Vector2 center, Vector2 size, Transform ignore)
    {
        foreach (var col in Physics2D.OverlapBoxAll(center, size, 0f))
            if (IsSolid(col) && !Ignored(col, ignore)) return true;
        return false;
    }

    static bool LineClear(Vector2 from, Vector2 to, Transform ignore)
    {
        foreach (var hit in Physics2D.LinecastAll(from, to))
            if (IsSolid(hit.collider) && !Ignored(hit.collider, ignore)) return false;
        return true;
    }

    // จุดตกของที่โยนจาก origin ไปทาง direction ไกลสุด distance ติดกำแพงก็ถอยเข้ามา
    public static Vector2 Landing(Vector2 origin, Vector2 direction, float distance, Transform ignore)
    {
        for (float d = distance; d >= 0.4f; d -= 0.2f)
        {
            Vector2 spot = origin + direction * d;
            if (!Blocked(spot, Vector2.one * 0.4f, ignore) && LineClear(origin, spot, ignore)) return spot;
        }
        return origin + direction * 0.4f;
    }

    // ที่วางกล่องในห้อง: เริ่มจากกลางห้องแล้ววนออกเป็นวง หาจุดที่อยู่ในห้อง ไม่ติดของแข็ง ไม่ทับประตูมิติ/ผู้เล่น
    public static Vector2 ChestSpot(RoomController room, Vector2 playerPosition)
    {
        var areas = room.GetComponents<Collider2D>();
        Bounds bounds = new Bounds(room.transform.position, Vector3.zero);
        bool first = true;
        foreach (var area in areas)
        {
            if (!area.enabled || !area.isTrigger) continue;
            if (first) { bounds = area.bounds; first = false; }
            else bounds.Encapsulate(area.bounds);
        }
        Vector2 center = bounds.center;
        var portals = Object.FindObjectsByType<MapPortal>(FindObjectsSortMode.None);

        const float step = 0.625f;
        for (int ring = 0; ring <= 8; ring++)
        {
            int count = ring == 0 ? 1 : ring * 8;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                Vector2 spot = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring * step);
                if (Valid(spot, areas, portals, playerPosition)) return spot;
            }
        }
        return center;
    }

    static bool Valid(Vector2 spot, Collider2D[] areas, MapPortal[] portals, Vector2 playerPosition)
    {
        bool inside = false;
        foreach (var area in areas)
            if (area.enabled && area.isTrigger && area.OverlapPoint(spot)) { inside = true; break; }
        if (!inside) return false;
        if (Vector2.Distance(spot, playerPosition) < PlayerClearance) return false;
        foreach (var portal in portals)
            if (Vector2.Distance(spot, portal.transform.position) < PortalClearance) return false;
        return !Blocked(spot + Vector2.up * ChestFootprintLift, ChestFootprint, null);
    }
}
