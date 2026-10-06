using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Spec = Map1MonsterBuilder.MonsterSpec;
using Tier = Map1MonsterBuilder.Tier;

// ขั้นที่ 1 ของแมพ 2 (มิติป่า): Dimensional Wolf, Rootlings, Woodmine, Zero Husk, Forest Wraith
// 1. ต่อ prefab ที่ยังมีแค่ภาพ/Animator ให้สู้ได้ (layer Enemy, Rigidbody, collider, MonsterController, MonsterCombatActions)
//    ไม่แตะ Animator และของที่เพื่อนทำไว้ (ท่ากระโจนของหมาป่า, ระเบิดตอนตายของ Zero Husk)
// 2. ค่าสถานะตามตาราง 1.6–1.7 (ความเร็วคูณ 0.015 แบบเดียวกับแมพ 1, Rootlings ไม่เดิน)
// 3. ชุดมอนประจำห้องของ 2-1 / 2-2 ใส่ให้ทุกห้องในทั้ง 3 ผัง ห้องทางออกมีหัวหน้าหน่วยคุมทุกครั้ง
// ท่าเฉพาะตัว: Rootlings รากแทงใต้เท้า + มุดดิน, Woodmine ยิงพิษ, Zero Husk ระเบิดตัวเองตอนเลือดน้อย,
// Forest Wraith ฟาดเถาวัลย์เป็นแนว, หมาป่ามาเป็นฝูง (ค่าที่ Configure ใส่ทุกครั้งที่สั่งเมนู)
// สั่งซ้ำได้: ขนาด collider และระยะประชิดตั้งให้เฉพาะตอนเพิ่มครั้งแรก ปรับเองใน prefab แล้วไม่โดนทับ
public static class Map2MonsterBuilder
{
    const string BulletSprite = "Assets/image/Monster/AttackRevision-v3/Purple-Bullet.png";
    const string RootPrefab = "Assets/Prefab/Boss/AncientEntborn/AncientEntbornRootPillar.prefab";
    static readonly Color PoisonGreen = new Color(0.55f, 1f, 0.35f);
    const float PoisonSeconds = 5f;   // เอกสาร 1.3.7: พิษต่อเนื่อง 5 วินาที
    const float PoisonDamage = 0.5f;  // รวมทั้งช่วง (0.1 ต่อวินาที)

    static readonly Color WolfTrail = new Color(0.5f, 0.85f, 1f);
    static readonly Color Leaves = new Color(1f, 0.55f, 0.2f);
    static readonly Color Dirt = new Color(0.55f, 0.42f, 0.28f);

    static Spec[] Monsters(Sprite bullet, GameObject root) => new[]
    {
        // หมาป่ามิติ: ท่ากระโจนของเพื่อน + เงาภาพค้างตอนกระโจน มาเป็นฝูง 2–3 ตัวเกิดใกล้กัน
        new Spec { Asset = "Dimensional-Wolf", Name = "Dimensional Wolf", Speed = 150f, Health = 18f, Cooldown = 1f, Damage = 0.5f,
                   Prefab = "Dimensional Wolf_0", Style = MonsterCombatActions.Style.Pounce, MeleeDistance = 1.35f,
                   ColliderSize = new Vector2(0.85f, 0.5f), PackMin = 2, PackMax = 3,
                   Configure = c => { c.pounceTrail = true; c.trailColor = WolfTrail; } },
        // รากสามหัว: ไม่เดิน (ความเร็ว "ไม่มี") เรียกรากแทงขึ้นใต้เท้าผู้เล่นทุก 4 วินาที โดนแล้วติดพิษ มีเศษดินร่วงตลอด
        new Spec { Asset = "Rootlings", Name = "Rootlings", Speed = 0f, Health = 22f, Cooldown = 4f, Damage = 0.5f,
                   Prefab = "Rootlings-3Heads_0", Style = MonsterCombatActions.Style.Root, MeleeDistance = 1.3f,
                   ColliderSize = new Vector2(1.4f, 1.2f),
                   Configure = c =>
                   {
                       c.rootPrefab = root; c.poisonSeconds = PoisonSeconds; c.poisonDamage = PoisonDamage;
                       c.ambientColor = Dirt; c.ambientEvery = 0.6f; c.ambientFromTop = false;
                   } },
        // ต้นไม้ตาเดียว: ตาเรืองเขียวก่อนยิง ลูกพลังเขียวมีหาง โดนแล้วแตกเป็นละอองและติดพิษ
        new Spec { Asset = "Woodmine", Name = "Woodmine", Speed = 100f, Health = 16f, Cooldown = 2f, Damage = 0.5f,
                   Prefab = "Woodmine_0", Style = MonsterCombatActions.Style.Rifle, MeleeDistance = 1.3f,
                   ColliderSize = new Vector2(1.5f, 2f),
                   Configure = c =>
                   {
                       c.holdAim = false;
                       c.projectileSprite = bullet;
                       c.projectileTint = PoisonGreen;
                       c.projectileSize = 1.2f;
                       c.projectileSpeed = 6f;
                       c.projectileLifetime = 2.5f;
                       c.rangedDistance = 6f;
                       c.projectileTrail = true;
                       c.warnTint = PoisonGreen;
                       c.poisonSeconds = PoisonSeconds;
                       c.poisonDamage = PoisonDamage;
                   } },
        // หัวหน้าหน่วย: โครงกระดูกไฟฟ้า ตะปบประชิด เลือด 25% เรืองฟ้าวิ่งเข้าหาแล้วระเบิดตัวเอง (ภาพระเบิดของเพื่อน)
        new Spec { Asset = "Zero-Husk", Name = "Zero Husk", Speed = 100f, Health = 70f, Cooldown = 3f, Damage = 1f,
                   Prefab = "Zero Husk_0", Style = MonsterCombatActions.Style.Wrench, MeleeDistance = 1.3f,
                   ColliderSize = new Vector2(0.7f, 1f),
                   Configure = c => { c.selfDestructAt = 0.25f; } },
        // หัวหน้าหน่วย: วิญญาณป่า ลอยขึ้นลง ใบไม้ร่วงตามตัว ฟาดเถาวัลย์เป็นแนวยาวมีแถบเตือนบนพื้น
        new Spec { Asset = "Forest-Wraith", Name = "Forest Wraith", Speed = 100f, Health = 100f, Cooldown = 1.5f, Damage = 1f,
                   Prefab = "Forest Wraith_0", Style = MonsterCombatActions.Style.Vine, MeleeDistance = 1.8f,
                   ColliderSize = new Vector2(0.7f, 0.95f),
                   Configure = c =>
                   {
                       c.floating = true;
                       c.ambientColor = Leaves; c.ambientEvery = 0.35f; c.ambientFromTop = true;
                   } },
    };

    static readonly string[] Commons = { "Dimensional-Wolf", "Woodmine", "Rootlings" };
    static readonly string[] Leaders = { "Zero-Husk", "Forest-Wraith" };

    // แมพป่ายากกว่าแมพ 1: 2-1 เริ่มที่ระลอกละ 3–4 ตัว 2 ระลอก, 2-2 เป็น 3 ระลอกและหัวหน้าหน่วยแทรกบ่อยขึ้น
    static readonly (string mapPrefab, Tier room, Tier exit)[] Maps =
    {
        ("Assets/Prefab/Map_2.prefab",
            new Tier { Asset = "Map 2-1 - Room", Pool = Commons, Min = 3, Max = 4, Waves = 2, Leaders = Leaders, LeaderChance = 0.2f },
            new Tier { Asset = "Map 2-1 - Exit", Pool = Commons, Min = 3, Max = 4, Waves = 2, Leaders = Leaders, LeaderChance = 1f }),
        ("Assets/Prefab/Map_2_2.prefab",
            new Tier { Asset = "Map 2-2 - Room", Pool = Commons, Min = 3, Max = 4, Waves = 3, Leaders = Leaders, LeaderChance = 0.35f },
            new Tier { Asset = "Map 2-2 - Exit", Pool = Commons, Min = 3, Max = 4, Waves = 3, Leaders = Leaders, LeaderChance = 1f }),
        // 2-3 ถึง 2-5 ไล่ขึ้นทีละขั้น: ระลอกละได้ถึง 5 ตัว แล้วอย่างน้อย 4 ตัว หัวหน้าหน่วยแทรกบ่อยขึ้น
        ("Assets/Prefab/ExpandedStages/Map_2_3.prefab",
            new Tier { Asset = "Map 2-3 - Room", Pool = Commons, Min = 3, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 0.4f },
            new Tier { Asset = "Map 2-3 - Exit", Pool = Commons, Min = 3, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 1f }),
        ("Assets/Prefab/ExpandedStages/Map_2_4.prefab",
            new Tier { Asset = "Map 2-4 - Room", Pool = Commons, Min = 3, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 0.45f },
            new Tier { Asset = "Map 2-4 - Exit", Pool = Commons, Min = 3, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 1f }),
        ("Assets/Prefab/ExpandedStages/Map_2_5.prefab",
            new Tier { Asset = "Map 2-5 - Room", Pool = Commons, Min = 4, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 0.55f },
            new Tier { Asset = "Map 2-5 - Exit", Pool = Commons, Min = 4, Max = 5, Waves = 3, Leaders = Leaders, LeaderChance = 1f }),
    };

    [MenuItem("Tools/Quantum Rift/Setup Monster/Map 2 Monsters + Rooms (มอนแมพป่า)")]
    public static void SetupMap2()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งมอนสเตอร์");

        int enemyLayer = LayerMask.NameToLayer(Map1MonsterBuilder.EnemyLayerName);
        if (enemyLayer < 0)
            throw new InvalidOperationException($"ยังไม่มี layer ชื่อ {Map1MonsterBuilder.EnemyLayerName}");

        var reference = Map1MonsterBuilder.Load<GameObject>(Map1MonsterBuilder.SortingReferencePrefab).GetComponent<SpriteRenderer>();
        var bullet = AssetDatabase.LoadAllAssetsAtPath(BulletSprite).OfType<Sprite>().FirstOrDefault();
        if (bullet == null) throw new InvalidOperationException($"ไม่เจอภาพกระสุน {BulletSprite}");
        var root = Map1MonsterBuilder.Load<GameObject>(RootPrefab);

        var data = new Dictionary<string, MonsterData>();
        foreach (var spec in Monsters(bullet, root))
            data[spec.Asset] = Map1MonsterBuilder.SetupMonster(spec, enemyLayer, reference.sortingLayerID);

        foreach (var (mapPrefab, room, exit) in Maps)
            Map1MonsterBuilder.AssignRooms(mapPrefab, Map1MonsterBuilder.BuildTier(room, data), Map1MonsterBuilder.BuildTier(exit, data));

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งมอนสเตอร์แมพ 2 ครบ 5 ตัว และใส่ชุดมอนสเตอร์ให้ทุกห้องในแมพ 2-1 ถึง 2-5 แล้ว");
    }
}
