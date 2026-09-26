using System.Collections;
using UnityEngine;

/// <summary>Small point-filtered floor fracture shared by all hammer impacts.</summary>
public sealed class GroundCrackVfx : MonoBehaviour
{
    static Sprite crack;
    SpriteRenderer display;

    public static void Spawn(Vector2 position, float diameter, Color tint)
    {
        if (crack == null) crack = BuildSprite();
        var go = new GameObject("HammerGroundCrack");
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = crack;
        renderer.color = tint;
        renderer.sortingLayerName = "object";
        renderer.sortingOrder = 1;
        go.transform.localScale = Vector3.one * (diameter / crack.bounds.size.x);
        var effect = go.AddComponent<GroundCrackVfx>();
        effect.display = renderer;
        effect.StartCoroutine(effect.Fade(tint));
    }

    static Sprite BuildSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        Color32 shade = new Color32(255,255,255,255);
        for (int ray = 0; ray < 9; ray++)
        {
            float angle = (ray * 40f + 9f) * Mathf.Deg2Rad;
            int x0 = 32, y0 = 32;
            for (int segment = 1; segment <= 4; segment++)
            {
                float distance = segment * 6.5f;
                float skew = (ray % 2 == 0 ? 1f : -1f) * ((segment % 2 == 0) ? 2f : 0f);
                int x1 = Mathf.RoundToInt(32 + Mathf.Cos(angle) * distance + Mathf.Sin(angle) * skew);
                int y1 = Mathf.RoundToInt(32 + Mathf.Sin(angle) * distance - Mathf.Cos(angle) * skew);
                int steps = Mathf.Max(Mathf.Abs(x1-x0), Mathf.Abs(y1-y0));
                for (int i = 0; i <= steps; i++)
                {
                    int x = Mathf.RoundToInt(Mathf.Lerp(x0,x1,i/(float)Mathf.Max(1,steps)));
                    int y = Mathf.RoundToInt(Mathf.Lerp(y0,y1,i/(float)Mathf.Max(1,steps)));
                    if (x > 0 && x < size-1 && y > 0 && y < size-1)
                    { pixels[y * size + x] = shade; pixels[(y+1)*size+x] = shade; }
                }
                x0=x1; y0=y1;
            }
        }
        texture.SetPixels32(pixels); texture.Apply();
        texture.hideFlags = HideFlags.DontSave;
        var sprite = Sprite.Create(texture, new Rect(0,0,size,size),new Vector2(.5f,.5f),64f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    IEnumerator Fade(Color tint)
    {
        for (float t=0f;t<.48f;t+=Time.deltaTime)
        {
            float p = t/.48f;
            display.color = new Color(tint.r,tint.g,tint.b,(1f-p)*tint.a);
            yield return null;
        }
        Destroy(gameObject);
    }
}
