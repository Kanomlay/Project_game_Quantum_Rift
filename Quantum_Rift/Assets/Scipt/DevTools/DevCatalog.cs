using System;
using UnityEngine;

// รายชื่อของทั้งหมดที่คอนโซลทดสอบเสกได้ อยู่ใน Resources คอนโซลจะได้โหลดเองโดยไม่ต้องวางอะไรในฉาก
// Editor เติมให้เองทุกครั้งที่กด Play (ดู Editor/DevCatalogBuilder.cs) ของใหม่ที่เพิ่มในโปรเจกต์จะโผล่ในคอนโซลเอง ไม่ต้องแก้มือ
public sealed class DevCatalog : ScriptableObject
{
    public const string ResourcePath = "DevCatalog";

    [Serializable]
    public sealed class MonsterEntry
    {
        public MonsterData data;
        public string map;   // แมพแรกที่เจอมอนตัวนี้ (ป้ายในปุ่ม)
        public bool boss;
    }

    [Serializable]
    public sealed class Placeable
    {
        public string label;
        public GameObject prefab;
    }

    public MonsterEntry[] monsters;    // เรียงตามแมพที่เจอครั้งแรก
    public WeaponData[] weapons;       // เรียงตามความหายาก
    public BlessingData[] blessings;
    public CharacterData[] characters;
    public MapData[] maps;             // เรียงตามลำดับด่าน (nextMap)
    public LootTable[] chests;
    public Placeable[] shops;
    public Placeable[] traps;
    public MapData labArena;           // ผังที่ห้องทดสอบยืมใช้: ห้องบอสแมพ 1 (ห้องเดียวกว้าง ๆ)

    static DevCatalog loaded;
    public static DevCatalog Load() => loaded != null ? loaded : loaded = Resources.Load<DevCatalog>(ResourcePath);
}
