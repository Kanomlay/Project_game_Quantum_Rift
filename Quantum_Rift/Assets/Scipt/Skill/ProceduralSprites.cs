using System;
using System.Collections.Generic;
using UnityEngine;

// ภาพพื้นฐานที่สร้างในโค้ด (1 หน่วย ย่อ/ขยายด้วย scale) ใช้ร่วมกันหลายระบบ: วงเตือนเกิดมอน สนาม/เกราะของพร
public static class ProceduralSprites
{
    static Sprite dashedRing, disc, glow, thinRing, bar;
    static readonly Dictionary<int, Sprite> sectors = new Dictionary<int, Sprite>();

    // วงแหวนนอกขาดเป็นช่วง 6 ช่วง (หมุนแล้วเห็นว่าหมุน) + วงในบาง ๆ แบบรอยแยกมิติ
    public static Sprite DashedRing
    {
        get
        {
            if (dashedRing != null) return dashedRing;
            dashedRing = Generate(64, (d, a) =>
            {
                if (d >= 0.8f && d <= 0.97f)
                {
                    float segment = Mathf.Repeat(a / (Mathf.PI * 2f) * 6f, 1f);
                    return segment < 0.78f ? 1f : 0f;
                }
                return d >= 0.56f && d <= 0.63f ? 0.55f : 0f;
            }, FilterMode.Point);
            return dashedRing;
        }
    }

    // วงแหวนเส้นเดียวต่อเนื่อง (ขอบเกราะ/ขอบสนาม)
    public static Sprite ThinRing
    {
        get
        {
            if (thinRing != null) return thinRing;
            thinRing = Generate(96, (d, a) => d >= 0.9f && d <= 0.98f ? 1f : 0f, FilterMode.Point);
            return thinRing;
        }
    }

    public static Sprite Disc
    {
        get
        {
            if (disc != null) return disc;
            disc = Generate(64, (d, a) => d <= 0.95f ? 1f : 0f, FilterMode.Point);
            return disc;
        }
    }

    // วงกลมฟุ้ง ทึบตรงกลางจางออกขอบ
    public static Sprite Glow
    {
        get
        {
            if (glow != null) return glow;
            glow = Generate(64, (d, a) => (1f - d) * (1f - d), FilterMode.Bilinear);
            return glow;
        }
    }

    // พัดรูปลิ่มชี้ไปทางขวา (+X) กว้างข้างละ halfAngle องศา ไส้ในจาง ขอบนอกและขอบข้างเข้ม (พื้นที่ท่าฟันของบอส)
    public static Sprite Sector(float halfAngle)
    {
        int key = Mathf.Clamp(Mathf.RoundToInt(halfAngle), 1, 180);
        if (sectors.TryGetValue(key, out var sprite)) return sprite;
        float half = key * Mathf.Deg2Rad;
        sprite = Generate(128, (d, a) =>
        {
            float fromEdge = half - Mathf.Abs(a);
            if (fromEdge < 0f || d < 0.08f) return 0f;
            bool rim = d > 0.93f || fromEdge * d < 0.035f;
            return rim ? 1f : 0.4f;
        }, FilterMode.Bilinear);
        sectors[key] = sprite;
        return sprite;
    }

    // แถบยาว 1x1 หน่วย ยึดขอบซ้ายกลาง (scale x = ความยาว, y = ความกว้าง) ไส้จาง ขอบบน/ล่างเข้ม ปลายขวามน
    // ใช้เป็นเส้นเตือนท่าที่พุ่งเป็นแนวตรง (เถาวัลย์ Forest Wraith)
    public static Sprite Bar
    {
        get
        {
            if (bar != null) return bar;
            const int w = 64, h = 16;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);       // 0 กลาง 1 ขอบ
                    float tip = (x + 0.5f) / w;
                    bool cut = tip > 0.9f && v > 1f - (1f - tip) * 10f; // ปลายโค้งมน
                    float alpha = cut ? 0f : v > 0.78f ? 1f : 0.4f;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            bar = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);
            return bar;
        }
    }

    // alpha ตามระยะจากกลาง d (0–1) และมุม a (เรเดียน)
    static Sprite Generate(int size, Func<float, float, float> alpha, FilterMode filter)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = filter,
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, d > 1f ? 0f : Mathf.Clamp01(alpha(d, Mathf.Atan2(dy, dx))));
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
