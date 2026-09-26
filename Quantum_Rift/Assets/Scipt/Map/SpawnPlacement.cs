using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// หาจุดเกิดมอนสเตอร์ในห้อง: สุ่มจากพื้นที่ห้องจริง (กล่อง trigger ของห้อง) แทนจุดเกิด 4 จุดที่กองอยู่กลางห้อง
// จุดที่ใช้ได้ต้องอยู่บนพื้นในห้องทั้งตัว ไม่จมกำแพง/ของ ห่างผู้เล่น ห่างประตู และห่างกันเอง
// สุ่มจุดที่ผ่านเงื่อนไขมาหลายจุด แล้วเลือกจุดที่ห่างจากตัวอื่นมากที่สุด มอนจึงกระจายรอบห้องไม่กองรวมกัน
// หาไม่ได้ (ห้องแคบ/ของเต็ม) จะผ่อนระยะลงทีละขั้น ถ้ายังไม่ได้อีกค่อยใช้จุดเกิดที่วางไว้ในห้อง
public static class SpawnPlacement
{
    // ขนาดตัวมอนสเตอร์จาก collider ของ prefab (ยังไม่ได้เสก จึงอ่าน bounds ไม่ได้ ต้องคำนวณเอง)
    public struct Footprint
    {
        public Vector2 size;   // หลังคูณ scale ของ prefab แล้ว
        public Vector2 offset; // กลาง collider เทียบกับจุดเกิด
        public Vector2 Feet => new Vector2(offset.x, offset.y - size.y * 0.5f); // ขอบล่างของตัว = ปลายเท้า
        public float Width => Mathf.Max(size.x, size.y * 0.6f);
    }

    const float DoorClearance = 2.5f;  // ไม่เกิดขวางประตูห้อง
    const float Spacing = 1.6f;        // ห่างกันเองอย่างน้อยเท่านี้ + ครึ่งตัว
    const float SpreadEnough = 6f;     // ห่างจากตัวอื่นเกินนี้ถือว่ากระจายพอแล้ว ไม่ต้องไล่ไปอยู่มุมห้อง
    const float Skin = 0.15f;          // เผื่อรอบตัวตอนเช็คกำแพง/ของ
    const int GoodCandidates = 12;     // ได้จุดที่ผ่านเงื่อนไขครบเท่านี้ก็เลือกเลย
    const int MaxTries = 90;
    static readonly float[] Relax = { 1f, 0.7f, 0.45f }; // หาไม่ได้ก็ลดระยะห่างผู้เล่น/ระหว่างกันลง
    static readonly Vector2[] Probe = { Vector2.zero, new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) };

    public static Footprint Measure(GameObject prefab)
    {
        var fallback = new Footprint { size = new Vector2(0.8f, 1f) };
        if (prefab == null) return fallback;

        Vector2 scale = prefab.transform.localScale;
        Vector2 absScale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        foreach (var col in prefab.GetComponents<Collider2D>())
        {
            if (col.isTrigger) continue;
            Vector2 size;
            switch (col)
            {
                case BoxCollider2D box: size = box.size; break;
                case CapsuleCollider2D capsule: size = capsule.size; break;
                case CircleCollider2D circle: size = Vector2.one * circle.radius * 2f; break;
                default: continue;
            }
            return new Footprint { size = Vector2.Scale(size, absScale), offset = Vector2.Scale(col.offset, scale) };
        }
        return fallback;
    }

    // จุดเกิดของทั้งระลอก (เรียงตาม prefabs) กระจายห่างกันเองและห่างมอนที่ยังอยู่ในห้องจากระลอกก่อน
    public static List<Vector2> Pick(RoomController room, IList<GameObject> prefabs, Vector2 player, float minPlayerDistance)
    {
        var area = new RoomArea(room);
        var occupied = area.LiveMonsters();
        var spots = new List<Vector2>(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
        {
            var body = Measure(prefabs[i]);
            if (!area.TryPick(body, occupied, player, minPlayerDistance, null, 0f, out Vector2 spot))
                spot = area.Fallback(spots.Count);
            spots.Add(spot);
            occupied.Add(spot);
        }
        return spots;
    }

    // จุดเกิดใกล้ ๆ จุดที่กำหนด (ลูกน้องบอสโผล่รอบตัวบอส) ต้องอยู่ในห้องและผ่านเงื่อนไขเดียวกัน
    public static bool TryPickNear(RoomController room, GameObject prefab, Vector2 center, float radius,
                                   Vector2 player, float minPlayerDistance, out Vector2 spot)
    {
        spot = center;
        if (room == null) return false;
        var area = new RoomArea(room);
        return area.TryPick(Measure(prefab), area.LiveMonsters(), player, minPlayerDistance, center, radius, out spot);
    }

    sealed class RoomArea
    {
        readonly RoomController room;
        readonly List<Collider2D> areas = new List<Collider2D>();
        readonly List<float> weights = new List<float>();
        readonly List<Vector2> doors = new List<Vector2>();
        readonly List<Tilemap> floors = new List<Tilemap>();
        readonly List<Tilemap> walls = new List<Tilemap>();
        readonly Transform[] fallbackPoints;
        float totalWeight;

        public RoomArea(RoomController owner)
        {
            room = owner;
            foreach (var col in owner.GetComponents<Collider2D>())
            {
                if (!col.enabled || !col.isTrigger) continue;
                Vector3 size = col.bounds.size;
                areas.Add(col);
                weights.Add(size.x * size.y);
                totalWeight += size.x * size.y;
            }
            if (owner.doors != null)
                foreach (var door in owner.doors)
                    if (door != null) doors.Add(door.transform.position);

            // พื้น/กำแพงของผังที่เปิดอยู่ (แมพละ 3 ผัง ผังที่ไม่ได้ใช้ถูกปิดไว้จึงไม่ติดมา)
            foreach (var map in owner.transform.root.GetComponentsInChildren<Tilemap>(false))
            {
                if (map.name.StartsWith("Floor")) floors.Add(map);
                else if (map.name.StartsWith("Bulkheads") || map.name.StartsWith("Wall")) walls.Add(map);
            }
            fallbackPoints = owner.monsterSpawnPoints;
        }

        // มอนที่ยังอยู่ในห้อง รวมตัวที่วงเตือนขึ้นแล้วแต่ยังไม่โผล่
        public List<Vector2> LiveMonsters()
        {
            var points = new List<Vector2>();
            foreach (var monster in room.GetComponentsInChildren<MonsterController>(false))
                if (monster.IsAlive) points.Add(monster.transform.position);
            foreach (var telegraph in room.GetComponentsInChildren<SpawnTelegraph>(false))
                points.Add(telegraph.Spot);
            return points;
        }

        public bool TryPick(Footprint body, List<Vector2> occupied, Vector2 player, float minPlayerDistance,
                            Vector2? near, float nearRadius, out Vector2 best)
        {
            best = near ?? (Vector2)room.transform.position;
            if (areas.Count == 0) return false;

            foreach (float relax in Relax)
            {
                float spacing = (Spacing + body.Width * 0.5f) * relax;
                float bestScore = float.MinValue;
                int good = 0;
                for (int t = 0; t < MaxTries && good < GoodCandidates; t++)
                {
                    Vector2 spot = near.HasValue ? near.Value + Random.insideUnitCircle * nearRadius : RandomPoint();
                    if (Vector2.Distance(spot, player) < minPlayerDistance * relax) continue;
                    if (NearDoor(spot)) continue;
                    float nearest = Nearest(spot, occupied);
                    if (nearest < spacing || !Fits(spot, body)) continue;

                    good++;
                    float score = Mathf.Min(nearest, SpreadEnough) + Random.value * 1.5f;
                    if (score > bestScore) { bestScore = score; best = spot; }
                }
                if (good > 0) return true;
            }
            return false;
        }

        // จุดเกิดที่วางไว้ในห้อง (ห้องแคบจนสุ่มไม่ได้) ใช้วนซ้ำได้ ขยับออกเป็นวงไม่ให้ซ้อนกันเป๊ะ
        public Vector2 Fallback(int index)
        {
            if (fallbackPoints == null || fallbackPoints.Length == 0)
                return (Vector2)room.transform.position + Random.insideUnitCircle * 0.8f;
            var point = fallbackPoints[index % fallbackPoints.Length];
            Vector2 at = point != null ? (Vector2)point.position : (Vector2)room.transform.position;
            int lap = index / fallbackPoints.Length;
            return lap == 0 ? at : at + Random.insideUnitCircle.normalized * 0.7f * lap;
        }

        Vector2 RandomPoint()
        {
            float pick = Random.value * totalWeight;
            int i = 0;
            while (i < areas.Count - 1 && (pick -= weights[i]) > 0f) i++;
            Bounds b = areas[i].bounds;
            return new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
        }

        bool NearDoor(Vector2 spot)
        {
            foreach (var door in doors)
                if (Vector2.Distance(spot, door) < DoorClearance) return true;
            return false;
        }

        static float Nearest(Vector2 spot, List<Vector2> others)
        {
            float nearest = float.MaxValue;
            foreach (var other in others) nearest = Mathf.Min(nearest, Vector2.Distance(spot, other));
            return nearest;
        }

        // ทั้งตัว (กลางตัวกับสี่มุม) ต้องอยู่ในห้องบนพื้นที่ไม่ใช่กำแพง และไม่ชนของแข็ง
        bool Fits(Vector2 spot, Footprint body)
        {
            Vector2 center = spot + body.offset;
            Vector2 half = body.size * 0.5f;
            foreach (var probe in Probe)
            {
                Vector2 p = center + Vector2.Scale(probe, half);
                if (!InRoom(p) || !OnFloor(p)) return false;
            }
            foreach (var col in Physics2D.OverlapBoxAll(center, body.size + Vector2.one * Skin * 2f, 0f))
                if (LootPlacement.IsSolid(col)) return false;
            return true;
        }

        bool InRoom(Vector2 p)
        {
            foreach (var col in areas)
                if (col.OverlapPoint(p)) return true;
            return false;
        }

        bool OnFloor(Vector2 p)
        {
            foreach (var wall in walls)
                if (wall.HasTile(wall.WorldToCell(p))) return false;
            if (floors.Count == 0) return true; // แมพที่ไม่มี tilemap พื้นชื่อนี้ เชื่อ collider อย่างเดียว
            foreach (var floor in floors)
                if (floor.HasTile(floor.WorldToCell(p))) return true;
            return false;
        }
    }
}
