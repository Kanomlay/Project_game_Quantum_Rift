using UnityEngine;

// เงาภาพค้าง (afterimage) ของเอฟเฟกต์ที่พุ่งเร็ว: ลอกภาพ ณ ตอนนั้นไว้ แล้วค่อย ๆ จางหายเอง
public sealed class SpriteGhost : MonoBehaviour
{
    private SpriteRenderer view;
    private float life, age, startAlpha;
    private Color tint = Color.white;

    // tint = สีของเงา (ไม่ใส่ = สีเดียวกับตัวจริง เช่น คลื่นที่ถูกย้อมสีไว้)
    public static void Spawn(SpriteRenderer source, float lifetime, float alpha, Color? tint = null)
    {
        if (source == null || source.sprite == null) return;
        var go = new GameObject("Ghost");
        // อ่านมุม/ขนาดจากเมทริกซ์จริง: rotation กับ lossyScale ของ Unity ไม่นับการพลิกของตัวแม่
        // (อาวุธตอนเล็งซ้าย WeaponHolder พลิกแกน Y) เงาจะหมุนผิดทางจากตัวจริง
        Matrix4x4 m = source.transform.localToWorldMatrix;
        Vector2 right = new Vector2(m.m00, m.m10), up = new Vector2(m.m01, m.m11);
        float flip = right.x * up.y - right.y * up.x < 0f ? -1f : 1f;
        go.transform.SetPositionAndRotation(source.transform.position,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg));
        go.transform.localScale = new Vector3(right.magnitude, up.magnitude * flip, 1f);

        var ghost = go.AddComponent<SpriteGhost>();
        ghost.view = go.AddComponent<SpriteRenderer>();
        ghost.view.sprite = source.sprite;
        ghost.view.flipX = source.flipX; // ตัวละครหันซ้าย/ขวาด้วย flipX ไม่ใช่ scale
        ghost.view.flipY = source.flipY;
        ghost.view.sortingLayerID = source.sortingLayerID;
        ghost.view.sortingOrder = source.sortingOrder - 1; // อยู่หลังตัวจริง
        ghost.startAlpha = alpha;
        ghost.life = Mathf.Max(0.01f, lifetime);
        ghost.tint = tint ?? source.color;
        ghost.view.color = new Color(ghost.tint.r, ghost.tint.g, ghost.tint.b, alpha);
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        view.color = new Color(tint.r, tint.g, tint.b, startAlpha * (1f - k));
        transform.localScale *= 1f - Time.deltaTime * 0.6f; // หดลงนิด ๆ ระหว่างจาง
    }
}
