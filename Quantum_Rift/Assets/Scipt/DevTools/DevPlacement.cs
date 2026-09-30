using System;
using UnityEngine;
using UnityEngine.EventSystems;

// โหมดคลิกวางของคอนโซลทดสอบ: ภาพจาง ๆ ของสิ่งที่จะเสกตามเมาส์ในฉาก พร้อมวงเส้นประที่พื้น (ตรงปลายเมาส์ = ปลายเท้า)
// เขียว = วางได้ แดง = วางไม่ได้ (ทับกำแพง/ของแข็ง หรือไม่ใช่พื้น) คลิกซ้ายวาง วางซ้ำได้เรื่อย ๆ จนกว่าจะเลิก
// เมาส์อยู่บนแผงคอนโซล = ซ่อนเงา ไม่วาง
public sealed class DevPlacement
{
    public sealed class Spec
    {
        public string title;
        public string details;
        public Sprite sprite;                    // ภาพในการ์ดตัวอย่างและเงาตามเมาส์
        public Vector2 spriteScale = Vector2.one;
        public Vector2 spriteOffset;             // ภาพเทียบกับจุดวาง
        public SpawnPlacement.Footprint body;    // พื้นที่ที่ต้องว่าง เทียบกับจุดวาง
        public Func<RoomController, Vector2, string> place; // วางจริงที่จุดวาง คืนข้อความผล
    }

    static readonly Color GoodTint = new Color(1f, 1f, 1f, 0.6f);
    static readonly Color BadTint = new Color(1f, 0.35f, 0.35f, 0.5f);
    static readonly Color GoodRing = new Color(0.45f, 1f, 0.6f, 0.9f);
    static readonly Color BadRing = new Color(1f, 0.3f, 0.3f, 0.9f);
    const float Flatness = 0.42f;  // พื้นมองเฉียง วงเป็นวงรีเท่าเงาใต้เท้า

    readonly Transform owner;
    GameObject ghost;
    SpriteRenderer image, ring;

    public Spec Current { get; private set; }
    public bool Active => Current != null;

    public DevPlacement(Transform owner)
    {
        this.owner = owner;
    }

    public void Begin(Spec spec)
    {
        if (ghost == null) Build();
        Current = spec;
        image.sprite = spec.sprite;
        image.transform.localScale = new Vector3(spec.spriteScale.x, spec.spriteScale.y, 1f);
        image.transform.localPosition = spec.spriteOffset;
        float width = Mathf.Max(0.6f, spec.body.Width * 1.3f);
        ring.transform.localScale = new Vector3(width, width * Flatness, 1f);
        ghost.SetActive(false); // โผล่ตอนเมาส์ออกจากแผงคอนโซล
    }

    public void Cancel()
    {
        Current = null;
        if (ghost != null) ghost.SetActive(false);
    }

    // ทุกเฟรม: ย้ายเงาตามเมาส์ คืน true เมื่อคลิกซ้ายในฉาก (problem = null แปลว่าวางได้)
    public bool Tick(Func<Vector2, RoomController> roomAt, out RoomController room, out Vector2 spot, out string problem)
    {
        room = null;
        spot = default;
        problem = null;
        var cam = Camera.main;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Current == null || cam == null || overUi)
        {
            if (ghost != null) ghost.SetActive(false);
            return false;
        }

        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;
        Vector2 feet = cam.ScreenToWorldPoint(mouse);
        spot = feet - Current.body.Feet;
        room = roomAt(feet);
        if (room == null) problem = "แมพนี้ไม่มีห้องให้วาง";
        else if (!SpawnPlacement.FitsAt(room, Current.body, spot)) problem = "วางตรงนี้ไม่ได้ (ทับกำแพง/ของ หรือไม่ใช่พื้น)";

        bool ok = problem == null;
        ghost.SetActive(true);
        ghost.transform.position = spot;
        ring.transform.position = feet;
        image.color = ok ? GoodTint : BadTint;
        ring.color = ok ? GoodRing : BadRing;
        ring.sprite = ok ? ProceduralSprites.DashedRing : ProceduralSprites.ThinRing;
        return Input.GetMouseButtonDown(0);
    }

    void Build()
    {
        ghost = new GameObject("DevPlacementGhost");
        ghost.transform.SetParent(owner, false);
        image = EchoFx.Layer(ghost.transform, "Image", null, SortingLayer.NameToID("Effect"), 900, GoodTint);
        ring = EchoFx.Layer(ghost.transform, "Ring", ProceduralSprites.DashedRing, SortingLayer.NameToID("bg2"), 6, GoodRing);
        ghost.SetActive(false);
    }
}
