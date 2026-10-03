using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// สร้าง/เติมคลังเสียง Resources/SfxLibrary.asset จากไฟล์ใน Assets/Audio/SFX
// ทำเองทุกครั้งที่กด Play: ผูกคลิปตามชื่อไฟล์ (Name.wav หรือ Name_1..n.wav = แบบย่อยที่สุ่มสลับ) และจับคู่อาวุธ/มอนกับเสียง
// ความดัง/ระดับเสียงในตารางนี้ใช้ตอนสร้างรายการครั้งแรกเท่านั้น ปรับใน Inspector แล้วไม่โดนทับ
// (เมนู Reset Volumes And Pitch = กลับไปใช้ค่าในตารางทั้งหมด)
// ตารางเปลี่ยนไฟล์ของรายการไหน รายการนั้นกลับไปใช้ค่าในตารางเอง (ค่าเดิมปรับไว้กับเสียงเก่า)
// ชื่อไฟล์ตรงกับ import_to_project.py และ Monsters/import_monsters.py ในโฟลเดอร์ต้นฉบับเสียงนอก repo
[InitializeOnLoad]
public static class SfxLibraryBuilder
{
    const string AssetPath = "Assets/Resources/SfxLibrary.asset";
    const string ClipFolder = "Assets/Audio/SFX";

    struct Row
    {
        public SfxId id;
        public string file;
        public float volume, pitch, jitter, interval;
        public Row(SfxId id, string file, float volume, float pitch = 1f, float jitter = 0.05f, float interval = 0.04f)
        {
            this.id = id; this.file = file; this.volume = volume; this.pitch = pitch; this.jitter = jitter; this.interval = interval;
        }
    }

    static readonly Row[] Table =
    {
        // อาวุธ
        new Row(SfxId.AttackSword, "SwordSlash", 0.7f),
        new Row(SfxId.AttackDagger, "SwishHit", 0.6f),
        new Row(SfxId.AttackBaton, "ZapNoiseSweep", 0.6f),
        new Row(SfxId.AttackRiftSword, "SwordSlash", 0.7f),
        new Row(SfxId.RiftWave, "MechaLaserRelease", 0.7f),
        new Row(SfxId.AttackHammer, "StrongSmack", 0.9f),            // ค้อนเหล็ก + ค้อนแรงโน้มถ่วง + ค้อนควอนตัม
        new Row(SfxId.QuantumPulse, "VoltaicBlast", 0.8f),
        new Row(SfxId.AttackSpear, "SwordSlash", 0.7f),              // หอกใช้เสียงเดียวกับดาบ
        new Row(SfxId.IonCharge, "StrongEnergy", 0.6f, 1f, 0f),
        new Row(SfxId.IonWave, "MechaWhiteLaser", 0.7f),
        new Row(SfxId.AttackClaw, "SwishHit", 0.6f),
        new Row(SfxId.BowDraw, "SheathSqueeze", 0.6f, 1f, 0.08f),
        new Row(SfxId.AttackBow, "BambooWhip", 0.7f),
        new Row(SfxId.AttackScatterBow, "HighWhoosh", 0.7f),
        new Row(SfxId.Ricochet, "Flick", 0.5f, 1f, 0.08f, 0.08f),
        new Row(SfxId.GunBullet, "Bang02", 0.6f, 1f, 0.08f),
        new Row(SfxId.GunEnergy, "LaserShot", 0.55f),
        new Row(SfxId.GunIon, "RetroLaser3", 0.6f),
        new Row(SfxId.RocketFire, "MechaPropulsorIgnition", 0.75f),
        new Row(SfxId.RocketExplode, "PyroBurst", 0.9f),

        // ตีโดน / ผู้เล่น / มอนทั่วไป
        new Row(SfxId.HitMonster, "HitNoise", 0.55f, 1f, 0.08f, 0.06f),
        new Row(SfxId.HitArmor, "ZapMetal", 0.55f, 1f, 0.08f, 0.06f),
        new Row(SfxId.PlayerHurt, "Bitcrusher", 0.8f),
        new Row(SfxId.PlayerDeath, "ForcedInterruption", 0.9f, 1f, 0f),
        new Row(SfxId.MonsterDeath, "CrunchyBurst", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonsterSpawn, "SharperSummon", 0.5f, 1f, 0.05f, 0.15f),

        // เก็บของ / UI
        new Row(SfxId.PickupCoin, "MagicCoin", 0.5f, 1f, 0.1f, 0.05f),
        new Row(SfxId.PickupPotion, "BuffPickup", 0.6f),
        new Row(SfxId.PickupMana, "BuffPickup", 0.4f, 1f, 0.08f, 0.08f),
        new Row(SfxId.PickupWeapon, "SeaxUnsheathe", 0.7f),
        new Row(SfxId.UiClick, "TonalClick", 0.5f, 1f, 0f),
        new Row(SfxId.UiDenied, "Denied", 0.6f, 1f, 0f, 0.4f),

        // สกิลประจำอาชีพ
        new Row(SfxId.SkillImpactDash, "MechaMediumJump", 0.7f),
        new Row(SfxId.SkillImpactDashHit, "StrongSmack", 0.9f, 1f, 0.05f, 0.1f),
        new Row(SfxId.SkillArmorOn, "DeflectChance", 0.7f),
        new Row(SfxId.SkillArmorReflect, "SwordReflect", 0.7f, 1f, 0.05f, 0.1f),
        new Row(SfxId.SkillDimensionalArrow, "LaserWhoosh2", 0.7f),
        new Row(SfxId.SkillShadowStep, "NoiseZap", 0.7f),
        new Row(SfxId.SkillDroneDeploy, "MechaLockIn", 0.7f),
        new Row(SfxId.SkillDroneShot, "LaserShot", 0.4f, 1.1f, 0.05f, 0.08f),
        new Row(SfxId.SkillTrapPlace, "ZapUp", 0.7f),
        new Row(SfxId.SkillTrapTrigger, "ShimmerElectric", 0.8f),
        new Row(SfxId.SkillFangs, "GorePierce", 0.7f, 1f, 0.05f, 0.1f),
        new Row(SfxId.SkillCellStim, "BonusRegenRate", 0.7f),

        // ท่าของมอน
        new Row(SfxId.MonMelee, "ClapSlapper", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonPounce, "WhooshSweep", 0.55f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonBite, "Crunching", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonLunge, "NoiseZap", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonShot, "LaserShot", 0.45f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonSpit, "SlimeBall", 0.55f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonRockThrow, "WindSweepSwish", 0.55f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonSlam, "Thud", 0.8f, 1f, 0.05f, 0.08f),
        new Row(SfxId.MonRoot, "SandImpact", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonBurrow, "SandSwipe", 0.5f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonVine, "SwishHit", 0.6f, 1f, 0.08f, 0.08f),
        new Row(SfxId.MonFuse, "EnergyRiser", 0.6f, 1f, 0.05f, 0.15f),
        new Row(SfxId.MonExplode, "FireHit", 0.85f, 1f, 0.05f, 0.08f),

        // บอส: ใช้ร่วมกัน
        new Row(SfxId.BossLaserCharge, "MechaLaserWindUp6", 0.8f, 1f, 0f, 0.2f),
        new Row(SfxId.BossLaserBeam, "MechaLaserBeam6", 0.8f, 1f, 0f, 0.2f),
        new Row(SfxId.BossTeleport, "Teleport02", 0.7f, 1f, 0.03f, 0.1f),
        new Row(SfxId.BossBullets, "ThousandTinyLasers", 0.6f, 1f, 0.05f, 0.25f),
        // Echo Commander
        new Row(SfxId.EchoSlashWindup, "MechaSwordCast", 0.7f, 1f, 0f),
        new Row(SfxId.EchoSlash, "SwordSlash", 0.9f),
        new Row(SfxId.EchoSummon, "SharperSummon", 0.7f, 1f, 0.05f, 0.2f),
        new Row(SfxId.EchoRage, "AuraRise", 0.9f, 1f, 0f),
        new Row(SfxId.EchoDeath, "MechaMultipleBangs", 0.9f, 1f, 0f),
        // Ancient Entborn (คำราม = เสียงร้องจริง ส่วนรากแทงกับตอนตายใช้ Eruption เดียวกัน แยกด้วยระดับเสียง: รากแทงสูงสั้น ตายต่ำสุด)
        new Row(SfxId.EntbornRoar, "AquaticBeastRoar02", 0.9f, 1f, 0.02f, 0.3f),
        new Row(SfxId.EntbornStomp, "BassHit", 0.9f),
        new Row(SfxId.EntbornRoot, "Eruption", 0.55f, 1.4f, 0.06f, 0.12f),
        new Row(SfxId.EntbornLeap, "MechaDamage", 0.9f),
        new Row(SfxId.EntbornDeath, "Eruption", 1f, 0.62f, 0f),
        // Architect of Collapse
        new Row(SfxId.ArchitectWave, "FlyingBlades", 0.7f, 1f, 0.05f, 0.1f),
        new Row(SfxId.ArchitectSlash, "CriticalStrike", 0.8f),
        new Row(SfxId.ArchitectStorm, "VoltaicBlast", 0.8f, 1f, 0.05f, 0.2f),
        new Row(SfxId.ArchitectTransform, "MechaTurnOnOff3", 0.9f, 1f, 0f),
        new Row(SfxId.ArchitectCollapse, "ExplodingMovement", 0.8f, 1f, 0.05f, 0.3f),
        new Row(SfxId.ArchitectDeath, "MechaMultipleBangs", 1f, 0.9f, 0f),

        // ---- เสียงร้องของตัวมอน (Assets/Audio/SFX/Monsters) เล่นซ้อนกับเสียงท่าข้างบน ----
        // มอนทั่วไป: โจมตี / โดนตี / ตาย
        new Row(SfxId.VoiceWolfAttack, "RpgWolfman", 0.55f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceWolfHurt, "BaradariPainB", 0.5f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceWolfDeath, "Beast2Roar13", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceJawAttack, "CreatureRoar01", 0.6f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceJawHurt, "CreatureGrunt02", 0.55f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceJawDeath, "CreatureMonster03", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceStalkerAttack, "GhostRoar12", 0.55f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceStalkerHurt, "RpgShade15", 0.5f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceStalkerDeath, "CreatureScream02", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceWraithAttack, "UndeadWraith01", 0.55f, 1f, 0.06f, 0.12f),       // ตัดเหลือ 1.0 วิ
        new Row(SfxId.VoiceWraithHurt, "AlienScientist26", 0.5f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceWraithDeath, "UndeadWraith07", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceHeavyAttack, "CreatureRoar05", 0.65f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceHeavyHurt, "OgrebaneMonster7", 0.55f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceHeavyDeath, "OgrebaneMonster10", 0.65f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceSoldierAttack, "SpaceAlert00", 0.5f, 1f, 0.06f, 0.12f),          // ตัดเหลือ 1.0 วิ
        new Row(SfxId.VoiceSoldierHurt, "SpacePain00", 0.5f, 1f, 0.06f, 0.1f),              // ตัดเหลือ 0.7 วิ
        new Row(SfxId.VoiceSoldierDeath, "SpaceDeath00", 0.55f, 1.2f, 0.05f, 0.1f),         // ไฟล์เดียวกับ Architect ตาย เสียงแหลมขึ้นให้เป็นตัวเล็กกว่า
        new Row(SfxId.VoiceWorkerAttack, "UndeadGhoulSmall02", 0.55f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceWorkerHurt, "CreatureHurt01", 0.55f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceWorkerDeath, "CreatureDie01", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceRootlingAttack, "CreatureMisc01", 0.5f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceRootlingHurt, "CreatureMisc20", 0.5f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceRootlingDeath, "DieingPixie", 0.55f, 0.8f, 0.05f, 0.1f),         // ลดระดับเสียงลงไม่ให้แหลม
        new Row(SfxId.VoiceWoodmineAttack, "CreatureSpit01", 0.55f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceWoodmineHurt, "CreatureSlime06", 0.5f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceWoodmineDeath, "CreatureBurble02", 0.6f, 1f, 0.05f, 0.1f),
        new Row(SfxId.VoiceHuskAttack, "CreatureBreath02", 0.6f, 1f, 0.06f, 0.12f),
        new Row(SfxId.VoiceHuskHurt, "CreatureMonster08", 0.55f, 1f, 0.06f, 0.1f),
        new Row(SfxId.VoiceHuskFuse, "UndeadWraith04", 0.7f, 1f, 0.03f, 0.3f),
        // Echo Commander
        new Row(SfxId.EchoVoiceIntro, "DemonRoar15", 0.85f, 1f, 0f, 1f),
        new Row(SfxId.EchoVoiceAttack, "VoiceDemon10", 0.7f, 1f, 0.05f, 0.3f),
        new Row(SfxId.EchoVoiceSummon, "DemonRoar19", 0.75f, 1f, 0.03f, 0.5f),
        new Row(SfxId.EchoVoiceHurt, "DemonRoar21", 0.65f, 1f, 0.05f, 0.5f),
        new Row(SfxId.EchoVoiceRage, "DemonRoar07", 0.9f, 1f, 0f, 1f),
        new Row(SfxId.EchoVoiceDeath, "HumanDyingBreath00", 0.9f, 1f, 0f, 1f),
        // Ancient Entborn: เสียงร้อง + ชั้นเสียงตัวไม้ (ลั่นตอนง้างท่า แตกตอนสตัน/ยืนหยัดครั้งสุดท้าย/ตาย)
        new Row(SfxId.EntbornVoiceAttack, "OgreRoar18", 0.75f, 1f, 0.05f, 0.5f),
        new Row(SfxId.EntbornVoiceHurt, "OgreRoar20", 0.65f, 1f, 0.05f, 0.5f),
        new Row(SfxId.EntbornVoiceWeak, "BeastRoar15", 0.7f, 1f, 0.04f, 0.5f),
        new Row(SfxId.EntbornVoiceDeath, "OgreRoar21", 0.95f, 1f, 0f, 1f),
        new Row(SfxId.EntbornWoodCreak, "ImpactWood18", 0.6f, 1f, 0.08f, 1f),               // ตัดเหลือ 1.2 วิ
        new Row(SfxId.EntbornWoodBreak, "ImpactWood05", 0.8f, 1f, 0.05f, 1f),
        // Architect of Collapse
        new Row(SfxId.ArchitectVoiceIntro, "SpaceAlert01", 0.85f, 1f, 0f, 1f),
        new Row(SfxId.ArchitectVoiceAttack, "SpaceAttack01", 0.65f, 1f, 0.05f, 1.5f),       // ตัดเหลือ 1.5 วิ เริ่มท่าใหม่เร็วกว่านี้ไม่ร้องซ้ำ
        new Row(SfxId.ArchitectVoiceHurt, "SpacePain02", 0.6f, 1f, 0.05f, 2f),              // ตัดเหลือ 0.7 วิ ร้องได้ไม่ถี่กว่า 2 วิ
        new Row(SfxId.ArchitectVoiceTransform, "SpaceAlert02", 0.9f, 1f, 0f, 1f),
        new Row(SfxId.ArchitectVoiceDeath, "SpaceDeath00", 0.95f, 1f, 0f, 1f),
    };

    // เสียงโจมตีของอาวุธแต่ละชิ้น (ชื่อไฟล์ WeaponData) อาวุธที่ไม่อยู่ในตารางใช้เสียงกลางของชนิดนั้น
    static readonly Dictionary<string, SfxId> Weapons = new Dictionary<string, SfxId>
    {
        { "Rusty Sword", SfxId.AttackSword }, { "Old Iron Sword", SfxId.AttackSword }, { "Steel Sword", SfxId.AttackSword },
        { "Rusty Dagger", SfxId.AttackDagger }, { "Twin Steel Daggers", SfxId.AttackDagger },
        { "Nano Baton", SfxId.AttackBaton }, { "Rift Sword", SfxId.AttackRiftSword },
        { "Iron Hammer", SfxId.AttackHammer }, { "Gravity Hammer", SfxId.AttackHammer }, { "Quantum Hammer", SfxId.AttackHammer },
        { "Starter Spear", SfxId.AttackSpear }, { "Reinforced Spear", SfxId.AttackSpear }, { "Ion Spear X", SfxId.AttackSpear },
        { "Rusty Claw", SfxId.AttackClaw },
        { "Rusty Bow", SfxId.AttackBow }, { "Wooden Bow", SfxId.AttackBow }, { "Wooden Bow Mk2", SfxId.AttackBow },
        { "Scatter Bow", SfxId.AttackScatterBow },
        { "Rusty Pistol", SfxId.GunBullet }, { "Old Rifle", SfxId.GunBullet },
        { "Energy Gun", SfxId.GunEnergy }, { "Ion Blaster", SfxId.GunIon }, { "Rocket Launcher", SfxId.RocketFire },
    };

    // มอนที่เสียงยิงไม่ใช่เสียงยิงปกติ (ชื่อไฟล์ MonsterData)
    static readonly Dictionary<string, SfxId> MonsterShots = new Dictionary<string, SfxId>
    {
        { "Woodmine", SfxId.MonSpit },
    };

    // เสียงร้องของมอนแต่ละตัว (ชื่อไฟล์ MonsterData): โจมตี, โดนตี, ตาย ช่องไหนไม่มีเสียงใส่ None
    // บอสใส่แค่เสียงโดนตี เสียงอื่นสคริปต์บอสเล่นเองตามจังหวะท่า (Architect ไม่ใช้ MonsterData เล่นเองทั้งหมด)
    static readonly Dictionary<string, SfxId[]> MonsterVoices = new Dictionary<string, SfxId[]>
    {
        { "Dimensional-Wolf", new[] { SfxId.VoiceWolfAttack, SfxId.VoiceWolfHurt, SfxId.VoiceWolfDeath } },
        { "Flux-Jaw", new[] { SfxId.VoiceJawAttack, SfxId.VoiceJawHurt, SfxId.VoiceJawDeath } },
        { "Echo-Stalker", new[] { SfxId.VoiceStalkerAttack, SfxId.VoiceStalkerHurt, SfxId.VoiceStalkerDeath } },
        { "Forest-Wraith", new[] { SfxId.VoiceWraithAttack, SfxId.VoiceWraithHurt, SfxId.VoiceWraithDeath } },
        { "Mutated-Heavy", new[] { SfxId.VoiceHeavyAttack, SfxId.VoiceHeavyHurt, SfxId.VoiceHeavyDeath } },
        { "Phase-Soldier", new[] { SfxId.VoiceSoldierAttack, SfxId.VoiceSoldierHurt, SfxId.VoiceSoldierDeath } },
        { "Rift_Walker", new[] { SfxId.VoiceWorkerAttack, SfxId.VoiceWorkerHurt, SfxId.VoiceWorkerDeath } }, // Rift-Drained Worker
        { "Rootlings", new[] { SfxId.VoiceRootlingAttack, SfxId.VoiceRootlingHurt, SfxId.VoiceRootlingDeath } },
        { "Woodmine", new[] { SfxId.VoiceWoodmineAttack, SfxId.VoiceWoodmineHurt, SfxId.VoiceWoodmineDeath } },
        { "Zero-Husk", new[] { SfxId.VoiceHuskAttack, SfxId.VoiceHuskHurt, SfxId.None } }, // ระเบิดตัวเอง ไม่มีเสียงร้องตอนตาย
        { "Echo_Commander", new[] { SfxId.None, SfxId.EchoVoiceHurt, SfxId.None } },
        { "Ancient_Entborn", new[] { SfxId.None, SfxId.EntbornVoiceHurt, SfxId.None } },
    };

    static SfxLibraryBuilder()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Refresh(false, false);
        };
    }

    [MenuItem("Tools/Quantum Rift/Audio/Refresh Sound Library")]
    static void RefreshFromMenu() => Selection.activeObject = Refresh(true, false);

    [MenuItem("Tools/Quantum Rift/Audio/Reset Volumes And Pitch")]
    static void ResetFromMenu() => Selection.activeObject = Refresh(true, true);

    public static SfxLibrary Refresh(bool log, bool resetValues)
    {
        var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(AssetPath);
        if (library == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            library = ScriptableObject.CreateInstance<SfxLibrary>();
            AssetDatabase.CreateAsset(library, AssetPath);
        }
        string before = EditorJsonUtility.ToJson(library);

        // คลิปตามชื่อ: "SwordSlash_2" เป็นแบบย่อยของ "SwordSlash"
        var clips = new Dictionary<string, List<AudioClip>>();
        if (AssetDatabase.IsValidFolder(ClipFolder))
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { ClipFolder }).OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip == null) continue;
                string name = BaseName(clip.name);
                if (!clips.TryGetValue(name, out var list)) clips[name] = list = new List<AudioClip>();
                list.Add(clip);
            }

        var old = new Dictionary<SfxId, SfxLibrary.Entry>();
        if (library.entries != null)
            foreach (var entry in library.entries)
                if (entry != null) old[entry.id] = entry;
        var missing = new List<string>();
        library.entries = Table.Select(row =>
        {
            // ค่าที่ปรับใน Inspector อยู่ต่อ ยกเว้นตารางเปลี่ยนไฟล์เสียงของรายการนั้น (ค่าเดิมปรับไว้กับเสียงเก่า)
            bool keep = !resetValues && old.TryGetValue(row.id, out var existing) && SourceOf(existing) == row.file;
            var entry = keep ? old[row.id] : new SfxLibrary.Entry
            {
                id = row.id, volume = row.volume, pitch = row.pitch, pitchJitter = row.jitter, minInterval = row.interval,
            };
            entry.clips = clips.TryGetValue(row.file, out var found) ? found.ToArray() : new AudioClip[0];
            if (entry.clips.Length == 0) missing.Add($"{row.id} ({row.file})");
            return entry;
        }).ToArray();

        library.weapons = Find<WeaponData>(Weapons).Select(p => new SfxLibrary.WeaponSound { weapon = p.asset, attack = p.id }).ToArray();
        library.monsters = All<MonsterData>().Where(p => MonsterShots.ContainsKey(p.name) || MonsterVoices.ContainsKey(p.name)).Select(p =>
        {
            MonsterVoices.TryGetValue(p.name, out var voice);
            return new SfxLibrary.MonsterSound
            {
                monster = p.asset,
                shot = MonsterShots.TryGetValue(p.name, out var shot) ? shot : SfxId.None,
                attackVoice = voice != null ? voice[0] : SfxId.None,
                hurtVoice = voice != null ? voice[1] : SfxId.None,
                deathVoice = voice != null ? voice[2] : SfxId.None,
            };
        }).ToArray();

        if (EditorJsonUtility.ToJson(library) != before)
        {
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
        }
        if (missing.Count > 0) Debug.LogWarning("คลังเสียง: ไม่เจอไฟล์เสียงของ " + string.Join(", ", missing));
        if (log) Debug.Log($"คลังเสียง: {library.entries.Length} รายการ ไฟล์เสียง {clips.Values.Sum(l => l.Count)} ไฟล์ " +
                           $"อาวุธ {library.weapons.Length} มอน {library.monsters.Length} ขาด {missing.Count}");
        return library;
    }

    // "SwordSlash_2" เป็นแบบย่อยของ "SwordSlash" (ชื่อไฟล์เสียงจึงห้ามลงท้ายด้วย _เลข ถ้าไม่ใช่แบบย่อย)
    static string BaseName(string name)
    {
        int cut = name.LastIndexOf('_');
        return cut > 0 && int.TryParse(name.Substring(cut + 1), out _) ? name.Substring(0, cut) : name;
    }

    // ชื่อไฟล์เสียงที่รายการในคลังใช้อยู่ตอนนี้ ไว้เช็คว่าตารางเปลี่ยนไฟล์ไปหรือยัง
    static string SourceOf(SfxLibrary.Entry entry) =>
        entry.clips != null && entry.clips.Length > 0 && entry.clips[0] != null ? BaseName(entry.clips[0].name) : null;

    static IEnumerable<(T asset, SfxId id)> Find<T>(Dictionary<string, SfxId> names) where T : Object
    {
        foreach (var (name, asset) in All<T>())
            if (names.TryGetValue(name, out var id)) yield return (asset, id);
    }

    // ไฟล์ข้อมูลชนิด T ทั้งโปรเจกต์ พร้อมชื่อไฟล์ (ตารางในไฟล์นี้อ้างด้วยชื่อไฟล์)
    static IEnumerable<(string name, T asset)> All<T>() where T : Object
    {
        foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name).OrderBy(AssetDatabase.GUIDToAssetPath))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) yield return (Path.GetFileNameWithoutExtension(path), asset);
        }
    }
}

// ตั้งค่านำเข้าไฟล์เสียงเอฟเฟกต์ให้เหมือนกันหมด: โมโน โหลดไว้ในหน่วยความจำ บีบแบบ ADPCM (ไฟล์สั้น เล่นถี่ ไม่กิน CPU)
public sealed class SfxImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').StartsWith("Assets/Audio/SFX/")) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.ADPCM;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
