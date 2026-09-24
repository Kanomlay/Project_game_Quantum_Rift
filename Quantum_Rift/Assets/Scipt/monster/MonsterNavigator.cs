using System.Collections.Generic;
using UnityEngine;

// หาทางเดินให้มอนสเตอร์อ้อมเสา/กำแพง แทนการเดินตรงเข้าหาผู้เล่นแล้วติดกำแพงค้าง
// - ทางตรงโล่ง (ลากกล่องขนาดตัวไปถึงเป้าแล้วไม่ชนของแข็ง) → เดินตรงเหมือนเดิม
// - โดนบัง → A* บนตารางช่องละ CellSize ภายในขอบเขตห้อง แล้วเดินตามจุดเลี้ยว เห็นจุดถัดไปเมื่อไหร่ก็ตัดมุมไปเลย
// ของแข็ง = collider ที่ไม่ใช่ trigger และไม่ได้ติด Rigidbody ที่ขยับได้
// (กำแพง tilemap, กล่องกำแพง, ประตูห้องที่ปิดอยู่, ของประดับ) ตัวมอนสเตอร์/ผู้เล่นเป็น Dynamic จึงไม่นับ
public sealed class MonsterNavigator
{
    public const float CellSize = 0.625f;      // ครึ่งช่อง tile (tile ในแมพกว้าง 1.25)
    const float Clearance = 0.12f;             // เผื่อระยะห่างกำแพงตอนวางเส้นทาง ตัวจริงจะได้ไม่เฉี่ยวมุมเสาจนติด
    const float CastMargin = 0.06f;            // กล่องลากเช็คทางตรง/ตัดมุม ใหญ่กว่าตัวนิดนึง ตัดมุมแล้วตัวจริงไม่เกี่ยวมุมเสา
                                               // (กำแพงข้าง ๆ ที่ตัวแนบอยู่จะทับกล่องตั้งแต่เริ่ม ระยะ 0 จึงไม่นับ เดินเลียบกำแพงได้)
    const float CastBackoff = 0.1f;            // เริ่มลากถอยหลังนิดนึง กำแพงที่แนบตัวอยู่ข้างหน้าจะได้ถูกนับ
    const float StrictTime = 1.5f;             // หลังติด: เดินตามจุดในตารางเป๊ะ ๆ ไม่ตัดมุม
    const float RepathInterval = 0.5f;
    const float DirectCheckInterval = 0.15f;
    const float StuckCheckInterval = 0.35f;
    const int MaxExpanded = 2500;
    const float CacheLifetime = 1f;            // ประตูห้องเปิด/ปิดได้ ล้างแคชช่องเดินได้ทุกวินาที
    const int EnemyLayer = 8;

    readonly Transform self;
    readonly MonsterController owner;
    readonly Collider2D bodyCollider;
    readonly Vector2 gridSize, castSize;
    static PhysicsMaterial2D slippery;

    readonly List<Vector2> path = new List<Vector2>();
    int pathIndex;
    float nextRepath, nextDirectCheck, lastCall, stuckCheckAt, strictUntil;
    bool directClear;
    Vector2 lastGoal, stuckFrom;

    public MonsterNavigator(Transform self, MonsterController owner)
    {
        this.self = self;
        this.owner = owner;
        foreach (var col in self.GetComponents<Collider2D>())
            if (!col.isTrigger) { bodyCollider = col; break; }
        Vector2 size = bodyCollider != null ? (Vector2)bodyCollider.bounds.size : Vector2.one * 0.5f;
        gridSize = size + Vector2.one * Clearance;
        castSize = size + Vector2.one * CastMargin;

        // collider ไม่มี material = แรงเสียดทานค่าเริ่มต้น ดันเฉียงเข้ากำแพงแล้วหนืดติด ใส่แบบลื่นให้ไถลเลียบกำแพงได้
        if (bodyCollider != null && bodyCollider.sharedMaterial == null)
        {
            if (slippery == null) slippery = new PhysicsMaterial2D("MonsterSlide") { friction = 0f, bounciness = 0f };
            bodyCollider.sharedMaterial = slippery;
        }
    }

    // กลาง collider เทียบจุด pivot (ตัวละครหลายตัว pivot อยู่ที่เท้า)
    Vector2 CenterOffset => bodyCollider != null && bodyCollider.enabled
        ? (Vector2)bodyCollider.bounds.center - (Vector2)self.position : Vector2.zero;

    // ทิศที่ควรเดิน (หน่วยเวกเตอร์) เพื่อไปถึง target ซึ่งเป็นตำแหน่ง pivot ปลายทาง
    public Vector2 DirectionTo(Vector2 target)
    {
        Vector2 offset = CenterOffset;
        Vector2 from = (Vector2)self.position + offset;
        Vector2 goal = target + offset;
        Vector2 toGoal = goal - from;
        if (toGoal.sqrMagnitude < 0.0001f) return Vector2.zero;

        bool stuck = CheckStuck(from);
        if (stuck)
        {
            // เดินตรงแล้วติดขอบ (มุมกำแพงเกี่ยวตัว) บังคับใช้เส้นทางสักพัก
            directClear = false;
            nextDirectCheck = Time.time + StrictTime;
            strictUntil = Time.time + StrictTime;
        }
        else if (Time.time >= nextDirectCheck)
        {
            nextDirectCheck = Time.time + DirectCheckInterval;
            directClear = BoxClear(from, goal);
        }
        if (directClear)
        {
            path.Clear();
            return Slide(from, toGoal.normalized);
        }

        if (stuck || path.Count == 0 || Time.time >= nextRepath || (goal - lastGoal).sqrMagnitude > 1f)
            Repath(from, goal);

        float reached = CellSize * 0.5f;
        while (pathIndex < path.Count && (path[pathIndex] - from).sqrMagnitude < reached * reached) pathIndex++;
        // ตัดมุม: มองเห็นจุดถัดไปแล้วไม่ต้องเดินไปแตะจุดนี้ (เพิ่งติดมาใหม่ ๆ ไม่ตัด)
        if (Time.time >= strictUntil && pathIndex + 1 < path.Count && BoxClear(from, path[pathIndex + 1])) pathIndex++;
        if (pathIndex >= path.Count) return Slide(from, toGoal.normalized);
        return Slide(from, (path[pathIndex] - from).normalized);
    }

    // ข้างหน้าชนกำแพงพอดี: ตัดส่วนที่ดันเข้ากำแพงทิ้ง เหลือแต่ทิศเลียบกำแพง ไม่ยืนดันมุมค้าง
    Vector2 Slide(Vector2 from, Vector2 direction)
    {
        const float look = 0.2f;
        castBuffer.Clear();
        Physics2D.BoxCast(from - direction * CastBackoff, castSize, 0f, direction, Filter, castBuffer, look + CastBackoff);
        foreach (var hit in castBuffer)
        {
            if (hit.distance <= 0.0001f || !IsSolid(hit.collider)) continue;
            float into = Vector2.Dot(direction, hit.normal);
            if (into >= 0f) continue;
            Vector2 along = direction - hit.normal * into;
            if (along.sqrMagnitude < 0.01f) along = new Vector2(-hit.normal.y, hit.normal.x); // ชนตั้งฉากพอดี เลือกเลี้ยวไปข้างหนึ่ง
            return along.normalized;
        }
        return direction;
    }

    public void DrawGizmos()
    {
        if (path.Count == 0) return;
        Gizmos.color = Color.yellow;
        Vector2 prev = (Vector2)self.position + CenterOffset;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Gizmos.DrawLine(prev, path[i]);
            Gizmos.DrawWireSphere(path[i], 0.08f);
            prev = path[i];
        }
    }

    // นับว่าติดเมื่อพยายามเดินต่อเนื่องแล้วตัวแทบไม่ขยับ (หยุดเดินไปโจมตีแล้วกลับมาเดินไม่นับ)
    bool CheckStuck(Vector2 from)
    {
        bool resumed = Time.time - lastCall > 0.3f;
        lastCall = Time.time;
        if (resumed)
        {
            stuckFrom = from;
            stuckCheckAt = Time.time + StuckCheckInterval;
            return false;
        }
        if (Time.time < stuckCheckAt) return false;
        bool stuck = (from - stuckFrom).sqrMagnitude < 0.12f * 0.12f; // เดินปกติ 0.35 วิ ได้ราว 0.5 หน่วย
        stuckFrom = from;
        stuckCheckAt = Time.time + StuckCheckInterval;
        return stuck;
    }

    // ---------- เช็คของแข็ง ----------

    static ContactFilter2D filter;
    static bool filterReady;
    static readonly List<Collider2D> overlapBuffer = new List<Collider2D>();
    static readonly List<RaycastHit2D> castBuffer = new List<RaycastHit2D>();

    static ContactFilter2D Filter
    {
        get
        {
            if (!filterReady)
            {
                filter = new ContactFilter2D { useTriggers = false };
                filter.SetLayerMask(Physics2D.AllLayers & ~(1 << EnemyLayer) & ~(1 << 2)); // 2 = Ignore Raycast
                filterReady = true;
            }
            return filter;
        }
    }

    static bool IsSolid(Collider2D col)
    {
        if (col == null || col.isTrigger) return false;
        var body = col.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    bool BoxClear(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return true;
        Vector2 direction = delta / distance;
        castBuffer.Clear();
        Physics2D.BoxCast(from - direction * CastBackoff, castSize, 0f, direction, Filter, castBuffer, distance + CastBackoff);
        foreach (var hit in castBuffer)
        {
            // ทับอยู่ตั้งแต่จุดเริ่ม (กำแพงข้างหลังที่ถอยไปโดน) ไม่นับ ส่วนกำแพงที่แนบอยู่ข้างหน้าจะถูกนับเพราะถอยเริ่มมาก่อนแล้ว
            if (hit.distance <= 0.0001f) continue;
            if (IsSolid(hit.collider)) return false;
        }
        return true;
    }

    // ช่องเดินได้ แชร์ระหว่างมอนสเตอร์ขนาดเดียวกัน ล้างทิ้งทุก CacheLifetime วินาที
    static readonly Dictionary<long, bool> walkCache = new Dictionary<long, bool>();
    static float cacheExpiresAt;

    bool Walkable(Vector2Int cell)
    {
        if (Time.time >= cacheExpiresAt)
        {
            walkCache.Clear();
            cacheExpiresAt = Time.time + CacheLifetime;
        }
        long sizeKey = Mathf.RoundToInt(gridSize.x * 20f) * 512L + Mathf.RoundToInt(gridSize.y * 20f);
        long key = (sizeKey << 42) ^ ((long)(cell.x + 0x100000) << 21) ^ (cell.y + 0x100000);
        if (walkCache.TryGetValue(key, out bool walkable)) return walkable;

        walkable = true;
        overlapBuffer.Clear();
        Physics2D.OverlapBox(CellCenter(cell), gridSize, 0f, Filter, overlapBuffer);
        foreach (var col in overlapBuffer)
            if (IsSolid(col)) { walkable = false; break; }
        walkCache[key] = walkable;
        return walkable;
    }

    static Vector2Int ToCell(Vector2 p) =>
        new Vector2Int(Mathf.RoundToInt(p.x / CellSize), Mathf.RoundToInt(p.y / CellSize));
    static Vector2 CellCenter(Vector2Int c) => new Vector2(c.x * CellSize, c.y * CellSize);

    // ขอบเขตค้นหา = trigger ของห้อง (ห้องหนึ่งมีกล่อง trigger หลายชิ้นตามรูปห้อง) ถ้าไม่มีห้องก็ใช้กรอบรอบตัวกับเป้า
    Rect SearchArea(Vector2 from, Vector2 goal)
    {
        Bounds area = new Bounds(from, Vector3.zero);
        area.Encapsulate(goal);
        bool hasRoom = false;
        if (owner != null && owner.currentRoom != null)
            foreach (var col in owner.currentRoom.GetComponents<Collider2D>())
                if (col.enabled) { area.Encapsulate(col.bounds); hasRoom = true; }
        area.Expand(hasRoom ? 3f : 16f);
        return new Rect(area.min, area.size);
    }

    // ---------- A* ----------

    static readonly Vector2Int[] Steps =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    static readonly List<Vector2Int> nodeCell = new List<Vector2Int>();
    static readonly List<float> nodeCost = new List<float>();
    static readonly List<int> nodeParent = new List<int>();
    static readonly List<bool> nodeClosed = new List<bool>();
    static readonly Dictionary<Vector2Int, int> nodeOf = new Dictionary<Vector2Int, int>();
    static readonly List<KeyValuePair<float, int>> open = new List<KeyValuePair<float, int>>();

    void Repath(Vector2 from, Vector2 goal)
    {
        nextRepath = Time.time + RepathInterval;
        lastGoal = goal;
        path.Clear();
        pathIndex = 0;

        Rect area = SearchArea(from, goal);
        Vector2Int start = ToCell(from), end = ToCell(goal);

        nodeCell.Clear(); nodeCost.Clear(); nodeParent.Clear(); nodeClosed.Clear(); nodeOf.Clear(); open.Clear();
        int first = AddNode(start, 0f, -1);
        Push(Heuristic(start, end), first);
        int best = first;
        float bestH = Heuristic(start, end);

        int expanded = 0;
        while (open.Count > 0 && expanded < MaxExpanded)
        {
            int current = Pop();
            if (nodeClosed[current]) continue;
            nodeClosed[current] = true;
            expanded++;

            Vector2Int cell = nodeCell[current];
            if (cell == end) { best = current; break; }
            float h = Heuristic(cell, end);
            if (h < bestH) { bestH = h; best = current; } // ไปไม่ถึงเป้า (เช่นผู้เล่นยืนชิดกำแพง) ก็ไปจุดที่ใกล้สุด

            foreach (var step in Steps)
            {
                Vector2Int next = cell + step;
                if (!area.Contains(CellCenter(next)) || !Walkable(next)) continue;
                bool diagonal = step.x != 0 && step.y != 0;
                // ห้ามเฉียงผ่านมุมกำแพง
                if (diagonal && (!Walkable(new Vector2Int(cell.x + step.x, cell.y)) || !Walkable(new Vector2Int(cell.x, cell.y + step.y))))
                    continue;

                float cost = nodeCost[current] + (diagonal ? 1.4142f : 1f);
                if (nodeOf.TryGetValue(next, out int index))
                {
                    if (nodeClosed[index] || cost >= nodeCost[index]) continue;
                    nodeCost[index] = cost;
                    nodeParent[index] = current;
                }
                else index = AddNode(next, cost, current);
                Push(cost + Heuristic(next, end), index);
            }
        }

        if (best == first) return; // ไม่มีทางไปต่อ DirectionTo จะเดินตรงแทน
        for (int i = best; i != first; i = nodeParent[i]) path.Add(CellCenter(nodeCell[i]));
        path.Reverse();
        if (nodeCell[best] == end) path[path.Count - 1] = goal;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
    }

    static int AddNode(Vector2Int cell, float cost, int parent)
    {
        nodeCell.Add(cell); nodeCost.Add(cost); nodeParent.Add(parent); nodeClosed.Add(false);
        nodeOf[cell] = nodeCell.Count - 1;
        return nodeCell.Count - 1;
    }

    // binary heap เรียงตามค่า f น้อยสุดก่อน
    static void Push(float f, int node)
    {
        open.Add(new KeyValuePair<float, int>(f, node));
        int i = open.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (open[parent].Key <= open[i].Key) break;
            (open[parent], open[i]) = (open[i], open[parent]);
            i = parent;
        }
    }

    static int Pop()
    {
        int top = open[0].Value;
        int last = open.Count - 1;
        open[0] = open[last];
        open.RemoveAt(last);
        int i = 0;
        while (true)
        {
            int left = i * 2 + 1, right = left + 1, smallest = i;
            if (left < open.Count && open[left].Key < open[smallest].Key) smallest = left;
            if (right < open.Count && open[right].Key < open[smallest].Key) smallest = right;
            if (smallest == i) break;
            (open[smallest], open[i]) = (open[i], open[smallest]);
            i = smallest;
        }
        return top;
    }
}
