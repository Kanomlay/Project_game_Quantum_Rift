using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// บันทึกอัตโนมัติของรอบเล่น: เก็บ "สถานะตอนเพิ่งเข้าแมพ" ทุกครั้งที่โหลดแมพเสร็จ (MapManager เรียก Capture) ไม่บันทึกกลางแมพ
// ใช้แค่กรณีออกเกมแล้วกลับมา: เมนูหลักมีปุ่ม "เล่นต่อ" (ContinueMenu) เริ่มที่ต้นแมพนั้นใหม่ ผังห้องสุ่มใหม่ตามปกติ
// ตายหรือเคลียร์เกมจบ = ลบเซฟ (roguelite เหมือนเดิม) สนามฝึกและห้องทดสอบไม่บันทึกและไม่แตะเซฟ
// ไฟล์เดียวเป็น JSON ใน Application.persistentDataPath เก็บของเป็นชื่อไฟล์ asset แล้วหาตัวจริงตอนโหลด
// (อาชีพ/อาวุธจาก Resources/DevCatalog, แมพจากสายด่านของ MapManager, พรจากรายการของ BlessingManager)
// หาไม่เจอ (เซฟจากเวอร์ชันเก่า ไฟล์เสีย) = ถือว่าไม่มีเซฟ ปุ่มเล่นต่อไม่ขึ้น
public static class RunSave
{
    const int Version = 1;
    const string FileName = "run-save.json";

    [Serializable]
    public sealed class BlessingEntry
    {
        public string name;
        public int level;
    }

    [Serializable]
    sealed class Data
    {
        public int version;
        public string map, character;
        public float maxHP, currentHP;
        public int maxEnergy, currentEnergy, currency, damageBonus;
        public string weapon1, weapon2;
        public int weaponSlot = 1;
        public List<BlessingEntry> blessings = new List<BlessingEntry>();
        public float elapsed; // เวลาเล่นสะสม (หน้าสรุป)
        public int kills;
    }

    // เซฟที่กำลังเล่นต่อ: ตั้งตอนกดปุ่มเล่นต่อ อยู่จนแมพแรกโหลดเสร็จ ระหว่างนั้น MapManager/PlayerStats/SummaryManager มาหยิบส่วนของตัวเอง
    static Data resuming;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { resuming = null; }

    static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // ---------- เมนูหลัก ----------

    public static bool HasSave => Read() != null;

    // ข้อความสั้น ๆ บอกว่าเซฟค้างอยู่ที่ไหน เช่น "นักรบ · Map 1-3"
    public static bool TryDescribe(out string text)
    {
        text = null;
        var data = Read();
        if (data == null) return false;
        var catalog = DevCatalog.Load();
        var map = Find(catalog.maps, data.map);
        text = Find(catalog.characters, data.character).DisplayName + " · " + (map != null ? map.mapName : data.map);
        return true;
    }

    // กดเล่นต่อ: ตั้งอาชีพจากเซฟแล้วให้ฉากเกมมาหยิบค่าที่เหลือ คืน false ถ้าเซฟใช้ไม่ได้ (คนเรียกไม่ต้องโหลดฉาก)
    public static bool BeginResume()
    {
        var data = Read();
        if (data == null) return false;
        GameManager.selectedCharacter = Find(DevCatalog.Load().characters, data.character);
        resuming = data;
        return true;
    }

    // ---------- ฉากเกม ----------

    // MapManager ตอนเริ่มฉาก: แมพที่ต้องเริ่มเมื่อเล่นต่อ (null = ไม่ได้เล่นต่อ) หาแมพไม่เจอ = ทิ้งเซฟ เริ่มด่านแรกเป็นเกมใหม่
    public static MapData ResumeMap(MapData firstMap)
    {
        if (resuming == null) return null;
        int guard = 0;
        for (var map = firstMap; map != null && guard++ < 100; map = map.nextMap)
            if (map.name == resuming.map) return map;
        Debug.LogWarning($"เซฟชี้ไปแมพ {resuming.map} ที่ไม่อยู่ในสายด่านแล้ว เริ่มเกมใหม่แทน");
        resuming = null;
        Delete();
        return null;
    }

    // PlayerStats ท้าย Start: เขียนทับค่าเริ่มต้นของอาชีพด้วยค่าจากเซฟ
    public static void RestorePlayer(PlayerStats player)
    {
        if (resuming == null || player == null) return;
        var data = resuming;
        // พรก่อน: การอัประดับพรบางอันเพิ่มเลือดสูงสุดทันที ค่าในเซฟรวมผลนั้นไว้แล้วจึงต้องเขียนทับทีหลัง
        var blessings = BlessingManager.Instance;
        if (blessings != null && data.blessings != null)
            foreach (var entry in data.blessings)
                if (entry != null) blessings.RestoreSaved(entry.name, entry.level);

        var weapons = DevCatalog.Load() != null ? DevCatalog.Load().weapons : null;
        // ชื่อว่าง = ช่องนั้นไม่มีอาวุธ มีชื่อแต่หาไม่เจอ = ใช้อาวุธเริ่มต้นของอาชีพแทน (ช่อง 1) หรือปล่อยว่าง (ช่อง 2)
        WeaponData first = string.IsNullOrEmpty(data.weapon1) ? null : Find(weapons, data.weapon1) ?? player.weapon1;
        WeaponData second = string.IsNullOrEmpty(data.weapon2) ? null : Find(weapons, data.weapon2);
        player.RestoreRun(data.maxHP, data.currentHP, data.maxEnergy, data.currentEnergy, data.currency, data.damageBonus,
                          first, second, data.weaponSlot);
    }

    // SummaryManager ตอนเริ่มฉาก: เวลาเล่นและจำนวนมอนนับต่อจากที่บันทึกไว้
    public static bool ResumeStats(out float elapsed, out int kills)
    {
        elapsed = resuming != null ? Mathf.Max(0f, resuming.elapsed) : 0f;
        kills = resuming != null ? Mathf.Max(0, resuming.kills) : 0;
        return resuming != null;
    }

    // MapManager หลังโหลดแมพเสร็จ: บันทึกสถานะตอนเข้าแมพ (แมพจริงเท่านั้น)
    public static void Capture(MapData map, GameObject playerObject)
    {
        resuming = null; // แมพแรกของการเล่นต่อโหลดเสร็จ ทุกระบบหยิบค่าไปครบแล้ว
        if (map == null || map.isTutorial || map.isTestLab || playerObject == null) return;
        var player = playerObject.GetComponent<PlayerStats>();
        var character = GameManager.selectedCharacter;
        if (player == null || player.isDead || character == null) return; // เปิดฉากเกมตรง ๆ ใน Editor ไม่มีอาชีพให้จำ

        var data = new Data
        {
            version = Version,
            map = map.name,
            character = character.name,
            maxHP = player.maxHP,
            currentHP = player.currentHP,
            maxEnergy = player.maxEnergy,
            currentEnergy = player.currentEnergy,
            currency = player.currentCurrency,
            damageBonus = player.runDamageBonus,
            weapon1 = player.weapon1 != null ? player.weapon1.name : "",
            weapon2 = player.weapon2 != null ? player.weapon2.name : "",
            weaponSlot = player.CurrentWeaponSlot,
            elapsed = SummaryManager.instance != null ? SummaryManager.instance.ElapsedSeconds : 0f,
            kills = SummaryManager.enemiesDefeatedCount,
        };
        var blessings = BlessingManager.Instance;
        if (blessings != null)
            foreach (var blessing in blessings.Owned)
                if (blessing != null) data.blessings.Add(new BlessingEntry { name = blessing.name, level = blessings.LevelOf(blessing) });
        Write(data);
    }

    // รอบนี้จบแล้ว (ตาย / เคลียร์เกม): ลบเซฟ เว้นแต่อยู่ในสนามฝึกหรือห้องทดสอบ ซึ่งไม่เกี่ยวกับเซฟของเกมจริง
    public static void EndRun()
    {
        var map = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
        if (map == null || map.isTutorial || map.isTestLab) return;
        Delete();
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception error)
        {
            Debug.LogWarning("ลบไฟล์เซฟไม่ได้: " + error.Message);
        }
    }

    // ---------- ไฟล์ ----------

    static void Write(Data data)
    {
        try
        {
            // เขียนไฟล์ชั่วคราวก่อนแล้วค่อยสลับ ปิดเกมกลางคันจะได้ไม่เหลือไฟล์เซฟครึ่ง ๆ กลาง ๆ
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temp, FilePath);
        }
        catch (Exception error)
        {
            Debug.LogWarning("บันทึกเกมไม่ได้: " + error.Message);
        }
    }

    // อ่านและตรวจว่าใช้เล่นต่อได้จริง: เวอร์ชันตรง อาชีพและแมพยังมีอยู่ในเกม
    static Data Read()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath));
            var catalog = DevCatalog.Load();
            if (data == null || data.version != Version || catalog == null) return null;
            if (Find(catalog.characters, data.character) == null) return null;
            var map = Find(catalog.maps, data.map);
            if (map == null || map.isTutorial) return null;
            return data;
        }
        catch (Exception error)
        {
            Debug.LogWarning("อ่านไฟล์เซฟไม่ได้: " + error.Message);
            return null;
        }
    }

    static T Find<T>(T[] list, string name) where T : UnityEngine.Object
    {
        if (list == null || string.IsNullOrEmpty(name)) return null;
        foreach (var item in list)
            if (item != null && item.name == name) return item;
        return null;
    }
}
