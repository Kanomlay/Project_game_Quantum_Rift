using System.Collections.Generic;
using UnityEngine;

// ประตูมิติของ Echo Commander: ตัดเฉพาะรูปประตูจากเฟรมท่าเรียกลูกน้อง (Summon_03–06) ในภาพบอสชุดเดิม ไม่ต้องมีไฟล์ภาพใหม่
// เปิด: รอยแยกบาง → เปิดกว้าง → แสงวาบ → ค้างไว้   ปิด: ย้อนกลับจนเหลือรอยแยกแล้วหายไป
// ลูกน้องบอสก้าวออกจากประตูนี้ และบอสใช้เดินทางตอนวาร์ปไปหาผู้เล่น
public sealed class EchoPortal : MonoBehaviour
{
    // กรอบรูปประตูในภาพ EchoCommander-Actions-28Frames-v2.png (พิกเซล จุดเริ่มมุมล่างซ้ายแบบ Unity) วัดจากภาพเต็ม 1659x948
    static readonly RectInt[] FrameRects =
    {
        new RectInt(838, 528, 37, 166),   // รอยแยก
        new RectInt(1064, 529, 75, 160),  // เปิด
        new RectInt(1291, 498, 101, 221), // แสงวาบ
        new RectInt(1540, 510, 86, 187),  // ค้าง
    };
    const int Slit = 0, Opened = 1, Flared = 2, Steady = 3;
    const float AtlasWidth = 1659f;
    const float PixelsPerUnit = 100f;          // เท่าภาพบอส: ขนาด 1 เท่าประตูในท่าเรียกของบอสพอดี
    public const float NaturalHeight = 1.87f;  // ความสูงเฟรมค้าง (187px) เป็นหน่วยในฉาก
    const float OpenTime = 0.3f;
    const float CloseTime = 0.24f;

    static readonly Dictionary<Texture2D, Sprite[]> cache = new Dictionary<Texture2D, Sprite[]>();

    public Vector2 Spot { get; private set; } // จุดที่มอนจะก้าวออกมา (ระบบหาที่เกิดเว้นระยะจากตรงนี้ด้วย)

    Sprite[] frames;
    SpriteRenderer view, glow;
    Color accent;
    float height, age, closeAt = float.MaxValue, closeAge, flareUntil, nextSpark;
    bool closing;

    // center = กลางประตู, spot = จุดเกิดของมอนที่จะออกมา, atlas = ภาพบอส (sprite.texture ของบอส)
    public static EchoPortal Open(Transform parent, Vector2 center, Vector2 spot, float height, Texture2D atlas,
                                  int sortingLayerID, int order, Color accent)
    {
        var go = new GameObject("EchoPortal");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var portal = go.AddComponent<EchoPortal>();
        portal.Spot = spot;
        portal.height = height;
        portal.accent = accent;
        portal.frames = FramesFrom(atlas);
        portal.glow = EchoFx.Layer(go.transform, "Glow", ProceduralSprites.Glow, sortingLayerID, order - 1, Color.clear);
        portal.view = EchoFx.Layer(go.transform, "Portal", null, sortingLayerID, order, Color.white);
        portal.Apply(Slit, 0.3f, 0f);
        return portal;
    }

    // ตัดรูปประตูจากภาพบอส (ภาพถูกย่อตอน import ก็ยังตัดตรง เพราะคิดตามสัดส่วน)
    public static Sprite[] FramesFrom(Texture2D atlas)
    {
        if (atlas == null) return null;
        if (cache.TryGetValue(atlas, out var sprites)) return sprites;
        float k = atlas.width / AtlasWidth;
        sprites = new Sprite[FrameRects.Length];
        for (int i = 0; i < FrameRects.Length; i++)
        {
            var r = FrameRects[i];
            var rect = new Rect(r.x * k, r.y * k, r.width * k, r.height * k);
            if (rect.xMax > atlas.width || rect.yMax > atlas.height) { sprites = null; break; }
            sprites[i] = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit * k, 0, SpriteMeshType.FullRect);
            sprites[i].name = "EchoPortal_" + i;
        }
        cache[atlas] = sprites;
        return sprites;
    }

    // แสงวาบตอนมีของผ่านประตู
    public void Flare(float time = 0.2f)
    {
        flareUntil = age + time;
        ImpactSparks.Spawn(transform.position, accent, 10, Vector2.zero, 4f);
    }

    public void Close(float delay = 0f)
    {
        if (closing) return;
        closeAt = Mathf.Min(closeAt, age + Mathf.Max(0f, delay));
    }

    void Update()
    {
        age += Time.deltaTime;
        if (!closing && age >= closeAt) closing = true;

        if (closing)
        {
            closeAge += Time.deltaTime;
            float k = Mathf.Clamp01(closeAge / CloseTime);
            int frame = k < 0.25f ? Flared : k < 0.55f ? Opened : Slit;
            Apply(frame, Mathf.Lerp(1f, 0.25f, k), 1f - k * k);
            if (k >= 1f) Destroy(gameObject);
            return;
        }

        if (age < OpenTime)
        {
            float k = age / OpenTime;
            int frame = k < 0.27f ? Slit : k < 0.55f ? Opened : Flared;
            Apply(frame, Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(k / 0.55f)), Mathf.Clamp01(k * 3f));
        }
        else
        {
            float pulse = 1f + 0.03f * Mathf.Sin(age * 9f);
            Apply(age < flareUntil ? Flared : Steady, pulse, 1f);
        }

        // ประกายหลุดจากขอบประตูลอยขึ้น
        if (age >= nextSpark)
        {
            nextSpark = age + 0.09f;
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector2 edge = (Vector2)transform.position + new Vector2(side * height * 0.22f, Random.Range(-0.4f, 0.4f) * height);
            ImpactSparks.Spawn(edge, accent, 1, Vector2.up, 2f, 25f);
        }
    }

    // width = ความกว้าง (1 = เต็ม) ใช้ตอนรอยแยกค่อย ๆ กาง/หุบ
    void Apply(int frame, float width, float alpha)
    {
        float s = height / NaturalHeight;
        if (frames != null)
        {
            view.sprite = frames[frame];
            view.transform.localScale = new Vector3(s * width, s, 1f);
        }
        else
        {
            // ไม่มีภาพบอสให้ตัด: ใช้แสงฟุ้งทรงรีแทน
            view.sprite = ProceduralSprites.Glow;
            view.color = accent;
            view.transform.localScale = new Vector3(height * 0.5f * width, height, 1f);
        }
        Color c = view.color;
        c.a = alpha;
        view.color = c;
        glow.transform.localScale = new Vector3(height * 0.9f * width, height * 1.35f, 1f);
        glow.color = new Color(accent.r, accent.g, accent.b, (frame == Flared ? 0.55f : 0.3f) * alpha);
    }
}
