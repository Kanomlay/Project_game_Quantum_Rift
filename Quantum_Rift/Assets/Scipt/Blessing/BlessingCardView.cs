using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// การ์ดพรหนึ่งใบในหน้าต่างเลือกพร กรอบสีตามหมวด (โจมตี/ป้องกัน/ทรัพยากร) โผล่แบบจางเข้า ชี้แล้วขยายนิด ๆ
// ส่วนประกอบสร้างโดย BlessingBuilder ใช้เวลาจริงเพราะตอนเปิดหน้าต่างเกมหยุดอยู่
[RequireComponent(typeof(CanvasGroup))]
public sealed class BlessingCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Image frame;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text categoryText;
    public TMP_Text abilityText;
    public TMP_Text limitText;
    public TMP_Text keyText;

    const float FadeTime = 0.18f;

    CanvasGroup group;
    Color tint = Color.white;
    float appearAt;
    bool hovered;

    public void Show(BlessingData data, int index, float delay)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        tint = BlessingData.CategoryColor(data.category);

        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
        }
        Set(nameText, data.DisplayName, Color.Lerp(tint, Color.white, 0.55f));
        Set(categoryText, CategoryName(data.category), tint);
        Set(abilityText, data.Ability, null);
        Set(limitText, (LanguageSettings.IsThai ? "ข้อจำกัด: " : "Limit: ") + data.Limit, null);
        Set(keyText, (index + 1).ToString(), tint);

        hovered = false;
        appearAt = Time.unscaledTime + delay;
        transform.localScale = Vector3.one * 0.9f;
        group.alpha = 0f;
        Update();
    }

    static void Set(TMP_Text field, string text, Color? color)
    {
        if (field == null) return;
        field.text = text;
        if (color.HasValue) field.color = color.Value;
    }

    static string CategoryName(BlessingCategory category)
    {
        bool thai = LanguageSettings.IsThai;
        switch (category)
        {
            case BlessingCategory.Attack: return thai ? "พรโจมตี" : "OFFENSE";
            case BlessingCategory.Defense: return thai ? "พรป้องกัน" : "DEFENSE";
            default: return thai ? "พรทรัพยากร" : "RESOURCE";
        }
    }

    void Update()
    {
        if (group == null) return;
        float t = Time.unscaledTime - appearAt;
        group.alpha = Mathf.Clamp01(t / FadeTime);
        float target = t < 0f ? 0.9f : hovered ? 1.05f : 1f;
        float follow = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, follow);
        if (frame != null) frame.color = hovered ? Color.Lerp(tint, Color.white, 0.35f) : Color.Lerp(tint, Color.black, 0.25f);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
}
