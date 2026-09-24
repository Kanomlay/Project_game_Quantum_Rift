using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("Room encounter")]
    public RoomEncounterData roomData;
    [Header("Room objects")]
    public GameObject[] doors;
    public Transform[] monsterSpawnPoints;
    public Transform chestSpawnPoint;
    public GameObject chestPrefab;

    [Header("เหตุการณ์พิเศษ (ร้านค้า ฯลฯ)")]
    public bool canHostEvent;       // ห้องนี้ยอมให้สุ่มเหตุการณ์มาลงไหม (ห้องบอสควรปิดไว้)
    public Transform eventAnchor;   // จุดวางของ ถ้าไม่ใส่จะใช้กลางห้อง

    public bool HasStarted => hasStarted;
    public event System.Action Cleared; // ประตูมิติในห้องรอฟังเพื่อโผล่ตอนเคลียร์
    public bool IsSafeRoom => isSafeRoom;
    public bool IsCleared => isCleared;
    public int AliveMonstersCount => aliveMonstersCount;
    private bool hasStarted;
    private bool isCleared;
    private int aliveMonstersCount;
    private bool isSafeRoom; // ห้องของเหตุการณ์ (ร้านค้า) ไม่มีมอนสเตอร์ ประตูไม่ปิด
    private bool hadMonsters; // มีมอนสเตอร์ให้สู้จริง (ห้องว่างเคลียร์ทันทีแต่ไม่ได้กล่อง)

    // เหตุการณ์ที่ MapEventDirector เลือกให้ห้องนี้ รอจังหวะเสก
    private GameObject pendingEventPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) StartEncounter();
    }

    public void StartEncounter()
    {
        if (hasStarted || isCleared) return;
        hasStarted = true;

        int slots = monsterSpawnPoints != null ? monsterSpawnPoints.Length : 0;
        var wave = roomData != null ? roomData.BuildWave(slots) : null;
        // สุ่มลำดับจุดเกิด มอนสเตอร์จะได้ไม่มายืนที่เดิมทุกครั้งที่เข้าด่าน
        var points = ShuffledSpawnPoints();

        if (wave != null)
        {
            for (int i = 0; i < wave.Count; i++)
            {
                var data = wave[i];
                // Missing/animation-only prefabs cannot report deaths; do not soft-lock.
                if (data == null || data.monsterPrefab == null ||
                    data.monsterPrefab.GetComponent<MonsterController>() == null) continue;
                Transform point = points != null && points.Length > 0 ? points[i % points.Length] : transform;
                // Map unloading also removes its spawned enemies.
                var obj = Instantiate(data.monsterPrefab, point.position, Quaternion.identity, transform);
                var monster = obj.GetComponent<MonsterController>();
                monster.currentRoom = this;
                monster.myData = data;
                aliveMonstersCount++;
            }
        }
        hadMonsters = aliveMonstersCount > 0;
        if (hadMonsters) SetDoors(true);
        else ClearRoom();
    }

    public void OnMonsterDied()
    {
        if (!hasStarted || isCleared || aliveMonstersCount <= 0) return;
        aliveMonstersCount--;
        if (aliveMonstersCount == 0) ClearRoom();
    }

    // MapEventDirector เรียกตอนแมพโหลดเสร็จ เพื่อจองห้องนี้ให้เป็นห้องเหตุการณ์ของแมพ
    // safeRoom = ห้องนี้เป็นของเหตุการณ์อย่างเดียว (ร้านค้า): เสกของทันที ไม่มีมอนสเตอร์ ไม่ปิดประตู ไม่มีหีบ
    public void AssignEvent(GameObject prefab, bool afterCleared, bool safeRoom = false)
    {
        if (prefab == null) return;

        if (safeRoom && !hasStarted)
        {
            isSafeRoom = true;
            hasStarted = true;
            isCleared = true;
            SpawnEvent(prefab);
            Cleared?.Invoke();
            return;
        }

        if (!afterCleared || isCleared)
        {
            SpawnEvent(prefab);
            return;
        }
        pendingEventPrefab = prefab;
    }

    private Transform[] ShuffledSpawnPoints()
    {
        if (monsterSpawnPoints == null || monsterSpawnPoints.Length == 0) return null;

        var points = (Transform[])monsterSpawnPoints.Clone();
        for (int i = points.Length - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            (points[i], points[swap]) = (points[swap], points[i]);
        }
        return points;
    }

    private void SetDoors(bool closed)
    {
        if (doors == null) return;
        foreach (var door in doors)
        {
            if (door == null) continue;
            var animated = door.GetComponent<AnimatedRoomGate>();
            if (animated != null) animated.SetClosed(closed);
            else door.SetActive(closed); // Preserve legacy non-animated doors.
        }
    }

    private void ClearRoom()
    {
        if (isCleared) return;
        isCleared = true;
        SetDoors(false);
        Cleared?.Invoke();
        if (chestPrefab != null)
            Instantiate(chestPrefab, chestSpawnPoint != null ? chestSpawnPoint.position : transform.position,
                Quaternion.identity, transform);
        else if (hadMonsters)
        {
            // กล่องสมบัติตามแมพ (MapData.chestLoot) ไม่ต้องใส่ทีละห้อง
            var map = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
            if (map != null) TreasureChest.Spawn(map.chestLoot, this);
        }

        if (pendingEventPrefab != null)
        {
            SpawnEvent(pendingEventPrefab);
            pendingEventPrefab = null;
        }
    }

    private void SpawnEvent(GameObject prefab)
    {
        Transform anchor = eventAnchor != null ? eventAnchor : transform;
        // เป็นลูกของห้อง พอเปลี่ยนด่านแล้วแมพถูกทำลาย เหตุการณ์จะหายตามไปด้วย
        Instantiate(prefab, anchor.position, Quaternion.identity, transform);
    }
}
