// เหตุการณ์ในเกมที่มีเสียง (เสียงจริงของแต่ละอันอยู่ใน Resources/SfxLibrary ดู Editor/SfxLibraryBuilder.cs)
// SfxLibrary เก็บค่านี้เป็นเลข: เพิ่มรายการใหม่ให้ต่อท้ายหมวดสุดท้าย อย่าแทรกกลางหรือสลับลำดับ
public enum SfxId
{
    None = 0,

    // อาวุธ
    AttackSword, AttackDagger, AttackBaton, AttackRiftSword, RiftWave,
    AttackHammer, QuantumPulse,
    AttackSpear, IonCharge, IonWave,
    AttackClaw,
    BowDraw, AttackBow, AttackScatterBow, Ricochet,
    GunBullet, GunEnergy, GunIon, RocketFire, RocketExplode,

    // ตีโดน / ผู้เล่น / มอนทั่วไป
    HitMonster, HitArmor, PlayerHurt, PlayerDeath, MonsterDeath, MonsterSpawn,

    // เก็บของ / UI
    PickupCoin, PickupPotion, PickupMana, PickupWeapon, UiClick, UiDenied,

    // สกิลประจำอาชีพ
    SkillImpactDash, SkillImpactDashHit, SkillArmorOn, SkillArmorReflect,
    SkillDimensionalArrow, SkillShadowStep,
    SkillDroneDeploy, SkillDroneShot, SkillTrapPlace, SkillTrapTrigger,
    SkillFangs, SkillCellStim,

    // ท่าของมอน
    MonMelee, MonPounce, MonBite, MonLunge, MonShot, MonSpit, MonRockThrow, MonSlam,
    MonRoot, MonBurrow, MonVine, MonFuse, MonExplode,

    // บอส: ใช้ร่วมกัน
    BossLaserCharge, BossLaserBeam, BossTeleport, BossBullets,
    // Echo Commander
    EchoSlashWindup, EchoSlash, EchoSummon, EchoRage, EchoDeath,
    // Ancient Entborn
    EntbornRoar, EntbornStomp, EntbornRoot, EntbornLeap, EntbornDeath,
    // Architect of Collapse
    ArchitectWave, ArchitectSlash, ArchitectStorm, ArchitectTransform, ArchitectCollapse, ArchitectDeath,
}
