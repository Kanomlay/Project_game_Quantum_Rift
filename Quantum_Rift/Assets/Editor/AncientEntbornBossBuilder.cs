using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ติดตั้งบอสประจำแมพ 2 (Ancient Entborn) ให้พร้อมสู้ แบบเดียวกับ BossBuilder ของแมพ 1
// (AncientEntbornBuilder ของเพื่อนสร้างภาพ/คลิป/prefab ส่วนไฟล์นี้ใส่ AI และผูกกับห้องบอส):
// 1. MonsterData ตามตาราง 1.8 (เลือด 320 ดาเมจ 4 ความเร็ว 60 → 0.9 สเกลเดียวกับมอนตัวอื่น)
// 2. prefab บอสที่เพื่อนทำภาพ/Animator ไว้: เติมฟิสิกส์ collider layer และ AncientEntbornBoss (ไม่แตะ Animator)
// 3. ห้องบอสแมพ 2 เสกบอสตัวนี้ (โหมดจัดฉาก) และตั้ง MapData_2_boss เป็นห้องบอส (จบแล้วขึ้นหน้าสรุปแบบแมพ 1)
// สั่งซ้ำได้: ขนาด collider และตัวเลขท่าที่ปรับเองใน prefab จะไม่ถูกทับ
public static class AncientEntbornBossBuilder
{
    const string BossPrefabPath = "Assets/Prefab/Boss/AncientEntborn/AncientEntborn.prefab";
    const string RootPrefabPath = "Assets/Prefab/Boss/AncientEntborn/AncientEntbornRootPillar.prefab";
    const string LaserSheet = "Assets/image/Boss/AncientEntborn/AncientEntborn-Laser-7Frames-v2-Clean.png";
    const string RootSheet = "Assets/image/Boss/AncientEntborn/AncientEntborn-RootPillar-7Frames-v2-Clean.png";
    const string BossDataPath = "Assets/Data/Monster/Ancient_Entborn.asset";
    const string MinionDataPath = "Assets/Data/Monster/Rootlings.asset";
    const string EncounterPath = "Assets/Data/Map/RoomData/Map 2 - Boss.asset";
    const string BossMapPrefabPath = "Assets/Prefab/Map_2_boss.prefab";
    const string BossMapDataPath = "Assets/Data/Map/MapData_2_boss.asset";
    const float BossScale = 1.5f; // ภาพสูง 2.25 → 3.4 หน่วย ผู้เล่นสูงราว 1.2

    [MenuItem("Tools/Quantum Rift/Setup Boss/Ancient Entborn (แมพ 2)")]
    public static void SetupAncientEntborn()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งบอส");

        int enemyLayer = LayerMask.NameToLayer(Map1MonsterBuilder.EnemyLayerName);
        if (enemyLayer < 0) throw new InvalidOperationException($"ยังไม่มี layer ชื่อ {Map1MonsterBuilder.EnemyLayerName}");
        var reference = Map1MonsterBuilder.Load<GameObject>(Map1MonsterBuilder.SortingReferencePrefab).GetComponent<SpriteRenderer>();

        var minion = AssetDatabase.LoadAssetAtPath<MonsterData>(MinionDataPath);
        if (minion == null) Debug.LogWarning($"ไม่เจอ {MinionDataPath} (สั่ง Map 2 Monsters ก่อน) บอสจะเรียกลูกน้องตอนคลั่งไม่ได้");

        var data = AssetDatabase.LoadAssetAtPath<MonsterData>(BossDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(data, BossDataPath);
        }
        data.monsterName = "Ancient Entborn";
        data.maxHealth = 320f;
        data.attackDamage = 4f; // ดาเมจตามขอบเขต ไม่สลับกับคอลัมน์หน่วงโจมตี
        data.attackCooldown = 4f;
        data.moveSpeed = 60f * 1.5f / 100f;
        data.attackRange = 2.4f;

        var laser = AssetDatabase.LoadAllAssetsAtPath(LaserSheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (laser.Length < 7) throw new InvalidOperationException($"ภาพเลเซอร์ใน {LaserSheet} มีไม่ครบ 7 เฟรม (เจอ {laser.Length})");
        var rootFrames = AssetDatabase.LoadAllAssetsAtPath(RootSheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (rootFrames.Length < 7) throw new InvalidOperationException($"ภาพรากใน {RootSheet} มีไม่ครบ 7 เฟรม (เจอ {rootFrames.Length})");
        var root = Map1MonsterBuilder.Load<GameObject>(RootPrefabPath);

        data.monsterPrefab = SetupPrefab(data, laser, rootFrames, root, minion, enemyLayer, reference.sortingLayerID);
        EditorUtility.SetDirty(data);

        var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounterData>(EncounterPath);
        if (encounter == null)
        {
            encounter = ScriptableObject.CreateInstance<RoomEncounterData>();
            AssetDatabase.CreateAsset(encounter, EncounterPath);
        }
        encounter.monstersToSpawn = new[] { data };
        encounter.possibleMonsters = Array.Empty<MonsterData>();
        EditorUtility.SetDirty(encounter);

        int rooms = AssignBossRoom(encounter);

        var map = Map1MonsterBuilder.Load<MapData>(BossMapDataPath);
        map.isBossRoom = true;
        EditorUtility.SetDirty(map);

        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้งบอส Ancient Entborn เรียบร้อย ห้องบอสแมพ 2 ({rooms} ห้อง) เสกบอสตัวนี้ และ MapData_2_boss เป็นห้องบอสแล้ว");
    }

    static GameObject SetupPrefab(MonsterData data, Sprite[] laser, Sprite[] rootFrames, GameObject root, MonsterData minion, int enemyLayer, int sortingLayerID)
    {
        var prefab = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            prefab.layer = enemyLayer;
            prefab.transform.localScale = Vector3.one * BossScale; // ตัวใหญ่ 2–3 เท่าของผู้เล่น (collider/ปลายมือ/ออร่าขยายตาม)

            var body = Ensure<Rigidbody2D>(prefab, out _);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.mass = 50f; // ลูกน้องเดินชนแล้วไม่ดันบอสให้ไถล
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // ภาพยึดฐานที่เท้า collider คลุมลำตัวตั้งแต่เท้าขึ้นไป (อาวุธผู้เล่นตีโดนทั้งตัว)
            var box = Ensure<BoxCollider2D>(prefab, out bool newBox);
            box.isTrigger = false;
            if (newBox)
            {
                box.size = new Vector2(1.5f, 1.8f);
                box.offset = new Vector2(0f, 0.9f);
            }

            var renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingLayerID = sortingLayerID; // ชั้นเดียวกับมอนสเตอร์ตัวอื่น

            var boss = Ensure<AncientEntbornBoss>(prefab, out bool newBoss);
            boss.myData = data;
            boss.laserFrames = laser;
            boss.rootFrames = rootFrames;
            boss.rootPrefab = root;
            if (newBoss || boss.minions == null || boss.minions.Length == 0)
                boss.minions = minion != null ? new[] { minion } : Array.Empty<MonsterData>();

            return PrefabUtility.SaveAsPrefabAsset(prefab, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    static int AssignBossRoom(RoomEncounterData encounter)
    {
        var root = PrefabUtility.LoadPrefabContents(BossMapPrefabPath);
        try
        {
            var rooms = root.GetComponentsInChildren<RoomController>(true);
            foreach (var room in rooms)
            {
                room.roomData = encounter;
                room.canHostEvent = false; // ไม่เอาร้านค้ามาโผล่กลางสนามบอส
            }
            PrefabUtility.SaveAsPrefabAsset(root, BossMapPrefabPath);
            return rooms.Length;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static T Ensure<T>(GameObject go, out bool added) where T : Component
    {
        var component = go.GetComponent<T>();
        added = component == null;
        return added ? go.AddComponent<T>() : component;
    }
}
