using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// เปลี่ยนกองกล่องประจำห้อง (Room/FixedGameplayObjects/Pile_*) ให้เป็นกำแพงในห้องที่ทุบได้ แล้วลบกล่องทำลายได้ชุดเก่าทิ้ง
// - กำแพงใหม่ระบายลง tilemap กำแพงเดิมของผัง (Bulkheads_Unified64) ตรงช่องตารางที่กล่องแต่ละใบยืนอยู่
//   ใช้ภาพกำแพงขอบแมพ (ยาน: WallPlain + WallLight แซม, ป่า: RootWall + GlowRoots แซม)
// - จดช่องไว้ใน RoomBreakableWalls ของห้องนั้น ตอนเล่นจะสร้าง collider ช่องละชิ้นที่ผู้เล่นตีแตกได้
//   และมอนสเตอร์ของห้องทุบได้เมื่อขวางทาง (กำแพงขอบห้อง/ขอบแมพเดิมยังแข็งเหมือนเดิม)
// - ใส่ RandomCrateClusters ที่ตัวแมพ: กล่องทำลายได้สุ่มใหม่ทุกครั้งที่เข้าแมพ วางพอดีช่องตาราง
// สั่งซ้ำได้: รอบหลังไม่มีกองกล่องเหลือแล้วจึงไม่เปลี่ยนกำแพง แค่ตั้งค่า RandomCrateClusters ให้ใหม่
// ห้ามสั่ง "Arrange Fixed Room Piles And Corridor Spikes" หลังจากนี้ ไม่งั้นกองกล่องตายตัวจะกลับมา
public static class CratePileWallBuilder
{
    static readonly string[] Maps =
    {
        "Assets/Prefab/map_1.prefab",
        "Assets/Prefab/map_1_2.prefab",
        "Assets/Prefab/map_1_3.prefab",
        "Assets/Prefab/Map_2.prefab",
        "Assets/Prefab/Map_2_2.prefab",
    };
    const string SpaceshipCrate = "Assets/Prefab/MapObjects/Spaceship/BreakableWall.prefab";
    const string ForestCrate = "Assets/Prefab/MapObjects/Forest/BreakableWall.prefab";
    static readonly string[] MainWall = { "WallPlain", "RootWall" };
    static readonly string[] AccentWall = { "WallLight", "GlowRoots" };
    static readonly Color SpaceshipDebris = new Color(0.5f, 0.6f, 0.75f);
    static readonly Color ForestDebris = new Color(0.55f, 0.42f, 0.3f);
    const int AccentEvery = 6;         // แซมกำแพงมีไฟ/รากเรืองแสงราว 1 ใน 6 ช่อง (เลือกตามพิกัด สั่งซ้ำได้ผลเดิม)
    const float DoorClearance = 1.9f;  // กันพลาด: ช่องที่เลื่อนมาชิดประตูไม่ทำเป็นกำแพง

    [MenuItem("Tools/Quantum Rift/Maps/Crate Piles To Walls + Random Crates (กองกล่อง→กำแพง)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่ง");

        var report = new StringBuilder("กองกล่อง→กำแพง:\n");
        foreach (var path in Maps)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                report.AppendLine(Convert(root, path));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    static string Convert(GameObject root, string path)
    {
        var features = root.GetComponent<MapGameplayFeatures>();
        bool forest = features != null && features.theme == MapGameplayFeatures.Theme.LivingForest;
        int walls = 0, solid = 0, skipped = 0, removed = 0, layouts = 0;

        foreach (var grid in root.GetComponentsInChildren<Grid>(true))
        {
            Tilemap wallMap = null;
            foreach (Transform child in grid.transform)
                if (child.name.StartsWith("Bulkheads")) wallMap = child.GetComponent<Tilemap>();
            if (wallMap == null) continue; // ตัว Grid ของรากแมพเอง (ไม่มี tilemap อยู่ใต้ตรง ๆ)
            layouts++;

            var boundary = grid.transform.Find("WalkableBoundary");
            var (main, accent) = PickWallTiles(wallMap);
            if (main == null) throw new InvalidOperationException($"{path}/{grid.name}: หาภาพกำแพงไม่เจอ");
            Vector2 cellSize = Vector2.Scale(grid.cellSize, wallMap.transform.lossyScale);
            var doors = grid.GetComponentsInChildren<RoomController>(true)
                .Where(r => r.doors != null).SelectMany(r => r.doors).Where(d => d != null)
                .Select(d => (Vector2)d.transform.position).ToList();

            var done = new HashSet<Vector3Int>();
            foreach (var prop in grid.GetComponentsInChildren<BreakableProp>(true))
            {
                var cell = wallMap.WorldToCell(Footprint(prop));
                if (!done.Add(cell) || wallMap.HasTile(cell)) continue;
                Vector2 center = wallMap.GetCellCenterWorld(cell);
                if (doors.Any(d => Vector2.Distance(d, center) < DoorClearance)) { skipped++; continue; }

                wallMap.SetTile(cell, accent != null && Mathf.Abs(cell.x * 7 + cell.y * 13) % AccentEvery == 0 ? accent : main);
                var room = prop.GetComponentInParent<RoomController>(true);
                if (room != null)
                {
                    // กำแพงในห้อง: ทุบได้ collider สร้างตอนเล่นโดย RoomBreakableWalls
                    var breakable = room.GetComponent<RoomBreakableWalls>();
                    if (breakable == null)
                    {
                        breakable = room.gameObject.AddComponent<RoomBreakableWalls>();
                        breakable.debrisColor = forest ? ForestDebris : SpaceshipDebris;
                    }
                    breakable.wallMap = wallMap;
                    if (!breakable.cells.Contains(cell)) breakable.cells.Add(cell);
                    walls++;
                }
                else if (boundary != null)
                {
                    // กล่องที่ไม่ได้อยู่ในห้อง (ทางเชื่อม): เป็นกำแพงแข็งแบบกำแพงเดิม
                    var box = boundary.gameObject.AddComponent<BoxCollider2D>();
                    box.offset = boundary.InverseTransformPoint(center);
                    box.size = cellSize;
                    solid++;
                }
            }

            // ลบกล่องชุดเก่าทั้งหมด (กลุ่มกองในห้อง + ใบที่ลากวางไว้ที่อื่น)
            foreach (var group in grid.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "FixedGameplayObjects").ToArray())
            {
                removed += group.GetComponentsInChildren<BreakableProp>(true).Length;
                Object.DestroyImmediate(group.gameObject);
            }
            foreach (var prop in grid.GetComponentsInChildren<BreakableProp>(true))
            {
                removed++;
                Object.DestroyImmediate(prop.gameObject);
            }
            if (boundary == null) Debug.LogWarning($"{path}/{grid.name}: ไม่มี WalkableBoundary กำแพงใหม่จะไม่มี collider");
        }

        var spawner = root.GetComponent<RandomCrateClusters>();
        if (spawner == null) spawner = root.AddComponent<RandomCrateClusters>();
        spawner.cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(forest ? ForestCrate : SpaceshipCrate);
        if (spawner.cratePrefab == null) Debug.LogWarning($"{path}: ไม่เจอ prefab กล่อง {(forest ? ForestCrate : SpaceshipCrate)}");

        return $"- {path}: {layouts} ผัง, กำแพงในห้อง (ทุบได้) {walls} ช่อง, กำแพงแข็งนอกห้อง {solid} ช่อง, ข้ามเพราะชิดประตู {skipped}, ลบกล่องเก่า {removed} ใบ";
    }

    // จุดที่กล่องยืน = กลาง collider ตัวชน (ฐานกล่อง) ไม่ใช่ pivot ของภาพ
    static Vector2 Footprint(BreakableProp prop)
    {
        foreach (var box in prop.GetComponents<BoxCollider2D>())
            if (!box.isTrigger) return prop.transform.TransformPoint(box.offset);
        return prop.transform.position;
    }

    static (TileBase main, TileBase accent) PickWallTiles(Tilemap wallMap)
    {
        var used = new TileBase[wallMap.GetUsedTilesCount()];
        wallMap.GetUsedTilesNonAlloc(used);
        TileBase main = used.FirstOrDefault(t => t != null && MainWall.Contains(t.name));
        TileBase accent = used.FirstOrDefault(t => t != null && AccentWall.Contains(t.name));
        return (main ?? used.FirstOrDefault(t => t != null), accent);
    }
}
