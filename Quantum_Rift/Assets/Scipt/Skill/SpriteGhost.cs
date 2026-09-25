using UnityEngine;

// เงาภาพค้าง (afterimage) ของเอฟเฟกต์ที่พุ่งเร็ว: ลอกภาพ ณ ตอนนั้นไว้ แล้วค่อย ๆ จางหายเอง
public sealed class SpriteGhost : MonoBehaviour
{
    private SpriteRenderer view;
    private float life, age, startAlpha;

    public static void Spawn(SpriteRenderer source, float lifetime, float alpha)
    {
        if (source == null || source.sprite == null) return;
        var go = new GameObject("Ghost");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;

        var ghost = go.AddComponent<SpriteGhost>();
        ghost.view = go.AddComponent<SpriteRenderer>();
        ghost.view.sprite = source.sprite;
        ghost.view.sortingLayerID = source.sortingLayerID;
        ghost.view.sortingOrder = source.sortingOrder - 1; // อยู่หลังตัวจริง
        ghost.startAlpha = alpha;
        ghost.life = Mathf.Max(0.01f, lifetime);
        ghost.view.color = new Color(1f, 1f, 1f, alpha);
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        view.color = new Color(1f, 1f, 1f, startAlpha * (1f - k));
        transform.localScale *= 1f - Time.deltaTime * 0.6f; // หดลงนิด ๆ ระหว่างจาง
    }
}
