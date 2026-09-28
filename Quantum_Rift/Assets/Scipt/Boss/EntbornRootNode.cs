using System.Collections;
using UnityEngine;

// รากดูดพลังของบอสป่า (เฟส 3): ผุดขึ้นในห้อง มีสายพลังสีเขียวโยงไปหาบอส บอสฟื้นเลือดตราบที่รากยังอยู่
// ผู้เล่นตีแตกได้ (ระบบเดียวกับกล่อง/กำแพงในห้อง IBreakable) แตกครบทุกต้นบอสสตัน
// ภาพใช้ชุดรากแทง 7 เฟรมของบอส: ผุด 0–3 ค้างเฟรม 3 (สูงสุด) แตกแล้วหด 4–6
public sealed class EntbornRootNode : MonoBehaviour, IBreakable
{
    static readonly Color Green = new Color(0.45f, 1f, 0.55f);
    static readonly Color Dirt = new Color(0.55f, 0.42f, 0.28f);
    const float RiseTime = 0.35f;

    public bool IsBroken { get; private set; }
    public bool IsReady => !IsBroken && age >= RiseTime; // ผุดเสร็จแล้วถึงเริ่มดูดพลัง

    AncientEntbornBoss boss;
    Sprite[] frames;
    SpriteRenderer view, glow, tether;
    BoxCollider2D body;
    float health, age, flash, nextSpark;

    public static EntbornRootNode Create(AncientEntbornBoss boss, Transform parent, Vector2 spot, Sprite[] frames, float scale, float health)
    {
        var go = new GameObject("EntbornRootNode");
        go.transform.SetParent(parent, true);
        go.transform.position = spot;
        go.transform.localScale = Vector3.one * scale;
        var node = go.AddComponent<EntbornRootNode>();
        node.boss = boss;
        node.frames = frames;
        node.health = health;

        node.view = go.AddComponent<SpriteRenderer>();
        node.view.sortingLayerName = "object";
        node.view.sprite = frames != null && frames.Length > 0 ? frames[0] : null;

        // ตัวชนที่โคนราก: ขวางทางเดิน และให้อาวุธตีโดน
        node.body = go.AddComponent<BoxCollider2D>();
        node.body.size = new Vector2(0.75f, 0.5f) / scale;
        node.body.offset = new Vector2(0f, 0.25f / scale);

        node.glow = Layer(go.transform, "Glow", ProceduralSprites.Glow, "bg2", 2);
        node.glow.transform.localScale = Vector3.one * 2.4f / scale;
        var line = new GameObject("Tether");
        line.transform.SetParent(parent, true);
        node.tether = line.AddComponent<SpriteRenderer>();
        node.tether.sprite = ProceduralSprites.Bar;
        node.tether.sortingLayerName = "bg2";
        node.tether.sortingOrder = 3;

        ImpactSparks.Spawn(spot, Dirt, 12, Vector2.up, 4f, 90f);
        return node;
    }

    static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, string layer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.sortingLayerName = layer;
        view.sortingOrder = order;
        return view;
    }

    void Update()
    {
        if (IsBroken) return;
        age += Time.deltaTime;
        if (frames != null && frames.Length >= 4)
            view.sprite = frames[Mathf.Min(3, Mathf.FloorToInt(age / RiseTime * 4f))];

        flash = Mathf.Max(0f, flash - Time.deltaTime * 6f);
        view.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.55f), flash);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        glow.color = new Color(Green.r, Green.g, Green.b, 0.18f + 0.14f * pulse);

        // สายพลังโยงไปหาบอส ไหลเป็นจังหวะ
        if (tether != null && boss != null)
        {
            Vector2 from = (Vector2)transform.position + Vector2.up * 0.6f;
            Vector2 to = boss.Core;
            Vector2 delta = to - from;
            tether.transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg));
            tether.transform.localScale = new Vector3(delta.magnitude, 0.16f + 0.06f * pulse, 1f);
            tether.color = new Color(Green.r, Green.g, Green.b, IsReady ? 0.35f + 0.3f * pulse : 0f);
            if (IsReady && Time.time >= nextSpark)
            {
                nextSpark = Time.time + 0.2f;
                ImpactSparks.Spawn(from + delta * Random.value, Green, 1, delta.normalized, 3f, 10f);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken || amount <= 0f) return;
        health -= amount;
        flash = 1f;
        DamageNumbers.Spawn((Vector2)transform.position + Vector2.up * 1.2f, amount, DamageNumbers.Kind.Enemy);
        ImpactSparks.Spawn((Vector2)transform.position + Vector2.up * 0.6f, Dirt, 5, Vector2.up, 3f, 120f);
        if (health <= 0f) Break();
    }

    void Break()
    {
        IsBroken = true;
        body.enabled = false;
        if (tether != null) Destroy(tether.gameObject);
        CameraFollow.Shake(0.1f, 0.15f);
        ImpactSparks.Spawn((Vector2)transform.position + Vector2.up * 0.6f, Green, 14, Vector2.zero, 4.5f);
        ImpactSparks.Spawn(transform.position, Dirt, 12, Vector2.up, 4f, 120f);
        if (boss != null) boss.OnRootNodeBroken(this);
        StartCoroutine(Wither());
    }

    // หดลงดินด้วยเฟรม 4–6 แล้วหายไป
    IEnumerator Wither()
    {
        view.color = Color.white;
        for (int i = 4; frames != null && i < frames.Length; i++)
        {
            view.sprite = frames[i];
            glow.color = new Color(Green.r, Green.g, Green.b, 0.2f * (frames.Length - i) / 3f);
            yield return new WaitForSeconds(0.1f);
        }
        Destroy(gameObject);
    }

    // บอสตาย/สตันจบ: รากที่เหลือหดหายโดยไม่นับว่าผู้เล่นตีแตก
    public void Withdraw()
    {
        if (IsBroken) return;
        IsBroken = true;
        body.enabled = false;
        if (tether != null) Destroy(tether.gameObject);
        StartCoroutine(Wither());
    }

    void OnDestroy()
    {
        if (tether != null) Destroy(tether.gameObject);
    }
}
