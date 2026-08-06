using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("ข้อมูลประจำห้อง (Data-Driven)")]
    public RoomEncounterData roomData; 

    [Header("ชิ้นส่วนในห้อง")]
    public GameObject[] doors; // กำแพงหรือประตูที่ปิดกั้นทางออก
    public Transform[] monsterSpawnPoints; // จุดเกิดมอนสเตอร์ในห้อง
    public Transform chestSpawnPoint; // จุดเกิดกล่องตรงกลาง
    public GameObject chestPrefab; // หน้าตากล่องสมบัติ

    private bool hasStarted = false;
    private bool isCleared = false;
    private int aliveMonstersCount = 0;

    // 🌟 1. เมื่อผู้เล่นเดินเข้ามาในเขตห้อง
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasStarted && !isCleared)
        {
            StartEncounter();
        }
    }

    private void StartEncounter()
    {
        hasStarted = true;
        
        // ล็อกประตูทุกบาน
        foreach (GameObject door in doors) { door.SetActive(true); }

        // 🌟 2. ดึงข้อมูลมอนสเตอร์จาก Data มาเสก
        if (roomData != null && roomData.monstersToSpawn.Length > 0)
        {
            aliveMonstersCount = roomData.monstersToSpawn.Length;

            for (int i = 0; i < roomData.monstersToSpawn.Length; i++)
            {
                // สุ่มจุดเกิดจากจุดที่เราตั้งไว้
                Transform spawnPoint = (i < monsterSpawnPoints.Length) ? monsterSpawnPoints[i] : transform;
                
                // [🔥 Object Pooling Concept] เสกมอนสเตอร์จาก Prefab ที่อยู่ใน MonsterData
                GameObject monsterObj = Instantiate(roomData.monstersToSpawn[i].monsterPrefab, spawnPoint.position, Quaternion.identity);
                
                // 🌟 แทรกแซงมอนสเตอร์: บอกมันว่า "นายเกิดที่ห้องนี้นะ ตอนตายอย่าลืมมารายงานตัวด้วย!"
                MonsterController monsterInfo = monsterObj.GetComponent<MonsterController>();
                if (monsterInfo != null)
                {
                    monsterInfo.currentRoom = this; // ส่งสคริปต์ห้องนี้ไปให้มอนสเตอร์จำไว้
                    monsterInfo.myData = roomData.monstersToSpawn[i]; // ยัด Data ให้มอนสเตอร์
                }
            }
        }
        else
        {
            ClearRoom(); // ถ้าห้องนี้ไม่ได้ใส่มอนสเตอร์ไว้ ให้เคลียร์ผ่านเลย
        }
    }

    // 🌟 3. รับแจ้งตายจากมอนสเตอร์
    public void OnMonsterDied()
    {
        aliveMonstersCount--;
        Debug.Log("มอนสเตอร์ตาย! เหลืออีก: " + aliveMonstersCount);

        if (aliveMonstersCount <= 0)
        {
            ClearRoom();
        }
    }

    // 🌟 4. ชนะแล้ว! ปลดล็อกห้อง
    private void ClearRoom()
    {
        isCleared = true;
        Debug.Log("🎉 เคลียร์ห้องสำเร็จ! เปิดประตู & เสกกล่องสมบัติ");

        // เปิดประตู
        foreach (GameObject door in doors) { door.SetActive(false); }

        // เสกกล่องสมบัติ (เดี๋ยวเราค่อยมาเขียนระบบเปิดกล่องทีหลัง)
        if (chestPrefab != null)
        {
            GameObject chestObj = Instantiate(chestPrefab, chestSpawnPoint.position, Quaternion.identity);
        }
    }
}