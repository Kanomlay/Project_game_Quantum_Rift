using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomEncounter", menuName = "Game Data/Room Encounter")]
public class RoomEncounterData : ScriptableObject
{
    [Header("แบบจัดฉาก (ใส่ตัวไหนก็ได้ตัวนั้นเป๊ะๆ)")]
    public MonsterData[] monstersToSpawn; // ใช้กับห้องบอสหรือห้องที่อยากคุมเองทั้งหมด

    [Header("แบบสุ่ม (ใช้เมื่อช่องด้านบนว่าง)")]
    public MonsterData[] possibleMonsters; // ชุดมอนสเตอร์ที่ห้องนี้สุ่มออกมาได้
    [Min(0)] public int minMonsters = 2;
    [Min(1)] public int maxMonsters = 4;

    [Header("หัวหน้าหน่วย (ตาราง 1.7) แทรกในกองแบบสุ่ม นับรวมใน min/max")]
    public MonsterData[] leaderMonsters; // สุ่มมา 1 ตัวจากชุดนี้
    [Range(0f, 1f)] public float leaderChance; // โอกาสที่เข้าห้องแล้วจะเจอหัวหน้าหน่วย (1 = เจอทุกครั้ง)

    [Header("ของรางวัลเมื่อเคลียร์ห้อง (ตู้สมบัติ)")]
    public WeaponData[] possibleWeaponDrops; // รายชื่ออาวุธที่มีโอกาสดรอป
    // (อนาคตสามารถเพิ่ม public ItemData[] possiblePotions; ได้ตรงนี้)

    // จัดกองมอนสเตอร์สำหรับการเข้าห้องครั้งนี้ สุ่มใหม่ทุกครั้งที่เข้าด่าน
    // limit = จำนวนจุดเกิดที่ห้องมี จะได้ไม่เสกซ้อนกันอยู่จุดเดียว
    public List<MonsterData> BuildWave(int limit)
    {
        var wave = new List<MonsterData>();

        if (monstersToSpawn != null && monstersToSpawn.Length > 0)
        {
            foreach (var monster in monstersToSpawn)
                if (monster != null) wave.Add(monster);
            return wave;
        }

        if (possibleMonsters == null || possibleMonsters.Length == 0) return wave;

        int highest = Mathf.Max(minMonsters, maxMonsters);
        int count = Random.Range(Mathf.Min(minMonsters, highest), highest + 1);
        count = Mathf.Min(count, Mathf.Max(1, limit));

        // Random.value ออก 1 ได้ จึงต้องเช็ค >= 1 แยก ไม่งั้นตั้ง 1 แล้วยังพลาดได้บางครั้ง
        bool hasLeader = leaderMonsters != null && leaderMonsters.Length > 0 &&
                         (leaderChance >= 1f || Random.value < leaderChance);
        if (hasLeader)
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
        return wave;
    }
}
