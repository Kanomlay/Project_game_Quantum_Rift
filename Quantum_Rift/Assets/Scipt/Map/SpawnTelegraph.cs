using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// วงเตือนก่อนมอนสเตอร์เกิด: วงรอยแยกขึ้นที่พื้นตรงปลายเท้า หมุนเร่งขึ้น ไส้ในค่อย ๆ เต็มเป็นตัวนับเวลา ช่วงท้ายกะพริบ
// ครบเวลา: แสงวาบ ประกายแตกออก แล้วมอนโผล่ (จางเข้า + เด้งขนาด) หัวหน้าหน่วย/บอสวงใหญ่กว่า คนละสี และกล้องสั่น
// ภาพสร้างในโค้ดทั้งหมด ไม่ต้องมีไฟล์ภาพ เป็นลูกของห้อง เปลี่ยนด่านกลางทางก็หายไปพร้อมกัน ไม่เสกมอนค้างข้ามด่าน
public sealed class SpawnTelegraph : MonoBehaviour
{
    public enum Emphasis { Normal, Leader, Boss }

    // วงอยู่บนพื้น: ใต้ตัวละคร/มอน/กำแพง แต่เหนือพื้นแมพ (พื้นอยู่ชั้น bg, ผู้เล่นอยู่ bg2 ลำดับ 5, พอร์ทัลออก bg2 ลำดับ 0)
    const string FloorLayer = "bg2";
    const int FloorOrder = 1;
    const float FloorSquash = 0.55f; // มองพื้นจากมุมเฉียง วงจึงเป็นวงรีแบน
    const float PopTime = 0.22f;
    const float FlashTime = 0.28f;

    static readonly Color SpaceshipColor = new Color(0.72f, 0.4f, 1f);  // สีเดียวกับกับดักรอยแยกของแมพ 1
    static readonly Color ForestColor = new Color(0.3f, 0.95f, 0.58f);
    static readonly Color LeaderColor = new Color(1f, 0.55f, 0.2f);
    static readonly Color BossColor = new Color(1f, 0.25f, 0.3f);
    static readonly float[] SizeBy = { 1f, 1.2f, 1.5f };
    static readonly int[] BurstBy = { 10, 16, 26 };
    static readonly float[] ShakeBy = { 0f, 0.1f, 0.25f };

    public Vector2 Spot { get; private set; } // ตำแหน่งที่มอนจะโผล่ (จุดหาที่เกิดตัวอื่นเว้นระยะจากตรงนี้ด้วย)

    Func<GameObject> spawn;
    Color color;
    Emphasis emphasis;
    float warning, delay, radius;
    Vector2 bodyCenter;
    Transform floor, spinner;
    SpriteRenderer ring, disc, glow;

    // spot = ตำแหน่งเกิดของมอน, body = ขนาดตัว (วงอยู่ที่ปลายเท้า), spawn = เสกมอนตอนครบเวลา คืนตัวที่เสกมา (null = ไม่เสกแล้ว)
    public static SpawnTelegraph Begin(Transform parent, Vector2 spot, SpawnPlacement.Footprint body, Emphasis emphasis,
                                       Color color, float warning, float delay, Func<GameObject> spawn)
    {
        var go = new GameObject("SpawnTelegraph");
        go.transform.SetParent(parent, false);
        go.transform.position = spot + body.Feet + Vector2.up * 0.1f;

        var telegraph = go.AddComponent<SpawnTelegraph>();
        telegraph.Spot = spot;
        telegraph.spawn = spawn;
        telegraph.color = color;
        telegraph.emphasis = emphasis;
        telegraph.warning = Mathf.Max(0.05f, warning);
        telegraph.delay = Mathf.Max(0f, delay);
        telegraph.radius = (Mathf.Max(body.size.x, 0.7f) * 0.7f + 0.35f) * SizeBy[(int)emphasis];
        telegraph.bodyCenter = spot + body.offset;
        telegraph.Build();
        telegraph.StartCoroutine(telegraph.Run());
        return telegraph;
    }

    // สีตามธีมแมพ (รอยแยกม่วงบนยาน / เขียวในป่า) หัวหน้าหน่วยสีส้ม บอสสีแดง
    public static Color ColorFor(Component inMap, Emphasis emphasis)
    {
        if (emphasis == Emphasis.Leader) return LeaderColor;
        if (emphasis == Emphasis.Boss) return BossColor;
        var features = inMap != null ? inMap.GetComponentInParent<MapGameplayFeatures>() : null;
        return features != null && features.theme == MapGameplayFeatures.Theme.LivingForest ? ForestColor : SpaceshipColor;
    }

    void Build()
    {
        floor = new GameObject("Floor").transform;
        floor.SetParent(transform, false);
        glow = Layer("Glow", floor, ProceduralSprites.Glow, FloorOrder);
        disc = Layer("Core", floor, ProceduralSprites.Disc, FloorOrder + 1);
        spinner = new GameObject("Spin").transform;
        spinner.SetParent(floor, false);
        ring = Layer("Ring", spinner, ProceduralSprites.DashedRing, FloorOrder + 2);
        floor.gameObject.SetActive(false); // ยังไม่ขึ้นจนกว่าจะพ้นช่วงเหลื่อมเวลา
    }

    static SpriteRenderer Layer(string name, Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = FloorLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    IEnumerator Run()
    {
        // มอนในระลอกเดียวกันไม่ขึ้นวงพร้อมกันเป๊ะ ดูเป็นธรรมชาติกว่า
        if (delay > 0f) yield return new WaitForSeconds(delay);
        floor.gameObject.SetActive(true);

        float nextSpark = 0f, angle = UnityEngine.Random.Range(0f, 360f);
        for (float t = 0f; t < warning; t += Time.deltaTime)
        {
            float k = t / warning;
            float open = 1f - (1f - Mathf.Clamp01(t / 0.15f)) * (1f - Mathf.Clamp01(t / 0.15f)); // กางวงออกเร็ว ๆ ตอนเริ่ม
            float blink = k > 0.75f ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 38f)) : 1f;         // ช่วงท้ายกะพริบ = ใกล้โผล่แล้ว

            SetFloorScale(Mathf.Lerp(0.4f, 1f, open));
            angle += Mathf.Lerp(120f, 560f, k * k) * Time.deltaTime; // หมุนเร่งขึ้นเรื่อย ๆ
            spinner.localRotation = Quaternion.Euler(0f, 0f, angle);
            ring.color = Tint(0.55f + 0.45f * k, blink);
            disc.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.78f, k);
            disc.color = Tint(0.14f + 0.22f * k, blink);
            glow.transform.localScale = Vector3.one * (1.55f + 0.1f * Mathf.Sin(t * 10f));
            glow.color = Tint(0.12f + 0.3f * k, 1f);

            if (Time.time >= nextSpark)
            {
                nextSpark = Time.time + 0.08f;
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector2 edge = (Vector2)transform.position + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * FloorSquash);
                ImpactSparks.Spawn(edge, color, 1, Vector2.up, 2.2f, 12f);
            }
            yield return null;
        }

        GameObject monster = spawn != null ? spawn() : null;
        if (monster == null) { Destroy(gameObject); yield break; }

        // โผล่: แสงวาบกลางตัว ประกายแตกออกรอบทิศ วงบนพื้นขยายแล้วจางหาย
        ImpactSparks.Spawn(bodyCenter, color, BurstBy[(int)emphasis], Vector2.zero, 4.5f);
        if (ShakeBy[(int)emphasis] > 0f) CameraFollow.Shake(ShakeBy[(int)emphasis], 0.2f);
        var flash = Flash();
        var pop = new Pop(monster);

        for (float t = 0f; t < Mathf.Max(FlashTime, PopTime); t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / FlashTime);
            SetFloorScale(1f + 0.25f * k);
            ring.color = Tint(1f - k, 1f);
            disc.color = Tint(0.36f * (1f - k), 1f);
            glow.color = Tint(0.42f * (1f - k), 1f);
            flash.color = new Color(1f, 1f, 1f, 0.85f * (1f - k)) * Color.Lerp(Color.white, color, 0.35f);
            flash.transform.localScale = Vector3.one * radius * (2.4f + 1.2f * k);
            pop.Apply(Mathf.Clamp01(t / PopTime));
            yield return null;
        }
        pop.Apply(1f);
        Destroy(gameObject);
    }

    void SetFloorScale(float s)
    {
        floor.localScale = new Vector3(radius * 2f * s, radius * 2f * s * FloorSquash, 1f);
    }

    Color Tint(float alpha, float blink) => new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha * blink));

    SpriteRenderer Flash()
    {
        var go = new GameObject("Flash");
        go.transform.SetParent(transform, false);
        go.transform.position = bodyCenter;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprites.Glow;
        renderer.sortingLayerName = "Effect"; // สว่างทับตัวมอนตอนโผล่
        renderer.sortingOrder = 6;
        return renderer;
    }

    // มอนโผล่: ทุกภาพในตัวจางจากใสเป็นทึบ ขนาดเด้งจากเล็กเกินแล้วกลับที่ (คืนค่าเดิมเป๊ะตอนจบ)
    sealed class Pop
    {
        readonly Transform body;
        readonly Vector3 scale;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        readonly List<float> alphas = new List<float>();

        public Pop(GameObject monster)
        {
            body = monster.transform;
            scale = body.localScale;
            foreach (var renderer in monster.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderers.Add(renderer);
                alphas.Add(renderer.color.a);
            }
            Apply(0f);
        }

        public void Apply(float k)
        {
            if (body == null) return;
            // 0.6 → 1.1 → 1.0
            float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.1f, k / 0.7f) : Mathf.Lerp(1.1f, 1f, (k - 0.7f) / 0.3f);
            body.localScale = k >= 1f ? scale : scale * s;
            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                Color c = renderer.color; // คงสีที่ตัวมอนตั้งเอง (โดนตีกะพริบแดง) ปรับแค่ความใส
                c.a = alphas[i] * k;
                renderer.color = c;
            }
        }
    }
}
