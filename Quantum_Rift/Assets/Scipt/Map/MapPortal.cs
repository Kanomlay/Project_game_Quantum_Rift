using UnityEngine;

public class MapPortal : MonoBehaviour
{
    // ประตูมิติวางไว้กลางห้องสุดท้ายของแต่ละผัง ห้องที่อยู่ในรัศมีนี้คือห้องที่เฝ้าประตู
    const float GuardRoomRadius = 3f;

    private RoomController guardRoom;
    private Renderer[] visuals;

    // ซ่อนประตูไว้จนกว่าจะเคลียร์มอนสเตอร์ในห้องที่ประตูอยู่ (ห้องบอสก็รอบอสตาย)
    // ทำใน Start เพราะตัวสุ่มผังปิดผังที่ไม่ใช้ไปแล้วตั้งแต่ Awake จะได้เจอเฉพาะห้องของผังที่ใช้จริง
    private void Start()
    {
        guardRoom = FindGuardRoom();
        if (guardRoom == null || guardRoom.IsCleared) return;

        visuals = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
        guardRoom.Cleared += Reveal;
    }

    private void OnDestroy()
    {
        if (guardRoom != null) guardRoom.Cleared -= Reveal;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ยังไม่เคลียร์ห้อง = ยังวาปไม่ได้ (กันกรณีภาพซ่อนไม่หมด หรือผู้เล่นวิ่งผ่านจุดประตูระหว่างสู้)
        if (guardRoom != null && !guardRoom.IsCleared) return;

        // ถ้าคนที่มาชนคือฮีโร่
        if (collision.CompareTag("Player"))
        {
            // สั่งให้ผู้จัดการเปลี่ยนด่าน!
            MapManager.instance.GoToNextMap();
        }
    }

    private void Reveal()
    {
        guardRoom.Cleared -= Reveal;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (visuals == null) return;
        foreach (var visual in visuals)
            if (visual != null) visual.enabled = visible;
    }

    // ไล่หาห้องจากพ่อแม่ขึ้นไปทีละชั้น เจอชั้นแรกที่มีห้องอยู่ใกล้ประตูก็ใช้ห้องนั้น
    // ไม่ค้นทั้งฉาก เพราะตอนเปลี่ยนด่านแมพเก่ายังไม่ถูกลบในเฟรมนั้น และแมพทุกด่านวางซ้อนที่จุดเดียวกัน
    private RoomController FindGuardRoom()
    {
        for (var parent = transform.parent; parent != null; parent = parent.parent)
        {
            RoomController nearest = null;
            float best = GuardRoomRadius;
            foreach (var room in parent.GetComponentsInChildren<RoomController>(false))
            {
                float distance = Vector2.Distance(room.transform.position, transform.position);
                if (distance < best) { best = distance; nearest = room; }
            }
            if (nearest != null) return nearest;
        }
        return null;
    }
}
