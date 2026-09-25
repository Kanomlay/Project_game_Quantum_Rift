using UnityEngine;

// หยุดภาพชั่วขณะตอนตีโดน (hit-stop): ลดความเร็วเกมเกือบเป็น 0 แป๊บเดียวแล้วคืน ให้รู้สึกว่าตีเข้าเนื้อ
// นับเวลาด้วยเวลาจริง (unscaled) และไม่ยุ่งกับเกมที่หยุดอยู่แล้ว (หน้าหยุดเกม / หน้าสรุปผล)
public sealed class HitStop : MonoBehaviour
{
    const float FrozenScale = 0.05f;

    static HitStop runner;
    static float until;
    static bool active;

    public static void Freeze(float seconds)
    {
        if (seconds <= 0f || PauseManager.isGamePaused) return;
        if (!active && !Mathf.Approximately(Time.timeScale, 1f)) return;

        if (runner == null) runner = new GameObject("HitStop").AddComponent<HitStop>();
        until = Mathf.Max(until, Time.unscaledTime + seconds);
        if (!active)
        {
            active = true;
            Time.timeScale = FrozenScale;
        }
    }

    void Update()
    {
        if (active && Time.unscaledTime >= until) Release();
    }

    void OnDestroy()
    {
        if (active) Release();
        if (runner == this) runner = null;
    }

    static void Release()
    {
        active = false;
        // คืนเฉพาะถ้ายังเป็นค่าที่เราตั้งไว้ (ระหว่างนั้นอาจกดหยุดเกมหรือตายจนหน้าสรุปตั้ง 0 ไปแล้ว)
        if (!PauseManager.isGamePaused && Mathf.Approximately(Time.timeScale, FrozenScale)) Time.timeScale = 1f;
    }
}
