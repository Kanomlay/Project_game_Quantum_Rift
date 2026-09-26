using TMPro;
using UnityEngine;

// ประตูมิติออกด่าน: เคลียร์ห้องแล้วประตูโผล่ เดินไปยืนบนประตูแล้วกด F เพื่อไปด่านถัดไป (ไม่วาปเองตอนเดินผ่าน)
public class MapPortal : MonoBehaviour
{
    // ประตูมิติวางไว้กลางห้องสุดท้ายของแต่ละผัง ห้องที่อยู่ในรัศมีนี้คือห้องที่เฝ้าประตู
    const float GuardRoomRadius = 3f;

    [Header("กดปุ่มที่ประตูเพื่อไปด่านถัดไป")]
    public KeyCode useKey = KeyCode.F;
    public float useRange = 2f;       // ระยะจากเท้าผู้เล่นถึงกลางประตู (กลาง collider)
    public float promptHeight = 2.8f; // ป้ายลอยเหนือฐานประตู (หน่วยในฉาก)

    private RoomController guardRoom;
    private Renderer[] visuals;
    private Collider2D area;
    private TextMeshPro prompt;
    private Transform player;
    private PlayerStats stats;
    private bool used;

    // ซ่อนประตูไว้จนกว่าจะเคลียร์มอนสเตอร์ในห้องที่ประตูอยู่ (ห้องบอสก็รอบอสตาย)
    // ทำใน Start เพราะตัวสุ่มผังปิดผังที่ไม่ใช้ไปแล้วตั้งแต่ Awake จะได้เจอเฉพาะห้องของผังที่ใช้จริง
    private void Start()
    {
        area = GetComponent<Collider2D>();
        guardRoom = FindGuardRoom();
        if (guardRoom != null && !guardRoom.IsCleared)
        {
            visuals = GetComponentsInChildren<Renderer>(true);
            SetVisible(false);
            guardRoom.Cleared += Reveal;
        }
        BuildPrompt(); // สร้างทีหลัง ป้ายไม่ต้องซ่อน/โชว์ตามภาพประตู (ขึ้นเองเมื่อเดินมาใกล้)
    }

    private void OnDestroy()
    {
        if (guardRoom != null) guardRoom.Cleared -= Reveal;
    }

    private void Update()
    {
        // ยังไม่เคลียร์ห้อง = ยังไปต่อไม่ได้ (กันกรณีภาพซ่อนไม่หมด หรือผู้เล่นวิ่งผ่านจุดประตูระหว่างสู้)
        bool ready = !used && (guardRoom == null || guardRoom.IsCleared) &&
                     !PauseManager.isGamePaused && !ShopWindow.IsOpen && !BlessingManager.IsChoosing;
        bool near = ready && PlayerNear();
        if (prompt != null)
        {
            if (prompt.gameObject.activeSelf != near)
            {
                prompt.gameObject.SetActive(near);
                if (near) prompt.text = LanguageSettings.IsThai ? $"[{useKey}] ไปด่านถัดไป" : $"[{useKey}] Next stage";
            }
            if (near) prompt.transform.localPosition = PromptLocal(0.06f * Mathf.Sin(Time.time * 4f));
        }

        // อาวุธบนพื้นใกล้ ๆ ใช้ปุ่มเดียวกัน ให้เก็บอาวุธก่อน
        if (!near || !Input.GetKeyDown(useKey) || WeaponPickup.AnyInReach) return;
        used = true;
        if (prompt != null) prompt.gameObject.SetActive(false);
        MapManager.instance.GoToNextMap();
    }

    private bool PlayerNear()
    {
        if (player == null)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return false;
            player = hero.transform;
            stats = hero.GetComponent<PlayerStats>();
        }
        if (stats != null && stats.isDead) return false;
        Vector2 center = area != null ? (Vector2)area.bounds.center : (Vector2)transform.position;
        return Vector2.Distance(player.position, center) <= useRange;
    }

    // ป้าย "[F] ไปด่านถัดไป" ลอยเหนือประตู แบบเดียวกับป้ายร้านค้า/อาวุธบนพื้น (ขึ้นเมื่อเดินมาใกล้)
    private void BuildPrompt()
    {
        var obj = new GameObject("Prompt");
        obj.transform.SetParent(transform, false);
        float scale = Mathf.Abs(transform.lossyScale.x) > 0.0001f ? 1f / Mathf.Abs(transform.lossyScale.x) : 1f;
        obj.transform.localScale = Vector3.one * scale; // ประตูถูกขยายไว้ ป้ายต้องขนาดเท่าป้ายอื่น
        prompt = obj.AddComponent<TextMeshPro>();
        prompt.fontSize = 2.6f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = new Color(0.55f, 0.95f, 1f);
        prompt.outlineWidth = 0.2f;
        prompt.outlineColor = new Color32(20, 12, 36, 255);
        prompt.rectTransform.sizeDelta = new Vector2(6f, 1f);
        obj.GetComponent<MeshRenderer>().sortingLayerName = "Effect";
        obj.transform.localPosition = PromptLocal(0f);
        obj.SetActive(false);
    }

    private Vector3 PromptLocal(float bob)
    {
        float height = Mathf.Abs(transform.lossyScale.y) > 0.0001f ? Mathf.Abs(transform.lossyScale.y) : 1f;
        return new Vector3(0f, (promptHeight + bob) / height, 0f);
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
