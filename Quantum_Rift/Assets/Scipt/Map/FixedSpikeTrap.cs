using UnityEngine;

/// <summary>หนามประจำจุด เตือนก่อนพุ่งขึ้นทุก 10 วินาที และทำความเสียหายหนึ่งครั้งต่อรอบ</summary>
public sealed class FixedSpikeTrap : MonoBehaviour
{
    public enum Phase { Retracted, Warning, Raised }
    public float cycleSeconds = 10f;
    public float warningSeconds = 1.2f;
    public float raisedSeconds = 1.4f;
    public float damage = 1f;
    public Transform spikes;
    public Renderer warningDisplay;
    public BoxCollider2D damageArea;
    public RoomController room;
    public SpriteRenderer animatedDisplay;
    public Sprite[] animationFrames;
    public Phase CurrentPhase { get; private set; }
    public int CompletedCycles { get; private set; }
    float elapsed;
    int hitCycle = -1;
    PlayerStats player;
    // ผู้เล่นอยู่ชั้น bg2 ลำดับ 5 (มอนสเตอร์/กล่องอยู่ชั้น object ซึ่งวาดทับ bg2 ทั้งชั้น)
    // ถ้าวางกับดักไว้ชั้น object จะทับตัวผู้เล่นตลอดแม้หนามหุบอยู่ จึงบังคับให้อยู่ชั้นเดียวกับผู้เล่น
    const string FloorLayer = "bg2";
    const int FlushOrder = 0;  // ช่องกับดักฝังพื้น: ใต้เท้าผู้เล่น (ชั้นเดียวกับประตูมิติออกด่าน/วงเตือนเกิดมอน)
    const int RaisedOrder = 6; // หนามพุ่งขึ้น ผู้เล่นอยู่หลังช่อง (เหนือช่องบนจอ): หนามบังเท้าผู้เล่น แต่ยังอยู่ใต้กำแพงและมอนสเตอร์
    const int BehindOrder = 4; // หนามพุ่งขึ้น ผู้เล่นยืนบนช่อง/หน้าช่อง: ตัวผู้เล่น (ลำดับ 5) ทับหนาม ไม่ถูกหนามบังทั้งตัว
    static int floorLayerId = -1;
    // โดนหนาม: กระเด็นออกจากช่อง สั่นจอเบา ๆ ประกายที่เท้า (ยาน = สะเก็ดเหล็กส้ม / ป่า = เศษหนามเขียว) หนามช่องที่แทงโดนเด้งขึ้น
    const float HitKnockback = 3.5f;   // เบากว่าโดนมอนตี
    const float PunchTime = 0.18f;
    static readonly Color MetalSparks = new Color(1f, 0.62f, 0.22f);
    static readonly Color ThornSparks = new Color(0.55f, 0.95f, 0.35f);
    float punch;
    Transform punchTarget;
    Vector3 punchScale;

    void OnEnable()
    {
        // Prefab ลากวางใหม่จะใช้ห้องที่ครอบอยู่ ไม่อ้างกลับไปยังห้องต้นฉบับ
        if (room == null) room = GetComponentInParent<RoomController>();
        elapsed = 0f; hitCycle = -1; Advance(0f);
    }
    void Update()
    {
        if (PauseManager.isGamePaused || ShopWindow.IsOpen || BlessingManager.IsChoosing || Time.timeScale <= 0f) return;
        if (player == null)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero != null) player = hero.GetComponent<PlayerStats>();
        }
        if (player != null && player.isDead) return;
        Advance(Time.deltaTime);
        Punch(Time.deltaTime);
        if (CurrentPhase == Phase.Raised && player != null && damageArea != null &&
            damageArea.OverlapPoint(player.transform.position)) TryHit(player);
    }

    // เริ่มรอบใหม่ แล้วเลื่อนจังหวะไปข้างหน้า offset วินาที (ตัวสุ่มกับดักใช้ให้แต่ละจุดพุ่งไม่พร้อมกัน)
    public void Restart(float offset = 0f)
    {
        elapsed = 0f;
        hitCycle = -1;
        Advance(offset);
    }

    public void Advance(float seconds)
    {
        if (room != null && room.IsSafeRoom)
        {
            CurrentPhase = Phase.Retracted;
            if (spikes != null) spikes.gameObject.SetActive(false);
            if (warningDisplay != null) warningDisplay.enabled = false;
            SetFrame(0);
            return;
        }
        elapsed += Mathf.Max(0f, seconds);
        float period = Mathf.Max(3f, cycleSeconds);
        float time = elapsed % period;
        CompletedCycles = Mathf.FloorToInt(elapsed / period);
        // ครั้งแรกพุ่งที่วินาที 10 จากนั้น 20, 30... ช่วงก่อนนั้นเห็นฐานกับไฟเตือน
        bool raised = CompletedCycles > 0 && time < raisedSeconds;
        bool warning = !raised && time >= period - warningSeconds;
        CurrentPhase = raised ? Phase.Raised : warning ? Phase.Warning : Phase.Retracted;
        int frame = 0;
        if (warning) frame = Mathf.FloorToInt(elapsed * 6f) % 2 == 0 ? 1 : 0;
        else if (raised)
        {
            float progress = time / Mathf.Max(.1f, raisedSeconds);
            frame = progress < .12f ? 2 : progress < .24f ? 3 : progress < .76f ? 4 : progress < .89f ? 5 : 6;
        }
        SetFrame(frame);
        if (warningDisplay != null) warningDisplay.enabled = warning && Mathf.FloorToInt(elapsed * 8f) % 2 == 0;
        if (spikes != null)
        {
            spikes.gameObject.SetActive(raised);
            float height = Mathf.Min(Mathf.Clamp01(time / .12f), Mathf.Clamp01((raisedSeconds-time) / .18f));
            spikes.localScale = new Vector3(1f, Mathf.Max(.05f,height), 1f);
        }
    }

    void SetFrame(int index)
    {
        if (animatedDisplay == null || animationFrames == null || animationFrames.Length == 0) return;
        animatedDisplay.sprite = animationFrames[Mathf.Clamp(index, 0, animationFrames.Length - 1)];
        // ช่องกับดักฝังเรียบใต้เท้าตัวละคร มีเฉพาะหนามที่พุ่งขึ้นมาบังด้านหน้า
        if (floorLayerId < 0) floorLayerId = SortingLayer.NameToID(FloorLayer);
        animatedDisplay.sortingLayerID = floorLayerId;
        animatedDisplay.sortingOrder = index >= 2 && index <= 5 ? (PlayerBehind() ? RaisedOrder : BehindOrder) : FlushOrder;
    }

    // เท้าผู้เล่นอยู่สูงกว่ากลางช่อง = ยืนอยู่หลังกับดัก หนามควรวาดทับ
    bool PlayerBehind()
    {
        if (player == null) return false;
        float center = damageArea != null ? damageArea.bounds.center.y : transform.position.y;
        return player.transform.position.y > center;
    }

    public bool TryHit(PlayerStats target)
    {
        if (target == null || target.isDead || CurrentPhase != Phase.Raised || hitCycle == CompletedCycles) return false;
        hitCycle = CompletedCycles;
        bool landed = target.CanTakeHit; // ติดอมตะอยู่ = ไม่กระเด็น ไม่มีเอฟเฟกต์
        target.TakeDamage(damage);
        if (landed) HitFeedback(target);
        return true;
    }

    void HitFeedback(PlayerStats target)
    {
        Vector2 feet = target.transform.position;
        Vector2 center = damageArea != null ? (Vector2)damageArea.bounds.center : (Vector2)transform.position;
        Vector2 away = feet - center;
        if (away.sqrMagnitude < 0.0004f) away = Random.insideUnitCircle; // ยืนกลางช่องพอดี สุ่มทางกระเด็น
        var movement = target.GetComponent<PlayerMovement>();
        if (movement != null) movement.TakeKnockback(feet - away.normalized, HitKnockback);

        CameraFollow.Shake(0.1f, 0.15f);
        var features = GetComponentInParent<MapGameplayFeatures>();
        bool forest = features != null && features.theme == MapGameplayFeatures.Theme.LivingForest;
        Color color = forest ? ThornSparks : MetalSparks;
        ImpactSparks.Spawn(feet, color, 10, Vector2.up, 4.5f, 80f);
        ImpactSparks.Spawn(feet, Color.white, 4, Vector2.up, 3f, 60f);

        punchTarget = animatedDisplay != null ? animatedDisplay.transform : spikes;
        if (punchTarget != null)
        {
            if (punch <= 0f) punchScale = punchTarget.localScale;
            punch = 1f;
        }
    }

    // หนามช่องที่แทงโดนยืดขึ้นแวบหนึ่งแล้วกลับขนาดเดิม
    void Punch(float dt)
    {
        if (punch <= 0f || punchTarget == null) return;
        punch = Mathf.Max(0f, punch - dt / PunchTime);
        float k = Mathf.Sin(punch * Mathf.PI) * 0.5f + punch * 0.5f;
        Vector3 scale = punchTarget == spikes ? punchTarget.localScale : punchScale;
        punchTarget.localScale = new Vector3(scale.x * (1f + 0.1f * k), scale.y * (1f + 0.22f * k), scale.z);
        if (punch <= 0f && punchTarget != spikes) punchTarget.localScale = punchScale;
    }
}
