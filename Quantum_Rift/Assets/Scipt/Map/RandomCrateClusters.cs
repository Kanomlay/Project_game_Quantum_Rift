using System.Collections.Generic;
using UnityEngine;

// กล่องทำลายได้แบบสุ่มใหม่ทุกครั้งที่เข้าแมพ (MapManager เรียก Spawn หลังสุ่มกำแพง)
// ห้องละ 2–3 กอง กองละ 3–5 กล่องติดกัน 1 กล่อง = 1 ช่องตารางของแมพพอดี (ขนาด/ตำแหน่งคิดจากช่องของ Floor tilemap)
// รอบกองต้องมีพื้นว่างอย่างน้อย 1 ช่อง ไม่ติดกำแพงหรือกองอื่น จึงไม่ปิดทางเดิน (ทุบไม่ทันก็เดินอ้อมได้เสมอ)
// เลี่ยงหน้าประตู จุดเกิดผู้เล่น/มอน ประตูมิติ กลางห้อง (ที่วางกล่องสมบัติ) ห้องร้านค้า และของที่มี collider อยู่แล้ว
public sealed class RandomCrateClusters : MonoBehaviour
{
    public GameObject cratePrefab;                            // BreakableWall ตามธีมแมพ
    public Vector2Int clustersPerRoom = new Vector2Int(2, 3);
    public Vector2Int cratesPerCluster = new Vector2Int(3, 5);
    public RoomGrid.Clearance clearance = new RoomGrid.Clearance(2.5f, 3f, 2f, 3.5f, 1.2f);

    const int SeedTries = 60;
    const int ClusterGap = 3; // กองในห้องเดียวกันห่างกันอย่างน้อยกี่ช่อง
    static readonly Vector3Int[] Sides = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

    public int Spawned { get; private set; }

    public void Spawn(Vector2 playerSpawn)
    {
        if (cratePrefab == null) return;
        var rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        var portals = new List<Vector2>();
        foreach (var portal in GetComponentsInChildren<MapPortal>(true)) portals.Add(portal.transform.position);
        foreach (var room in GetComponentsInChildren<RoomController>(false))
            if (!room.IsSafeRoom) FillRoom(room, portals.ToArray(), playerSpawn, rng);
    }

    void FillRoom(RoomController room, Vector2[] portals, Vector2 playerSpawn, System.Random rng)
    {
        var grid = RoomGrid.For(room, portals);
        if (grid == null) return;
        var candidates = new List<Vector3Int>();
        foreach (var cell in grid.AllCells()) if (grid.Allowed(cell, clearance, playerSpawn)) candidates.Add(cell);
        if (candidates.Count == 0) return;

        var placed = new List<List<Vector3Int>>();
        int want = rng.Next(clustersPerRoom.x, clustersPerRoom.y + 1);
        for (int tries = 0; tries < SeedTries && placed.Count < want; tries++)
        {
            var seed = candidates[rng.Next(candidates.Count)];
            if (!RoomGrid.Apart(new[] { seed }, placed, ClusterGap)) continue;
            var cluster = Grow(seed, rng.Next(cratesPerCluster.x, cratesPerCluster.y + 1), c => grid.Allowed(c, clearance, playerSpawn), rng);
            if (cluster == null || !RoomGrid.Apart(cluster, placed, ClusterGap) || !grid.RingClear(cluster)) continue;
            placed.Add(cluster);
            grid.Taken.UnionWith(cluster);
        }

        if (placed.Count == 0) return;
        var holder = new GameObject("RandomCrates").transform;
        holder.SetParent(room.transform, false);
        holder.position = Vector3.zero;
        holder.localScale = Vector3.one;
        foreach (var cluster in placed)
            foreach (var cell in cluster)
                PlaceCrate(holder, grid.CenterOf(cell), grid.CellSize);
    }

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
