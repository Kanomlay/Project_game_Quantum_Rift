using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>แสดงเลือดบอสแบบหลอดเดียว ไม่มีเฟส; ไม่เปลี่ยนค่าเลือดหรือ AI ของบอส</summary>
public sealed class BossHealthHud : MonoBehaviour
{
    public CanvasGroup visibility;
    public RectTransform safeArea;
    public Image healthFill;
    public Image damageTrail;
    public TMP_Text bossNameLabel;
    public TMP_Text healthLabel;
    [Min(0)] public float damageDelay = .4f;
    [Min(.01f)] public float trailSpeed = .65f;
    [SerializeField] string displayName;
    float previousRatio = 1f;
    float delayRemaining;
    public bool IsVisible => visibility != null && visibility.alpha > 0;
    public float Ratio => healthFill != null ? healthFill.fillAmount : 0;

    void Awake()
    {
        if (bossNameLabel != null) bossNameLabel.text = displayName;
        Hide();
        ApplySafeArea();
    }

    // เรียกเมื่อเริ่มสู้ พร้อมเลือดจริงของบอส ไม่แสดงค่าตัวอย่างในเกม
    public void Show(float current, float maximum)
    {
        if (!Valid(current, maximum)) { Hide(); return; }
        if (bossNameLabel != null) bossNameLabel.text = displayName;
        SetHealth(current, maximum, true);
        visibility.alpha = current > 0 ? 1 : 0;
        visibility.interactable = false;
        visibility.blocksRaycasts = false;
        ApplySafeArea();
    }

    public void SetHealth(float current, float maximum, bool instant = false)
    {
        if (!Valid(current, maximum)) return;
        current = Mathf.Clamp(current, 0, maximum);
        float ratio = current / maximum;
        if (instant || ratio >= previousRatio) { damageTrail.fillAmount = ratio; delayRemaining = 0; }
        else if (ratio < previousRatio) delayRemaining = damageDelay;
        healthFill.fillAmount = ratio;
        previousRatio = ratio;
        healthLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        if (current <= 0) Hide();
    }

    public void Hide()
    {
        if (visibility != null) { visibility.alpha = 0; visibility.blocksRaycasts = false; visibility.interactable = false; }
    }

    static bool Valid(float current, float maximum) => maximum > 0 && !float.IsNaN(current) && !float.IsInfinity(current)
        && !float.IsNaN(maximum) && !float.IsInfinity(maximum);

    void Update()
    {
        ApplySafeArea();
        if (!IsVisible) return;
        if (delayRemaining > 0) delayRemaining -= Time.deltaTime;
        else damageTrail.fillAmount = Mathf.MoveTowards(damageTrail.fillAmount, healthFill.fillAmount, trailSpeed * Time.deltaTime);
    }

    public void ApplySafeArea()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect r = Screen.safeArea;
        safeArea.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    void OnDisable() { Hide(); }
}
