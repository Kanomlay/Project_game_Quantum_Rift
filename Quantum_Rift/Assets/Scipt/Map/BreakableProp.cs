using UnityEngine;

/// <summary>กล่องเสบียงประจำจุด ทำลายด้วยอาวุธแล้วปล่อยเม็ดพลังงานสีฟ้าลอยเข้าหาผู้เล่น (ไม่ดรอปขวดยาแล้ว)</summary>
public sealed class BreakableProp : MonoBehaviour
{
    [SerializeField] SpriteRenderer display;
    [SerializeField] Collider2D[] blockers;
    [SerializeField] float maxHealth = 12f;
    [SerializeField] int minMana = 2, maxMana = 3; // จำนวนเม็ดพลังงาน เม็ดละ 1 หน่วย
    float health;
    bool broken;
    bool initialized;
    public bool IsBroken => broken;

    void Awake() { Initialize(); }
    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (display == null) display = GetComponent<SpriteRenderer>();
        health = maxHealth;
    }

    // healthDrop / energyDrop / mapRoot ไม่ใช้แล้ว (เลิกดรอปขวดยา) คงไว้ให้เครื่องมือจัดแมพเดิมเรียกได้
    public void Configure(SpriteRenderer renderer, Collider2D[] solidColliders,
        Sprite healthDrop, Sprite energyDrop, Transform mapRoot, float hitPoints = 12f)
    {
        display = renderer;
        blockers = solidColliders;
        maxHealth = hitPoints;
        health = hitPoints;
        var hitbox = GetComponent<CircleCollider2D>();
        if (hitbox == null) hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.isTrigger = true;
        float width = renderer != null && renderer.sprite != null ?
            renderer.sprite.bounds.size.x * Mathf.Abs(renderer.transform.lossyScale.x) : 1f;
        hitbox.radius = Mathf.Clamp(width * .42f, .35f, .85f) /
            Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
    }

    public void TakeDamage(float amount)
    {
        Initialize();
        if (broken || amount <= 0f || display == null || !display.enabled) return;
        health -= amount;
        if (health > 0f)
        {
            ImpactSparks.Spawn(transform.position, new Color(.7f,.85f,.9f), 3, Vector2.up);
            return;
        }
        broken = true;
        Vector2 center = display.bounds.center; // เก็บก่อนปิดภาพ
        ImpactSparks.Spawn(transform.position, new Color(.75f,.9f,1f), 7, Vector2.up);
        foreach (var visual in GetComponentsInChildren<Renderer>()) visual.enabled = false;
        if (blockers != null)
            foreach (var blocker in blockers) if (blocker != null) blocker.enabled = false;
        foreach (var hitbox in GetComponents<CircleCollider2D>()) hitbox.enabled = false;

        ManaMotes.Spawn(center, Random.Range(minMana, maxMana + 1));
    }
}
