using System;
using UnityEngine;

// ตัวเล่นเอฟเฟกต์สั้น ๆ ที่ติดไปกับวัตถุเอฟเฟกต์เอง: step(k) ถูกเรียกทุกเฟรม k 0→1 ครบเวลาแล้วเรียก done
// ไม่ผูกกับ coroutine ของบอส บอสตาย (StopAllCoroutines) เอฟเฟกต์ที่ปล่อยไปแล้วก็ยังเล่นจนจบ
public sealed class EchoFxTween : MonoBehaviour
{
    Action<float> step;
    Action done;
    float duration, age;

    public static EchoFxTween Play(GameObject target, float duration, Action<float> step, Action done = null)
    {
        var tween = target.AddComponent<EchoFxTween>();
        tween.duration = Mathf.Max(0.01f, duration);
        tween.step = step;
        tween.done = done;
        step?.Invoke(0f);
        return tween;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);
        step?.Invoke(k);
        if (k < 1f) return;
        done?.Invoke();
        Destroy(this);
    }
}

// เอฟเฟกต์ประกอบของบอส Echo Commander สร้างจากภาพที่มีอยู่แล้ว (ภาพบอส/กระสุน) กับภาพที่สร้างในโค้ด
public static class EchoFx
{
    public static readonly Color Purple = new Color(0.72f, 0.4f, 1f);
    public static readonly Color PaleViolet = new Color(0.9f, 0.75f, 1f);

    public static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, int sortingLayerID, int order, Color color)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        var view = go.AddComponent<SpriteRenderer>();
        view.sprite = sprite;
        view.sortingLayerID = sortingLayerID;
        view.sortingOrder = order;
        view.color = color;
        return view;
    }

    // เงาภาพของตัวบอสที่ลอยแยกออกไป ขยายขึ้นแล้วจางหาย (ร่างเสียงสะท้อนแตกออกจากตัว)
    public static void DriftGhost(SpriteRenderer source, Vector2 velocity, float life, float alpha, Color tint, float grow = 0.35f)
    {
        if (source == null || source.sprite == null) return;
        var ghost = Layer(null, "EchoGhost", source.sprite, source.sortingLayerID, source.sortingOrder - 1, tint);
        ghost.flipX = source.flipX;
        var t = ghost.transform;
        t.position = source.transform.position;
        Vector3 scale = source.transform.lossyScale;
        Vector3 start = t.position;
        EchoFxTween.Play(ghost.gameObject, life, k =>
        {
            t.position = start + (Vector3)(velocity * k * life * (1f - k * 0.4f));
            t.localScale = scale * (1f + grow * k);
            ghost.color = new Color(tint.r, tint.g, tint.b, alpha * (1f - k) * (1f - k));
        }, () => UnityEngine.Object.Destroy(ghost.gameObject));
    }

    // วงคลื่นกระแทกบนพื้นแผ่ออก (เข้าช่วงคลั่ง / บอสตาย / โผล่จากประตูมิติ)
    public static void Shockwave(Vector2 at, Color color, float radius, float time)
    {
        var ring = Layer(null, "Shockwave", ProceduralSprites.ThinRing, SortingLayer.NameToID("bg2"), 2, color);
        var t = ring.transform;
        t.position = at;
        EchoFxTween.Play(ring.gameObject, time, k =>
        {
            float grow = 1f - (1f - k) * (1f - k);
            float d = Mathf.Lerp(0.3f, radius * 2f, grow);
            t.localScale = new Vector3(d, d * 0.55f, 1f); // วงบนพื้นมองเฉียงเป็นวงรี
            ring.color = new Color(color.r, color.g, color.b, 1f - k);
        }, () => UnityEngine.Object.Destroy(ring.gameObject));
    }

    // แสงฟุ้งวาบครั้งเดียว
    public static void Flash(Vector2 at, Color color, float size, float time)
    {
        var glow = Layer(null, "Flash", ProceduralSprites.Glow, SortingLayer.NameToID("Effect"), 6, color);
        var t = glow.transform;
        t.position = at;
        EchoFxTween.Play(glow.gameObject, time, k =>
        {
            t.localScale = Vector3.one * size * (0.7f + 0.6f * k);
            glow.color = new Color(color.r, color.g, color.b, 0.9f * (1f - k));
        }, () => UnityEngine.Object.Destroy(glow.gameObject));
    }

    // ดาวกระจายของบอส (ภาพกระสุนเดิม) หมุนแตกกระจายออกรอบตัวแล้วจางหาย เป็นแค่ภาพ ไม่ทำดาเมจ
    public static void Shards(Sprite sprite, Vector2 at, int count, float speed, float life)
    {
        if (sprite == null) return;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + UnityEngine.Random.Range(-12f, 12f);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            var shard = Layer(null, "Shard", sprite, SortingLayer.NameToID("Effect"), 5, Color.white);
            var t = shard.transform;
            t.position = at;
            float spin = UnityEngine.Random.Range(540f, 900f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            float s = UnityEngine.Random.Range(0.8f, 1.1f);
            EchoFxTween.Play(shard.gameObject, life, k =>
            {
                float travel = 1f - (1f - k) * (1f - k);
                t.position = at + direction * speed * life * travel;
                t.rotation = Quaternion.Euler(0f, 0f, spin * k * life);
                t.localScale = Vector3.one * s * (1f - 0.5f * k);
                shard.color = new Color(1f, 1f, 1f, k < 0.6f ? 1f : (1f - k) / 0.4f);
            }, () => UnityEngine.Object.Destroy(shard.gameObject));
        }
    }

    // มอนก้าวออกจากประตูมิติ: จางจากใสเป็นทึบ ขนาดเด้งจากเล็กแล้วกลับที่ (คืนค่าเดิมเป๊ะตอนจบ)
    public static void Emerge(GameObject monster, float time)
    {
        var body = monster.transform;
        Vector3 scale = body.localScale;
        var renderers = monster.GetComponentsInChildren<SpriteRenderer>(true);
        var alphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) alphas[i] = renderers[i].color.a;
        EchoFxTween.Play(monster, time, k =>
        {
            if (body == null) return;
            float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.1f, k / 0.7f) : Mathf.Lerp(1.1f, 1f, (k - 0.7f) / 0.3f);
            body.localScale = k >= 1f ? scale : scale * s;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color; // คงสีที่ตัวมอนตั้งเอง (โดนตีกะพริบแดง) ปรับแค่ความใส
                c.a = alphas[i] * k;
                renderers[i].color = c;
            }
        });
    }
}
