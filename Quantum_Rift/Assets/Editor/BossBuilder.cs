using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ติดตั้งบอสประจำแมพ 1 (Echo Commander) ให้พร้อมสู้:
// สร้าง MonsterData ตามตาราง 1.8, ใส่ฟิสิกส์/collider/สคริปต์ให้ prefab บอกับกระสุน
// แล้วผูกเป็นการต่อสู้ของห้องบอสผ่าน RoomController ที่เพื่อนวางไว้ให้แล้ว
//
// สั่งซ้ำได้: ค่าที่ปรับเองใน prefab (ขนาด collider, คูลดาวน์ท่า) จะไม่ถูกทับ
public static class BossBuilder
{
    const string BossPrefabPath = "Assets/Prefab/Boss/EchoCommander/EchoCommander.prefab";
    const string ProjectilePrefabPath = "Assets/Prefab/Boss/EchoCommander/EchoCommanderProjectile.prefab";
    const string BossDataPath = "Assets/Data/Monster/Echo_Commander.asset";
    const string MinionDataPath = "Assets/Data/Monster/Rift_Walker.asset";
    const string EncounterPath = "Assets/Data/Map/RoomData/Map 1 - Boss.asset";
    const string BossMapPrefabPath = "Assets/Prefab/map_1_bossroom.prefab";
    const string HeroPrefabPath = "Assets/Prefab/Hero/Warrior_0.prefab";
    const string EnemyLayerName = "Enemy";
    const string EffectSortingLayer = "Effect";

    [MenuItem("Tools/Quantum Rift/Setup Boss/Echo Commander (แมพ 1)")]
    public static void SetupEchoCommander()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งบอส");

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer < 0)
            throw new InvalidOperationException($"ยังไม่มี layer ชื่อ {EnemyLayerName} สั่ง Setup Monster ก่อนหนึ่งครั้ง");

        // ตาราง 1.8: Echo Commander เลือด 500 ดาเมจ 2 หน่วงโจมตี 2 วินาที
        // ความเร็วในเอกสารเขียน 110 แต่สเกลในโปรเจกต์คนละหน่วย (Rift-Drained Worker เอกสาร 100 ในไฟล์ 1.5)
        // จึงเทียบสัดส่วนเป็น 1.65 ไม่งั้นบอสจะวิ่งเร็วกว่าผู้เล่นจนหนีไม่ได้
        var data = LoadOrCreate<MonsterData>(BossDataPath, out _);
        data.monsterName = "Echo Commander";
        data.maxHealth = 500f;
        data.attackDamage = 2f;
        data.moveSpeed = 1.65f;
        data.attackRange = 1.8f;
        data.attackCooldown = 2f;

        var projectile = SetupProjectile();
        var bossPrefab = SetupBossPrefab(data, projectile, enemyLayer);

        data.monsterPrefab = bossPrefab; // RoomController เสกบอสจากช่องนี้
        EditorUtility.SetDirty(data);

        var encounter = LoadOrCreate<RoomEncounterData>(EncounterPath, out _);
        encounter.monstersToSpawn = new[] { data }; // ห้องบอสใช้โหมดจัดฉาก ไม่สุ่ม
        encounter.possibleMonsters = Array.Empty<MonsterData>();
        EditorUtility.SetDirty(encounter);

        AssignEncounterToBossRoom(encounter);

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งบอส Echo Commander เรียบร้อย เดินเข้าห้องบอสแมพ 1 เพื่อเริ่มสู้ได้เลย");
    }

    static GameObject SetupBossPrefab(MonsterData data, GameObject projectile, int enemyLayer)
    {
        var minion = AssetDatabase.LoadAssetAtPath<MonsterData>(MinionDataPath);
        if (minion == null) Debug.LogWarning($"ไม่เจอ {MinionDataPath} บอสจะยังเรียกลูกน้องไม่ได้");

        var root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            root.layer = enemyLayer; // ดาบเช็คการโดนด้วย LayerMask นี้

            var body = Ensure<Rigidbody2D>(root, out _);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var renderer = root.GetComponent<SpriteRenderer>();
            var box = Ensure<BoxCollider2D>(root, out bool newCollider);
            box.isTrigger = false;
            if (newCollider && renderer != null && renderer.sprite != null)
                box.size = renderer.sprite.bounds.size;

            // prefab บอสมาเป็น sorting layer Default จะจมอยู่ใต้พื้นแมพ ต้องใช้ชั้นเดียวกับตัวละคร
            if (renderer != null) renderer.sortingLayerID = ReadHeroSortingLayer();

            var boss = Ensure<EchoCommanderBoss>(root, out bool newBoss);
            boss.myData = data;
            boss.projectilePrefab = projectile;
            boss.projectileDamage = data.attackDamage;
            if (newBoss && minion != null) boss.minions = new[] { minion };

            return PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static GameObject SetupProjectile()
    {
        var root = PrefabUtility.LoadPrefabContents(ProjectilePrefabPath);
        try
        {
            var renderer = root.GetComponent<SpriteRenderer>();

            var circle = Ensure<CircleCollider2D>(root, out bool newCollider);
            circle.isTrigger = true; // ต้องเป็น trigger ไม่งั้นกระสุนจะดันตัวผู้เล่นแทนที่จะทะลุเข้าไปโดน
            if (newCollider && renderer != null && renderer.sprite != null)
                circle.radius = Mathf.Max(0.05f, renderer.sprite.bounds.extents.x * 0.8f);

            // ต้องมี Rigidbody2D ถึงจะรู้ว่าชนกำแพง (collider ของ Tilemap เป็นแบบ static)
            var body = Ensure<Rigidbody2D>(root, out _);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            Ensure<BossProjectile>(root, out _);

            if (renderer != null)
            {
                int effect = SortingLayer.NameToID(EffectSortingLayer);
                if (SortingLayer.IsValid(effect)) renderer.sortingLayerID = effect;
                renderer.sortingOrder = 10; // ให้อยู่หน้าตัวละครเหมือนคลื่นดาบ
            }

            return PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ห้องบอสมี RoomController ติดมากับแมพอยู่แล้ว ขาดแค่ข้อมูลว่าจะเสกอะไร
    static void AssignEncounterToBossRoom(RoomEncounterData encounter)
    {
        var root = PrefabUtility.LoadPrefabContents(BossMapPrefabPath);
        try
        {
            var rooms = root.GetComponentsInChildren<RoomController>(true);
            if (rooms.Length == 0)
            {
                Debug.LogWarning($"ไม่เจอ RoomController ใน {BossMapPrefabPath} ต้องใส่ Room Data เองใน Inspector");
                return;
            }

            foreach (var room in rooms)
            {
                room.roomData = encounter;
                room.canHostEvent = false; // ไม่เอาร้านค้ามาโผล่กลางสนามบอส
            }

            PrefabUtility.SaveAsPrefabAsset(root, BossMapPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ยึด sorting layer ของตัวละครเป็นหลัก เหมือนที่ MonsterBuilder ทำ
    static int ReadHeroSortingLayer()
    {
        var hero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        var renderer = hero != null ? hero.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (renderer == null)
        {
            Debug.LogWarning($"อ่าน sorting layer จาก {HeroPrefabPath} ไม่ได้ บอสอาจถูกวาดหลังพื้นแมพ");
            return 0;
        }
        return renderer.sortingLayerID;
    }

    static T Ensure<T>(GameObject go, out bool added) where T : Component
    {
        var component = go.GetComponent<T>();
        added = component == null;
        return added ? go.AddComponent<T>() : component;
    }

    static T LoadOrCreate<T>(string path, out bool isNew) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        isNew = asset == null;
        if (!isNew) return asset;

        string folder = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            throw new InvalidOperationException($"ยังไม่มีโฟลเดอร์ {folder}");

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
