using System;
using System.Collections.Generic;
using UnityEngine;

// กับดักหนามฝังพื้น สุ่มครั้งเดียวตอนเข้าห้องครั้งแรก จุดเดิมคงอยู่เมื่อย้อนกลับมา ไม่สุ่มไล่ตามผู้เล่น
// ห้องละ minCount–count จุด แต่ละจุดสุ่มเป็น 1 อัน, แนว 2 หรือ 3 อัน (แนวนอน/ตั้ง) หรือสี่เหลี่ยม 2×2
// รวมทั้งห้องไม่เกิน maxCells ช่อง ในจุดเดียวกันพุ่งพร้อมกัน (อ่านจังหวะได้) คนละจุดเริ่มรอบเหลื่อมกันไม่เกิน maxDesync วินาที
// รอบจุดต้องเป็นพื้นว่าง 1 ช่อง = เดินอ้อมได้เสมอ ไม่ขวางทางแคบ/หน้าประตู (กติกาตารางเดียวกับกำแพง/กล่องสุ่ม ดู RoomGrid)
public sealed class RoomEntryTrapSpawner : MonoBehaviour
{
    public Sprite[] frames;
    public int count = 3;                 // จำนวนจุดสูงสุดต่อห้อง
    public int minCount = 2;
    [Min(1)] public int maxCells = 6;     // รวมทุกจุดไม่เกินกี่ช่อง (หนามพุ่งพร้อมกันเยอะไปจะเดินไม่ได้)
    [Range(0f, 9f)] public float maxDesync = 6f;
    public RoomGrid.Clearance clearance = new RoomGrid.Clearance(3f, 3f, 2.3f, 3f, 0f);

    static readonly Vector2Int[][] Shapes =
    {
        new[] { new Vector2Int(0, 0) },                                                             // อันเดียว
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },                                       // แนว 2
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },                 // แนว 3
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // 2×2
    };
    static readonly float[] Weights = { 3f, 2f, 2f, 1f };
    const int Gap = 3;     // จุดห่างกันอย่างน้อย 3 ช่อง (ราว 2.5 หน่วยเท่าระยะเดิม)
    const int Tries = 80;

    public bool HasRolled { get; private set; }
    public readonly List<FixedSpikeTrap> Spawned = new List<FixedSpikeTrap>();

    public void Activate() { ActivateWithSeed(Guid.NewGuid().GetHashCode()); }

    public void ActivateWithSeed(int seed)
    {
        if (HasRolled) return;
        HasRolled = true;
        var room = GetComponent<RoomController>();
        if (room == null || room.IsSafeRoom || frames == null || frames.Length != 7) return;
        var map = GetComponentInParent<MapLayoutRandomizer>();
        if (map == null) return;
        // ประตูวาร์ปยังซ่อนก่อนเคลียร์ก็ต้องกันพื้นที่ไว้ รวมทุกผังได้เพราะพิกัดปลอดภัยสำคัญกว่าเพิ่มจุดสุ่ม
        var portals = new List<Vector2>();
        foreach (var portal in map.GetComponentsInChildren<MapPortal>(true)) portals.Add(portal.transform.position);
        var grid = RoomGrid.For(room, portals.ToArray());
        if (grid == null) return;
        var hero = GameObject.FindGameObjectWithTag("Player");
        Vector2 player = hero != null ? (Vector2)hero.transform.position : Vector2.one * 1e6f;
        var rng = new System.Random(seed);

        var candidates = new List<Vector3Int>();
        foreach (var cell in grid.AllCells()) if (grid.Allowed(cell, clearance, player)) candidates.Add(cell);
        if (candidates.Count == 0) return;

        var placed = new List<List<Vector3Int>>();
        int want = rng.Next(Mathf.Min(minCount, count), count + 1);
        int used = 0;
        for (int t = 0; t < Tries && placed.Count < want && used < maxCells; t++)
        {
            var shape = PickShape(rng, maxCells - used);
            if (shape == null) break;
            bool upright = rng.Next(2) == 0; // แนวตั้งแทนแนวนอน
            var anchor = candidates[rng.Next(candidates.Count)];
            var group = new List<Vector3Int>(shape.Length);
            bool fits = true;
            foreach (var o in shape)
            {
                var cell = anchor + (upright ? new Vector3Int(o.y, o.x, 0) : new Vector3Int(o.x, o.y, 0));
                if (!grid.Allowed(cell, clearance, player)) { fits = false; break; }
                group.Add(cell);
            }
            if (!fits || !RoomGrid.Apart(group, placed, Gap) || !grid.RingClear(group)) continue;
            placed.Add(group);
            grid.Taken.UnionWith(group);
            used += group.Count;
        }
        if (placed.Count == 0) return;

        var holder = new GameObject("RoomEntrySquareTraps");
        holder.transform.SetParent(transform, false);
        foreach (var group in placed)
        {
            float offset = (float)rng.NextDouble() * maxDesync; // ทั้งจุดใช้จังหวะเดียวกัน
            foreach (var cell in group) SpawnTrap(holder.transform, room, grid, cell, offset);
        }
    }

    Vector2Int[] PickShape(System.Random rng, int room)
    {
        float total = 0f;
        for (int i = 0; i < Shapes.Length; i++) if (Shapes[i].Length <= room) total += Weights[i];
        if (total <= 0f) return null;
        float roll = (float)rng.NextDouble() * total;
        for (int i = 0; i < Shapes.Length; i++)
        {
            if (Shapes[i].Length > room) continue;
            roll -= Weights[i];
            if (roll <= 0f) return Shapes[i];
        }
        return Shapes[0];
    }

    void SpawnTrap(Transform parent, RoomController room, RoomGrid grid, Vector3Int cell, float offset)
    {
        var go = new GameObject("SquareTrap_" + cell.x + "_" + cell.y);
        go.transform.SetParent(parent, false);
        go.transform.position = grid.CenterOf(cell);
        var display = go.AddComponent<SpriteRenderer>();
        display.sprite = frames[0];
        go.transform.localScale = Vector3.one * (grid.CellSize.x / frames[0].bounds.size.x);
        var area = go.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = frames[0].bounds.size;
        var trap = go.AddComponent<FixedSpikeTrap>();
        trap.room = room;
        trap.animatedDisplay = display;
        trap.animationFrames = frames;
        trap.damageArea = area;
        trap.cycleSeconds = 10;
        trap.warningSeconds = 1.2f;
        trap.Restart(offset); // ตั้งภาพ/ชั้นการวาดทันที (FixedSpikeTrap บังคับชั้นให้อยู่ใต้เท้าผู้เล่นเอง)
        Spawned.Add(trap);
    }
}
