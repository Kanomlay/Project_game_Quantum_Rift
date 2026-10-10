using UnityEngine;
using UnityEngine.UI;

// วนภาพ BG เมนู 7 เฟรมโดยไม่ขยับ RectTransform ไม่ดักคลิก และไม่หยุดตาม Time.timeScale ของเกม
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class MenuBackgroundAnimation : MonoBehaviour
{
    public Image display;
    public Sprite[] frames;
    [Min(.01f)] public float frameSeconds = .18f;
    public int CurrentFrame { get; private set; }
    float elapsed;

    void OnEnable() { Restart(); }
    void Update() { Advance(Time.unscaledDeltaTime); }

    // กลับจากหน้าเลือกอาชีพหรือโหลดเมนูใหม่ เริ่มรอบภาพใหม่โดยไม่แตะเซฟผู้เล่น
    public void Restart()
    {
        if (display == null) display = GetComponent<Image>();
        elapsed = 0f;
        CurrentFrame = 0;
        Apply();
    }

    public void Advance(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds) || frames == null || frames.Length == 0) return;
        float duration = Mathf.Max(.01f, frameSeconds);
        elapsed += seconds;
        int steps = Mathf.FloorToInt(elapsed / duration);
        if (steps <= 0) return;
        elapsed %= duration;
        CurrentFrame = (CurrentFrame + steps) % frames.Length;
        Apply();
    }

    void Apply()
    {
        if (display != null && frames != null && frames.Length > CurrentFrame && frames[CurrentFrame] != null)
            display.sprite = frames[CurrentFrame];
    }
}
