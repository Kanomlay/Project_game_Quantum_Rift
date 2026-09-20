using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ติดตั้งระบบเหตุการณ์พิเศษประจำแมพ: สร้าง MapEventData ของร้านค้าทั้งสองธีม
// เปิดให้ห้องปกติรับเหตุการณ์ได้ และใส่ชุดเหตุการณ์ให้ MapData ของแต่ละแมพ
//
// ห้องบอสไม่เปิดรับเหตุการณ์ จะได้ไม่มีร้านค้าโผล่กลางสนามบอส
// สั่งซ้ำได้ ของเดิมจะถูกใช้ซ้ำ ไม่สร้างซ้อน
public static class MapEventBuilder
{
    const string EventFolder = "Assets/Data/Map/Events";
    const string PropSortingLayer = "object"; // ชั้นเดียวกับของตกแต่งในแมพ

    // แมพ 1 เป็นเรืออวกาศ แมพ 2 เป็นป่า ร้านค้าจึงคนละ prefab กัน
    static readonly string[] SpaceshipMaps = { "MapData_1_1", "MapData_1_2", "MapData_1_3" };
    static readonly string[] ForestMaps = { "MapData_2_1", "MapData_2_2" };
    static readonly string[] SpaceshipMapPrefabs = { "map_1", "map_1_2", "map_1_3" };
    static readonly string[] ForestMapPrefabs = { "Map_2", "Map_2_2" };

    [MenuItem("Tools/Quantum Rift/Setup Map Events")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งเหตุการณ์");

        if (!AssetDatabase.IsValidFolder(EventFolder))
            AssetDatabase.CreateFolder("Assets/Data/Map", "Events");

        var spaceshipShop = EnsureEvent("Shop_Spaceship", "ร้านค้า (เรืออวกาศ)", "Assets/Prefab/Shop/ShopSpaceship.prefab");
        var forestShop = EnsureEvent("Shop_Forest", "ร้านค้า (ป่ามิติ)", "Assets/Prefab/Shop/ShopForest.prefab");

        foreach (var map in SpaceshipMaps) AssignEvents(map, spaceshipShop);
        foreach (var map in ForestMaps) AssignEvents(map, forestShop);

        foreach (var prefab in SpaceshipMapPrefabs.Concat(ForestMapPrefabs)) OpenRoomsForEvents(prefab);

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งระบบเหตุการณ์ประจำแมพเรียบร้อย ร้านค้าจะสุ่มโผล่ห้องละหนึ่งที่ต่อการเข้าแมพหนึ่งครั้ง");
    }

    static MapEventData EnsureEvent(string assetName, string displayName, string prefabPath)
    {
        string path = $"{EventFolder}/{assetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<MapEventData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapEventData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.eventName = displayName;
        data.eventPrefab = Load<GameObject>(prefabPath);
        FixPrefabSortingLayer(prefabPath);
        data.weight = 1f;
        data.spawnAfterRoomCleared = true; // เคลียร์ห้องก่อนค่อยเปิดร้าน จะได้ไม่ต้องซื้อของกลางวงต่อสู้
        EditorUtility.SetDirty(data);
        return data;
    }

    // prefab ร้านค้ามาเป็น sorting layer Default ซึ่งเป็นชั้นล่างสุด พื้นแมพจะวาดทับจนมองไม่เห็น
    // ของตกแต่งอื่นในแมพอยู่ชั้น object เลยย้ายไปชั้นเดียวกัน
    static void FixPrefabSortingLayer(string prefabPath)
    {
        int layer = SortingLayer.NameToID(PropSortingLayer);
        if (SortingLayer.layers.All(l => l.name != PropSortingLayer))
        {
            Debug.LogWarning($"ไม่เจอ sorting layer ชื่อ {PropSortingLayer} ข้ามการแก้ชั้นการวาดของ {prefabPath}");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            bool changed = false;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sortingLayerID == layer) continue;
                renderer.sortingLayerID = layer;
                changed = true;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void AssignEvents(string mapDataName, params MapEventData[] events)
    {
        string path = $"Assets/Data/Map/{mapDataName}.asset";
        var map = AssetDatabase.LoadAssetAtPath<MapData>(path);
        if (map == null)
        {
            Debug.LogWarning($"ไม่เจอ {path} ข้ามไป");
            return;
        }

        map.possibleEvents = events;
        EditorUtility.SetDirty(map);
    }

    // เปิดช่อง Can Host Event ให้ทุกห้องในแมพปกติ ตัวเลือกห้องจริงๆ ไปสุ่มตอนโหลดแมพ
    static void OpenRoomsForEvents(string mapPrefabName)
    {
        string path = $"Assets/Prefab/{mapPrefabName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"ไม่เจอ {path} ข้ามไป");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            bool changed = false;
            foreach (var room in root.GetComponentsInChildren<RoomController>(true))
            {
                if (room.canHostEvent) continue;
                room.canHostEvent = true;
                changed = true;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
