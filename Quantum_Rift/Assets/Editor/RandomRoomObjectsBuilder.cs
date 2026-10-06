using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// ตั้งแมพ 1–2 ให้สุ่มของในห้องใหม่ทุกครั้งที่เข้าแมพ
// 1. สร้าง Data/Map/WallShapes.asset (รูปทรงกำแพง) จากกองกล่องของเพื่อน (Prefab/MapObjects/Spaceship/Piles/Pile_*)
//    ทำครั้งแรกครั้งเดียว แก้/เพิ่มรูปทรงใน Inspector ได้ สั่งซ้ำไม่ทับ
// 2. ลบของที่เคยวางตายตัวในห้อง: กำแพงในห้อง (RoomBreakableWalls ที่จดช่องไว้ + tile ของมัน) และกล่อง/กองกล่องที่เหลือ
// 3. ใส่ RandomRoomWalls (กำแพงสุ่มรูปทรง ทุบได้) + RandomCrateClusters (กล่องสุ่ม) ที่ตัวแมพ ตั้งภาพตามธีมยาน/ป่า
// กับดัก: RoomEntryTrapSpawner ของแต่ละห้องสุ่มเป็นจุดเดี่ยว/แนว/สี่เหลี่ยมเองตอนเดินเข้าห้องครั้งแรก ไม่ต้องตั้งเพิ่ม
// สั่งซ้ำได้ (ตัวเลขใน RandomRoomWalls/RandomCrateClusters ตั้งเฉพาะตอนใส่ครั้งแรก ปรับใน Inspector แล้วไม่โดนทับ)
// ห้ามสั่ง "Arrange Fixed Room Piles And Corridor Spikes" อีก ไม่งั้นกองกล่องตายตัวจะกลับมา
public static class RandomRoomObjectsBuilder
{
    static readonly string[] Maps =
    {
        "Assets/Prefab/map_1.prefab",
        "Assets/Prefab/map_1_2.prefab",
        "Assets/Prefab/map_1_3.prefab",
        "Assets/Prefab/Map_2.prefab",
        "Assets/Prefab/Map_2_2.prefab",
        "Assets/Prefab/ExpandedStages/Map_1_4.prefab",
        "Assets/Prefab/ExpandedStages/Map_1_5.prefab",
        "Assets/Prefab/ExpandedStages/Map_2_3.prefab",
        "Assets/Prefab/ExpandedStages/Map_2_4.prefab",
        "Assets/Prefab/ExpandedStages/Map_2_5.prefab",
    };
    static readonly string[] WallTileFolder = { "Assets/Data/Map/Map1-Unified-v2/", "Assets/Data/Map/Map2-Forest-v1/" };
    const string ShapesPath = "Assets/Data/Map/WallShapes.asset";
    const string PileFolder = "Assets/Prefab/MapObjects/Spaceship/Piles";
    const float PileStep = 1.25f; // กล่องในกองห่างกันหนึ่งช่องตาราง
    const string SpaceshipCrate = "Assets/Prefab/MapObjects/Spaceship/BreakableWall.prefab";
    const string ForestCrate = "Assets/Prefab/MapObjects/Forest/BreakableWall.prefab";
    static readonly string[] MainWall = { "WallPlain", "RootWall" };
    static readonly string[] AccentWall = { "WallLight", "GlowRoots" };
    static readonly Color SpaceshipDebris = new Color(0.5f, 0.6f, 0.75f);
    static readonly Color ForestDebris = new Color(0.55f, 0.42f, 0.3f);

    // ใช้เมื่อหากองกล่องของเพื่อนไม่เจอ (รูปเดียวกับที่ถอดมาจากกองเหล่านั้น)
    static readonly (string name, string[] rows)[] FallbackShapes =
    {
        ("Circle", new[] { ".XXX.", "X...X", "X...X", "X...X", ".XXX." }),
        ("Compact", new[] { "XXX", "XXX" }),
        ("Heart", new[] { ".X.X.", "XXXXX", "XXXXX", ".XXX.", "..X.." }),
        ("LShape", new[] { "XX..", "XX..", "XX..", "XXXX" }),
        ("Rectangle", new[] { "XXXX", "XXXX", "XXXX" }),
        ("Steps", new[] { "..XX", ".XXX", "XXX.", "XX.." }),
        ("TwinStacks", new[] { "XX.XX", "XX.XX", "XX.XX" }),
    };

    [MenuItem("Tools/Quantum Rift/Maps/Random Walls + Crates + Traps (กำแพง/กล่อง/กับดักสุ่ม)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่ง");

        var library = EnsureShapes(out string shapeReport);
        var report = new StringBuilder("กำแพง/กล่อง/กับดักสุ่ม:\n").AppendLine(shapeReport);
        foreach (var path in Maps)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                report.AppendLine(Setup(root, path, library));
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

    static string Setup(GameObject root, string path, WallShapeLibrary library)
    {
        var features = root.GetComponent<MapGameplayFeatures>();
        bool forest = features != null && features.theme == MapGameplayFeatures.Theme.LivingForest;

        // กำแพงในห้องแบบตายตัว: ลบ tile ตามช่องที่จดไว้ แล้วเอาตัวจดออก (ตอนเล่น RandomRoomWalls ใส่ให้เองทุกครั้ง)
        int fixedWalls = 0, crates = 0;
        foreach (var walls in root.GetComponentsInChildren<RoomBreakableWalls>(true))
        {
            if (walls.wallMap != null)
                foreach (var cell in walls.cells)
                {
                    if (walls.wallMap.HasTile(cell)) fixedWalls++;
                    walls.wallMap.SetTile(cell, null);
                }
            Object.DestroyImmediate(walls);
        }
        foreach (var group in root.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "FixedGameplayObjects").ToArray())
        {
            crates += group.GetComponentsInChildren<BreakableProp>(true).Length;
            Object.DestroyImmediate(group.gameObject);
        }
        foreach (var prop in root.GetComponentsInChildren<BreakableProp>(true))
        {
            crates++;
            Object.DestroyImmediate(prop.gameObject);
        }

        TileBase main = null, accent = null;
        foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
        {
            if (!map.name.StartsWith("Bulkheads")) continue;
            (main, accent) = PickWallTiles(map, forest);
            if (main != null) break;
        }
        if (main == null) throw new InvalidOperationException($"{path}: หาภาพกำแพงไม่เจอ");

        var walls2 = root.GetComponent<RandomRoomWalls>();
        if (walls2 == null)
        {
            walls2 = root.AddComponent<RandomRoomWalls>();
            walls2.debrisColor = forest ? ForestDebris : SpaceshipDebris;
        }
        walls2.shapes = library;
        walls2.wallTile = main;
        walls2.accentTile = accent;

        var crateSpawner = root.GetComponent<RandomCrateClusters>();
        if (crateSpawner == null) crateSpawner = root.AddComponent<RandomCrateClusters>();
        crateSpawner.cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(forest ? ForestCrate : SpaceshipCrate);
        if (crateSpawner.cratePrefab == null) Debug.LogWarning($"{path}: ไม่เจอ prefab กล่อง");

        int rooms = root.GetComponentsInChildren<RoomController>(true).Length;
        int trapRooms = root.GetComponentsInChildren<RoomEntryTrapSpawner>(true).Length;
        return $"- {path}: ลบกำแพงตายตัว {fixedWalls} ช่อง, ลบกล่องตายตัว {crates} ใบ, กำแพงสุ่มใช้ {main.name}/{(accent != null ? accent.name : "-")}, " +
               $"ห้องที่มีตัวสุ่มกับดัก {trapRooms}/{rooms}";
    }

    // รูปทรงกำแพงจากกองกล่องของเพื่อน สร้างครั้งแรกครั้งเดียว (มีไฟล์แล้วไม่ทับ ปรับใน Inspector ได้)
    static WallShapeLibrary EnsureShapes(out string report)
    {
        var library = AssetDatabase.LoadAssetAtPath<WallShapeLibrary>(ShapesPath);
        if (library != null && library.shapes != null && library.shapes.Length > 0)
        {
            report = $"- รูปทรงกำแพง: ใช้ {ShapesPath} เดิม ({library.shapes.Length} แบบ)";
            return library;
        }

        var shapes = new List<WallShapeLibrary.Shape>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PileFolder }))
        {
            var pile = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (pile == null || !pile.name.StartsWith("Pile_")) continue;
            var points = pile.GetComponentsInChildren<BreakableProp>(true)
                .Select(c => (Vector2)(c.transform.position - pile.transform.position)).ToList();
            if (points.Count == 0) continue;
            float minX = points.Min(p => p.x), minY = points.Min(p => p.y);
            var cells = new HashSet<Vector2Int>(points.Select(p =>
                new Vector2Int(Mathf.RoundToInt((p.x - minX) / PileStep), Mathf.RoundToInt((p.y - minY) / PileStep))));
            shapes.Add(MakeShape(pile.name.Substring("Pile_".Length), Rows(cells)));
        }
        string source = $"กองกล่องใน {PileFolder}";
        if (shapes.Count == 0)
        {
            foreach (var (name, rows) in FallbackShapes) shapes.Add(MakeShape(name, rows));
            source = "รูปสำรองในโค้ด (หากองกล่องไม่เจอ)";
        }

        if (library == null)
        {
            library = ScriptableObject.CreateInstance<WallShapeLibrary>();
            library.shapes = shapes.ToArray();
            AssetDatabase.CreateAsset(library, ShapesPath);
        }
        else
        {
            library.shapes = shapes.ToArray();
            EditorUtility.SetDirty(library);
        }
        report = $"- รูปทรงกำแพง: สร้าง {ShapesPath} จาก{source} {shapes.Count} แบบ ({string.Join(", ", shapes.Select(s => s.name))})";
        return library;
    }

    // หัวใจห้ามหมุน (จะกลับหัว) วงกลมหมุนแล้วก็รูปเดิม
    static WallShapeLibrary.Shape MakeShape(string name, string[] rows) =>
        new WallShapeLibrary.Shape { name = name, rows = rows, canRotate = name != "Heart" && name != "Circle", weight = 1f };

    static string[] Rows(HashSet<Vector2Int> cells)
    {
        int width = cells.Max(c => c.x) + 1, height = cells.Max(c => c.y) + 1;
        var rows = new string[height];
        for (int r = 0; r < height; r++)
        {
            var row = new StringBuilder(width);
            for (int c = 0; c < width; c++) row.Append(cells.Contains(new Vector2Int(c, height - 1 - r)) ? 'X' : '.');
            rows[r] = row.ToString();
        }
        return rows;
    }

    static (TileBase main, TileBase accent) PickWallTiles(Tilemap wallMap, bool forest)
    {
        var used = new TileBase[wallMap.GetUsedTilesCount()];
        wallMap.GetUsedTilesNonAlloc(used);
        TileBase main = used.FirstOrDefault(t => t != null && MainWall.Contains(t.name));
        TileBase accent = used.FirstOrDefault(t => t != null && AccentWall.Contains(t.name));
        // ด่าน 1-4, 1-5, 2-3 ถึง 2-5 ขอบแมพใช้ tile ที่มี collider ในตัว (Data/Map/ExpandedStages) ห้ามเอามาทำกำแพงทุบได้
        // ไม่งั้นทุบแตกแล้วยังเดินผ่านไม่ได้ จึงใช้ tile ต้นฉบับของธีม (ไม่มี collider) เหมือนแมพเดิม
        int theme = forest ? 1 : 0;
        if (main == null) main = AssetDatabase.LoadAssetAtPath<TileBase>(WallTileFolder[theme] + MainWall[theme] + ".asset");
        if (accent == null) accent = AssetDatabase.LoadAssetAtPath<TileBase>(WallTileFolder[theme] + AccentWall[theme] + ".asset");
        return (main ?? used.FirstOrDefault(t => t != null), accent);
    }
}
