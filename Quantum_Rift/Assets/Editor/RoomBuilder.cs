using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// ติดตั้งห้องต่อสู้ให้แมพ: ใส่ RoomController กับโซน trigger ที่วัดขนาดจากพื้นห้องให้อัตโนมัติ
// พร้อมสร้างจุดเกิดมอนสเตอร์ตามจำนวนที่ระบุไว้ใน RoomEncounterData
// สั่งซ้ำได้ ของที่มีอยู่แล้วจะถูกใช้ซ้ำ ไม่สร้างซ้อน
public static class RoomBuilder
{
    const string GroundName = "G_Map_1"; // tilemap พื้นห้อง ใช้วัดขนาดโซน trigger
    const float TriggerInset = 2f;       // หดโซนเข้ามาจากขอบ ผู้เล่นจะได้ต้องเดินเข้าห้องจริงๆ ห้องถึงจะเริ่ม

    [MenuItem("Tools/Quantum Rift/Setup Room/Map 1 - Room 1")]
    public static void SetupMap1Room1()
    {
        Setup("Assets/Prefab/map_1.prefab", "Map_1_1", "Assets/Data/Map/RoomData/Map 1 - 1.asset");
    }

    static void Setup(string mapPrefabPath, string roomName, string roomDataPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งห้อง");

        var roomData = Load<RoomEncounterData>(roomDataPath);
        if (roomData.monstersToSpawn == null || roomData.monstersToSpawn.Length == 0)
            throw new InvalidOperationException($"{roomDataPath} ยังไม่ได้ใส่มอนสเตอร์ ห้องจะเคลียร์ผ่านทันทีโดยไม่มีอะไรเกิด");

        foreach (var monster in roomData.monstersToSpawn)
        {
            if (monster == null || monster.monsterPrefab == null)
                throw new InvalidOperationException($"มี MonsterData ใน {roomDataPath} ที่ยังไม่ได้ใส่ prefab สั่ง Setup Monster ก่อน");
        }

        var root = PrefabUtility.LoadPrefabContents(mapPrefabPath);
        try
        {
            var room = root.transform.Find(roomName);
            if (room == null)
                throw new InvalidOperationException($"ไม่เจอห้องชื่อ \"{roomName}\" ใน {mapPrefabPath}");

            var controller = Ensure<RoomController>(room.gameObject);
            controller.roomData = roomData;

            SetupTriggerZone(room);
            controller.monsterSpawnPoints = EnsureSpawnPoints(room, roomData.monstersToSpawn.Length);

            PrefabUtility.SaveAsPrefabAsset(root, mapPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้งห้อง {roomName} ใน {mapPrefabPath} เรียบร้อย (มอนสเตอร์ {roomData.monstersToSpawn.Length} ตัว)");
    }

    // โซนที่ผู้เล่นเดินเข้ามาแล้วห้องจะเริ่ม วัดขนาดจาก tilemap พื้นของห้องนั้น
    static void SetupTriggerZone(Transform room)
    {
        var zone = Ensure<BoxCollider2D>(room.gameObject);
        zone.isTrigger = true;

        var groundObject = room.Find(GroundName);
        var ground = groundObject != null ? groundObject.GetComponent<Tilemap>() : null;
        if (ground == null)
        {
            Debug.LogWarning($"ห้อง {room.name} ไม่มี {GroundName} เลยวัดขนาดโซนอัตโนมัติไม่ได้ ต้องปรับ Box Collider 2D เองใน Inspector");
            return;
        }

        ground.CompressBounds();
        var bounds = ground.localBounds;

        zone.offset = room.InverseTransformPoint(ground.transform.TransformPoint(bounds.center));
        zone.size = new Vector2(
            Mathf.Max(1f, bounds.size.x - TriggerInset * 2f),
            Mathf.Max(1f, bounds.size.y - TriggerInset * 2f));
    }

    static Transform[] EnsureSpawnPoints(Transform room, int count)
    {
        var zone = room.GetComponent<BoxCollider2D>();
        var center = zone != null ? zone.offset : Vector2.zero;
        var radius = zone != null ? zone.size * 0.25f : new Vector2(3f, 3f);

        var points = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            string pointName = $"SpawnPoint_{i + 1}";
            var point = room.Find(pointName);
            if (point == null)
            {
                var go = new GameObject(pointName);
                go.transform.SetParent(room, false);
                point = go.transform;
            }

            // กระจายจุดเกิดเป็นวงรอบกลางห้อง มอนสเตอร์จะได้ไม่โผล่ซ้อนกันจุดเดียว
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            point.localPosition = new Vector3(
                center.x + Mathf.Cos(angle) * radius.x,
                center.y + Mathf.Sin(angle) * radius.y,
                0f);

            points[i] = point;
        }

        return points;
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
