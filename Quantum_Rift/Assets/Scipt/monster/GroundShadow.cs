using UnityEngine;

// เงาวงรีใต้เท้าตัวละคร (มอนสเตอร์ บอส ผู้เล่น) ให้ตัวดูยืนบนพื้น ไม่ลอย
// ขนาดตามความกว้างของ collider ตัวชน วางที่ขอบล่างของ collider (เท้า) อยู่ชั้นพื้น bg2 ใต้ตัวละครทุกตัว
public static class GroundShadow
{
    const string FloorLayer = "bg2";
    const int FloorOrder = 2;          // เหนือกับดักฝังพื้น (0) ใต้ผู้เล่น (5) มอนอยู่ชั้น object เหนือกว่าอยู่แล้ว
    const float WidthScale = 1.15f;
    const float Flatness = 0.36f;      // มองพื้นเฉียง เงาเลยแบน
    const float MinWidth = 0.45f;

    public static SpriteRenderer Attach(GameObject owner, float alpha = 0.3f)
    {
        Collider2D body = null;
        foreach (var col in owner.GetComponents<Collider2D>())
            if (!col.isTrigger) { body = col; break; }

        Vector2 size = new Vector2(0.6f, 0.4f), offset = Vector2.zero;
        switch (body)
        {
            case BoxCollider2D box: size = box.size; offset = box.offset; break;
            case CapsuleCollider2D capsule: size = capsule.size; offset = capsule.offset; break;
            case CircleCollider2D circle: size = Vector2.one * circle.radius * 2f; offset = circle.offset; break;
        }

        Vector3 lossy = owner.transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(lossy.x)), sy = Mathf.Max(0.01f, Mathf.Abs(lossy.y));
        float width = Mathf.Max(MinWidth, size.x * sx * WidthScale);

        var go = new GameObject("GroundShadow");
        go.transform.SetParent(owner.transform, false);
        go.transform.localPosition = new Vector3(offset.x, offset.y - size.y * 0.5f + 0.04f / sy, 0f);
        go.transform.localScale = new Vector3(width / sx, width * Flatness / sy, 1f);
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = ProceduralSprites.Disc;
        view.color = new Color(0f, 0f, 0f, alpha);
        view.sortingLayerName = FloorLayer;
        view.sortingOrder = FloorOrder;
        return view;
    }
}
