using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomEncounter", menuName = "Game Data/Room Encounter")]
public class RoomEncounterData : ScriptableObject
{
    [Header("แบบจัดฉาก (ใส่ตัวไหนก็ได้ตัวนั้นเป๊ะๆ) เกิดระลอกเดียว ตรงจุดเกิดที่วางไว้ในห้อง")]
    public MonsterData[] monstersToSpawn; // ใช้กับห้องบอสหรือห้องที่อยากคุมเองทั้งหมด

    [Header("แบบสุ่ม (ใช้เมื่อช่องด้านบนว่าง) จุดเกิดสุ่มกระจายทั่วห้อง")]
    public MonsterData[] possibleMonsters; // ชุดมอนสเตอร์ที่ห้องนี้สุ่มออกมาได้
    [Min(0)] public int minMonsters = 2;   // จำนวนต่อระลอก
    [Min(1)] public int maxMonsters = 4;

    [Header("ระลอก (แบบสุ่ม): ระลอกถัดไปมาเมื่อมอนในห้องเหลือไม่เกิน nextWaveWhenAlive ตัว")]
    [Min(1)] public int minWaves = 1;
    [Min(1)] public int maxWaves = 1;
    [Min(0)] public int nextWaveWhenAlive = 1;
    [Min(0f)] public float waveDelay = 0.6f; // เว้นจังหวะก่อนวงเตือนของระลอกถัดไปขึ้น

    [Header("หัวหน้าหน่วย (ตาราง 1.7) มากับระลอกสุดท้าย นับรวมในจำนวนของระลอกนั้น")]
    public MonsterData[] leaderMonsters; // สุ่มมา 1 ตัวจากชุดนี้
    [Range(0f, 1f)] public float leaderChance; // โอกาสที่เข้าห้องแล้วจะเจอหัวหน้าหน่วย (1 = เจอทุกครั้ง)

    [Header("ของรางวัลเมื่อเคลียร์ห้อง (ตู้สมบัติ)")]
    public WeaponData[] possibleWeaponDrops; // รายชื่ออาวุธที่มีโอกาสดรอป
    // (อนาคตสามารถเพิ่ม public ItemData[] possiblePotions; ได้ตรงนี้)

    public bool IsStaged => monstersToSpawn != null && monstersToSpawn.Length > 0;

    public bool IsLeader(MonsterData monster) =>
        monster != null && leaderMonsters != null && System.Array.IndexOf(leaderMonsters, monster) >= 0;

    // จัดมอนสเตอร์ทั้งห้องเป็นระลอก สุ่มใหม่ทุกครั้งที่เข้าด่าน (แบบจัดฉากได้ระลอกเดียวตามรายชื่อ)
    public List<List<MonsterData>> BuildWaves()
    {
        var waves = new List<List<MonsterData>>();

        if (IsStaged)
        {
            var staged = new List<MonsterData>();
            foreach (var monster in monstersToSpawn)
                if (monster != null) staged.Add(monster);
            if (staged.Count > 0) waves.Add(staged);
            return waves;
        }

        if (possibleMonsters == null || possibleMonsters.Length == 0) return waves;

        // Random.value ออก 1 ได้ จึงต้องเช็ค >= 1 แยก ไม่งั้นตั้ง 1 แล้วยังพลาดได้บางครั้ง
        bool hasLeader = leaderMonsters != null && leaderMonsters.Length > 0 &&
                         (leaderChance >= 1f || Random.value < leaderChance);

        int waveCount = RandomBetween(minWaves, maxWaves);
        for (int w = 0; w < waveCount; w++)
        {
            var wave = new List<MonsterData>();
            int count = RandomBetween(minMonsters, maxMonsters);

            if (hasLeader && w == waveCount - 1)
            {
                var leader = leaderMonsters[Random.Range(0, leaderMonsters.Length)];
                if (leader != null) wave.Add(leader);
            }

            while (wave.Count < count)
            {
                var pick = possibleMonsters[Random.Range(0, possibleMonsters.Length)];
                if (pick == null) break;
                wave.Add(pick);
            }
            if (wave.Count > 0) waves.Add(wave);
        }
        return waves;
    }

    static int RandomBetween(int a, int b)
    {
        int highest = Mathf.Max(a, b);
        return Random.Range(Mathf.Min(a, highest), highest + 1);
    }
}
