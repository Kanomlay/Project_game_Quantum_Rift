using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// กำแพงในห้องแบบสุ่มทุกครั้งที่เข้าแมพ (MapManager เรียก Spawn ก่อนสุ่มกล่อง)
// ห้องละ 1–2 ก้อน รูปทรงสุ่มจาก WallShapeLibrary (หัวใจ วงกลม ตัว L ขั้นบันได ฯลฯ) หมุน/สะท้อนได้ตามที่รูปอนุญาต
// รูปใหญ่ลงได้เฉพาะห้องใหญ่ ห้องต้องกว้างพอถึงมีก้อนที่สอง รอบก้อนมีพื้นว่าง 1 ช่องเสมอ (เดินอ้อมได้ ไม่ปิดทาง)
// เว้นประตู ประตูมิติ (รวมที่วางกล่องสมบัติหน้าประตู) กลางห้อง จุดเกิดผู้เล่น/มอน และห้องร้านค้า
// ระบาย tile ลงแผนที่กำแพงของผัง แล้วให้ RoomBreakableWalls ทำเป็นกำแพงที่ทุบได้ (ผู้เล่นตีได้ มอนของห้องทุบได้)
public sealed class RandomRoomWalls : MonoBehaviour
{
    public WallShapeLibrary shapes;
    public TileBase wallTile;
    public TileBase accentTile;                                   // แซมบางช่อง (ไฟบนผนังยาน / รากเรืองแสงในป่า)
    [Range(0f, 1f)] public float accentChance = 0.18f;

    [Header("จำนวนและขนาด")]
    public Vector2Int shapesPerRoom = new Vector2Int(1, 2);
    public int secondShapeMinCells = 170; // ห้องต้องมีพื้นที่วางได้อย่างน้อยกี่ช่องถึงจะสุ่มก้อนที่สอง
    public int bigShape = 14;             // รูปที่มีตั้งแต่กี่ช่องนับเป็นรูปใหญ่ (หัวใจ 16)
    public int bigShapeMinCells = 120;    // รูปใหญ่ลงได้เฉพาะห้องที่มีพื้นที่วางได้อย่างน้อยเท่านี้
    [Min(1)] public int gap = 3;          // ก้อนในห้องเดียวกันห่างกันอย่างน้อยกี่ช่อง
    public RoomGrid.Clearance clearance = new RoomGrid.Clearance(3f, 4.2f, 2.3f, 4f, 1.2f);

    [Header("กำแพงที่ได้")]
    [Min(1f)] public float cellHealth = 30f;
    public Vector2Int manaPerCell = new Vector2Int(1, 2);
    public Color debrisColor = new Color(0.5f, 0.6f, 0.75f);

    const int AnchorTries = 60;
    const int ShapeTries = 4;

    public int PlacedShapes { get; private set; }
    public int PlacedCells { get; private set; }

    public void Spawn(Vector2 playerSpawn)
    {
        if (shapes == null || shapes.shapes == null || wallTile == null) return;
        var pool = new List<(WallShapeLibrary.Shape shape, List<List<Vector2Int>> variants, int size)>();
        foreach (var shape in shapes.shapes)
        {
            if (shape == null || shape.weight <= 0f) continue;
            var variants = shape.Variants();
            if (variants.Count > 0) pool.Add((shape, variants, variants[0].Count));
        }
        if (pool.Count == 0) return;

        var rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        var portals = new List<Vector2>();
        foreach (var portal in GetComponentsInChildren<MapPortal>(true)) portals.Add(portal.transform.position);
        foreach (var room in GetComponentsInChildren<RoomController>(false))
            if (!room.IsSafeRoom) FillRoom(room, portals.ToArray(), playerSpawn, pool, rng);
    }

    void FillRoom(RoomController room, Vector2[] portals, Vector2 playerSpawn,
                  List<(WallShapeLibrary.Shape shape, List<List<Vector2Int>> variants, int size)> pool, System.Random rng)
    {
        var grid = RoomGrid.For(room, portals);
        if (grid == null || grid.Walls == null) return;

        var allowed = new List<Vector3Int>();
        foreach (var cell in grid.AllCells()) if (grid.Allowed(cell, clearance, playerSpawn)) allowed.Add(cell);
        if (allowed.Count == 0) return;
        var allowedSet = new HashSet<Vector3Int>(allowed);

        int want = allowed.Count >= secondShapeMinCells ? rng.Next(shapesPerRoom.x, shapesPerRoom.y + 1) : shapesPerRoom.x;
        var placed = new List<List<Vector3Int>>();
        for (int n = 0; n < want; n++)
            for (int s = 0; s < ShapeTries; s++)
            {
                var pick = Pick(pool, allowed.Count, rng);
                if (pick.variants == null) break;
                var variant = pick.variants[rng.Next(pick.variants.Count)];
                var cells = TryPlace(grid, variant, allowed, allowedSet, placed, rng);
                if (cells == null) continue;
                placed.Add(cells);
                grid.Taken.UnionWith(cells);
                break;
            }
        if (placed.Count == 0) return;

        var all = new List<Vector3Int>();
        foreach (var cells in placed)
            foreach (var cell in cells)
            {
                grid.Walls.SetTile(cell, accentTile != null && rng.NextDouble() < accentChance ? accentTile : wallTile);
                all.Add(cell);
            }
        var walls = room.GetComponent<RoomBreakableWalls>();
        if (walls == null)
        {
            walls = room.gameObject.AddComponent<RoomBreakableWalls>();
            walls.cellHealth = cellHealth;
            walls.manaPerCell = manaPerCell;
            walls.debrisColor = debrisColor;
        }
        walls.AddCells(grid.Walls, all);
        PlacedShapes += placed.Count;
        PlacedCells += all.Count;
    }

    // สุ่มรูปตามน้ำหนัก ห้องเล็กไม่เอารูปใหญ่
    (WallShapeLibrary.Shape shape, List<List<Vector2Int>> variants, int size) Pick(
        List<(WallShapeLibrary.Shape shape, List<List<Vector2Int>> variants, int size)> pool, int roomCells, System.Random rng)
    {
        float total = 0f;
        foreach (var item in pool) if (item.size < bigShape || roomCells >= bigShapeMinCells) total += item.shape.weight;
        if (total <= 0f) return default;
        float roll = (float)rng.NextDouble() * total;
        foreach (var item in pool)
        {
            if (item.size >= bigShape && roomCells < bigShapeMinCells) continue;
            roll -= item.shape.weight;
            if (roll <= 0f) return item;
        }
        return default;
    }

    List<Vector3Int> TryPlace(RoomGrid grid, List<Vector2Int> variant, List<Vector3Int> allowed, HashSet<Vector3Int> allowedSet,
                              List<List<Vector3Int>> placed, System.Random rng)
    {
        for (int t = 0; t < AnchorTries; t++)
        {
            var anchor = allowed[rng.Next(allowed.Count)];
            var cells = new List<Vector3Int>(variant.Count);
            bool fits = true;
            foreach (var offset in variant)
            {
                var cell = anchor + new Vector3Int(offset.x, offset.y, 0);
                if (!allowedSet.Contains(cell) || !grid.Free(cell)) { fits = false; break; }
                cells.Add(cell);
            }
            if (!fits || !RoomGrid.Apart(cells, placed, gap) || !grid.RingClear(cells)) continue;
            return cells;
        }
        return null;
    }
}
