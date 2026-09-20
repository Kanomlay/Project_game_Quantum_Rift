using System.Collections.Generic;
using UnityEngine;

// เลือกเหตุการณ์พิเศษให้แมพ "หนึ่งอย่างต่อการเข้าหนึ่งครั้ง" แล้วจองห้องให้หนึ่งห้อง
//
// ตรรกะนี้อยู่ระดับแมพ ไม่ใช่ระดับห้อง เพราะห้องแต่ละห้องไม่รู้ว่าห้องอื่นได้เหตุการณ์ไปหรือยัง
// ถ้าปล่อยให้แต่ละห้องสุ่มเอง จะได้ร้านค้าหลายร้านในแมพเดียว
public static class MapEventDirector
{
    public static void PlaceEvent(GameObject mapInstance, MapData map)
    {
        if (mapInstance == null || map == null) return;

        var chosenEvent = PickEvent(map.possibleEvents);
        if (chosenEvent == null || chosenEvent.eventPrefab == null) return;

        // พอร์ทัลออกด่านมักวางไว้กลางห้องพอดี ถ้าเหตุการณ์ไปลงห้องนั้นจะทับกัน
        // เดินไปหาร้านค้าแล้วโดนดูดไปด่านต่อไปแทน จึงตัดห้องที่มีพอร์ทัลออกก่อน
        var portals = mapInstance.GetComponentsInChildren<MapPortal>(true);

        var rooms = new List<RoomController>();
        foreach (var room in mapInstance.GetComponentsInChildren<RoomController>(true))
            if (room.canHostEvent && !HasPortalInside(room, portals)) rooms.Add(room);

        if (rooms.Count == 0)
        {
            Debug.LogWarning($"แมพ {map.mapName} ไม่มีห้องว่างให้วางเหตุการณ์ {chosenEvent.eventName} " +
                             "(ต้องเปิด Can Host Event และห้องนั้นต้องไม่มีพอร์ทัลออกด่าน)");
            return;
        }

        var host = rooms[Random.Range(0, rooms.Count)];
        host.AssignEvent(chosenEvent.eventPrefab, chosenEvent.spawnAfterRoomCleared);

        // บอกไว้ใน Console ว่าเหตุการณ์ไปลงห้องไหน จะได้ไม่ต้องเดินหาทั้งแมพตอนทดสอบ
        Debug.Log($"เหตุการณ์ {chosenEvent.eventName} ของแมพ {map.mapName} ไปลงที่ห้อง {host.name} " +
                  $"(ตำแหน่ง {host.transform.position})" +
                  (chosenEvent.spawnAfterRoomCleared ? " จะโผล่หลังเคลียร์ห้องนั้น" : ""));
    }

    // ใช้ขอบเขตของ trigger ห้องเป็นตัววัด เพราะพอร์ทัลไม่ได้เป็นลูกของห้อง แต่วางทับพื้นที่ห้องอยู่
    private static bool HasPortalInside(RoomController room, MapPortal[] portals)
    {
        if (portals == null || portals.Length == 0) return false;

        var area = room.GetComponent<Collider2D>();
        if (area == null) return false;

        // ใช้ OverlapPoint ไม่ใช่ bounds.Contains เพราะ collider 2D หนา 0 ตามแกน z
        // ถ้าพอร์ทัลวางอยู่คนละ z นิดเดียวจะเช็คไม่เจอ
        foreach (var portal in portals)
            if (portal != null && area.OverlapPoint(portal.transform.position)) return true;

        return false;
    }

    // สุ่มแบบถ่วงน้ำหนัก เหตุการณ์ที่ weight มากกว่าจะออกบ่อยกว่า
    private static MapEventData PickEvent(MapEventData[] pool)
    {
        if (pool == null || pool.Length == 0) return null;

        float total = 0f;
        foreach (var option in pool)
            if (option != null) total += Mathf.Max(0f, option.weight);

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (var option in pool)
        {
            if (option == null) continue;
            roll -= Mathf.Max(0f, option.weight);
            if (roll <= 0f) return option;
        }
        return null;
    }
}
