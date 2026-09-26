using System;
using UnityEngine;

// ภาพพื้นฐานที่สร้างในโค้ด (1 หน่วย ย่อ/ขยายด้วย scale) ใช้ร่วมกันหลายระบบ: วงเตือนเกิดมอน สนาม/เกราะของพร
public static class ProceduralSprites
{
    static Sprite dashedRing, disc, glow, thinRing;

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
