using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// กล่องทำลายได้แบบสุ่มใหม่ทุกครั้งที่เข้าแมพ (MapManager เรียก Spawn หลังวางผู้เล่น)
// ห้องละ 2–3 กอง กองละ 3–5 กล่องติดกัน 1 กล่อง = 1 ช่องตารางของแมพพอดี (ขนาด/ตำแหน่งคิดจากช่องของ Floor tilemap)
// รอบกองต้องมีพื้นว่างอย่างน้อย 1 ช่อง ไม่ติดกำแพงหรือกองอื่น จึงไม่ปิดทางเดิน (ทุบไม่ทันก็เดินอ้อมได้เสมอ)
// เลี่ยงหน้าประตู จุดเกิดผู้เล่น/มอน ประตูมิติ กลางห้อง (ที่วางกล่องสมบัติ) ห้องร้านค้า และของที่มี collider อยู่แล้ว
public sealed class RandomCrateClusters : MonoBehaviour
{
    public GameObject cratePrefab;                            // BreakableWall ตามธีมแมพ
    public Vector2Int clustersPerRoom = new Vector2Int(2, 3);
    public Vector2Int cratesPerCluster = new Vector2Int(3, 5);
    public float doorClearance = 2.5f;
    public float portalClearance = 3f;
    public float roomCenterClearance = 2f;
    public float playerClearance = 3.5f;
    public float spawnPointClearance = 1.2f;

    const int SeedTries = 60;
    const int ClusterGap = 3; // กองในห้องเดียวกันห่างกันอย่างน้อยกี่ช่อง
    static readonly Vector3Int[] Sides = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

    public int Spawned { get; private set; }

    public void Spawn(Vector2 playerSpawn)
    {
        if (cratePrefab == null) return;
        var rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        var portals = GetComponentsInChildren<MapPortal>(true).Select(p => (Vector2)p.transform.position).ToArray();
        foreach (var room in GetComponentsInChildren<RoomController>(false))
            if (!room.IsSafeRoom) FillRoom(room, playerSpawn, portals, rng);
    }

    void FillRoom(RoomController room, Vector2 playerSpawn, Vector2[] portals, System.Random rng)
    {
        var grid = room.GetComponentInParent<Grid>();
        if (grid == null) return;
        Tilemap floor = null, walls = null;
        foreach (var map in grid.GetComponentsInChildren<Tilemap>(false))
        {
            if (map.name.StartsWith("Floor")) floor = map;
            else if (map.name.StartsWith("Bulkheads")) walls = map;
        }
        var areas = room.GetComponents<Collider2D>().Where(a => a.enabled && a.isTrigger).ToArray();
        if (floor == null || areas.Length == 0) return;

        Bounds bounds = areas[0].bounds;
        foreach (var area in areas) bounds.Encapsulate(area.bounds);
        Vector3Int min = floor.WorldToCell(bounds.min), max = floor.WorldToCell(bounds.max);
        var doors = room.doors != null ? room.doors.Where(d => d != null).Select(d => (Vector2)d.transform.position).ToArray() : new Vector2[0];
        var spawnPoints = room.monsterSpawnPoints != null
            ? room.monsterSpawnPoints.Where(p => p != null).Select(p => (Vector2)p.position).ToArray() : new Vector2[0];
        Vector2 center = room.transform.position;

        var free = new Dictionary<Vector3Int, bool>();
        var taken = new HashSet<Vector3Int>();
        var placed = new List<List<Vector3Int>>();

        // พื้นว่างจริง: มีพื้น ไม่มีกำแพง ไม่มีของแข็งทับ (ของตกแต่ง/ร้าน/ประตู) และยังไม่มีกล่อง
        bool Free(Vector3Int cell)
        {
            if (taken.Contains(cell)) return false;
            if (free.TryGetValue(cell, out bool ok)) return ok;
            ok = floor.HasTile(cell) && (walls == null || !walls.HasTile(cell));
            if (ok)
            {
                Vector2 p = floor.GetCellCenterWorld(cell);
                foreach (var col in Physics2D.OverlapBoxAll(p, Vector2.one * 1.1f, 0f))
                    if (LootPlacement.IsSolid(col)) { ok = false; break; }
            }
            free[cell] = ok;
            return ok;
        }

        bool Far(Vector2 p, Vector2[] points, float distance)
        {
            foreach (var point in points) if (Vector2.Distance(p, point) < distance) return false;
            return true;
        }

        // ช่องที่วางกล่องได้: อยู่ในห้อง และเว้นระยะจากจุดสำคัญ
        bool Allowed(Vector3Int cell)
        {
            if (!Free(cell)) return false;
            Vector2 p = floor.GetCellCenterWorld(cell);
            if (!areas.Any(a => a.OverlapPoint(p))) return false;
            if (Vector2.Distance(p, center) < roomCenterClearance) return false;
            if (Vector2.Distance(p, playerSpawn) < playerClearance) return false;
            return Far(p, doors, doorClearance) && Far(p, portals, portalClearance) && Far(p, spawnPoints, spawnPointClearance);
        }

        var candidates = new List<Vector3Int>();
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (Allowed(cell)) candidates.Add(cell);
            }
        if (candidates.Count == 0) return;

        int want = rng.Next(clustersPerRoom.x, clustersPerRoom.y + 1);
        for (int tries = 0; tries < SeedTries && placed.Count < want; tries++)
        {
            var seed = candidates[rng.Next(candidates.Count)];
            if (placed.Any(other => other.Any(c => Chebyshev(c, seed) < ClusterGap))) continue;
            var cluster = Grow(seed, rng.Next(cratesPerCluster.x, cratesPerCluster.y + 1), Allowed, rng);
            if (cluster == null) continue;
            if (placed.Any(other => other.Any(c => cluster.Any(n => Chebyshev(c, n) < ClusterGap)))) continue;
            if (!RingClear(cluster, Free)) continue;

            placed.Add(cluster);
            foreach (var cell in cluster) taken.Add(cell);
        }

        if (placed.Count == 0) return;
        var holder = new GameObject("RandomCrates").transform;
        holder.SetParent(room.transform, false);
        holder.position = Vector3.zero;
        holder.localScale = Vector3.one;
        Vector2 cellSize = Vector2.Scale(floor.layoutGrid.cellSize, floor.transform.lossyScale);
        foreach (var cluster in placed)
            foreach (var cell in cluster)
                PlaceCrate(holder, floor.GetCellCenterWorld(cell), cellSize);
    }

    static int Chebyshev(Vector3Int a, Vector3Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    // กองติดกันแบบสุ่ม: เริ่มจากช่องเดียวแล้วต่อด้านข้างทีละช่องจนครบจำนวน
    static List<Vector3Int> Grow(Vector3Int seed, int size, System.Func<Vector3Int, bool> allowed, System.Random rng)
    {
        var cluster = new List<Vector3Int> { seed };
        var options = new List<Vector3Int>();
        while (cluster.Count < size)
        {
            options.Clear();
            foreach (var cell in cluster)
                foreach (var side in Sides)
                {
                    var next = cell + side;
                    if (!cluster.Contains(next) && !options.Contains(next) && allowed(next)) options.Add(next);
                }
            if (options.Count == 0) return null;
            cluster.Add(options[rng.Next(options.Count)]);
        }
        return cluster;
    }

    // รอบกอง (8 ทิศ) ต้องเป็นพื้นว่างทั้งหมด = เดินอ้อมรอบกองได้ ไม่ปิดทางใคร
    static bool RingClear(List<Vector3Int> cluster, System.Func<Vector3Int, bool> free)
    {
        foreach (var cell in cluster)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = cell + new Vector3Int(dx, dy, 0);
                    if (!cluster.Contains(n) && !free(n)) return false;
                }
        return true;
    }

    // กล่องกว้างเท่าช่องพอดี ฐานกล่องวางที่ขอบล่างของช่อง (ภาพกล่องมองเฉียง ด้านบนเลยสูงเกินช่องนิดหน่อย)
    // collider ทั้งแบบชนและแบบโดนตีครอบเต็มช่อง
    void PlaceCrate(Transform holder, Vector2 cellCenter, Vector2 cellSize)
    {
        var crate = Instantiate(cratePrefab, holder);
        var view = crate.GetComponent<SpriteRenderer>();
        if (view == null || view.sprite == null) { crate.transform.position = cellCenter; Spawned++; return; }

        Vector2 lo = Vector2.positiveInfinity, hi = Vector2.negativeInfinity;
        foreach (var v in view.sprite.vertices) { lo = Vector2.Min(lo, v); hi = Vector2.Max(hi, v); }
        float scale = cellSize.x / Mathf.Max(0.01f, hi.x - lo.x);
        crate.transform.localScale = Vector3.one * scale;
        crate.transform.position = new Vector3(cellCenter.x - (lo.x + hi.x) * 0.5f * scale,
                                               cellCenter.y - cellSize.y * 0.5f - lo.y * scale, 0f);

        Vector2 localCenter = crate.transform.InverseTransformPoint(cellCenter);
        foreach (var box in crate.GetComponents<BoxCollider2D>())
        {
            box.offset = localCenter;
            box.size = cellSize / scale * (box.isTrigger ? 1.05f : 1f);
        }
        Spawned++;
    }
}
