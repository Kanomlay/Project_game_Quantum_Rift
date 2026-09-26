using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ขั้นที่ 2 มอนสเตอร์ของแมพ 1 (เรือบนอวกาศ): Rift-Drained Worker, Flux Jaw, Echo Stalker,
// Phase Soldier, Mutated Heavy
// 1. ติดตั้ง Flux Jaw กับ Echo Stalker ที่ยังมีแค่ภาพ/Animator ให้สู้ได้ (ไม่แตะ Animator ที่เพื่อนตั้งไว้)
// 2. ตั้งค่าสถานะทั้ง 5 ตัวตามตาราง 1.6–1.7
// 3. สร้างชุดมอนสเตอร์ประจำห้องของแมพ 1-1 / 1-2 / 1-3 แล้วใส่ให้ทุกห้องในทั้ง 3 ผัง
//    ห้องที่มีประตูออกด่านได้หัวหน้าหน่วยคุมทุกครั้ง ห้องอื่นสุ่มตามความยากของด่าน
// 4. Phase Soldier ยกปืนเล็งค้างยิงต่อเนื่อง (เพิ่ม state ยก/ค้าง/ยิง/ลดปืน ใน Animator เดิม)
//
// สั่งซ้ำได้: ขนาด collider และระยะโจมตีตั้งให้เฉพาะตอนเพิ่มครั้งแรก ปรับเองใน prefab แล้วไม่โดนทับ
public static class Map1MonsterBuilder
{
    const string MonsterDataFolder = "Assets/Data/Monster";
    const string MonsterPrefabFolder = "Assets/Prefab/Monster";
    const string RoomDataFolder = "Assets/Data/Map/RoomData";
    const string SortingReferencePrefab = "Assets/Prefab/Monster/Rift-Drained Worker_0.prefab";
    const string EnemyLayerName = "Enemy";

    // ความเร็วในเอกสารเป็นคนละหน่วยกับในเกม เทียบจาก Rift-Drained Worker ที่เอกสารเขียน 100
    // แต่ในเกมเดินที่ 1.5 หน่วย/วินาที (ตัวที่เล่นได้จริงตัวแรก) จึงคูณ 0.015 ทุกตัว
    const float SpeedScale = 1.5f / 100f;

    // พอร์ทัลออกด่านวางไว้กลางห้องสุดท้ายของแต่ละผัง ใช้หาห้องทางออก (ค่าเดียวกับ MapEventDirector)
    const float PortalRoomRadius = 3f;

    sealed class MonsterSpec
    {
        public string Asset;   // ชื่อไฟล์ MonsterData
        public string Name;
        // ตาราง 1.6 / 1.7: ความเร็ว (หน่วยเอกสาร), พลังชีวิต, หน่วงโจมตี (วินาที), ความเสียหาย
        public float Speed, Health, Cooldown, Damage;

        // ตัวที่ยังไม่เคยติดตั้ง (มีแค่ภาพ) ใส่ข้อมูลส่วนนี้ ตัวที่เพื่อนทำท่าโจมตีไว้แล้วเว้นว่าง แก้แค่ค่าสถานะ
        public string Prefab;
        public MonsterCombatActions.Style Style;
        public float MeleeDistance;
        public float LungeTrigger;
        public Vector2 ColliderSize;   // หน่วยของ sprite ก่อนย่อ prefab (วัดจากภาพท่ายืน)
    }

    static readonly MonsterSpec[] Monsters =
    {
        new MonsterSpec { Asset = "Rift_Walker", Name = "Rift-Drained Worker", Speed = 100f, Health = 22f, Cooldown = 2f, Damage = 0.5f },
        // หมาจักรกลงับ: ท่าโจมตีเป็นการงับตรงหน้า ใช้แบบประชิดทั่วไป (Wrench) กระทบที่เฟรม 4 ตอนปากหุบ
        new MonsterSpec { Asset = "Flux-Jaw", Name = "Flux Jaw", Speed = 100f, Health = 18f, Cooldown = 1.5f, Damage = 0.5f,
                          Prefab = "Flux Jaw_0", Style = MonsterCombatActions.Style.Wrench, MeleeDistance = 1.4f,
                          ColliderSize = new Vector2(0.85f, 0.6f) },
        // นักล่าเงา: ภาพท่าโจมตีพุ่งตัวไปข้างหน้าพร้อมกรงเล็บ จึงพุ่งเข้าหาช่วงง้างท่า (Lunge) เร็วสุดในแมพ 1
        new MonsterSpec { Asset = "Echo-Stalker", Name = "Echo Stalker", Speed = 125f, Health = 15f, Cooldown = 1f, Damage = 0.5f,
                          Prefab = "Echo Stalker_0", Style = MonsterCombatActions.Style.Lunge, MeleeDistance = 1.4f, LungeTrigger = 3.2f,
                          ColliderSize = new Vector2(0.7f, 0.95f) },
        new MonsterSpec { Asset = "Phase-Soldier", Name = "Phase Soldier", Speed = 100f, Health = 140f, Cooldown = 2f, Damage = 1f },
        new MonsterSpec { Asset = "Mutated-Heavy", Name = "Mutated Heavy", Speed = 100f, Health = 100f, Cooldown = 1.5f, Damage = 1f },
    };

    sealed class Tier
    {
        public string Asset;
        public string[] Pool;     // มอนสเตอร์ทั่วไป (ชื่อไฟล์ MonsterData)
        public int Min, Max;      // จำนวนต่อระลอก (จุดเกิดสุ่มกระจายทั่วห้อง ไม่จำกัดตามจุดที่วางไว้แล้ว)
        public int Waves = 1;     // จำนวนระลอก ระลอกถัดไปมาเมื่อมอนในห้องเหลือไม่เกิน NextWaveWhenAlive ตัว
        public string[] Leaders;  // หัวหน้าหน่วย มากับระลอกสุดท้าย
        public float LeaderChance;
    }

    const int NextWaveWhenAlive = 1;

    static readonly string[] Starters = { "Rift_Walker", "Flux-Jaw" };
    static readonly string[] Commons = { "Rift_Walker", "Flux-Jaw", "Echo-Stalker" };
    static readonly string[] Leaders = { "Phase-Soldier", "Mutated-Heavy" };

    // ความยากไล่ขึ้นตามด่าน: 1-1 ปูพื้นด้วยตัวช้า ๆ 2 ระลอก, 1-2 เริ่มมี Echo Stalker และหัวหน้าหน่วยแทรก,
    // 1-3 กองใหญ่ขึ้นเป็น 3 ระลอก ห้องทางออกมีหัวหน้าหน่วยคุมระลอกสุดท้ายทุกครั้ง
    static readonly (string mapPrefab, Tier room, Tier exit)[] Maps =
    {
        ("Assets/Prefab/map_1.prefab",
            new Tier { Asset = "Map 1-1 - Room", Pool = Starters, Min = 2, Max = 3, Waves = 2 },
            new Tier { Asset = "Map 1-1 - Exit", Pool = Commons, Min = 3, Max = 4, Waves = 2, Leaders = new[] { "Phase-Soldier" }, LeaderChance = 1f }),
        ("Assets/Prefab/map_1_2.prefab",
            new Tier { Asset = "Map 1-2 - Room", Pool = Commons, Min = 3, Max = 4, Waves = 2, Leaders = Leaders, LeaderChance = 0.2f },
            new Tier { Asset = "Map 1-2 - Exit", Pool = Commons, Min = 3, Max = 4, Waves = 2, Leaders = Leaders, LeaderChance = 1f }),
        ("Assets/Prefab/map_1_3.prefab",
            new Tier { Asset = "Map 1-3 - Room", Pool = Commons, Min = 3, Max = 4, Waves = 3, Leaders = Leaders, LeaderChance = 0.35f },
            new Tier { Asset = "Map 1-3 - Exit", Pool = Commons, Min = 3, Max = 4, Waves = 3, Leaders = Leaders, LeaderChance = 1f }),
    };

    [MenuItem("Tools/Quantum Rift/Setup Monster/Map 1 Monsters + Rooms (ตาราง 1.6–1.7)")]
    public static void SetupMap1()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งมอนสเตอร์");

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer < 0)
            throw new InvalidOperationException($"ยังไม่มี layer ชื่อ {EnemyLayerName} สั่ง Setup Monster/Rift-Drained Worker ก่อนหนึ่งครั้ง");

        var reference = Load<GameObject>(SortingReferencePrefab).GetComponent<SpriteRenderer>();
        if (reference == null)
            throw new InvalidOperationException($"{SortingReferencePrefab} ไม่มี SpriteRenderer ให้ลอก sorting layer");

        var data = new Dictionary<string, MonsterData>();
        foreach (var spec in Monsters)
            data[spec.Asset] = SetupMonster(spec, enemyLayer, reference.sortingLayerID);

        foreach (var (mapPrefab, room, exit) in Maps)
            AssignRooms(mapPrefab, BuildTier(room, data), BuildTier(exit, data));

        SetupPhaseSoldierStance();

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งมอนสเตอร์แมพ 1 ครบ 5 ตัว และใส่ชุดมอนสเตอร์ให้ทุกห้องในแมพ 1-1 / 1-2 / 1-3 แล้ว");
    }

    // ตั้งแค่ชุดมอนสเตอร์ประจำห้อง (จำนวน/ระลอก/หัวหน้าหน่วย) ของ 1-1 / 1-2 / 1-3 ตามตาราง Maps
    // ไม่แตะ prefab มอนสเตอร์และแมพ ใช้หลังปรับตัวเลขในตาราง หรือหลังเพิ่มระบบระลอก
    [MenuItem("Tools/Quantum Rift/Setup Monster/Map 1 Room Waves (ระลอกมอนสเตอร์)")]
    public static void SetupMap1Waves()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งตั้งระลอกมอนสเตอร์");

        var data = Monsters.ToDictionary(spec => spec.Asset, spec => Load<MonsterData>($"{MonsterDataFolder}/{spec.Asset}.asset"));
        foreach (var (_, room, exit) in Maps)
        {
            foreach (var tier in new[] { room, exit })
            {
                var encounter = BuildTier(tier, data);
                Debug.Log($"{tier.Asset}: {encounter.minWaves} ระลอก ระลอกละ {encounter.minMonsters}–{encounter.maxMonsters} ตัว" +
                          (encounter.leaderChance > 0f ? $" หัวหน้าหน่วย {encounter.leaderChance:P0}" : ""));
            }
        }
        AssetDatabase.SaveAssets();
    }

    static MonsterData SetupMonster(MonsterSpec spec, int enemyLayer, int sortingLayerID)
    {
        string path = $"{MonsterDataFolder}/{spec.Asset}.asset";
        var data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (data == null)
        {
            if (spec.Prefab == null) throw new InvalidOperationException($"ไม่เจอ {path}");
            data = ScriptableObject.CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.monsterName = spec.Name;
        data.moveSpeed = spec.Speed * SpeedScale;
        data.maxHealth = spec.Health;
        data.attackCooldown = spec.Cooldown;
        data.attackDamage = spec.Damage;

        if (spec.Prefab != null)
        {
            string prefabPath = $"{MonsterPrefabFolder}/{spec.Prefab}.prefab";
            SetupPrefab(prefabPath, spec, data, enemyLayer, sortingLayerID);
            data.monsterPrefab = Load<GameObject>(prefabPath); // RoomController เสกมอนสเตอร์จากช่องนี้
            data.attackRange = spec.MeleeDistance;
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    // แบบเดียวกับ MonsterBuilder แต่ไม่แตะ Animator (เพื่อนตั้ง isWalking/Attack/isDead ครบแล้ว)
    static void SetupPrefab(string prefabPath, MonsterSpec spec, MonsterData data, int enemyLayer, int sortingLayerID)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            root.layer = enemyLayer; // อาวุธผู้เล่นเช็คการโดนด้วย LayerMask นี้

            var body = Ensure<Rigidbody2D>(root, out _);
            body.bodyType = RigidbodyType2D.Dynamic;  // โดนตีกระเด็น/พุ่งตัวผ่าน linearVelocity
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var box = Ensure<BoxCollider2D>(root, out bool newBox);
            box.isTrigger = false;
            if (newBox)
            {
                box.size = spec.ColliderSize;
                box.offset = Vector2.zero; // pivot ของภาพตั้งไว้กลางตัวอยู่แล้ว
            }

            // prefab มาเป็น sorting layer Default จะถูกวาดจมใต้พื้นแมพ ใช้ชั้นเดียวกับมอนสเตอร์ตัวอื่น
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingLayerID = sortingLayerID;

            Ensure<MonsterController>(root, out _).myData = data;

            var combat = Ensure<MonsterCombatActions>(root, out bool newCombat);
            combat.style = spec.Style;
            if (newCombat)
            {
                combat.meleeDistance = spec.MeleeDistance;
                if (spec.LungeTrigger > 0f) combat.lungeTriggerDistance = spec.LungeTrigger;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static RoomEncounterData BuildTier(Tier tier, Dictionary<string, MonsterData> data)
    {
        string path = $"{RoomDataFolder}/{tier.Asset}.asset";
        var encounter = AssetDatabase.LoadAssetAtPath<RoomEncounterData>(path);
        if (encounter == null)
        {
            encounter = ScriptableObject.CreateInstance<RoomEncounterData>();
            AssetDatabase.CreateAsset(encounter, path);
        }

        encounter.monstersToSpawn = Array.Empty<MonsterData>(); // ใช้โหมดสุ่ม
        encounter.possibleMonsters = tier.Pool.Select(name => data[name]).ToArray();
        encounter.minMonsters = tier.Min;
        encounter.maxMonsters = tier.Max;
        encounter.minWaves = tier.Waves;
        encounter.maxWaves = tier.Waves;
        encounter.nextWaveWhenAlive = NextWaveWhenAlive;
        encounter.leaderMonsters = tier.Leaders != null ? tier.Leaders.Select(name => data[name]).ToArray() : Array.Empty<MonsterData>();
        encounter.leaderChance = tier.LeaderChance;
        EditorUtility.SetDirty(encounter);
        return encounter;
    }

    // ทุกห้องในทั้ง 3 ผัง (รวมผังที่ถูกปิดตอนเล่น เพราะสุ่มผังกันตอนเริ่มด่าน)
    // ห้องทางออกได้ชุดหัวหน้าหน่วย และไม่ให้ร้านค้ามาลง (ไม่งั้นร้านทับพอร์ทัลกลางห้อง)
    static void AssignRooms(string mapPrefab, RoomEncounterData room, RoomEncounterData exit)
    {
        var root = PrefabUtility.LoadPrefabContents(mapPrefab);
        try
        {
            // ใน prefab ทุกผังเปิดอยู่พร้อมกัน ห้องของผังอื่นอาจอยู่ใกล้ประตูในรัศมีด้วย
            // (Switchback ห้อง 3 ห่างประตูของ OriginalLoop แค่ 2.5) จึงให้แต่ละประตูเลือกห้องที่ใกล้ที่สุดห้องเดียว
            var rooms = root.GetComponentsInChildren<RoomController>(true);
            var exitRooms = new HashSet<RoomController>();
            foreach (var portal in root.GetComponentsInChildren<MapPortal>(true))
            {
                var nearest = rooms.OrderBy(r => Vector2.Distance(r.transform.position, portal.transform.position)).FirstOrDefault();
                if (nearest != null && Vector2.Distance(nearest.transform.position, portal.transform.position) < PortalRoomRadius)
                    exitRooms.Add(nearest);
            }

            int exits = 0, total = 0;
            foreach (var controller in rooms)
            {
                bool isExit = exitRooms.Contains(controller);
                controller.roomData = isExit ? exit : room;
                controller.canHostEvent = !isExit;
                total++;
                if (isExit) exits++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, mapPrefab);
            Debug.Log($"{mapPrefab}: ใส่ชุดมอนสเตอร์ {total} ห้อง (ห้องทางออก {exits} ห้อง มีหัวหน้าหน่วยคุม)");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---- Phase Soldier: ยกปืนค้าง ----
    // ภาพ Phase-Shoot 7 เฟรม: 0 ถือปืนลง, 1–2 ยกเล็ง, 3 ไฟแลบ, 4–5 เล็งหลังยิง, 6 ลดปืน
    // ท่าโจมตีเดิมเล่นครบ 7 เฟรมทุกนัด (ยก-ยิง-ลด) จึงแยกเป็น 4 ท่า แล้วให้ MonsterCombatActions (holdAim) คุม:
    // idle/move --isAiming--> ยกปืน → เล็งค้าง --Fire--> ยิง → เล็งค้าง --!isAiming--> ลดปืน → idle
    const string PhasePrefab = "Assets/Prefab/Monster/Phase Soldier_0.prefab";
    const string PhaseController = "Assets/image/Monster/Phase Soldier/Phase Soldier_0.controller";
    const string PhaseClipFolder = "Assets/image/Monster/Phase Soldier";
    const string PhaseShootSheet = "Assets/image/Monster/AttackRevision-v3/Phase-Shoot.png";
    const float PhaseFrameRate = 12f; // เท่าคลิปเดิมของเพื่อน

    static void SetupPhaseSoldierStance()
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(PhaseShootSheet).OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);
        Sprite F(int i) => frames.TryGetValue($"Phase-Shoot_{i}", out var sprite)
            ? sprite : throw new InvalidOperationException($"ไม่เจอ Phase-Shoot_{i} ใน {PhaseShootSheet}");

        var raise = SpriteClip("Phase Soldier_aim", false, F(0), F(1), F(2));
        var hold = SpriteClip("Phase Soldier_aimHold", true, F(2));
        var fire = SpriteClip("Phase Soldier_fire", false, F(3), F(4), F(5));
        var lower = SpriteClip("Phase Soldier_lower", false, F(6), F(0));

        var controller = Load<AnimatorController>(PhaseController);
        EnsureParameter(controller, "isAiming", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Fire", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        var idle = FindState(machine, "Phase Soldier_idel");
        var move = FindState(machine, "Phase Soldier_move");
        if (idle == null || move == null)
            throw new InvalidOperationException($"ไม่เจอ state idle/move ใน {PhaseController}");

        var aimState = EnsureState(machine, "Phase Soldier_aim", raise, new Vector3(500f, 0f));
        var holdState = EnsureState(machine, "Phase Soldier_aimHold", hold, new Vector3(750f, 0f));
        var fireState = EnsureState(machine, "Phase Soldier_fire", fire, new Vector3(750f, 120f));
        var lowerState = EnsureState(machine, "Phase Soldier_lower", lower, new Vector3(500f, 120f));

        var aimOn = (AnimatorConditionMode.If, "isAiming");
        var aimOff = (AnimatorConditionMode.IfNot, "isAiming");
        var alive = (AnimatorConditionMode.IfNot, "isDead");

        SetTransition(idle, aimState, false, aimOn, alive);
        SetTransition(move, aimState, false, aimOn, alive);
        SetTransition(aimState, holdState, true);           // ยกเสร็จค้างไว้
        SetTransition(aimState, lowerState, false, aimOff); // ยกไม่ทันเสร็จผู้เล่นก็หลุดระยะ
        SetTransition(holdState, fireState, false, (AnimatorConditionMode.If, "Fire"));
        SetTransition(holdState, lowerState, false, aimOff);
        SetTransition(fireState, holdState, true);          // ยิงจบกลับไปเล็งค้าง (ถ้าต้องลดปืน holdState จะพาไปเอง)
        SetTransition(lowerState, idle, true);
        EditorUtility.SetDirty(controller);

        var root = PrefabUtility.LoadPrefabContents(PhasePrefab);
        try
        {
            var combat = root.GetComponent<MonsterCombatActions>();
            if (combat == null) throw new InvalidOperationException($"{PhasePrefab} ไม่มี MonsterCombatActions");
            combat.holdAim = true;
            PrefabUtility.SaveAsPrefabAsset(root, PhasePrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // คลิปสลับ sprite ทีละเฟรม เติมคีย์ท้ายซ้ำเฟรมสุดท้าย ไม่งั้นเฟรมสุดท้ายจะโชว์แค่ 0 วินาที
    static AnimationClip SpriteClip(string name, bool loop, params Sprite[] sprites)
    {
        string path = $"{PhaseClipFolder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = PhaseFrameRate;

        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i <= sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / PhaseFrameRate, value = sprites[Mathf.Min(i, sprites.Length - 1)] };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.All(p => p.name != name)) controller.AddParameter(name, type);
    }

    static AnimatorState FindState(AnimatorStateMachine machine, string name) =>
        machine.states.Select(child => child.state).FirstOrDefault(state => state != null && state.name == name);

    static AnimatorState EnsureState(AnimatorStateMachine machine, string name, AnimationClip clip, Vector3 position)
    {
        var state = FindState(machine, name) ?? machine.AddState(name, position);
        state.motion = clip;
        state.tag = MonsterCombatActions.AimTag; // ท่าที่ยกปืนอยู่ ห้ามเดินระหว่างเล่น
        return state;
    }

    // แทนเส้นเดิมระหว่างสอง state ด้วยเส้นใหม่ ไม่มี exit time = สลับทันทีที่เงื่อนไขครบ
    static void SetTransition(AnimatorState from, AnimatorState to, bool afterClip,
                              params (AnimatorConditionMode mode, string parameter)[] conditions)
    {
        foreach (var existing in from.transitions.Where(t => t.destinationState == to).ToArray())
            from.RemoveTransition(existing);

        var transition = from.AddTransition(to);
        transition.hasExitTime = afterClip;
        transition.exitTime = 1f;
        transition.duration = 0f;
        foreach (var (mode, parameter) in conditions)
            transition.AddCondition(mode, 0f, parameter);
    }

    static T Ensure<T>(GameObject go, out bool added) where T : Component
    {
        var component = go.GetComponent<T>();
        added = component == null;
        return added ? go.AddComponent<T>() : component;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
