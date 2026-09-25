using UnityEngine;

// ประกาย/ฝุ่นตอนตีโดน: จุดพิกเซลสี่เหลี่ยมเล็ก ๆ กระเด็นออก ชะลอ แล้วจางหาย (สร้างภาพเองในโค้ด ไม่ต้องมีไฟล์ภาพ)
public sealed class ImpactSparks : MonoBehaviour
{
    static Sprite pixel;

    Vector2 velocity;
    float life, age, size;
    SpriteRenderer view;
    Color color;

    // direction = ทิศหลักที่กระเด็น (เช่นทิศที่ฟัน) ถ้าเป็นศูนย์จะกระจายรอบทิศ
    public static void Spawn(Vector2 at, Color color, int count, Vector2 direction, float speed = 5f, float spread = 70f)
    {
        if (count <= 0) return;
        var sprite = Pixel;
        bool aimed = direction.sqrMagnitude > 0.0001f;
        float baseAngle = aimed ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = aimed ? baseAngle + Random.Range(-spread, spread) : Random.Range(0f, 360f);
            var go = new GameObject("Spark");
            go.transform.position = at;

            var spark = go.AddComponent<ImpactSparks>();
            spark.view = go.AddComponent<SpriteRenderer>();
            spark.view.sprite = sprite;
            spark.view.sortingLayerName = "Effect";
            spark.view.sortingOrder = 7;
            spark.color = color;
            spark.view.color = color;
            spark.size = Random.Range(0.06f, 0.13f);
            go.transform.localScale = Vector3.one * spark.size;
            spark.velocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad))
                           * speed * Random.Range(0.5f, 1.2f);
            spark.life = Random.Range(0.15f, 0.3f);
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= Mathf.Max(0f, 1f - 7f * Time.deltaTime);
        transform.localScale = Vector3.one * size * (1f - k * 0.6f);
        view.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
    }

    // สี่เหลี่ยมขาว 1 หน่วย ย้อมสีด้วย SpriteRenderer.color
    static Sprite Pixel
    {
        get
        {
            if (pixel != null) return pixel;
            var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            pixel = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return pixel;
        }
    }
}
