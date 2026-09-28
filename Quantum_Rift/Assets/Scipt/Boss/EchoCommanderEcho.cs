using UnityEngine;

// ร่างเสียงสะท้อนของ Echo Commander (ช่วงคลั่ง): ร่างโปร่งสีม่วงของบอสโผล่ในห้อง เล่นท่ายิงพร้อมบอสแล้วสลายไป
// ใช้ภาพและ Animator ชุดเดียวกับบอส ไม่มี collider (ตีไม่ได้ ไม่ขวางทาง) กระสุนบอสเป็นคนยิงให้ตามตำแหน่งร่างนี้
public sealed class EchoCommanderEcho : MonoBehaviour
{
    public const float AppearTime = 0.45f;
    const float VanishTime = 0.35f;
    const float Opacity = 0.6f;
    static readonly Color Tint = new Color(0.62f, 0.45f, 1f);

    SpriteRenderer view, glow;
    Animator animator;
    Vector2 bodyOffset;
    float age, vanishAge;
    bool vanishing;

    public static EchoCommanderEcho Create(Transform parent, Vector2 feet, Vector2 bodyOffset, SpriteRenderer source,
                                           RuntimeAnimatorController controller, bool faceLeft)
    {
        var go = new GameObject("EchoCommanderEcho");
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        go.transform.localScale = source.transform.lossyScale;

        var echo = go.AddComponent<EchoCommanderEcho>();
        echo.bodyOffset = bodyOffset;
        echo.view = go.AddComponent<SpriteRenderer>();
        echo.view.sprite = source.sprite;
        echo.view.sortingLayerID = source.sortingLayerID;
        echo.view.sortingOrder = source.sortingOrder - 1;
        echo.view.flipX = faceLeft;
        echo.view.color = Color.clear;
        echo.animator = go.AddComponent<Animator>();
        echo.animator.runtimeAnimatorController = controller;
        echo.glow = EchoFx.Layer(go.transform, "Glow", ProceduralSprites.Glow, source.sortingLayerID, source.sortingOrder - 2, Color.clear);
        echo.glow.transform.localPosition = bodyOffset;

        Vector2 center = feet + bodyOffset;
        EchoFx.Flash(center, EchoFx.Purple, 2.2f, AppearTime);
        ImpactSparks.Spawn(center, EchoFx.Purple, 10, Vector2.zero, 3.5f);
        EchoFx.Shockwave(feet, EchoFx.Purple, 1.2f, 0.4f);
        return echo;
    }

    public Vector2 BodyCenter => (Vector2)transform.position + bodyOffset;

    public void Shoot(Vector2 target)
    {
        if (vanishing) return;
        view.flipX = target.x < transform.position.x;
        animator.Play("Shoot", 0, 0f);
    }

    public void Vanish()
    {
        if (vanishing) return;
        vanishing = true;
        EchoFx.DriftGhost(view, Vector2.up * 1.2f, 0.45f, 0.4f, Tint, 0.3f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float alpha;
        if (vanishing)
        {
            vanishAge += Time.deltaTime;
            float k = Mathf.Clamp01(vanishAge / VanishTime);
            alpha = Opacity * (1f - k);
            if (k >= 1f) { Destroy(gameObject); return; }
        }
        else
        {
            float k = Mathf.Clamp01(age / AppearTime);
            // โผล่: สั่นไหวเหมือนภาพสัญญาณรบกวนก่อนนิ่ง
            float flicker = k < 1f ? 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(age * 45f)) : 0.9f + 0.1f * Mathf.Sin(age * 6f);
            alpha = Opacity * k * flicker;
        }
        view.color = new Color(Tint.r, Tint.g, Tint.b, alpha);
        glow.transform.localScale = new Vector3(2f, 2.6f, 1f);
        glow.color = new Color(Tint.r, Tint.g, Tint.b, alpha * 0.45f);
    }
}
