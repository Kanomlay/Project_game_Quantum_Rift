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
    public bool HasStarted => hasStarted;
    public bool IsCleared => isCleared;
    public int AliveMonstersCount => aliveMonstersCount;
    private bool hasStarted;
    private bool isCleared;
    private int aliveMonstersCount;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) StartEncounter();
    }

    public void StartEncounter()
    {
        if (hasStarted || isCleared) return;
        hasStarted = true;
        var entries = roomData != null ? roomData.monstersToSpawn : null;
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var data = entries[i];
                // Missing/animation-only prefabs cannot report deaths; do not soft-lock.
                if (data == null || data.monsterPrefab == null ||
                    data.monsterPrefab.GetComponent<MonsterController>() == null) continue;
                Transform point = monsterSpawnPoints != null && i < monsterSpawnPoints.Length &&
                    monsterSpawnPoints[i] != null ? monsterSpawnPoints[i] : transform;
                // Map unloading also removes its spawned enemies.
                var obj = Instantiate(data.monsterPrefab, point.position, Quaternion.identity, transform);
                var monster = obj.GetComponent<MonsterController>();
                monster.currentRoom = this;
                monster.myData = data;
                aliveMonstersCount++;
            }
        }
        if (aliveMonstersCount > 0) SetDoors(true);
        else ClearRoom();
    }

    public void OnMonsterDied()
    {
        if (!hasStarted || isCleared || aliveMonstersCount <= 0) return;
        aliveMonstersCount--;
        if (aliveMonstersCount == 0) ClearRoom();
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
        if (chestPrefab != null)
            Instantiate(chestPrefab, chestSpawnPoint != null ? chestSpawnPoint.position : transform.position,
                Quaternion.identity, transform);
    }
}
