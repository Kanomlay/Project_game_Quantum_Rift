using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// ประวัติที่จบรอบแล้วแยกจาก run-save.json: เริ่มเกมใหม่หรือลบเซฟเล่นต่อไม่ลบสถิติ
// เก็บ 20 รอบล่าสุด แต่ยอดสะสมและเวลาชนะดีที่สุดไม่หายเมื่อรายการเก่าถูกตัดออก
public static class RunHistory
{
    public const int Limit = 20;
    const int Version = 1;
    const string FileName = "run-history.json";

    [Serializable]
    public sealed class Entry
    {
        // เก็บเวลามาตรฐาน UTC และชื่อทั้งสองภาษา หน้าประวัติแปลงเป็นเวลาท้องถิ่นตอนแสดง
        public string id, finishedUtc, classEnglish, classThai, stage;
        public bool won;
        public float seconds;
        public int kills, remainingCoins;
    }

    [Serializable]
    public sealed class Data
    {
        public int version = Version;
        // ยอดสะสมทั้งเกม ไม่คำนวณจากรายการ 20 รอบ เพราะรอบเก่าจะถูกตัดออก
        public int totalRuns, totalWins;
        public float bestWinSeconds = -1f;
        public List<Entry> entries = new List<Entry>();
    }

    static string activeId;
    static bool excluded;
    static bool pendingExcluded;
    public static string ActiveId => activeId;
    public static bool Excluded => excluded;
    public static bool LastSaveFailed { get; private set; }

#if UNITY_EDITOR
    // การตรวจในสำเนาเปลี่ยนพาธนี้ผ่าน reflection เพื่อไม่แตะประวัติจริงของผู้เล่น
    static string testStorageDirectory;
#endif
    static string FilePath
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(testStorageDirectory)) return Path.Combine(testStorageDirectory, FileName);
#endif
            return Path.Combine(Application.persistentDataPath, FileName);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        ForgetActiveRun();
        LastSaveFailed = false;
#if UNITY_EDITOR
        testStorageDirectory = null;
#endif
    }

    public static void EnsureActiveRun(string resumedId = null, bool resumedExcluded = false)
    {
        if (string.IsNullOrEmpty(activeId))
        {
            activeId = string.IsNullOrEmpty(resumedId) ? Guid.NewGuid().ToString("N") : resumedId;
            excluded = resumedExcluded || pendingExcluded;
        }
        excluded |= resumedExcluded || DevConsole.Enabled || RunSave.UseTestFile;
    }

    // เปิดคอนโซลกลางรอบแล้วปิดทีหลังยังเป็นรอบทดสอบ รวมถึงกรณีออกเกมแล้วเล่นต่อ
    public static void MarkTestRun()
    {
        if (string.IsNullOrEmpty(activeId))
        {
            // ช่วงเฟรมแรกก่อน Capture ยังไม่มีรหัส แต่เปิดโหมดทดสอบในฉากเกมแล้วต้องจำไว้
            var map = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
            if (map != null && !map.isTutorial) pendingExcluded = true;
            return;
        }
        excluded = true;
        RunSave.PersistHistoryExclusion(activeId);
    }

    public static void ForgetActiveRun()
    {
        activeId = null;
        excluded = false;
        pendingExcluded = false;
    }

    public static Data Load()
    {
        if (TryRead(FilePath, out var data)) return data;
        if (TryRead(FilePath + ".bak", out data)) return data;
        return new Data();
    }

    static bool TryRead(string path, out Data data)
    {
        data = null;
        if (!File.Exists(path)) return false;
        try
        {
            var parsed = JsonUtility.FromJson<Data>(File.ReadAllText(path));
            if (parsed == null || parsed.version != Version || parsed.entries == null || parsed.entries.Count > Limit
                || parsed.totalRuns < parsed.entries.Count || parsed.totalWins < 0 || parsed.totalWins > parsed.totalRuns
                || !Finite(parsed.bestWinSeconds) || (parsed.totalWins == 0 ? parsed.bestWinSeconds != -1 : parsed.bestWinSeconds < 0))
                throw new InvalidDataException("Invalid history header");
            var ids = new HashSet<string>();
            foreach (var row in parsed.entries)
                if (row == null || string.IsNullOrEmpty(row.id) || !ids.Add(row.id) || !Finite(row.seconds) || row.seconds < 0
                    || row.kills < 0 || row.remainingCoins < 0 || string.IsNullOrEmpty(row.stage)
                    || !DateTimeOffset.TryParse(row.finishedUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    throw new InvalidDataException("Invalid history entry");
            data = parsed;
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("อ่านประวัติการเล่นไม่ได้: " + error.Message);
            return false;
        }
    }

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // เรียกก่อนลบเซฟเฉพาะตอนแพ้หรือชนะด่านสุดท้าย ไม่บันทึกการผ่านบอสระหว่างทาง
    public static bool RecordFinished(MapData map, bool won, CharacterData character, float seconds, int kills, int coins)
    {
        LastSaveFailed = false;
        if (map == null || map.isTutorial || map.isTestLab || (won && map.nextMap != null)) return false;
        if (string.IsNullOrEmpty(activeId)) return false;
        if (excluded || DevConsole.Enabled || RunSave.UseTestFile) return false;
        if (!Finite(seconds) || seconds < 0) return false; // เวลาผิดปกติไม่สร้างสถิติชนะ 00:00
        var data = Load();
        // ป้องกัน callback จบรอบซ้ำ หรือเซฟรอบเก่าที่ลบไม่สำเร็จถูกเล่นต่อ
        if (data.entries.Exists(row => row.id == activeId)) return false;
        var entry = new Entry
        {
            id = activeId,
            finishedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            won = won,
            classEnglish = character != null ? character.className : "Unknown",
            classThai = character != null && !string.IsNullOrEmpty(character.classNameThai) ? character.classNameThai : character != null ? character.className : "ไม่ทราบ",
            stage = string.IsNullOrEmpty(map.mapName) ? map.name : map.mapName,
            seconds = seconds,
            kills = Mathf.Max(0, kills),
            remainingCoins = Mathf.Max(0, coins)
        };
        data.entries.Insert(0, entry);
        if (data.entries.Count > Limit) data.entries.RemoveRange(Limit, data.entries.Count - Limit);
        data.totalRuns++;
        if (won)
        {
            data.totalWins++;
            if (data.bestWinSeconds < 0 || entry.seconds < data.bestWinSeconds) data.bestWinSeconds = entry.seconds;
        }
        bool saved = Write(data);
        LastSaveFailed = !saved;
        return saved;
    }

    static bool Write(Data data)
    {
        string path = FilePath;
        string temp = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // ไม่ทับประวัติที่อ่านไม่ได้โดยเงียบ ๆ เก็บไฟล์ต้นฉบับไว้ให้กู้คืนก่อน
            if (File.Exists(path) && !TryRead(path, out _))
                File.Copy(path, path + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(path))
            {
                // ไฟล์สำรองเป็นประวัติที่ตรวจแล้วเท่านั้น ไม่แทนด้วยไฟล์เสีย
                if (TryRead(path, out _)) File.Replace(temp, path, path + ".bak");
                else File.Replace(temp, path, null);
            }
            else File.Move(temp, path);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("บันทึกประวัติการเล่นไม่ได้: " + error.Message);
            return false;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (Exception error) { Debug.LogWarning("ล้างไฟล์ชั่วคราวของประวัติไม่ได้: " + error.Message); }
        }
    }
}
