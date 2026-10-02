using System;
using System.Collections.Generic;
using UnityEngine;

// คลังเสียงเอฟเฟกต์ทั้งเกม อยู่ใน Resources ตัวเล่นเสียง (Sfx) โหลดเองโดยไม่ต้องวางอะไรในฉาก
// Editor สร้างและเติมคลิปให้เองทุกครั้งที่กด Play (Editor/SfxLibraryBuilder.cs)
// ความดัง/ระดับเสียงปรับใน Inspector ได้ ตัวสร้างไม่ทับค่าของรายการที่มีอยู่แล้ว
public sealed class SfxLibrary : ScriptableObject
{
    public const string ResourcePath = "SfxLibrary";

    [Serializable]
    public sealed class Entry
    {
        public SfxId id;
        public AudioClip[] clips;                      // หลายคลิป = สุ่มสลับ ไม่ซ้ำคลิปเดิมติดกัน
        [Range(0f, 1.5f)] public float volume = 0.7f;
        [Range(0.3f, 2f)] public float pitch = 1f;
        [Range(0f, 0.3f)] public float pitchJitter = 0.05f; // สุ่มระดับเสียงขึ้นลงเล็กน้อยทุกครั้ง
        [Min(0f)] public float minInterval = 0.04f;    // เสียงเดียวกันเล่นซ้ำเร็วกว่านี้ไม่ได้ (ตีโดนหลายตัวพร้อมกัน ฯลฯ)
        [NonSerialized] public float lastPlayed = -99f;
        [NonSerialized] public int lastClip = -1;
    }

    [Serializable]
    public sealed class WeaponSound
    {
        public WeaponData weapon;
        public SfxId attack;
    }

    [Serializable]
    public sealed class MonsterSound
    {
        public MonsterData monster;
        public SfxId shot;
    }

    public Entry[] entries;
    public WeaponSound[] weapons;    // เสียงโจมตีของอาวุธแต่ละชิ้น
    public MonsterSound[] monsters;  // เสียงยิงของมอนที่ไม่ใช้เสียงยิงปกติ (Woodmine ยิงสปอร์)

    Dictionary<SfxId, Entry> byId;
    Dictionary<WeaponData, SfxId> byWeapon;
    Dictionary<MonsterData, SfxId> byMonster;

    public Entry Find(SfxId id)
    {
        if (byId == null)
        {
            byId = new Dictionary<SfxId, Entry>();
            if (entries != null)
                foreach (var entry in entries)
                    if (entry != null) byId[entry.id] = entry;
        }
        return byId.TryGetValue(id, out var found) ? found : null;
    }

    // เสียงโจมตีของอาวุธ: ตามตารางก่อน ไม่มีในตาราง (อาวุธใหม่) ใช้เสียงกลางของชนิดนั้น
    public SfxId AttackOf(WeaponData weapon)
    {
        if (weapon == null) return SfxId.None;
        if (byWeapon == null)
        {
            byWeapon = new Dictionary<WeaponData, SfxId>();
            if (weapons != null)
                foreach (var pair in weapons)
                    if (pair != null && pair.weapon != null) byWeapon[pair.weapon] = pair.attack;
        }
        if (byWeapon.TryGetValue(weapon, out var id)) return id;
        switch (weapon.weaponType)
        {
            case WeaponType.Gun: return SfxId.GunBullet;
            case WeaponType.Bow: return SfxId.AttackBow;
            case WeaponType.Claw: return SfxId.AttackClaw;
            case WeaponType.Spear: return SfxId.AttackSpear;
            case WeaponType.Hammer: return SfxId.AttackHammer;
            default: return SfxId.AttackSword;
        }
    }

    public SfxId ShotOf(MonsterData monster)
    {
        if (byMonster == null)
        {
            byMonster = new Dictionary<MonsterData, SfxId>();
            if (monsters != null)
                foreach (var pair in monsters)
                    if (pair != null && pair.monster != null) byMonster[pair.monster] = pair.shot;
        }
        return monster != null && byMonster.TryGetValue(monster, out var id) ? id : SfxId.MonShot;
    }
}
