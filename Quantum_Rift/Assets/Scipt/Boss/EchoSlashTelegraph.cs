using UnityEngine;

// พัดเตือนท่าฟันของบอสบนพื้น: ขอบพัดขึ้นทันที ไส้ในค่อย ๆ เต็มจากตัวบอสออกไปเป็นตัวนับเวลา ช่วงท้ายกะพริบ
// เต็มแล้ว = ฟันลง (Strike) แสงวาบแล้วจางหาย พื้นที่ในพัดคือพื้นที่โดนจริง (ดู EchoCommanderBoss.InSlash)
public sealed class EchoSlashTelegraph : MonoBehaviour
{
    const string FloorLayer = "bg2"; // ใต้ตัวละคร/มอน เหนือพื้นแมพ (ชั้นเดียวกับวงเตือนเกิดมอน)
    const int FloorOrder = 1;
    const float StrikeTime = 0.22f;

    SpriteRenderer outline, fill;
    Color color;
    float diameter, warning, age, strikeAge;
    bool striking;

    public static EchoSlashTelegraph Show(Vector2 origin, Vector2 direction, float radius, float arc, float warning, Color color)
    {
        var go = new GameObject("EchoSlashTelegraph");
        go.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        var telegraph = go.AddComponent<EchoSlashTelegraph>();
        telegraph.color = color;
        telegraph.diameter = radius * 2f;
        telegraph.warning = Mathf.Max(0.05f, warning);
        int layer = SortingLayer.NameToID(FloorLayer);
        var sector = ProceduralSprites.Sector(arc * 0.5f);
        telegraph.outline = EchoFx.Layer(go.transform, "Outline", sector, layer, FloorOrder, Color.clear);
        telegraph.fill = EchoFx.Layer(go.transform, "Fill", sector, layer, FloorOrder + 1, Color.clear);
        telegraph.outline.transform.localScale = Vector3.one * telegraph.diameter;
        telegraph.Update();
        return telegraph;
    }

    public void Strike()
    {
        striking = true;
        strikeAge = 0f;
    }

    void Update()
    {
        if (striking)
        {
            strikeAge += Time.deltaTime;
            float k = Mathf.Clamp01(strikeAge / StrikeTime);
            float s = diameter * (1f + 0.08f * k);
            fill.transform.localScale = Vector3.one * s;
            outline.transform.localScale = Vector3.one * s;
            Color flash = Color.Lerp(Color.white, color, 0.4f);
            fill.color = new Color(flash.r, flash.g, flash.b, 0.85f * (1f - k));
            outline.color = new Color(color.r, color.g, color.b, 0.6f * (1f - k));
            if (k >= 1f) Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        float p = Mathf.Clamp01(age / warning);
        float blink = p > 0.7f ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(age * 32f)) : 1f; // ใกล้ฟันแล้วกะพริบ
        outline.color = new Color(color.r, color.g, color.b, 0.32f * blink);
        fill.transform.localScale = Vector3.one * diameter * Mathf.Lerp(0.15f, 1f, p);
        fill.color = new Color(color.r, color.g, color.b, (0.25f + 0.3f * p) * blink);
    }
}
