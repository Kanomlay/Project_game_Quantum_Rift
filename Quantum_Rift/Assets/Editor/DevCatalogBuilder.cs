using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// เติมรายชื่อของให้คอนโซลทดสอบ (Resources/DevCatalog.asset) จากไฟล์ทั้งหมดในโปรเจกต์
// ทำเองทุกครั้งที่กด Play ของใหม่ (มอน อาวุธ พร แมพ ร้าน) จะโผล่ในคอนโซลเอง ไม่ต้องสั่งเมนู
// เขียนไฟล์เฉพาะตอนรายชื่อเปลี่ยนจริง git จะได้ไม่ขึ้นว่าไฟล์เปลี่ยนทุกครั้งที่กด Play
[InitializeOnLoad]
public static class DevCatalogBuilder
{
    const string AssetPath = "Assets/Resources/DevCatalog.asset";
    const string ShopFolder = "Assets/Prefab/Shop";
    const string MapObjectFolder = "Assets/Prefab/MapObjects";

    static readonly Dictionary<string, string> Themes = new Dictionary<string, string>
    {
        { "Spaceship", "ยาน" }, { "Forest", "ป่า" },
    };

    static DevCatalogBuilder()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Refresh(false);
        };
    }

    [MenuItem("Tools/Quantum Rift/Dev Console/Refresh Catalog (คอนโซลทดสอบ)")]
    static void RefreshFromMenu()
    {
        Selection.activeObject = Refresh(true);
    }

    public static DevCatalog Refresh(bool log)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<DevCatalog>(AssetPath);
        if (catalog == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            catalog = ScriptableObject.CreateInstance<DevCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        string before = EditorJsonUtility.ToJson(catalog);

        var maps = MapChain();
        catalog.maps = maps.ToArray();
        catalog.labArena = maps.FirstOrDefault(m => m.isBossRoom);
        catalog.monsters = Monsters(maps);
        catalog.weapons = Load<WeaponData>().Where(w => w.weaponPrefab != null)
                                            .OrderBy(w => w.rarity).ThenBy(w => w.name).ToArray();
        catalog.blessings = Load<BlessingData>().OrderBy(b => b.name).ToArray();
        catalog.characters = Load<CharacterData>().Where(c => c.characterPrefab != null).OrderBy(c => c.name).ToArray();
        catalog.chests = Load<LootTable>().Where(t => t.chestPrefab != null)
                                          .OrderBy(t => t.bossChest).ThenBy(t => t.name).ToArray();
        catalog.shops = Prefabs<ShopClickable>(ShopFolder)
            .Select(p => Placeable(p.GetComponentInChildren<ShopClickable>(true).isBuffShop ? "ร้านบัพ" : "ร้านค้า" + Theme(p), p)).ToArray();
        catalog.traps = Prefabs<FixedSpikeTrap>(MapObjectFolder).Select(p => Placeable("หนามพื้น" + Theme(p), p)).ToArray();

        if (EditorJsonUtility.ToJson(catalog) != before)
        {
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
        if (log)
            Debug.Log($"คอนโซลทดสอบ: มอน {catalog.monsters.Length} อาวุธ {catalog.weapons.Length} พร {catalog.blessings.Length} " +
                      $"อาชีพ {catalog.characters.Length} แมพ {catalog.maps.Length} กล่อง {catalog.chests.Length} " +
                      $"ร้าน {catalog.shops.Length} กับดัก {catalog.traps.Length} ห้องทดสอบยืมผัง {(catalog.labArena != null ? catalog.labArena.name : "-")}");
        return catalog;
    }

    static List<T> Load<T>() where T : Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name)
                     .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                     .Where(asset => asset != null).ToList();

    // เรียงตามลำดับเล่น: เริ่มจากแมพที่ไม่มีใครชี้มาหา แล้วเดินตาม nextMap ที่เหลือต่อท้ายตามชื่อ
    static List<MapData> MapChain()
    {
        var all = Load<MapData>().OrderBy(m => m.name).ToList();
        var pointedAt = new HashSet<MapData>(all.Where(m => m.nextMap != null).Select(m => m.nextMap));
        var chain = new List<MapData>();
        foreach (var head in all.Where(m => !pointedAt.Contains(m)))
            for (var map = head; map != null && !chain.Contains(map); map = map.nextMap) chain.Add(map);
        chain.AddRange(all.Where(m => !chain.Contains(m)));
        return chain;
    }

    // มอนทุกตัวที่เสกได้ เรียงตามแมพแรกที่เจอ (จากห้องในผังของแมพนั้น รวมผังสุ่มที่ปิดอยู่) บอส = มีหลอดเลือดบอส
    static DevCatalog.MonsterEntry[] Monsters(List<MapData> maps)
    {
        var firstSeen = new Dictionary<MonsterData, int>();
        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].mapPrefab == null) continue;
            foreach (var room in maps[i].mapPrefab.GetComponentsInChildren<RoomController>(true))
            {
                var data = room.roomData;
                if (data == null) continue;
                foreach (var list in new[] { data.monstersToSpawn, data.possibleMonsters, data.leaderMonsters })
                    if (list != null)
                        foreach (var monster in list)
                            if (monster != null && !firstSeen.ContainsKey(monster)) firstSeen[monster] = i;
            }
        }

        return Load<MonsterData>()
            .Where(m => m.monsterPrefab != null && m.monsterPrefab.GetComponent<MonsterController>() != null)
            .Select(m => (data: m, index: firstSeen.TryGetValue(m, out int at) ? at : int.MaxValue))
            .OrderBy(x => x.index).ThenBy(x => x.data.name)
            .Select(x => new DevCatalog.MonsterEntry
            {
                data = x.data,
                map = x.index < maps.Count ? "เจอที่ " + maps[x.index].mapName : "",
                boss = x.data.monsterPrefab.GetComponent<BossHealthHudLink>() != null,
            }).ToArray();
    }

    static IEnumerable<GameObject> Prefabs<T>(string folder) where T : Component
    {
        if (!AssetDatabase.IsValidFolder(folder)) yield break;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }).OrderBy(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null && prefab.GetComponentInChildren<T>(true) != null) yield return prefab;
        }
    }

    // ธีมจากชื่อ prefab หรือโฟลเดอร์ (ShopForest, MapObjects/Spaceship/FloorSpikes)
    static string Theme(GameObject prefab)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        foreach (var theme in Themes)
            if (path.Contains(theme.Key)) return $" ({theme.Value})";
        return "";
    }

    static DevCatalog.Placeable Placeable(string label, GameObject prefab) =>
        new DevCatalog.Placeable { label = label, prefab = prefab };
}
