using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// อาวุธประจำอาชีพทั้ง 4 ชิ้นตามตาราง 1.2 (ขั้นที่ 1 ระบบต่อสู้พื้นฐาน) ใช้ภาพจากชุด Quantum-Rift-Weapons-23-v1
// - ดาบขึ้นสนิม (นักรบ): เปลี่ยนเป็นภาพชุดใหม่ และแก้ค่าให้ตรงเอกสาร (เดิมดาเมจ 20 เอกสาร 4)
// - กรงเล็บขึ้นสนิม (มนุษย์กลายพันธุ์): ภาพชุดใหม่ ถือสองมือ ตะปบสลับกัน
// - ธนูขึ้นสนิม (นักธนู) กับ ปืนขึ้นสนิม (นักประดิษฐ์): สร้าง prefab อาวุธ + prefab กระสุน + WeaponData
//   แล้วใส่ให้ฮีโร่ แทนดาบที่ถือไว้ชั่วคราวตอน A2
// (แทน WeaponBuilder เดิมที่สร้างกรงเล็บจากภาพเก่า)
//
// สั่งซ้ำได้: ขนาด prefab กับตำแหน่ง AttackPoint ตั้งให้เฉพาะครั้งแรก ปรับเองใน prefab แล้วไม่โดนทับ
// ส่วนค่าสเตตัสจากเอกสารกับภาพแต่ละเฟรมจะถูกตั้งใหม่ทุกครั้ง
public static class StarterWeaponBuilder
{
    internal const string PackFolder = "Assets/image/Weapon/Quantum-Rift-Weapons-23-v1/sprites";
    internal const string WeaponPrefabFolder = "Assets/Prefab/Weapon";
    internal const string ProjectilePrefabFolder = "Assets/Prefab/Weapon/Projectile";
    internal const string DataFolder = "Assets/Data/Weapon";
    const string HeroFolder = "Assets/Prefab/Hero/Heroes-BlondeStyle-NoHands-v2";
    internal const string SwordDataPath = "Assets/Data/Weapon/Rusty Sword.asset";
    const string EffectSortingLayer = "Effect";

    // ต้องตรงกับ ArtPackImporter ไม่งั้นระยะเลื่อนของเฟรมที่คำนวณจากพิกเซลจะเพี้ยน
    internal const float PixelsPerUnit = 100f;

    // ขนาดอาวุธชุดนี้ทั้งชุดใช้ค่าเดียวกัน อาวุธจะใหญ่เล็กตามภาพจริง (ธนูสูงราวตัวละคร)
    internal const float PackWeaponScale = 0.8f;
    internal const float ProjectileScale = 0.4f;

    // เฟรมหนึ่งภาพ + จุดจับ (พิกเซล นับจากมุมซ้ายบนของภาพ 192 × 128) วัดจากภาพจริงทีละเฟรม
    internal struct FrameSpec
    {
        public string File;
        public Vector2 Grip;
        public FrameSpec(string file, float x, float y) { File = file; Grip = new Vector2(x, y); }
    }

    internal sealed class RangedSpec
    {
        public string Name;
        public string Folder;          // โฟลเดอร์ย่อยในชุดภาพ
        public WeaponType Type;
        public FrameSpec Idle;
        public FrameSpec[] Attack;
        public int ReleaseFrame;
        public float FrameTime;
        public Vector2 Muzzle;         // ปลายลูกธนู/ปากกระบอก (พิกเซล) ในเฟรม MuzzleFrame
        public int MuzzleFrame;
        public string ProjectileName;
        public float Scale = PackWeaponScale;          // ขนาด prefab อาวุธ (ตั้งให้ตอนสร้างครั้งแรก)
        public float ProjectileSize = ProjectileScale; // ขนาด prefab กระสุน
        public float HoldForward;      // ดันอาวุธออกไปข้างหน้ามือ (หน่วยก่อนย่อ) ไม่ให้บังตัวละคร
        public bool HoldToCharge;      // กดค้างง้าง ปล่อยเมาส์ถึงยิง
        public float ProjectileSpeed;
        // ตาราง 1.2
        public float Damage;
        public float AttackSpeed;
        public int EnergyCost;
        public string Hero;
    }

    static readonly RangedSpec[] Specs =
    {
        // ธนู: เฟรม 1 พาดลูกไว้ (ท่าถือ) → เฟรม 2 ง้างสุด → เฟรม 3 ปล่อยสาย
        // คันธนูแต่ละเฟรมวาดไว้คนละตำแหน่ง จุดจับคือกลางคันธนูของเฟรมนั้น ๆ
        new RangedSpec
        {
            Name = "Rusty Bow", Folder = "02-rusty-bow", Type = WeaponType.Bow,
            Idle = new FrameSpec("weapon-01.png", 99.5f, 64f),
            Attack = new[] { new FrameSpec("weapon-02.png", 116f, 64f), new FrameSpec("weapon-03.png", 103f, 64f) },
            ReleaseFrame = 1, FrameTime = 0.1f,
            Muzzle = new Vector2(139f, 64f), MuzzleFrame = 0, // ปลายลูกธนูตอนง้างสุด
            ProjectileName = "Rusty Arrow", ProjectileSpeed = 14f,
            // ธนูที่ 0.8 ดูเล็กกว่าตัวละคร ขยายให้สูงราวตัว ลูกธนูที่บินออกไปยาวเท่าลูกที่ง้างอยู่บนคัน
            // คันธนูสูงเกือบเท่าตัว ถ้าถือตรงมือจะทับหน้าตัวละคร จึงดันออกไปข้างหน้า
            Scale = 1f, ProjectileSize = 0.65f, HoldForward = 0.35f,
            HoldToCharge = true, // กดค้างค้างท่าง้าง (เฟรม 2) ปล่อยเมาส์ค่อยปล่อยสาย
            Damage = 2.5f, AttackSpeed = 1.2f, EnergyCost = 1,
            Hero = "Hero02_Blonde",
        },
        // ปืน: ท่าถือใช้ weapon-idle.png (เฟรม 2 ที่ลบประกายไฟออก) → เฟรม 1 ไฟแลบ → เฟรม 2 ถีบกลับ
        // เฟรม 2 ขยับจุดจับไป 4 พิกเซลให้ปืนถอยในมือนิดหนึ่งเหมือนแรงถีบ
        new RangedSpec
        {
            Name = "Rusty Pistol", Folder = "03-rusty-pistol", Type = WeaponType.Gun,
            Idle = new FrameSpec("weapon-idle.png", 33f, 70f),
            Attack = new[] { new FrameSpec("weapon-01.png", 50f, 70f), new FrameSpec("weapon-02.png", 37f, 70f) },
            ReleaseFrame = 0, FrameTime = 0.06f,
            Muzzle = new Vector2(145f, 44f), MuzzleFrame = 0,
            ProjectileName = "Rusty Bullet", ProjectileSpeed = 16f,
            Damage = 2f, AttackSpeed = 1.5f, EnergyCost = 1,
            Hero = "Hero03_BlueHair",
        },
    };

    [MenuItem("Tools/Quantum Rift/Setup Weapon/Starter Weapons (ตาราง 1.2)")]
    public static void SetupStarterWeapons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้างอาวุธ");

        var sword = Load<WeaponData>(SwordDataPath);
        if (sword.weaponPrefab == null)
            throw new InvalidOperationException($"{SwordDataPath} ยังไม่ได้ใส่ Weapon Prefab");
        ReskinSword(sword);

        var swordRenderer = sword.weaponPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (swordRenderer == null)
            throw new InvalidOperationException($"prefab ของ {SwordDataPath} ไม่มี SpriteRenderer ให้ลอก sorting layer");

        BuildDualClaw(sword, swordRenderer);

        EnsureFolder(ProjectilePrefabFolder);

        foreach (var spec in Specs)
            BuildRanged(spec, swordRenderer);

        // ตาราง 1.2: นักรบ ดาบขึ้นสนิม ดาเมจ 4, 1.0 ครั้ง/วินาที, ใช้พลังงาน 0 (เดิมตั้งดาเมจไว้ 20)
        sword.attackDamage = 4f;
        sword.attackSpeed = 1f;
        sword.energyCost = 0;
        EditorUtility.SetDirty(sword);

        AssetDatabase.SaveAssets();
        Debug.Log("อาวุธประจำอาชีพครบ 4 อาชีพแล้ว: ดาบ (นักรบ) ธนู (นักธนู) ปืน (นักประดิษฐ์) กรงเล็บ (มนุษย์กลายพันธุ์)");
    }

    // ดาบขึ้นสนิมเปลี่ยนไปใช้ภาพจากชุดอาวุธ ให้หน้าตาเข้าชุดกับธนู/ปืน
    // ภาพเดิมใหญ่ 2172 px ย่อด้วย scale แปลก ๆ ไว้ที่ตัว root ส่วนภาพใหม่ใช้โครงเดียวกับอาวุธยิง:
    // root = จุดจับ (กลางด้าม) ย่อ PackWeaponScale, ภาพอยู่ในลูก Visual, AttackPoint ค่อนไปทางปลายดาบ
    const string SwordSpritePath = PackFolder + "/01-rusty-sword/weapon-01.png";
    static readonly Vector2 SwordGrip = new Vector2(44f, 64.5f);      // กลางด้ามจับ ระหว่างหัวด้ามกับกระบัง
    static readonly Vector2 SwordAttackPoint = new Vector2(155f, 64.5f); // ราว 80% ของใบดาบ (ปลายอยู่ที่ x=175)

    static void ReskinSword(WeaponData sword)
    {
        var sprite = Load<Sprite>(SwordSpritePath);
        string path = AssetDatabase.GetAssetPath(sword.weaponPrefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                var visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();

                // ย้ายครั้งเดียว: ลอกชั้นการวาดจาก renderer เดิมที่ root แล้วลบทิ้ง แล้วปรับขนาด/จุดตีใหม่
                var oldRenderer = root.GetComponent<SpriteRenderer>();
                if (oldRenderer != null)
                {
                    visualRenderer.sortingLayerID = oldRenderer.sortingLayerID;
                    visualRenderer.sortingOrder = oldRenderer.sortingOrder;
                    visualRenderer.sharedMaterial = oldRenderer.sharedMaterial;
                    UnityEngine.Object.DestroyImmediate(oldRenderer);
                }
                root.transform.localScale = new Vector3(PackWeaponScale, PackWeaponScale, 1f);

                var point = root.transform.Find("AttackPoint");
                if (point == null)
                {
                    point = new GameObject("AttackPoint").transform;
                    point.SetParent(root.transform, false);
                }
                point.localPosition = new Vector3((SwordAttackPoint.x - SwordGrip.x) / PixelsPerUnit,
                                                  (SwordGrip.y - SwordAttackPoint.y) / PixelsPerUnit, 0f);
            }

            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            visual.localPosition = GripOffset(sprite, SwordGrip);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        sword.weaponIcon = sprite;
    }

    // กรงเล็บขึ้นสนิม (มนุษย์กลายพันธุ์): ใช้ภาพจากชุดอาวุธ ถือสองมือ ตะปบสลับกัน (ดู DualClawWeapon)
    // prefab เดิมเป็นภาพเก่าชิ้นเดียวที่ root แปลงครั้งเดียวเป็นโครงใหม่:
    // root (DualClawWeapon) → Claw_Upper / Claw_Lower (จุดหมุน = กำปั้น) → Visual + Tip
    const string ClawDataPath = "Assets/Data/Weapon/Rusty Claw.asset";
    const string ClawSpritePath = PackFolder + "/04-rusty-claw/weapon-01.png";
    const string ClawHero = "Hero04_WhiteHair";
    static readonly Vector2 ClawGrip = new Vector2(38f, 64f);  // กลางกำปั้นในถุงมือเหล็ก
    static readonly Vector2 ClawTip = new Vector2(150f, 64f);  // ค่อนไปทางปลายเล็บ (ปลายสุดราว x=175)
    const float ClawScale = 0.65f;  // ถือสองชิ้น ย่อกว่าอาวุธชิ้นเดียวหน่อยไม่ให้บังตัวละคร
    const float ClawSpread = 0.26f; // มือบน/ล่างห่างจากแนวเล็ง (หน่วยก่อนย่อ)

    static void BuildDualClaw(WeaponData sword, SpriteRenderer reference)
    {
        var sprite = Load<Sprite>(ClawSpritePath);
        var data = Load<WeaponData>(ClawDataPath);
        if (data.weaponPrefab == null)
            throw new InvalidOperationException($"{ClawDataPath} ยังไม่ได้ใส่ Weapon Prefab");

        string path = AssetDatabase.GetAssetPath(data.weaponPrefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var claws = root.GetComponent<DualClawWeapon>();
            if (claws == null)
            {
                // แปลงครั้งเดียว: ลบภาพเก่าที่ root ปรับขนาด และย้ายจุดวัดความยาวอาวุธ (ใช้คิดขนาดคลื่นฟัน)
                var oldRenderer = root.GetComponent<SpriteRenderer>();
                if (oldRenderer != null) UnityEngine.Object.DestroyImmediate(oldRenderer);
                root.transform.localScale = new Vector3(ClawScale, ClawScale, 1f);
                claws = root.AddComponent<DualClawWeapon>();

                var point = root.transform.Find("AttackPoint");
                if (point == null)
                {
                    point = new GameObject("AttackPoint").transform;
                    point.SetParent(root.transform, false);
                }
                point.localPosition = new Vector3((ClawTip.x - ClawGrip.x) / PixelsPerUnit, 0f, 0f);
            }

            // มือล่างพลิกภาพกลับหัว ให้สองข้างเป็นภาพสะท้อนกันเหมือนมือซ้าย/ขวา และวาดทับมือบน
            claws.upperClaw = EnsureClawHand(root.transform, "Claw_Upper", ClawSpread, false, sprite, reference, 0, out claws.upperTip);
            claws.lowerClaw = EnsureClawHand(root.transform, "Claw_Lower", -ClawSpread, true, sprite, reference, 1, out claws.lowerTip);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ตาราง 1.2: มนุษย์กลายพันธุ์ กรงเล็บขึ้นสนิม ดาเมจ 5, 1.8 ครั้ง/วินาที, ใช้พลังงาน 0
        data.weaponIcon = sprite;
        data.weaponType = WeaponType.Claw;
        data.rarity = WeaponRarity.Starter;
        data.attackDamage = 5f;
        data.attackSpeed = 1.8f;
        data.energyCost = 0;
        if (data.slashEffectPrefab == null) data.slashEffectPrefab = sword.slashEffectPrefab;
        EditorUtility.SetDirty(data);

        EquipAsStarter($"{HeroFolder}/{ClawHero}.prefab", data);
    }

    static Transform EnsureClawHand(Transform root, string name, float spread, bool mirrored, Sprite sprite,
                                    SpriteRenderer reference, int orderOffset, out Transform tip)
        => EnsureClawHand(root, name, spread, mirrored, sprite, reference, orderOffset, ClawGrip, ClawTip, out tip);

    internal static Transform EnsureClawHand(Transform root, string name, float spread, bool mirrored, Sprite sprite,
                                    SpriteRenderer reference, int orderOffset, Vector2 grip, Vector2 tipPixel, out Transform tip)
    {
        var hand = root.Find(name);
        if (hand == null)
        {
            hand = new GameObject(name).transform;
            hand.SetParent(root, false);
            hand.localPosition = new Vector3(0f, spread, 0f);
            hand.localScale = new Vector3(1f, mirrored ? -1f : 1f, 1f);
        }

        var visual = hand.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(hand, false);
        }
        var renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = reference.sortingLayerID;
        renderer.sortingOrder = reference.sortingOrder + orderOffset;
        renderer.sharedMaterial = reference.sharedMaterial;
        visual.localPosition = GripOffset(sprite, grip);

        tip = hand.Find("Tip");
        if (tip == null)
        {
            tip = new GameObject("Tip").transform;
            tip.SetParent(hand, false);
            tip.localPosition = new Vector3((tipPixel.x - grip.x) / PixelsPerUnit, 0f, 0f);
        }

        return hand;
    }

    static void BuildRanged(RangedSpec spec, SpriteRenderer reference)
    {
        string folder = $"{PackFolder}/{spec.Folder}";

        // ภาพท่าถือของปืนสร้างเพิ่มทีหลัง อาจยังไม่ได้ตั้ง Point/ไม่บีบอัด
        ArtPackImporter.ConfigureFile($"{folder}/{spec.Idle.File}");

        var forward = new Vector2(spec.HoldForward, 0f);
        var idle = ToFrame(folder, spec.Idle);
        idle.offset += forward;
        var attack = new WeaponSpriteAnimator.Frame[spec.Attack.Length];
        for (int i = 0; i < attack.Length; i++)
        {
            attack[i] = ToFrame(folder, spec.Attack[i]);
            attack[i].offset += forward;
        }

        var projectile = BuildProjectile(spec, $"{folder}/projectile-01.png");
        var prefab = BuildWeaponPrefab(spec, idle, attack, reference);

        var data = LoadOrCreate<WeaponData>($"{DataFolder}/{spec.Name}.asset");
        data.weaponName = spec.Name;
        data.weaponIcon = idle.sprite;
        data.weaponPrefab = prefab;
        data.weaponType = spec.Type;
        data.rarity = WeaponRarity.Starter;
        data.attackDamage = spec.Damage;
        data.attackSpeed = spec.AttackSpeed;
        data.energyCost = spec.EnergyCost;
        data.projectilePrefab = projectile;
        data.projectileSpeed = spec.ProjectileSpeed;
        data.slashEffectPrefab = null; // อาวุธยิงไม่มีคลื่นดาบ
        EditorUtility.SetDirty(data);

        EquipAsStarter($"{HeroFolder}/{spec.Hero}.prefab", data);
    }

    // ภาพตั้ง pivot ไว้กลางภาพ (ArtPackImporter) จึงต้องเลื่อนภาพไปให้จุดจับตรงกับจุดหมุนของ prefab (มือ)
    internal static WeaponSpriteAnimator.Frame ToFrame(string folder, FrameSpec spec)
    {
        var sprite = Load<Sprite>($"{folder}/{spec.File}");
        return new WeaponSpriteAnimator.Frame { sprite = sprite, offset = GripOffset(sprite, spec.Grip) };
    }

    internal static Vector2 GripOffset(Sprite sprite, Vector2 gripTopLeft)
    {
        // พิกัดพิกเซลนับจากมุมซ้ายบน แต่ pivot ของ sprite นับจากมุมซ้ายล่าง
        var grip = new Vector2(gripTopLeft.x, sprite.rect.height - gripTopLeft.y);
        return (sprite.pivot - grip) / PixelsPerUnit;
    }

    internal static GameObject BuildWeaponPrefab(RangedSpec spec, WeaponSpriteAnimator.Frame idle,
                                        WeaponSpriteAnimator.Frame[] attack, SpriteRenderer reference)
    {
        string path = $"{WeaponPrefabFolder}/{spec.Name}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.Name);
        try
        {
            if (!exists) root.transform.localScale = new Vector3(spec.Scale, spec.Scale, 1f);

            // ภาพอยู่ในลูกชื่อ Visual ตัว root คือจุดจับ WeaponController หมุน/พลิก root รอบมือได้ตรง ๆ
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
            }
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.sprite;
            renderer.sortingLayerID = reference.sortingLayerID; // ชั้นเดียวกับดาบ ไม่จมหลังตัวละคร
            renderer.sortingOrder = reference.sortingOrder;
            renderer.sharedMaterial = reference.sharedMaterial;
            visual.localPosition = idle.offset;

            var frames = root.GetComponent<WeaponSpriteAnimator>();
            if (frames == null) frames = root.AddComponent<WeaponSpriteAnimator>();
            frames.target = renderer;
            frames.idle = idle;
            frames.attackFrames = attack;
            frames.releaseFrame = spec.ReleaseFrame;
            frames.frameTime = spec.FrameTime;
            frames.holdToCharge = spec.HoldToCharge;

            // WeaponController ปล่อยกระสุนจากลูกชื่อ AttackPoint
            if (root.transform.Find("AttackPoint") == null)
            {
                var muzzleGrip = spec.Attack[spec.MuzzleFrame].Grip;
                var point = new GameObject("AttackPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = new Vector3((spec.Muzzle.x - muzzleGrip.x) / PixelsPerUnit + spec.HoldForward,
                                                  (muzzleGrip.y - spec.Muzzle.y) / PixelsPerUnit, 0f);
            }

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    internal static GameObject BuildProjectile(RangedSpec spec, string spritePath)
    {
        var sprite = Load<Sprite>(spritePath);
        string path = $"{ProjectilePrefabFolder}/{spec.ProjectileName}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.ProjectileName);
        try
        {
            if (!exists) root.transform.localScale = new Vector3(spec.ProjectileSize, spec.ProjectileSize, 1f);

            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            int effect = SortingLayer.NameToID(EffectSortingLayer);
            if (SortingLayer.IsValid(effect)) renderer.sortingLayerID = effect; // ลอยเหนือตัวละครและพื้น
            renderer.sortingOrder = 5;

            if (root.GetComponent<PlayerProjectile>() == null) root.AddComponent<PlayerProjectile>();

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void EquipAsStarter(string heroPath, WeaponData weapon)
    {
        var root = PrefabUtility.LoadPrefabContents(heroPath);
        try
        {
            var stats = root.GetComponent<PlayerStats>();
            if (stats == null)
            {
                Debug.LogWarning($"{heroPath} ยังไม่มี PlayerStats สั่ง Setup Hero ก่อน แล้วค่อยสร้างอาวุธใหม่");
                return;
            }

            stats.weapon1 = weapon;
            PrefabUtility.SaveAsPrefabAsset(root, heroPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    internal static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    internal static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
