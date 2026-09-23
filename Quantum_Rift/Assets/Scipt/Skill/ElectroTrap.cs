using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// กับดักแม่เหล็กไฟฟ้าของนักประดิษฐ์: วางไว้บนพื้น รอจนมีศัตรูเดินเข้าใกล้ แล้วปล่อยวงไฟฟ้า
// ทำดาเมจและสตันทุกตัวในวง ใช้ได้ครั้งเดียว หมดเวลาแล้วยังไม่มีใครเหยียบก็หายไปเอง
// ภาพ 7 เฟรม: 1–2 ดิสก์รอ (วนช้า ๆ) → 3 ประกายไฟ → 4 วงไฟฟ้าเต็ม (ทำดาเมจ) → 5 วงขยาย → 6–7 ดับ
public sealed class ElectroTrap : MonoBehaviour
{
    const int MaxTraps = 2; // วางค้างได้พร้อมกันไม่เกินนี้ เกินแล้วอันเก่าสุดหายไป
    private static readonly List<ElectroTrap> placed = new List<ElectroTrap>();

    private Sprite[] frames;
    private float armAt, until, triggerRadius, blastRadius, damage, stunTime;
    private bool triggered;
    private SpriteRenderer view;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { placed.Clear(); }

    public static void Place(Vector2 position, Sprite[] frames, float scale, float armTime, float lifetime,
                             float triggerRadius, float blastRadius, float damage, float stunTime)
    {
        placed.RemoveAll(t => t == null);
        while (placed.Count >= MaxTraps)
        {
            Destroy(placed[0].gameObject);
            placed.RemoveAt(0);
        }

        var go = new GameObject("ElectroTrap");
        // ผูกกับแมพ เปลี่ยนด่านแล้วกับดักหายตาม (แมพทุกด่านวางซ้อนที่เดียวกัน ถ้าค้างไว้จะไปโผล่ด่านใหม่)
        var map = MapManager.instance != null ? MapManager.instance.CurrentMapRoot : null;
        if (map != null) go.transform.SetParent(map, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var trap = go.AddComponent<ElectroTrap>();
        trap.view = go.AddComponent<SpriteRenderer>();
        trap.view.sortingLayerName = "object"; // วางอยู่บนพื้น ศัตรูเดินทับได้
        trap.view.sortingOrder = -1;
        trap.frames = frames;
        trap.armAt = Time.time + armTime;
        trap.until = Time.time + lifetime;
        trap.triggerRadius = triggerRadius;
        trap.blastRadius = blastRadius;
        trap.damage = damage;
        trap.stunTime = stunTime;
        trap.view.sprite = frames[0];
        placed.Add(trap);
    }

    void Update()
    {
        if (triggered) return;

        if (Time.time >= until)
        {
            Destroy(gameObject);
            return;
        }

        view.sprite = frames[(int)(Time.time * 3f) % 2]; // ไฟตรงกลางกะพริบรอ

        if (Time.time >= armAt && SkillCombat.MonstersIn(transform.position, triggerRadius).Count > 0)
            StartCoroutine(Blast());
    }

    private IEnumerator Blast()
    {
        triggered = true;
        const float frameTime = 1f / 14f;

        for (int i = 2; i < frames.Length; i++)
        {
            view.sprite = frames[i];
            if (i == 3)
            {
                foreach (var monster in SkillCombat.MonstersIn(transform.position, blastRadius))
                    monster.Stun(stunTime);
                SkillCombat.DamageArea(transform.position, blastRadius, damage);
            }
            yield return new WaitForSeconds(frameTime);
        }
        Destroy(gameObject);
    }

    void OnDestroy() { placed.Remove(this); }
}
