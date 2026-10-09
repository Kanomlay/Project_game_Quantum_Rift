using UnityEngine;

// ประตูมิติออกจากรอยแยกหลังชนะบอสตัวสุดท้าย: บ่อกลางห้องที่บอสสลายเข้าไปเปลี่ยนเป็นรอยแยกสีม่วงฟ้า
// ภาพทั้งหมดทำจากรูปทรงที่สร้างตอนเล่น (แผ่นแสงทับบ่อ วงแหวนหมุนสวนกัน แสงเรือง ประกายลอยขึ้น) ไม่มีไฟล์ภาพใหม่
// ตัวกด F ยังเป็น MapPortal เดิมที่ LivingBossArena ย้ายมาไว้ตรงนี้ อยู่นอกจอมีลิ่มที่ขอบจอชี้มาหา
public sealed class CoreRiftPortal : MonoBehaviour
{
    static readonly Color Violet = new Color(0.78f, 0.42f, 1f);
    static readonly Color Cyan = new Color(0.45f, 0.95f, 1f);
    static readonly Vector2 Pool = new Vector2(3.2f, 2.1f); // ขนาดบ่อบนจอ (กว้าง, สูง) มองเฉียงจึงเป็นวงรี

    SpriteRenderer core, pointer;
    Transform surface, glow, ringOuter, ringInner;
    float age, nextSpark;

    public static void Open(SpriteRenderer core)
    {
        if (core == null || core.GetComponent<CoreRiftPortal>() != null) return;
        var portal = core.gameObject.AddComponent<CoreRiftPortal>();
        portal.core = core;
        int layer = core.sortingLayerID, order = core.sortingOrder; // เหนือบ่อ ใต้ตัวละคร
        portal.surface = Piece("RiftSurface", ProceduralSprites.Disc, layer, order + 1);
        portal.glow = Piece("RiftGlow", ProceduralSprites.Glow, layer, order + 2);
        portal.ringOuter = Piece("RiftRingOuter", ProceduralSprites.DashedRing, layer, order + 3);
        portal.ringInner = Piece("RiftRingInner", ProceduralSprites.DashedRing, layer, order + 4);
        portal.pointer = EchoFx.Layer(null, "RiftPointer", ProceduralSprites.Sector(24f), SortingLayer.NameToID("Effect"), 60, Color.clear);
        Vector2 at = core.transform.position;
        EchoFx.Flash(at, Cyan, 8f, 0.6f);
        EchoFx.Shockwave(at, Cyan, 7f, 0.9f);
    }

    // แต่ละชิ้นมีตัวแม่ไว้บีบเป็นวงรีบนพื้น ส่วนตัวภาพข้างในหมุนได้อิสระ วงแหวนจึงดูหมุนอยู่บนพื้น ไม่ใช่เอียงทั้งวง
    static Transform Piece(string name, Sprite sprite, int layer, int order)
    {
        var pivot = new GameObject(name).transform;
        EchoFx.Layer(pivot, "View", sprite, layer, order, Color.clear);
        return pivot;
    }

    void LateUpdate()
    {
        if (core == null) return;
        age += Time.deltaTime;
        float open = Mathf.Clamp01(age / 0.8f); // ค่อย ๆ ฉีกเปิด
        float beat = 0.5f + 0.5f * Mathf.Sin(age * 3f);
        Vector2 at = core.transform.position;
        Color tone = Color.Lerp(Violet, Cyan, beat);

        core.color = Color.Lerp(Color.white, new Color(0.6f, 0.55f, 0.85f), open); // เนินรอบบ่อหม่นลง ให้รอยแยกเด่น
        Place(surface, at, Pool * open, 0f, new Color(tone.r * 0.55f, tone.g * 0.5f, tone.b, 0.8f * open));
        Place(glow, at, Pool * (1.9f + 0.2f * beat) * open, 0f, new Color(tone.r, tone.g, tone.b, (0.35f + 0.2f * beat) * open));
        Place(ringOuter, at, Pool * 1.15f * open, age * 40f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f * open));
        Place(ringInner, at, Pool * 0.7f * open, -age * 70f, new Color(Violet.r, Violet.g, Violet.b, 0.9f * open));

        if (age >= nextSpark)
        {
            nextSpark = age + 0.12f;
            Vector2 from = at + Vector2.Scale(Random.insideUnitCircle, Pool * 0.45f);
            ImpactSparks.Spawn(from, Random.value < 0.5f ? Violet : Cyan, 1, Vector2.up, 3f, 20f);
        }
        ArchitectCore.PointAt(pointer, at, beat, Cyan);
    }

    static void Place(Transform pivot, Vector2 at, Vector2 size, float spin, Color color)
    {
        if (pivot == null || pivot.childCount == 0) return;
        pivot.position = at;
        pivot.localScale = new Vector3(size.x, size.y, 1f);
        var view = pivot.GetChild(0);
        view.localRotation = Quaternion.Euler(0f, 0f, spin);
        view.GetComponent<SpriteRenderer>().color = color;
    }

    void OnDestroy()
    {
        foreach (var piece in new[] { surface, glow, ringOuter, ringInner })
            if (piece != null) Destroy(piece.gameObject);
        if (pointer != null) Destroy(pointer.gameObject);
    }
}
