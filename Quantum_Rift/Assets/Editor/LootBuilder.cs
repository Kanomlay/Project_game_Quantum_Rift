using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// กล่องสมบัติ เงิน และขวดยา (ขอบเขต 1.3.8 / 1.3.9.1 / 1.3.10)
// - ตั้งค่า import ภาพขวดยา (image/Item) ให้เป็น pixel art
// - สร้าง/อัปเดต prefab กล่อง (ภาพจากชุด QuantumRift-UI-Objects-v1)
// - สร้าง LootTable (Data/Loot/ChestLoot) ตัวเลขตั้งให้เฉพาะตอนสร้าง ส่วนรายการอาวุธเติมใหม่ทุกครั้งจาก WeaponData ตามระดับ
// - ใส่ LootTable ให้ MapData ของแมพ 1 และ 2 (แมพ 3 ไม่มีกล่องตามขอบเขต)
// สั่งซ้ำได้ ไม่ทับค่าที่ปรับใน Inspector
public static class LootBuilder
{
    const string UiObjects = "Assets/image/UI_Image/QuantumRift-UI-Objects-v1/runtime";
    const string ItemArt = "Assets/image/Item";
    const string ChestPrefabPath = "Assets/Prefab/Loot/Treasure Chest.prefab";
    internal const string LootPath = "Assets/Data/Loot/ChestLoot.asset";
    const string MapFolder = "Assets/Data/Map";
    static readonly string[] ChestMaps = { "MapData_1_1", "MapData_1_2", "MapData_1_3", "MapData_1_bossroom", "MapData_2_1", "MapData_2_2", "MapData_2_boss" };

    [MenuItem("Tools/Quantum Rift/Setup Chest Loot (กล่องสมบัติ เงิน ขวดยา)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งกล่องสมบัติ");

        ConfigurePixelSprite($"{ItemArt}/Potion-HP.png");
        ConfigurePixelSprite($"{ItemArt}/Potion-Energy.png");

        var chest = BuildChestPrefab();
        var loot = BuildLootTable(chest);

        int maps = 0;
        foreach (var name in ChestMaps)
        {
            var map = AssetDatabase.LoadAssetAtPath<MapData>($"{MapFolder}/{name}.asset");
            if (map == null) { Debug.LogWarning($"ไม่เจอ {MapFolder}/{name}.asset ข้าม"); continue; }
            map.chestLoot = loot;
            EditorUtility.SetDirty(map);
            maps++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้งกล่องสมบัติแล้ว ใส่ให้ {maps} แมพ | อาวุธในกล่อง: ธรรมดา {Count(loot.commonWeapons)} / หายาก {Count(loot.rareWeapons)} / ตำนาน {Count(loot.legendaryWeapons)} ชิ้น");
    }

    static int Count(WeaponData[] list) => list == null ? 0 : list.Count(w => w != null);

    // ภาพขวดยา 16x16 วาดเอง: ไม่เบลอ ไม่บีบอัด 32 px = 1 หน่วย
    static void ConfigurePixelSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    static TreasureChest BuildChestPrefab()
    {
        var closed = Load<Sprite>($"{UiObjects}/Chest-Closed.png");
        var open = Load<Sprite>($"{UiObjects}/Chest-Open.png");
        Directory.CreateDirectory(Path.GetDirectoryName(ChestPrefabPath));

        bool isNew = !File.Exists(ChestPrefabPath);
        GameObject root;
        if (isNew)
        {
            root = new GameObject("Treasure Chest");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "object"; // ชั้นเดียวกับตัวละคร เรียงหน้า-หลังตามแกน Y

            // ตัวกล่องเป็นของแข็ง เดินชนได้ มอนสเตอร์ก็เดินอ้อม
            var body = root.AddComponent<BoxCollider2D>();
            body.size = new Vector2(1.2f, 0.5f);
            body.offset = new Vector2(0f, 0.3f);
            root.AddComponent<TreasureChest>();
        }
        else root = PrefabUtility.LoadPrefabContents(ChestPrefabPath);

        var chest = root.GetComponent<TreasureChest>();
        chest.closedSprite = closed;
        chest.openSprite = open;
        var display = root.GetComponentInChildren<SpriteRenderer>();
        if (display != null)
        {
            // ภาพในฉาก edit mode (ตอนเล่นกล่องจัดขนาด/ตำแหน่งเองใน Awake)
            display.sprite = closed;
            float scale = chest.width / closed.bounds.size.x;
            display.transform.localScale = Vector3.one * scale;
            display.transform.localPosition = new Vector3(0f, -closed.bounds.min.y * scale, 0f);
        }

        var saved = PrefabUtility.SaveAsPrefabAsset(root, ChestPrefabPath);
        if (isNew) UnityEngine.Object.DestroyImmediate(root);
        else PrefabUtility.UnloadPrefabContents(root);
        return saved.GetComponent<TreasureChest>();
    }

    static LootTable BuildLootTable(TreasureChest chest)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LootPath));
        var loot = AssetDatabase.LoadAssetAtPath<LootTable>(LootPath);
        if (loot == null)
        {
            loot = ScriptableObject.CreateInstance<LootTable>(); // ค่าเริ่มต้นตามขอบเขตอยู่ในคลาสแล้ว
            AssetDatabase.CreateAsset(loot, LootPath);
        }

        loot.chestPrefab = chest;
        loot.coinSprite = Load<Sprite>($"{UiObjects}/Coin-Single.png");
        loot.hpPotionSprite = Load<Sprite>($"{ItemArt}/Potion-HP.png");
        loot.energyPotionSprite = Load<Sprite>($"{ItemArt}/Potion-Energy.png");

        RefillWeapons(loot);
        return loot;
    }

    // อาวุธทุกชิ้นในโปรเจกต์ แยกตามระดับ (อาวุธเริ่มต้นประจำอาชีพไม่ดรอป) WeaponCollectionBuilder เรียกหลังสร้างอาวุธเสร็จ
    internal static void RefillWeapons(LootTable loot)
    {
        var weapons = AssetDatabase.FindAssets("t:WeaponData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(w => w != null && w.weaponPrefab != null)
            .OrderBy(w => w.name)
            .ToArray();
        loot.commonWeapons = weapons.Where(w => w.rarity == WeaponRarity.Common).ToArray();
        loot.rareWeapons = weapons.Where(w => w.rarity == WeaponRarity.Rare).ToArray();
        loot.legendaryWeapons = weapons.Where(w => w.rarity == WeaponRarity.Legendary).ToArray();
        EditorUtility.SetDirty(loot);
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
