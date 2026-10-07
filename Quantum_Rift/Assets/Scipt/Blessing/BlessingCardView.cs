using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// การ์ดพรหนึ่งใบในหน้าต่างเลือกพร กรอบสีตามหมวด (โจมตี/ป้องกัน/ทรัพยากร) โผล่แบบจางเข้า ชี้แล้วขยายนิด ๆ
// การ์ดอัปเกรด: กรอบสีทอง ชื่อต่อท้ายระดับ (Lv.2 / Lv.3) โชว์ความสามารถหลังอัป และความสามารถตอนนี้ไว้เทียบ
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
    static readonly Color UpgradeColor = new Color(1f, 0.78f, 0.3f);

    CanvasGroup group;
    Color tint = Color.white;
    float appearAt;
    bool hovered;
    bool arranged;

    // จัดช่วงบนของการ์ดใหม่ตอนเล่น (ไม่แก้ฉาก): ของเดิมไอคอน ชื่อพร และแถบหมวดซ้อนกัน ชื่อพรโดนแถบหมวดทับครึ่งล่าง
    // การ์ดสูง 460 (กลางการ์ด = 0): ไอคอน 94..202 · ชื่อ 42..92 · แถบหมวด -10..38 · คำอธิบาย -96..-12 · เส้นคั่น -101 · ข้อจำกัด -222..-106
    // การ์ดอัปเกรดมีข้อความเยอะสุด (คำอธิบาย 3–4 บรรทัด + ข้อจำกัด + "ตอนนี้: ...") กล่องข้อจำกัดจึงสูงขึ้นและย่อตัวอักษรได้มากขึ้น ล้นจริง ๆ ตัดด้วย …
    void Arrange()
    {
        if (arranged) return;
        arranged = true;
        if (icon != null) Place(icon.rectTransform, 148f, new Vector2(108f, 108f));
        if (nameText != null)
        {
            Place(nameText.rectTransform, 67f, new Vector2(nameText.rectTransform.sizeDelta.x, 50f));
            nameText.textWrappingMode = TextWrappingModes.NoWrap; // ชื่อยาวให้ย่อตัวอักษรลงในบรรทัดเดียว ไม่ขึ้นบรรทัดใหม่ไปทับแถบหมวด
        }
        const float CategoryY = 14f;
        if (categoryText != null) Place(categoryText.rectTransform, CategoryY, categoryText.rectTransform.sizeDelta);
        var plate = transform.Find("__Plate_Category") as RectTransform;
        if (plate != null) Place(plate, CategoryY, plate.sizeDelta);

        if (abilityText != null)
        {
            Place(abilityText.rectTransform, -54f, new Vector2(abilityText.rectTransform.sizeDelta.x, 84f));
            Fit(abilityText, 17f);
        }
        var divider = transform.Find("Divider") as RectTransform;
        if (divider != null) Place(divider, -101f, divider.sizeDelta);
        const float LimitY = -164f;
        var limitPlate = transform.Find("__Plate_Limit") as RectTransform;
        if (limitPlate != null) Place(limitPlate, LimitY, new Vector2(limitPlate.sizeDelta.x, 116f));
        if (limitText != null)
        {
            Place(limitText.rectTransform, LimitY, new Vector2(limitText.rectTransform.sizeDelta.x, 106f));
            Fit(limitText, 14f);
        }
    }

    static void Fit(TMP_Text text, float minSize)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(text.fontSizeMin, minSize);
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void Place(RectTransform rect, float y, Vector2 size)
    {
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = size;
    }

    public void Show(BlessingOffer offer, int index, float delay)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        Arrange();
        var data = offer.data;
        bool thai = LanguageSettings.IsThai;
        tint = offer.IsUpgrade ? UpgradeColor : BlessingData.CategoryColor(data.category);

        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
        }
        string limit = (thai ? "ข้อจำกัด: " : "Limit: ") + data.Limit;
        if (offer.IsUpgrade)
        {
            Set(nameText, $"{data.DisplayName}  Lv.{offer.level}", Color.Lerp(tint, Color.white, 0.45f));
            Set(categoryText, thai ? $"อัปเกรด Lv.{offer.level - 1} → Lv.{offer.level}" : $"UPGRADE Lv.{offer.level - 1} → Lv.{offer.level}", tint);
            limit += $"\n<size=90%><color=#9AA0B8>{(thai ? "ตอนนี้" : "Now")}: {offer.current.Ability}</color></size>";
        }
        else
        {
            Set(nameText, data.DisplayName, Color.Lerp(tint, Color.white, 0.55f));
            Set(categoryText, CategoryName(data.category), tint);
        }
        Set(abilityText, data.Ability, null);
        Set(limitText, limit, null);
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
