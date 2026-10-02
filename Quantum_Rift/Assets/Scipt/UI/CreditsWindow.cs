using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าเครดิต: เปิดจากแถว "เครดิต" ในหน้าตั้งค่า (เมนูหลักและในเกม) ข้อความมาจาก Resources/Credits.txt
// ต้องมีในเกมเพราะเสียงเอฟเฟกต์ใช้สัญญาอนุญาต CC BY 4.0 (ต้องบอกชื่อผู้ทำ ที่มา และสัญญาอนุญาต)
// เพิ่มเครดิตใหม่ (เพลง ภาพ) แก้ที่ไฟล์ Credits.txt อย่างเดียว หน้าต่างสร้างจากโค้ด ไม่มีอะไรในฉาก
public sealed class CreditsWindow : MonoBehaviour
{
    const string ResourcePath = "Credits";
    static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.8f);
    static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.1f, 0.98f);
    static readonly Color BorderColor = new Color(0.35f, 0.75f, 1f, 0.6f);
    static readonly Color Accent = new Color(0.55f, 0.92f, 1f);

    static CreditsWindow open;
    static int closedFrame = -1;
    // ESC ที่ใช้ปิดหน้าเครดิตไม่นับเป็นการปิดหน้าตั้งค่า/พักเกม (PauseManager เช็คตัวนี้)
    public static bool BlocksEscape => open != null || Time.frameCount == closedFrame;

    public static void Attach(SettingsMenu menu)
    {
        var button = SettingsExtraRow.Add(menu, "Credits", "CREDITS", "เครดิต", out TMP_Text value);
        if (button == null) return;
        if (value != null)
        {
            var label = value.gameObject.AddComponent<LocalizedText>();
            label.englishText = "VIEW";
            label.thaiText = "ดู";
            label.Apply();
        }
        button.onClick.AddListener(Show);
    }

    public static void Show()
    {
        if (open != null) return;
        var go = new GameObject("CreditsWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        open = go.AddComponent<CreditsWindow>();
        open.Build();
    }

    void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950; // เหนือหน้าตั้งค่าและคอนโซลทดสอบ
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var backdrop = Rect("Backdrop", transform);
        Stretch(backdrop, 0f, 0f, 0f, 0f);
        backdrop.gameObject.AddComponent<Image>().color = Backdrop;

        var panel = Rect("Panel", transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(980f, 760f);
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        panel.gameObject.AddComponent<Outline>().effectColor = BorderColor;

        var title = Text(panel, "CREDITS", 40f, Accent, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 80f);
        title.rectTransform.anchoredPosition = Vector2.zero;

        var view = Rect("Scroll", panel);
        Stretch(view, 40f, 96f, 40f, 88f);
        view.gameObject.AddComponent<Image>().color = Color.clear; // รับล้อเมาส์ทั้งช่อง
        view.gameObject.AddComponent<RectMask2D>();
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.viewport = view;

        var source = Resources.Load<TextAsset>(ResourcePath);
        var body = Text(view, source != null ? source.text : "Credits.txt is missing from Resources.", 22f, Color.white,
                        TextAlignmentOptions.Top);
        body.rectTransform.anchorMin = new Vector2(0f, 1f);
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.pivot = new Vector2(0.5f, 1f);
        body.rectTransform.sizeDelta = Vector2.zero;
        body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = body.rectTransform;

        var close = Rect("Close", panel);
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(0.5f, 0f);
        close.sizeDelta = new Vector2(260f, 56f);
        close.anchoredPosition = new Vector2(0f, 22f);
        var closeImage = close.gameObject.AddComponent<Image>();
        closeImage.color = new Color(0.16f, 0.18f, 0.27f);
        var button = close.gameObject.AddComponent<Button>();
        button.targetGraphic = closeImage;
        button.onClick.AddListener(Close);
        var closeLabel = Text(close, LanguageSettings.IsThai ? "ปิด" : "CLOSE", 26f, Color.white, TextAlignmentOptions.Center);
        Stretch(closeLabel.rectTransform, 0f, 0f, 0f, 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Close()
    {
        closedFrame = Time.frameCount;
        open = null;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (open == this) open = null; // เปลี่ยนฉากระหว่างเปิดอยู่
    }

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static TextMeshProUGUI Text(Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var rect = Rect("Text", parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }
}
