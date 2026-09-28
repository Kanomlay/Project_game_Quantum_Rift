using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ตัวเลขดาเมจเด้งเหนือหัว: มอนสเตอร์/บอสโดนตี = ขาวไล่ฟ้า (ตีแรง = เหลืองไล่ส้ม ตัวใหญ่ สั่น) ผู้เล่นโดนตี = แดง
// ตัวเลขกระโดดออกเป็นโค้งไปทางที่มอนกระเด็น แล้วตกลงนิดหนึ่งก่อนจาง มีขอบเข้มและเงาตกกระทบให้อ่านง่ายบนพื้นทุกสี
// เรียก DamageNumbers.Spawn จากจุดที่หักเลือด ไม่ต้องตั้งอะไรในฉาก (ตัวเลขสร้างเองแล้วเก็บไว้ใช้ซ้ำ)
public sealed class DamageNumbers : MonoBehaviour
{
    public enum Kind { Enemy, Player, Heal }

    const float Life = 0.85f;
    const float PopTime = 0.14f;
    const float Gravity = 5.5f;
    const float BigHit = 15f;  // ดาเมจเท่านี้ขึ้นไปนับเป็นตีแรง
    static readonly VertexGradient EnemyGradient = Gradient(new Color(1f, 1f, 1f), new Color(0.7f, 0.86f, 1f));
    static readonly VertexGradient BigGradient = Gradient(new Color(1f, 0.95f, 0.45f), new Color(1f, 0.5f, 0.12f));
    static readonly VertexGradient HealGradient = Gradient(new Color(0.75f, 1f, 0.7f), new Color(0.2f, 0.85f, 0.4f));
    static readonly VertexGradient PlayerGradient = Gradient(new Color(1f, 0.55f, 0.5f), new Color(0.9f, 0.12f, 0.18f));
    static readonly Stack<DamageNumbers> pool = new Stack<DamageNumbers>();
    static Material style;

    TextMeshPro label;
    Vector3 position, velocity;
    float age, popScale, tilt;
    bool big;

    // จุดวางตัวเลข: ค่อนไปทางหัวของภาพ
    public static Vector3 Above(SpriteRenderer view, Vector3 fallback)
    {
        if (view == null || view.sprite == null) return fallback + Vector3.up;
        Bounds b = view.bounds;
        return new Vector3(b.center.x, Mathf.Lerp(b.center.y, b.max.y, 0.6f), 0f);
    }

    // side: ทางที่ตัวเลขเอียงไป (บวก = ขวา) 0 = สุ่ม
    public static void Spawn(Vector3 position, float amount, Kind kind, float side = 0f)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        DamageNumbers number = null;
        while (pool.Count > 0 && number == null) number = pool.Pop(); // ของที่หายไปตอนเปลี่ยนฉากจะเป็น null
        if (number == null) number = Create();
        number.Show(position, amount, kind, side);
    }

    static VertexGradient Gradient(Color top, Color bottom) => new VertexGradient(top, top, bottom, bottom);

    // material เดียวใช้ร่วมกันทุกตัว: ขอบม่วงเข้มหนา + เงาตกกระทบเยื้องล่างขวา
    static Material Style(TMP_FontAsset font)
    {
        if (style != null || font == null) return style;
        style = new Material(font.material) { name = "DamageNumbers" };
        style.EnableKeyword(ShaderUtilities.Keyword_Outline);
        style.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        style.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        style.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(28, 14, 44, 255));
        style.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        style.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
        style.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.45f);
        style.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f);
        style.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
        style.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
        return style;
    }

    static DamageNumbers Create()
    {
        var obj = new GameObject("DamageNumber");
        var number = obj.AddComponent<DamageNumbers>();
        var label = obj.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = -4f;
        label.enableVertexGradient = true;
        label.rectTransform.sizeDelta = new Vector2(3f, 1f);
        label.raycastTarget = false;
        var material = Style(label.font);
        if (material != null) label.fontSharedMaterial = material;
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 50;
        number.label = label;
        return number;
    }

    void Show(Vector3 at, float amount, Kind kind, float side)
    {
        big = kind == Kind.Enemy && amount >= BigHit;
        label.colorGradient = kind == Kind.Heal ? HealGradient : kind == Kind.Player ? PlayerGradient : big ? BigGradient : EnemyGradient;
        label.fontSize = kind != Kind.Enemy ? 3.8f : big ? 5.2f : Mathf.Lerp(3.4f, 4.4f, Mathf.Clamp01(amount / BigHit));
        float rounded = Mathf.Round(amount * 10f) / 10f;
        string text = Mathf.Approximately(rounded, Mathf.Round(rounded)) ? Mathf.RoundToInt(rounded).ToString() : rounded.ToString("0.0");
        label.text = kind == Kind.Player ? "-" + text : kind == Kind.Heal ? "+" + text : text;
        label.alpha = 1f;

        float dir = side != 0f ? Mathf.Sign(side) : (Random.value < 0.5f ? -1f : 1f);
        position = at + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0f, 0.12f), 0f);
        velocity = new Vector3(dir * Random.Range(0.5f, 1f), big ? 3.4f : 2.8f, 0f); // กระโดดเป็นโค้งไปทางที่โดนตี
        popScale = big ? 2.1f : 1.7f;
        tilt = big ? -dir * 8f : -dir * Random.Range(2f, 5f);
        age = 0f;
        Apply();
        gameObject.SetActive(true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (age >= Life)
        {
            gameObject.SetActive(false);
            pool.Push(this);
            return;
        }
        velocity.y -= Gravity * dt;
        velocity.x *= 1f - Mathf.Min(1f, 3f * dt);
        position += velocity * dt;
        Apply();
    }

    void Apply()
    {
        float k = age / Life;
        // เด้งใหญ่แล้วหดเกินนิดหนึ่งก่อนเข้าที่ ช่วงท้ายหดลงพร้อมจาง
        float scale;
        if (age < PopTime) scale = Mathf.Lerp(popScale, 0.9f, age / PopTime);
        else if (age < PopTime * 2f) scale = Mathf.Lerp(0.9f, 1f, (age - PopTime) / PopTime);
        else scale = k < 0.7f ? 1f : Mathf.Lerp(1f, 0.75f, (k - 0.7f) / 0.3f);
        Vector3 shake = big && age < 0.2f ? (Vector3)(Random.insideUnitCircle * 0.05f) : Vector3.zero;

        transform.position = position + shake;
        transform.localScale = Vector3.one * scale;
        transform.rotation = Quaternion.Euler(0f, 0f, tilt * (1f - k));
        label.alpha = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
    }
}
