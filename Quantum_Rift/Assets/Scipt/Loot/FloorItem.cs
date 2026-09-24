using UnityEngine;

// ของที่ตกอยู่บนพื้น (เหรียญ ขวดยา อาวุธ): กระเด็นออกจากกล่องเป็นวงโค้ง ตกถึงพื้นแล้วลอยขึ้นลงเบา ๆ
// ตัว root อยู่ที่พื้น ภาพเป็นลูก (Visual) ขยับขึ้นลงแยก เงาการเรียงลำดับจะได้ไม่กระโดดตามความสูง
public abstract class FloorItem : MonoBehaviour
{
    const float TossTime = 0.45f;
    const float TossHeight = 0.7f;
    const float BobHeight = 0.05f;
    const float BobSpeed = 3f;
    const int FloorOrder = -1; // อยู่ชั้น object เดียวกับตัวละคร แต่ลำดับต่ำกว่า = วางอยู่บนพื้นใต้เท้าเสมอ

    protected SpriteRenderer display;
    protected bool Landed { get; private set; } = true;
    Vector2 tossFrom, tossTo;
    float tossStart, bobPhase;

    // สร้างตัวของพร้อมภาพ ย่อภาพให้ด้านยาวสุดเท่ากับ size หน่วย
    protected static T Create<T>(string name, Sprite sprite, float size, Transform parent, Vector2 position) where T : FloorItem
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "object";
        renderer.sortingOrder = FloorOrder;
        if (sprite != null)
        {
            float longest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (longest > 0f) visual.transform.localScale = Vector3.one * (size / longest);
        }

        var item = root.AddComponent<T>();
        item.display = renderer;
        item.bobPhase = Random.value * Mathf.PI * 2f;
        return item;
    }

    // โยนจาก from ไปตกที่ to (ตำแหน่ง root เลื่อนตรง ๆ ส่วนภาพลอยเป็นวงโค้ง)
    public void Toss(Vector2 from, Vector2 to)
    {
        tossFrom = from;
        tossTo = to;
        tossStart = Time.time;
        transform.position = from;
        Landed = false;
    }

    protected virtual void Update()
    {
        if (display == null) return;
        if (!Landed)
        {
            float t = Mathf.Clamp01((Time.time - tossStart) / TossTime);
            transform.position = Vector2.Lerp(tossFrom, tossTo, t);
            display.transform.localPosition = new Vector3(0f, Mathf.Sin(t * Mathf.PI) * TossHeight, 0f);
            if (t >= 1f) Landed = true;
            return;
        }

        display.transform.localPosition = new Vector3(0f, BobHeight + Mathf.Sin(Time.time * BobSpeed + bobPhase) * BobHeight, 0f);
        if (PauseManager.isGamePaused) return;
        var player = Player;
        if (player != null && !player.isDead) OnLandedUpdate(player);
    }

    protected abstract void OnLandedUpdate(PlayerStats player);

    // ระยะจากของถึงผู้เล่น วัดที่เท้าขยับขึ้นนิดนึง (ของวางบนพื้น ตัวละคร pivot อยู่ที่เท้า)
    protected float DistanceTo(PlayerStats player) =>
        Vector2.Distance(transform.position, (Vector2)player.transform.position + Vector2.up * 0.2f);

    static PlayerStats cachedPlayer;
    protected static PlayerStats Player
    {
        get
        {
            if (cachedPlayer == null)
            {
                var hero = GameObject.FindGameObjectWithTag("Player");
                if (hero != null) cachedPlayer = hero.GetComponent<PlayerStats>();
            }
            return cachedPlayer;
        }
    }
}
