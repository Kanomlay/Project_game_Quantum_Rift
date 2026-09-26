using System.Collections.Generic;
using UnityEngine;

// ป้ายบอกว่าเป็นกระสุนของศัตรูที่ฟันลบได้/ชะลอได้ (กระสุนมอนสเตอร์ หินที่ขว้าง กระสุนบอส)
// เลเซอร์กับพื้นอันตรายไม่ได้ติดป้ายนี้ พรคมสลายมิติ/สนามชะลอกระสุนจึงไม่มีผลกับพวกนั้น
public interface IEnemyBullet { }

// ทะเบียนกระสุนศัตรูที่ยังบินอยู่ ใช้กับพร:
// - คมสลายมิติ: ฟันโดนแล้วกระสุนหาย (จำนวนสูงสุดต่อการฟันหนึ่งครั้ง)
// - สนามชะลอกระสุน: กระสุนในรัศมีรอบตัวผู้เล่นบินช้าลง (กระสุนแต่ละแบบถาม SpeedFactor ทุกเฟรม)
public static class EnemyBullets
{
    static readonly List<Component> active = new List<Component>();

    // สนามชะลอ (BlessingManager ตั้งให้ทุกเฟรม)
    static bool slowActive;
    static Vector2 slowCenter;
    static float slowRadius, slowFactor = 1f;
    public static bool AnyInSlowField { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        active.Clear();
        slowActive = false;
        AnyInSlowField = false;
    }

    public static void Register(Component bullet)
    {
        if (bullet != null && !active.Contains(bullet)) active.Add(bullet);
    }

    public static void Unregister(Component bullet) => active.Remove(bullet);

    public static void SetSlowField(bool on, Vector2 center, float radius, float factor)
    {
        slowActive = on;
        slowCenter = center;
        slowRadius = radius;
        slowFactor = factor;
        AnyInSlowField = false;
        if (!on) return;
        foreach (var bullet in active)
            if (bullet != null && Inside(bullet.transform.position)) { AnyInSlowField = true; break; }
    }

    // ตัวคูณความเร็วของกระสุนที่ตำแหน่งนี้ (1 = ปกติ)
    public static float SpeedFactor(Vector2 position) => slowActive && Inside(position) ? slowFactor : 1f;

    static bool Inside(Vector2 position) => (position - slowCenter).sqrMagnitude <= slowRadius * slowRadius;

    // ลบกระสุนในวงไม่เกิน max ลูก ใส่ลูกที่ลบแล้วลงใน attack (ชุดของที่การฟันครั้งนี้โดนไปแล้ว) คืนจำนวนที่ลบ
    public static int CleaveAround(Vector2 center, float radius, int max, HashSet<Component> attack)
    {
        if (max <= 0) return 0;
        int cleaved = 0;
        for (int i = active.Count - 1; i >= 0 && cleaved < max; i--)
        {
            var bullet = active[i];
            if (bullet == null) { active.RemoveAt(i); continue; }
            Vector2 at = bullet.transform.position;
            if ((at - center).sqrMagnitude > radius * radius) continue;

            var view = bullet.GetComponentInChildren<SpriteRenderer>();
            Color color = view != null ? view.color : Color.white;
            ImpactSparks.Spawn(at, Color.Lerp(color, Color.white, 0.4f), 6, Vector2.zero, 3.5f);
            active.RemoveAt(i);
            attack?.Add(bullet);
            Object.Destroy(bullet.gameObject);
            cleaved++;
        }
        return cleaved;
    }

    // กระสุนที่การฟันครั้งนี้ลบไปแล้วกี่ลูก (นับจากชุดของที่โดน)
    public static int CountIn(HashSet<Component> attack)
    {
        int count = 0;
        if (attack == null) return 0;
        foreach (var item in attack)
            if (item is IEnemyBullet) count++;
        return count;
    }
}
