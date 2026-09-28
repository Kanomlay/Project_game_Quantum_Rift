using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ตารางพื้นของห้องหนึ่งห้อง (ช่องละ 1.25 ตาม Floor tilemap ของผัง) ใช้ร่วมกันโดยตัวสุ่มกำแพง กล่อง และกับดัก
// - Free: มีพื้น ไม่มีกำแพง ไม่มีของแข็งทับ และยังไม่ถูกจองในรอบสุ่มนี้
// - Allowed: Free + อยู่ในห้อง + ห่างประตู ประตูมิติ กลางห้อง ผู้เล่น จุดเกิดมอน ตามระยะที่ให้มา
// - RingClear: รอบกลุ่ม (8 ทิศ) เป็นพื้นว่างทั้งหมด = เดินอ้อมได้เสมอ ไม่ปิดทางใคร
public sealed class RoomGrid
{
    [System.Serializable]
    public struct Clearance
    {
        public float door, portal, center, player, spawnPoint;

        public Clearance(float door, float portal, float center, float player, float spawnPoint)
        {
            this.door = door; this.portal = portal; this.center = center; this.player = player; this.spawnPoint = spawnPoint;
        }
    }

    public readonly RoomController Room;
    public readonly Tilemap Floor, Walls;
    public readonly Vector2 CellSize;
    public readonly HashSet<Vector3Int> Taken = new HashSet<Vector3Int>();

    readonly Collider2D[] areas;
    readonly Vector2[] doors, spawnPoints, portals;
    readonly Vector2 center;
    readonly Vector3Int min, max;
    readonly Dictionary<Vector3Int, bool> freeCache = new Dictionary<Vector3Int, bool>();

    RoomGrid(RoomController room, Tilemap floor, Tilemap walls, Collider2D[] areas, Vector2[] portals)
    {
        Room = room;
        Floor = floor;
        Walls = walls;
        this.areas = areas;
        this.portals = portals ?? new Vector2[0];
        CellSize = Vector2.Scale(floor.layoutGrid.cellSize, floor.transform.lossyScale);
        center = room.transform.position;

        var doorList = new List<Vector2>();
        if (room.doors != null) foreach (var door in room.doors) if (door != null) doorList.Add(door.transform.position);
        doors = doorList.ToArray();
        var pointList = new List<Vector2>();
        if (room.monsterSpawnPoints != null) foreach (var point in room.monsterSpawnPoints) if (point != null) pointList.Add(point.position);
        spawnPoints = pointList.ToArray();

        Bounds bounds = areas[0].bounds;
        foreach (var area in areas) bounds.Encapsulate(area.bounds);
        min = floor.WorldToCell(bounds.min);
        max = floor.WorldToCell(bounds.max);
    }

    // ห้องที่ไม่มีพื้น/trigger ของห้อง (หรือไม่ได้อยู่ใต้ผังที่มี Grid) คืน null
    public static RoomGrid For(RoomController room, Vector2[] portals)
    {
        if (room == null) return null;
        var grid = room.GetComponentInParent<Grid>();
        if (grid == null) return null;
        Tilemap floor = null, walls = null;
        foreach (var map in grid.GetComponentsInChildren<Tilemap>(false))
        {
            if (map.name.StartsWith("Floor")) floor = map;
            else if (map.name.StartsWith("Bulkheads")) walls = map;
        }
        var areas = new List<Collider2D>();
        foreach (var col in room.GetComponents<Collider2D>()) if (col.enabled && col.isTrigger) areas.Add(col);
        if (floor == null || areas.Count == 0) return null;
        return new RoomGrid(room, floor, walls, areas.ToArray(), portals);
    }

    public Vector2 CenterOf(Vector3Int cell) => Floor.GetCellCenterWorld(cell);

    public IEnumerable<Vector3Int> AllCells()
    {
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                yield return new Vector3Int(x, y, 0);
    }

    public bool Free(Vector3Int cell)
    {
        if (Taken.Contains(cell)) return false;
        if (freeCache.TryGetValue(cell, out bool ok)) return ok;
        ok = Floor.HasTile(cell) && (Walls == null || !Walls.HasTile(cell));
        if (ok)
            foreach (var col in Physics2D.OverlapBoxAll(CenterOf(cell), CellSize * 0.88f, 0f))
                if (LootPlacement.IsSolid(col)) { ok = false; break; }
        freeCache[cell] = ok;
        return ok;
    }

    public bool Inside(Vector2 point)
    {
        foreach (var area in areas) if (area.OverlapPoint(point)) return true;
        return false;
    }

    public bool Allowed(Vector3Int cell, Clearance clearance, Vector2 player)
    {
        if (!Free(cell)) return false;
        Vector2 p = CenterOf(cell);
        if (!Inside(p)) return false;
        if (Vector2.Distance(p, center) < clearance.center) return false;
        if (Vector2.Distance(p, player) < clearance.player) return false;
        return Far(p, doors, clearance.door) && Far(p, portals, clearance.portal) && Far(p, spawnPoints, clearance.spawnPoint);
    }

    static bool Far(Vector2 p, Vector2[] points, float distance)
    {
        foreach (var point in points) if (Vector2.Distance(p, point) < distance) return false;
        return true;
    }

    // รอบกลุ่ม (8 ทิศ) ต้องเป็นพื้นว่างทั้งหมด
    public bool RingClear(ICollection<Vector3Int> group)
    {
        foreach (var cell in group)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = cell + new Vector3Int(dx, dy, 0);
                    if (!group.Contains(n) && !Free(n)) return false;
                }
        return true;
    }

    public static int Chebyshev(Vector3Int a, Vector3Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    // กลุ่มใหม่ห่างทุกกลุ่มที่วางไปแล้วอย่างน้อย gap ช่อง (วัดแบบนับช่องทแยงเป็น 1)
    public static bool Apart(ICollection<Vector3Int> group, List<List<Vector3Int>> placed, int gap)
    {
        foreach (var other in placed)
            foreach (var a in other)
                foreach (var b in group)
                    if (Chebyshev(a, b) < gap) return false;
        return true;
    }
}
