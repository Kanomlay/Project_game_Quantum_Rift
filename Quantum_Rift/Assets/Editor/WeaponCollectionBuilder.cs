using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// อาวุธทั่วไป / หายาก / ตำนาน 19 ชิ้นตามตาราง 1.3–1.5 (ภาพชุด Quantum-Rift-Weapons-23-v1 ลำดับ 05–23)
// - ประชิด: ดาบ ค้อน กระบองนาโน มีดสั้น = ท่าฟันแบบดาบ, มีดคู่ = ตะปบสลับมือแบบกรงเล็บ, หอก = แทงตรง
// - ยิง: ปืน/เครื่องยิงจรวด = กดยิง, ธนู = กดค้างง้าง ปล่อยยิง (โครงเดียวกับอาวุธประจำอาชีพ)
// - ตำนานมีความสามารถพิเศษ (WeaponSpecial) ใช้เอฟเฟกต์ 3 เฟรมจากชุดภาพ
// เสร็จแล้วเติมรายการอาวุธในกล่องสมบัติ (ChestLoot) ตามระดับให้ด้วย
//
// สั่งซ้ำได้: ตำแหน่ง AttackPoint / รัศมีตีโดนของมีดคู่ ตั้งให้เฉพาะตอนสร้างครั้งแรก
// ส่วนขนาด prefab (Scale ในสเปก) ค่าสเตตัสตามเอกสาร ภาพ และค่าความสามารถพิเศษตั้งใหม่ทุกครั้ง
// อยากปรับขนาดอาวุธให้แก้ Scale ในสเปกนี้ ไม่ใช่ใน prefab (สั่งซ้ำแล้วจะถูกทับ)
public static class WeaponCollectionBuilder
{
    const string Pack = StarterWeaponBuilder.PackFolder;
    const float Ppu = StarterWeaponBuilder.PixelsPerUnit;

    sealed class MeleeSpec
    {
        public string Name;        // ชื่อไฟล์ prefab/asset
        public string Thai;        // ชื่อที่แสดงในเกม (ตามเอกสาร)
        public string Folder;
        public WeaponType Type;
        public WeaponRarity Rarity;
        public float Scale;        // ขนาด prefab (ภาพทุกชิ้นวาดยาวเต็มช่องเท่ากัน ต้องย่อ/ขยายตามชนิด)
        public Vector2 Grip;       // จุดจับ (พิกเซล นับจากมุมซ้ายบน)
        public float AttackX;      // จุดตีโดนตามแนวอาวุธ (พิกเซล)
        public float Range;        // รัศมีตีโดนรอบ AttackPoint (หน่วยในฉาก)
        public float Damage, Speed;
        public int Energy;
        public bool Dual;          // มีดคู่: ถือสองมือ ตะปบสลับ
        public string Description;
        public WeaponSpecial Special;
        public Action<WeaponData> Configure;
    }

    static readonly MeleeSpec[] Melee =
    {
        // ---- ตาราง 1.3 อาวุธทั่วไป ----
        new MeleeSpec { Name = "Old Iron Sword", Thai = "ดาบเหล็กเก่า", Folder = "05-old-iron-sword", Type = WeaponType.Sword, Rarity = WeaponRarity.Common,
                        Scale = 1.05f, Grip = new Vector2(40f, 64f), AttackX = 150f, Range = 1.9f, Damage = 6f, Speed = 1.0f, Energy = 0 },
        new MeleeSpec { Name = "Rusty Dagger", Thai = "มีดสั้นสนิม", Folder = "06-rusty-dagger", Type = WeaponType.Sword, Rarity = WeaponRarity.Common,
                        Scale = 0.7f, Grip = new Vector2(48f, 63f), AttackX = 150f, Range = 1.2f, Damage = 4f, Speed = 1.6f, Energy = 0 },
        new MeleeSpec { Name = "Starter Spear", Thai = "หอกเริ่มต้น", Folder = "07-starter-spear", Type = WeaponType.Spear, Rarity = WeaponRarity.Common,
                        Scale = 1.3f, Grip = new Vector2(70f, 64f), AttackX = 165f, Range = 0.65f, Damage = 7f, Speed = 1.1f, Energy = 0 },
        new MeleeSpec { Name = "Iron Hammer", Thai = "ค้อนเหล็ก", Folder = "10-iron-hammer", Type = WeaponType.Hammer, Rarity = WeaponRarity.Common,
                        Scale = 1.05f, Grip = new Vector2(45f, 64f), AttackX = 157f, Range = 2.0f, Damage = 10f, Speed = 0.8f, Energy = 0 },

        // ---- ตาราง 1.4 อาวุธหายาก ----
        new MeleeSpec { Name = "Steel Sword", Thai = "ดาบเหล็กกล้า", Folder = "11-steel-sword", Type = WeaponType.Sword, Rarity = WeaponRarity.Rare,
                        Scale = 1.1f, Grip = new Vector2(38f, 64f), AttackX = 150f, Range = 1.95f, Damage = 8f, Speed = 1.1f, Energy = 1 },
        new MeleeSpec { Name = "Twin Steel Daggers", Thai = "มีดคู่เหล็ก", Folder = "12-twin-steel-daggers", Type = WeaponType.Claw, Rarity = WeaponRarity.Rare,
                        Scale = 0.65f, Grip = new Vector2(48f, 64f), AttackX = 150f, Range = 1f, Damage = 8.4f, Speed = 1.8f, Energy = 2, Dual = true },
        new MeleeSpec { Name = "Reinforced Spear", Thai = "หอกเสริมแกร่ง", Folder = "13-reinforced-spear", Type = WeaponType.Spear, Rarity = WeaponRarity.Rare,
                        Scale = 1.35f, Grip = new Vector2(70f, 64f), AttackX = 165f, Range = 0.7f, Damage = 10f, Speed = 1.1f, Energy = 2 },
        new MeleeSpec { Name = "Gravity Hammer", Thai = "ค้อนแรงโน้มถ่วง", Folder = "17-gravity-hammer", Type = WeaponType.Hammer, Rarity = WeaponRarity.Rare,
                        Scale = 1.2f, Grip = new Vector2(45f, 64f), AttackX = 152f, Range = 2.2f, Damage = 15f, Speed = 0.7f, Energy = 4 },
        new MeleeSpec { Name = "Nano Baton", Thai = "กระบองนาโน", Folder = "18-nano-baton", Type = WeaponType.Sword, Rarity = WeaponRarity.Rare,
                        Scale = 1.0f, Grip = new Vector2(43f, 64f), AttackX = 150f, Range = 1.8f, Damage = 8.5f, Speed = 1.3f, Energy = 2 },

        // ---- ตาราง 1.5 อาวุธระดับตำนาน ----
        new MeleeSpec { Name = "Rift Sword", Thai = "ดาบผ่ามิติ", Folder = "19-rift-sword", Type = WeaponType.Sword, Rarity = WeaponRarity.Legendary,
                        Scale = 1.25f, Grip = new Vector2(40f, 64f), AttackX = 140f, Range = 2.05f, Damage = 17f, Speed = 1.1f, Energy = 3,
                        Special = WeaponSpecial.ComboWave, Description = "ฟันครั้งที่ 3 ปล่อยคลื่นพลัง",
                        Configure = d => { d.comboCount = 3; d.specialScale = 3f; d.specialRadius = 1.3f; d.specialSpeed = 10f; d.specialRange = 8f; } },
        new MeleeSpec { Name = "Ion Spear X", Thai = "หอกไอออน-X", Folder = "21-ion-spear-x", Type = WeaponType.Spear, Rarity = WeaponRarity.Legendary,
                        Scale = 1.4f, Grip = new Vector2(75f, 64f), AttackX = 165f, Range = 0.75f, Damage = 15f, Speed = 1.0f, Energy = 3,
                        Special = WeaponSpecial.ChargeWave, Description = "กดค้างชาร์จ 1 วินาที ปล่อยคลื่นพลัง",
                        Configure = d => { d.chargeTime = 1f; d.specialScale = 2.6f; d.specialRadius = 0.9f; d.specialSpeed = 13f; d.specialRange = 9f; } },
        new MeleeSpec { Name = "Quantum Hammer", Thai = "ค้อนควอนตัม", Folder = "23-quantum-hammer", Type = WeaponType.Hammer, Rarity = WeaponRarity.Legendary,
                        Scale = 1.3f, Grip = new Vector2(40f, 64f), AttackX = 140f, Range = 2.3f, Damage = 20f, Speed = 0.7f, Energy = 5,
                        Special = WeaponSpecial.GroundPulse, Description = "ทุบแล้วเกิดวงพลัง ทำความเสียหายพื้นที่ต่อเนื่อง 0.5 วินาที",
                        Configure = d => { d.specialDuration = 0.5f; d.specialDamage = 5f; d.specialRadius = 1.7f; d.specialScale = 2.1f; } },
    };

    sealed class RangedInfo
    {
        public StarterWeaponBuilder.RangedSpec Spec;
        public string Thai;
        public WeaponRarity Rarity;
        public string Description;
        public WeaponSpecial Special;
        public Action<WeaponData> Configure;
    }

    static StarterWeaponBuilder.FrameSpec F(string file, float x, float y) => new StarterWeaponBuilder.FrameSpec(file, x, y);

    // ปืน: ท่าถือ = weapon-idle.png (เฟรม 2 ที่ลบประกายไฟ) → เฟรม 1 ไฟแลบ → เฟรม 2 ถีบกลับ (จุดจับเลื่อน 4 px ให้ปืนถอยในมือ)
    // ธนู: ท่าถือ = เฟรม 1 พาดลูก → เฟรม 2 ง้างสุด (ค้างไว้ตอนกดค้าง) → เฟรม 3 ปล่อยสาย จุดจับ = กลางคันธนูของแต่ละเฟรม
    static readonly RangedInfo[] Ranged =
    {
        new RangedInfo { Thai = "ธนูไม้", Rarity = WeaponRarity.Common, Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Wooden Bow", Folder = "08-wooden-bow", Type = WeaponType.Bow,
            Idle = F("weapon-01.png", 107f, 64f), Attack = new[] { F("weapon-02.png", 107f, 64f), F("weapon-03.png", 102f, 64f) },
            ReleaseFrame = 1, FrameTime = 0.1f, Muzzle = new Vector2(130f, 65f), MuzzleFrame = 0,
            ProjectileName = "Wooden Arrow", ProjectileSpeed = 14f, Scale = 1.2f, ProjectileSize = 0.75f, HoldForward = 0.35f, HoldToCharge = true,
            Damage = 5f, AttackSpeed = 1.0f, EnergyCost = 2 } },
        new RangedInfo { Thai = "ปืนไรเฟิลเก่า", Rarity = WeaponRarity.Common, Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Old Rifle", Folder = "09-old-rifle", Type = WeaponType.Gun,
            Idle = F("weapon-idle.png", 60f, 68f), Attack = new[] { F("weapon-01.png", 63f, 66f), F("weapon-02.png", 64f, 68f) },
            ReleaseFrame = 0, FrameTime = 0.06f, Muzzle = new Vector2(153f, 57f), MuzzleFrame = 0,
            ProjectileName = "Rifle Bullet", ProjectileSpeed = 20f, Scale = 1.15f, ProjectileSize = 0.5f,
            Damage = 9f, AttackSpeed = 0.7f, EnergyCost = 5 } },

        new RangedInfo { Thai = "ธนูไม้ รุ่น 2", Rarity = WeaponRarity.Rare, Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Wooden Bow Mk2", Folder = "14-wooden-bow-mk2", Type = WeaponType.Bow,
            Idle = F("weapon-01.png", 93f, 64f), Attack = new[] { F("weapon-02.png", 105f, 64f), F("weapon-03.png", 101f, 64f) },
            ReleaseFrame = 1, FrameTime = 0.1f, Muzzle = new Vector2(130f, 64f), MuzzleFrame = 0,
            ProjectileName = "Wooden Arrow Mk2", ProjectileSpeed = 15f, Scale = 1.2f, ProjectileSize = 0.75f, HoldForward = 0.35f, HoldToCharge = true,
            Damage = 9f, AttackSpeed = 0.9f, EnergyCost = 3 } },
        new RangedInfo { Thai = "ปืนพลังงาน", Rarity = WeaponRarity.Rare, Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Energy Gun", Folder = "15-energy-gun", Type = WeaponType.Gun,
            Idle = F("weapon-idle.png", 44f, 86f), Attack = new[] { F("weapon-01.png", 70f, 86f), F("weapon-02.png", 48f, 86f) },
            ReleaseFrame = 0, FrameTime = 0.06f, Muzzle = new Vector2(133f, 69f), MuzzleFrame = 0,
            ProjectileName = "Energy Bolt", ProjectileSpeed = 15f, Scale = 0.9f, ProjectileSize = 0.5f,
            Damage = 8f, AttackSpeed = 1.5f, EnergyCost = 3 } },
        new RangedInfo { Thai = "ปืนไอออนบลาสเตอร์", Rarity = WeaponRarity.Rare, Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Ion Blaster", Folder = "16-ion-blaster", Type = WeaponType.Gun,
            Idle = F("weapon-idle.png", 38f, 82f), Attack = new[] { F("weapon-01.png", 44f, 82f), F("weapon-02.png", 42f, 82f) },
            ReleaseFrame = 0, FrameTime = 0.06f, Muzzle = new Vector2(130f, 59f), MuzzleFrame = 0,
            ProjectileName = "Ion Bolt", ProjectileSpeed = 14f, Scale = 1.0f, ProjectileSize = 0.55f,
            Damage = 10f, AttackSpeed = 1.2f, EnergyCost = 2 } },

        new RangedInfo { Thai = "ธนูยิงกระจาย", Rarity = WeaponRarity.Legendary, Special = WeaponSpecial.Ricochet,
            Description = "ยิงลูกธนู 3 ลูกเป็นพัด ลูกธนูชิ่งกำแพงได้ 3 ครั้ง ไม่จำกัดระยะ",
            // ไม่จำกัดระยะ: บินจนชนศัตรูหรือชนกำแพงครบจำนวนชิ่ง (อายุ 10 วิ = ระยะ 150 หน่วย ไว้กันหลุดออกนอกแมพเท่านั้น)
            Configure = d => { d.projectileCount = 3; d.spreadAngle = 12f; d.bounces = 3; d.projectileLifetime = 10f; d.specialScale = 0.45f; },
            Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Scatter Bow", Folder = "20-scatter-bow", Type = WeaponType.Bow,
            Idle = F("weapon-01.png", 95f, 64f), Attack = new[] { F("weapon-02.png", 100f, 64f), F("weapon-03.png", 100f, 64f) },
            ReleaseFrame = 1, FrameTime = 0.1f, Muzzle = new Vector2(135f, 64f), MuzzleFrame = 0,
            ProjectileName = "Scatter Arrow", ProjectileSpeed = 15f, Scale = 1.2f, ProjectileSize = 0.75f, HoldForward = 0.35f, HoldToCharge = true,
            Damage = 16f, AttackSpeed = 1.0f, EnergyCost = 4 } },
        new RangedInfo { Thai = "เครื่องยิงจรวด", Rarity = WeaponRarity.Legendary, Special = WeaponSpecial.Explosive,
            Description = "จรวดระเบิดเป็นวง ทำความเสียหายพื้นที่",
            Configure = d => { d.specialRadius = 1.4f; d.specialScale = 2f; },
            Spec = new StarterWeaponBuilder.RangedSpec {
            Name = "Rocket Launcher", Folder = "22-rocket-launcher", Type = WeaponType.Gun,
            Idle = F("weapon-idle.png", 60f, 72f), Attack = new[] { F("weapon-01.png", 77f, 72f), F("weapon-02.png", 64f, 72f) },
            ReleaseFrame = 0, FrameTime = 0.08f, Muzzle = new Vector2(142f, 63f), MuzzleFrame = 0,
            ProjectileName = "Rocket", ProjectileSpeed = 10f, Scale = 1.15f, ProjectileSize = 0.65f,
            Damage = 20f, AttackSpeed = 0.8f, EnergyCost = 5 } },
    };

    [MenuItem("Tools/Quantum Rift/Setup Weapon/All Weapons (ตาราง 1.3–1.5)")]
    public static void SetupAllWeapons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้างอาวุธ");

        var sword = StarterWeaponBuilder.Load<WeaponData>(StarterWeaponBuilder.SwordDataPath);
        var reference = sword.weaponPrefab != null ? sword.weaponPrefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (reference == null)
            throw new InvalidOperationException("ดาบขึ้นสนิมยังไม่มี prefab/SpriteRenderer ให้ลอก sorting layer สั่ง Starter Weapons ก่อน");

        StarterWeaponBuilder.EnsureFolder(StarterWeaponBuilder.ProjectilePrefabFolder);

        foreach (var spec in Melee) BuildMelee(spec, sword, reference);
        foreach (var info in Ranged) BuildRanged(info, reference);

        AttackMotionBuilder.AssignAll(); // ท่าตีตามชนิดอาวุธ (ใส่เฉพาะชิ้นที่ยังไม่มี)

        var loot = AssetDatabase.LoadAssetAtPath<LootTable>(LootBuilder.LootPath);
        if (loot != null) LootBuilder.RefillWeapons(loot);

        AssetDatabase.SaveAssets();
        Debug.Log($"สร้างอาวุธครบ {Melee.Length + Ranged.Length} ชิ้น (ทั่วไป {Count(WeaponRarity.Common)} / หายาก {Count(WeaponRarity.Rare)} / ตำนาน {Count(WeaponRarity.Legendary)})"
                  + (loot != null ? " และเติมลงกล่องสมบัติแล้ว" : " (ยังไม่มี ChestLoot สั่ง Setup Chest Loot แล้วอาวุธจะถูกใส่ในกล่องเอง)"));
    }

    static int Count(WeaponRarity rarity) =>
        Melee.Count(m => m.Rarity == rarity) + Ranged.Count(r => r.Rarity == rarity);

    // ---------- ประชิด ----------

    static void BuildMelee(MeleeSpec spec, WeaponData sword, SpriteRenderer reference)
    {
        string folder = $"{Pack}/{spec.Folder}";
        var sprite = StarterWeaponBuilder.Load<Sprite>($"{folder}/weapon-01.png");
        GameObject prefab = spec.Dual ? BuildDualPrefab(spec, folder, reference) : BuildSinglePrefab(spec, sprite, reference);

        var data = StarterWeaponBuilder.LoadOrCreate<WeaponData>($"{StarterWeaponBuilder.DataFolder}/{spec.Name}.asset");
        data.weaponName = spec.Thai;
        data.weaponIcon = sprite;
        data.weaponPrefab = prefab;
        data.weaponType = spec.Type;
        data.rarity = spec.Rarity;
        data.attackDamage = spec.Damage;
        data.attackSpeed = spec.Speed;
        data.energyCost = spec.Energy;
        data.attackRange = spec.Range;
        data.attackAngle = 90f;
        // หอกแทงตรง / ค้อนทุบ ไม่มีคลื่นฟัน ที่เหลือใช้คลื่นฟันเดียวกับดาบ (ย่อ/ขยายตามความยาวอาวุธเอง)
        bool noSlash = spec.Type == WeaponType.Spear || spec.Type == WeaponType.Hammer;
        data.slashEffectPrefab = noSlash ? null : sword.slashEffectPrefab;
        data.projectilePrefab = null;
        ApplySpecial(data, spec.Special, folder, spec.Description, spec.Configure);
        EditorUtility.SetDirty(data);
    }

    static GameObject BuildSinglePrefab(MeleeSpec spec, Sprite sprite, SpriteRenderer reference)
    {
        string path = $"{StarterWeaponBuilder.WeaponPrefabFolder}/{spec.Name}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.Name);
        try
        {
            // root = จุดจับ (มือ) ภาพอยู่ในลูก Visual เลื่อนให้ด้ามตรงมือ
            root.transform.localScale = new Vector3(spec.Scale, spec.Scale, 1f);

            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
            }
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder;
            renderer.sharedMaterial = reference.sharedMaterial;
            visual.localPosition = StarterWeaponBuilder.GripOffset(sprite, spec.Grip);

            if (root.transform.Find("AttackPoint") == null)
            {
                var point = new GameObject("AttackPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = new Vector3((spec.AttackX - spec.Grip.x) / Ppu, 0f, 0f);
            }
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // มีดคู่: โครงเดียวกับกรงเล็บคู่ (DualClawWeapon) มือละหนึ่งเล่ม ใช้ภาพมีดเล่มเดียวที่ตัดจากภาพคู่ (weapon-hand.png)
    static GameObject BuildDualPrefab(MeleeSpec spec, string folder, SpriteRenderer reference)
    {
        string handPath = $"{folder}/weapon-hand.png";
        ArtPackImporter.ConfigureFile(handPath);
        var hand = StarterWeaponBuilder.Load<Sprite>(handPath);

        string path = $"{StarterWeaponBuilder.WeaponPrefabFolder}/{spec.Name}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.Name);
        try
        {
            var blades = root.GetComponent<DualClawWeapon>();
            root.transform.localScale = new Vector3(spec.Scale, spec.Scale, 1f);
            if (blades == null)
            {
                blades = root.AddComponent<DualClawWeapon>();
                blades.hitRadius = 0.9f;
                blades.swipeAngle = 12f; // มีดแทงตรงกว่ากรงเล็บ
                var point = new GameObject("AttackPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = new Vector3((spec.AttackX - spec.Grip.x) / Ppu, 0f, 0f);
            }

            var tip = new Vector2(spec.AttackX, spec.Grip.y);
            blades.upperClaw = StarterWeaponBuilder.EnsureClawHand(root.transform, "Claw_Upper", 0.26f, false, hand, reference, 0, spec.Grip, tip, out blades.upperTip);
            blades.lowerClaw = StarterWeaponBuilder.EnsureClawHand(root.transform, "Claw_Lower", -0.26f, true, hand, reference, 1, spec.Grip, tip, out blades.lowerTip);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ---------- ยิง ----------

    static void BuildRanged(RangedInfo info, SpriteRenderer reference)
    {
        var spec = info.Spec;
        string folder = $"{Pack}/{spec.Folder}";
        ArtPackImporter.ConfigureFile($"{folder}/{spec.Idle.File}"); // weapon-idle.png สร้างเพิ่มทีหลัง

        var forward = new Vector2(spec.HoldForward, 0f);
        var idle = StarterWeaponBuilder.ToFrame(folder, spec.Idle);
        idle.offset += forward;
        var attack = new WeaponSpriteAnimator.Frame[spec.Attack.Length];
        for (int i = 0; i < attack.Length; i++)
        {
            attack[i] = StarterWeaponBuilder.ToFrame(folder, spec.Attack[i]);
            attack[i].offset += forward;
        }

        var projectile = StarterWeaponBuilder.BuildProjectile(spec, $"{folder}/projectile-01.png");
        var prefab = StarterWeaponBuilder.BuildWeaponPrefab(spec, idle, attack, reference);
        // ตัวสร้างของอาวุธประจำอาชีพตั้งขนาดให้แค่ตอนสร้างครั้งแรก ชุดนี้ใช้ขนาดจากสเปกเสมอ
        projectile = ForceScale(projectile, spec.ProjectileSize);
        prefab = ForceScale(prefab, spec.Scale);

        var data = StarterWeaponBuilder.LoadOrCreate<WeaponData>($"{StarterWeaponBuilder.DataFolder}/{spec.Name}.asset");
        data.weaponName = info.Thai;
        data.weaponIcon = idle.sprite;
        data.weaponPrefab = prefab;
        data.weaponType = spec.Type;
        data.rarity = info.Rarity;
        data.attackDamage = spec.Damage;
        data.attackSpeed = spec.AttackSpeed;
        data.energyCost = spec.EnergyCost;
        data.projectilePrefab = projectile;
        data.projectileSpeed = spec.ProjectileSpeed;
        data.slashEffectPrefab = null;
        data.projectileCount = 1;
        data.spreadAngle = 0f;
        ApplySpecial(data, info.Special, folder, info.Description, info.Configure);
        EditorUtility.SetDirty(data);
    }

    static GameObject ForceScale(GameObject prefab, float scale)
    {
        var target = new Vector3(scale, scale, 1f);
        if (prefab.transform.localScale == target) return prefab;
        string path = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.localScale = target;
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- ความสามารถพิเศษ ----------

    static void ApplySpecial(WeaponData data, WeaponSpecial special, string folder, string description, Action<WeaponData> configure)
    {
        data.special = special;
        data.abilityDescription = description ?? "";
        data.specialDamage = 0f;
        data.bounces = 0;
        data.specialFrames = special == WeaponSpecial.None
            ? new Sprite[0]
            : Enumerable.Range(1, 3).Select(i => StarterWeaponBuilder.Load<Sprite>($"{folder}/effect-0{i}.png")).ToArray();
        configure?.Invoke(data);
    }
}
