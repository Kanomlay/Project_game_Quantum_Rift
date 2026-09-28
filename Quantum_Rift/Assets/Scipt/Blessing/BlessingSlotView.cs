using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ช่องพรหนึ่งช่องบน HUD: ไอคอน + ตัวเลขมุมขวาล่าง + แผ่นมืดแบบวงหมุน (คูลดาวน์) + แสงฟุ้งด้านหลัง (กำลังทำงาน)
// + ป้ายระดับมุมซ้ายบน (Lv2 / Lv3 สีทอง สร้างในโค้ด ระดับ 1 ไม่โชว์)
// ส่วนประกอบสร้างโดย BlessingBuilder
public sealed class BlessingSlotView : MonoBehaviour
{
    public Image glow;
    public Image icon;
    public Image cooldown;  // ภาพเดียวกับไอคอน ย้อมดำ แบบ Filled Radial
    public GameObject badgeBack;
    public TMP_Text badge;

    const float PingTime = 0.3f;

    static readonly Color LevelColor = new Color(1f, 0.8f, 0.3f);

    BlessingData shown;
    TMP_Text levelText;
    float pingAt = -99f;
    bool glowing;

    void Awake()
    {
        // ภาพแสงฟุ้งสร้างในโค้ด ใส่ตอนเริ่มเกม (builder อ้างถึงภาพที่ยังไม่มีไฟล์ไม่ได้)
        if (glow != null) glow.sprite = ProceduralSprites.Glow;
    }

    public void Show(BlessingData blessing, int level = 1)
    {
        shown = blessing;
        gameObject.SetActive(blessing != null);
        if (blessing == null) return;
        ShowLevel(level);
        if (icon != null) icon.sprite = blessing.hudIcon;
        if (cooldown != null)
        {
            cooldown.sprite = blessing.hudIcon;
            cooldown.fillAmount = 0f;
        }
        if (glow != null) glow.color = Clear(BlessingData.CategoryColor(blessing.category));
        SetState("", 0f, false);
    }

    public void SetState(string text, float dim, bool on)
    {
        if (shown == null) return;
        if (badge != null) badge.text = text;
        if (badgeBack != null) badgeBack.SetActive(!string.IsNullOrEmpty(text));
        if (cooldown != null) cooldown.fillAmount = Mathf.Clamp01(dim);
        glowing = on;
    }

    public void Ping() => pingAt = Time.unscaledTime;

    void ShowLevel(int level)
    {
        if (level <= 1 && levelText == null) return;
        if (levelText == null)
        {
            var go = new GameObject("Level", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(-4f, 6f);
            rect.sizeDelta = new Vector2(40f, 20f);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (badge != null) label.font = badge.font;
            label.fontSize = 17f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = LevelColor;
            label.outlineWidth = 0.3f;
            label.outlineColor = new Color32(20, 12, 36, 255);
            label.raycastTarget = false;
            levelText = label;
        }
        levelText.gameObject.SetActive(level > 1);
        levelText.text = "Lv" + level;
    }

    void Update()
    {
        if (shown == null) return;
        float k = (Time.unscaledTime - pingAt) / PingTime;
        float bump = k >= 0f && k < 1f ? Mathf.Sin(k * Mathf.PI) * 0.25f : 0f;
        transform.localScale = Vector3.one * (1f + bump);

        if (glow == null) return;
        Color tint = BlessingData.CategoryColor(shown.category);
        float alpha = glowing ? 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
        alpha = Mathf.Max(alpha, bump * 3f); // เด้งทีไรแสงวาบด้วย
        glow.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alpha));
    }

    static Color Clear(Color color) => new Color(color.r, color.g, color.b, 0f);
}
