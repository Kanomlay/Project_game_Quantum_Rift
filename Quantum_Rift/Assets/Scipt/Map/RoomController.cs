using System.Collections;
using System.Collections.Generic;
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
    public bool HasBeenVisited { get; private set; }
    public void MarkVisited() { HasBeenVisited = true; }
    public event System.Action Cleared; // ประตูมิติในห้องรอฟังเพื่อโผล่ตอนเคลียร์
    // ห้องที่มีมอนสเตอร์ให้สู้: เริ่มสู้ / เคลียร์แล้ว (พรที่นับต่อห้องฟังอยู่ ดู BlessingManager)
    public static event System.Action<RoomController> CombatStarted;
    public static event System.Action<RoomController> CombatCleared;
    public bool IsSafeRoom => isSafeRoom;
    public bool IsCleared => isCleared;
    public int AliveMonstersCount => aliveMonstersCount;
    private bool hasStarted;
    private bool isCleared;
    private int aliveMonstersCount;
    private bool isSafeRoom; // ห้องของเหตุการณ์ (ร้านค้า) ไม่มีมอนสเตอร์ ประตูไม่ปิด
    private bool hadMonsters; // มีมอนสเตอร์ให้สู้จริง (ห้องว่างเคลียร์ทันทีแต่ไม่ได้กล่อง)
    private int incomingMonsters; // วงเตือนขึ้นแล้ว ยังไม่โผล่
    private int wavesLeft;        // ระลอกที่ยังไม่เริ่ม

    // การเกิดมอนสเตอร์ (ใช้ทุกห้อง)
    private const float SpawnWarning = 0.8f;    // วงเตือนขึ้นก่อนมอนโผล่
    private const float SpawnStagger = 0.3f;    // มอนในระลอกเดียวกันขึ้นวงเหลื่อมกันไม่เกินเท่านี้
    private const float WakeUpDelay = 0.5f;     // โผล่แล้วยืนนิ่งก่อนเริ่มเดิน/โจมตี
    private const float MinPlayerDistance = 4f; // ไม่เกิดใกล้ผู้เล่นกว่านี้

    // เหตุการณ์ที่ MapEventDirector เลือกให้ห้องนี้ รอจังหวะเสก
    private GameObject pendingEventPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) { MarkVisited(); StartEncounter(); }
    }

    public void StartEncounter()
    {
        if (hasStarted || isCleared) return;
        hasStarted = true;

        var waves = PlayableWaves();
        hadMonsters = waves.Count > 0;
        if (!hadMonsters)
        {
            ClearRoom();
            return;
        }
        SetDoors(true);
        CombatStarted?.Invoke(this);
        StartCoroutine(RunWaves(waves));
    }

    // ตัดตัวที่เสกไม่ได้ออก (ไม่มี prefab หรือ prefab มีแค่ภาพ รายงานการตายไม่ได้) ไม่งั้นห้องค้างไม่เคลียร์
    private List<List<MonsterData>> PlayableWaves()
    {
        var waves = roomData != null ? roomData.BuildWaves() : new List<List<MonsterData>>();
        foreach (var wave in waves)
            wave.RemoveAll(data => data == null || data.monsterPrefab == null ||
                                   data.monsterPrefab.GetComponent<MonsterController>() == null);
        waves.RemoveAll(wave => wave.Count == 0);
        return waves;
    }

    // ระลอกแรกมาทันทีที่เข้าห้อง ระลอกถัดไปรอจนมอนในห้องเหลือไม่เกิน nextWaveWhenAlive ตัว
    // ประตูปิดไว้จนเคลียร์ระลอกสุดท้าย
    private IEnumerator RunWaves(List<List<MonsterData>> waves)
    {
        wavesLeft = waves.Count;
        for (int i = 0; i < waves.Count; i++)
        {
            if (i > 0)
            {
                while (aliveMonstersCount + incomingMonsters > roomData.nextWaveWhenAlive) yield return null;
                if (roomData.waveDelay > 0f) yield return new WaitForSeconds(roomData.waveDelay);
            }
            wavesLeft--;
            SpawnWave(waves[i]);
        }
    }

    // ทุกตัวขึ้นวงเตือนก่อนแล้วค่อยโผล่ จัดฉาก (ห้องบอส) เกิดตรงจุดที่วางไว้ แบบสุ่มกระจายทั่วห้องห่างผู้เล่น
    private void SpawnWave(List<MonsterData> wave)
    {
        var spots = roomData.IsStaged
            ? AuthoredSpots(wave.Count)
            : SpawnPlacement.Pick(this, wave.ConvertAll(data => data.monsterPrefab), PlayerPosition(), MinPlayerDistance);

        for (int i = 0; i < wave.Count; i++)
        {
            MonsterData data = wave[i];
            Vector2 spot = spots[i];
            var emphasis = EmphasisOf(data);
            incomingMonsters++;
            SpawnTelegraph.Begin(transform, spot, SpawnPlacement.Measure(data.monsterPrefab), emphasis,
                                 SpawnTelegraph.ColorFor(this, emphasis), SpawnWarning,
                                 i == 0 ? 0f : Random.Range(0f, SpawnStagger), () => SpawnMonster(data, spot));
        }
    }

    // วงเตือนครบเวลา: เสกมอนจริง เป็นลูกของห้อง (เปลี่ยนด่านแล้วหายไปพร้อมแมพ)
    private GameObject SpawnMonster(MonsterData data, Vector2 spot)
    {
        incomingMonsters--;
        if (isCleared) return null;
        var obj = Instantiate(data.monsterPrefab, spot, Quaternion.identity, transform);
        var monster = obj.GetComponent<MonsterController>();
        monster.currentRoom = this;
        monster.myData = data;
        monster.WakeUpAfter(WakeUpDelay);
        aliveMonstersCount++;
        return obj;
    }

    private SpawnTelegraph.Emphasis EmphasisOf(MonsterData data)
    {
        if (data.monsterPrefab.GetComponent<BossHealthHudLink>() != null) return SpawnTelegraph.Emphasis.Boss;
        return roomData.IsLeader(data) ? SpawnTelegraph.Emphasis.Leader : SpawnTelegraph.Emphasis.Normal;
    }

    // สุ่มลำดับจุดเกิดที่วางไว้ มอนสเตอร์จะได้ไม่มายืนที่เดิมทุกครั้งที่เข้าด่าน
    private List<Vector2> AuthoredSpots(int count)
    {
        var points = ShuffledSpawnPoints();
        var spots = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            Transform point = points != null && points.Length > 0 ? points[i % points.Length] : null;
            spots.Add(point != null ? point.position : transform.position);
        }
        return spots;
    }

    private Vector2 PlayerPosition()
    {
        var hero = GameObject.FindGameObjectWithTag("Player");
        return hero != null ? hero.transform.position : transform.position;
    }

    // เคลียร์เมื่อตัวสุดท้ายของระลอกสุดท้ายล้ม (ไม่มีวงเตือนค้างและไม่เหลือระลอกรอ)
    public void OnMonsterDied()
    {
        if (!hasStarted || isCleared || aliveMonstersCount <= 0) return;
        aliveMonstersCount--;
        if (aliveMonstersCount == 0 && incomingMonsters == 0 && wavesLeft == 0) ClearRoom();
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
        if (hadMonsters) CombatCleared?.Invoke(this);
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
