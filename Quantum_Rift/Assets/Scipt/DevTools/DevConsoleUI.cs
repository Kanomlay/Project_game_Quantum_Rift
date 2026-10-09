using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// หน้าต่างของคอนโซลทดสอบ สร้างจากโค้ดทั้งหมด (ไม่มี prefab ไม่แตะฉาก)
// แผงชิดขวาจอ: หัว / แท็บ / เนื้อหาเลื่อนได้ (หัวข้อ ข้อความ ตารางปุ่ม แถวปุ่ม) / การ์ดตัวอย่างตอนคลิกวาง / แถบข้อความผลล่างสุด
// เนื้อหาสร้างใหม่ทั้งแท็บทุกครั้งที่ค่าเปลี่ยน (Clear แล้วเติมใหม่) ปุ่มจะได้สีตรงกับสถานะเสมอ
public sealed class DevConsoleUI
{
    public static readonly Color ButtonColor = new Color(0.16f, 0.18f, 0.27f);
    public static readonly Color OnColor = new Color(0.15f, 0.44f, 0.78f);
    public static readonly Color DangerColor = new Color(0.55f, 0.16f, 0.2f);
    public static readonly Color BossColor = new Color(0.42f, 0.14f, 0.24f);
    public static readonly Color MutedColor = new Color(0.09f, 0.1f, 0.16f);
    static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.09f, 0.95f);
    static readonly Color BorderColor = new Color(0.35f, 0.75f, 1f, 0.6f);
    static readonly Color AccentText = new Color(0.55f, 0.92f, 1f);
    static readonly Color NoteText = new Color(0.7f, 0.74f, 0.85f);

    const float Width = 580f, Pad = 14f, Gap = 6f;
    const float HeaderHeight = 56f, TabHeight = 46f, StatusHeight = 66f, CardHeight = 156f, CardImage = 128f;
    static readonly Color CardColor = new Color(0.07f, 0.1f, 0.18f, 0.98f);

    readonly GameObject root;
    readonly RectTransform view, content;
    readonly TextMeshProUGUI status;
    readonly GameObject card;
    readonly Image cardImage;
    readonly TextMeshProUGUI cardTitle, cardDetails;
    Action cancelCard;
    readonly Image[] tabImages;
    readonly TMP_FontAsset font;

    public bool Visible
    {
        get => root.activeSelf;
        set => root.SetActive(value);
    }

    float ContentWidth => Width - Pad * 2f;

    public DevConsoleUI(Transform owner, string title, string[] tabs, Action<int> onTab, Action onClose)
    {
        font = TMP_Settings.defaultFontAsset;
        root = new GameObject("DevConsoleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(owner, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // เหนือ HUD หน้าร้าน และจอมืดตอนเปลี่ยนด่าน
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var panel = Rect("Panel", root.transform);
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.sizeDelta = new Vector2(Width, -40f);
        panel.anchoredPosition = new Vector2(-16f, 0f);
        Fill(panel, PanelColor).gameObject.AddComponent<Outline>().effectColor = BorderColor;

        var header = Strip("Header", panel, 0f, HeaderHeight);
        var heading = Text(header, title, 28f, AccentText, TextAlignmentOptions.Left);
        Stretch(heading.rectTransform, Pad, 0f, 150f, 0f);
        var close = Button(header, "ปิด (F2)", DangerColor, onClose);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.sizeDelta = new Vector2(126f, 38f);
        closeRect.anchoredPosition = new Vector2(-Pad, 0f);

        var tabRow = Strip("Tabs", panel, HeaderHeight, TabHeight);
        var tabLayout = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset((int)Pad, (int)Pad, 2, 4);
        tabLayout.spacing = 4f;
        tabLayout.childControlWidth = tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;
        tabImages = new Image[tabs.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabImages[i] = Button(tabRow, tabs[i], ButtonColor, () => onTab(index)).image;
        }

        view = Rect("Scroll", panel);
        view.anchorMin = Vector2.zero;
        view.anchorMax = Vector2.one;
        view.offsetMin = new Vector2(0f, StatusHeight);
        view.offsetMax = new Vector2(0f, -(HeaderHeight + TabHeight + 4f));
        Fill(view, Color.clear); // รับล้อเมาส์ทั้งช่อง ไม่ใช่เฉพาะตรงปุ่ม
        view.gameObject.AddComponent<RectMask2D>();
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.viewport = view;

        content = Rect("Content", view);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)Pad, (int)Pad, 8, 18);
        layout.spacing = Gap;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        var bar = Rect("Status", panel);
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, StatusHeight);
        Fill(bar, MutedColor);
        status = Text(bar, "", 20f, Color.white, TextAlignmentOptions.MidlineLeft);
        Stretch(status.rectTransform, Pad, 4f, Pad, 4f);

        // การ์ดตัวอย่าง (โหมดคลิกวาง): รูปซ้าย ชื่อ/รายละเอียดขวา วิธีใช้กับปุ่มเลิกด้านล่าง อยู่เหนือแถบข้อความ
        var cardRect = Rect("Card", panel);
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = new Vector2(1f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.sizeDelta = new Vector2(0f, CardHeight);
        cardRect.anchoredPosition = new Vector2(0f, StatusHeight);
        Fill(cardRect, CardColor).gameObject.AddComponent<Outline>().effectColor = BorderColor;
        card = cardRect.gameObject;

        var frame = Rect("Picture", cardRect);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0.5f);
        frame.sizeDelta = Vector2.one * CardImage;
        frame.anchoredPosition = new Vector2(Pad, 0f);
        Fill(frame, MutedColor);
        var picture = Rect("Image", frame);
        Stretch(picture, 8f, 8f, 8f, 8f);
        cardImage = picture.gameObject.AddComponent<Image>();
        cardImage.preserveAspect = true;
        cardImage.raycastTarget = false;

        float left = Pad + CardImage + 12f;
        cardTitle = Text(cardRect, "", 24f, AccentText, TextAlignmentOptions.TopLeft);
        Stretch(cardTitle.rectTransform, left, CardHeight - 46f, Pad, 10f);
        cardTitle.enableAutoSizing = true;
        cardTitle.fontSizeMin = 16f;
        cardTitle.fontSizeMax = 24f;
        cardDetails = Text(cardRect, "", 17f, Color.white, TextAlignmentOptions.TopLeft);
        Stretch(cardDetails.rectTransform, left, 46f, Pad, 44f);
        cardDetails.enableAutoSizing = true;
        cardDetails.fontSizeMin = 12f;
        cardDetails.fontSizeMax = 17f;
        var hint = Text(cardRect, "คลิกซ้ายในฉาก = วาง (วางซ้ำได้)\nคลิกขวา / ESC = เลิก", 15f, NoteText, TextAlignmentOptions.BottomLeft);
        Stretch(hint.rectTransform, left, 8f, Pad + 118f, CardHeight - 44f);
        var stop = Button(cardRect, "เลิกวาง", DangerColor, () => cancelCard?.Invoke());
        var stopRect = (RectTransform)stop.transform;
        stopRect.anchorMin = stopRect.anchorMax = stopRect.pivot = new Vector2(1f, 0f);
        stopRect.sizeDelta = new Vector2(110f, 34f);
        stopRect.anchoredPosition = new Vector2(-Pad, 8f);
        card.SetActive(false);
    }

    // การ์ดตัวอย่างของที่กำลังจะวาง ช่องเลื่อนหดขึ้นให้การ์ดไม่บังปุ่ม
    public void ShowCard(Sprite sprite, string title, string details, Action onCancel)
    {
        cardImage.sprite = sprite;
        cardImage.enabled = sprite != null;
        cardTitle.text = title;
        cardDetails.text = details;
        cancelCard = onCancel;
        card.SetActive(true);
        view.offsetMin = new Vector2(0f, StatusHeight + CardHeight);
    }

    public void HideCard()
    {
        cancelCard = null;
        card.SetActive(false);
        view.offsetMin = new Vector2(0f, StatusHeight);
    }

    public void SelectTab(int index)
    {
        for (int i = 0; i < tabImages.Length; i++) tabImages[i].color = i == index ? OnColor : ButtonColor;
    }

    public void Status(string text) => status.text = text;

    // เลื่อนไปแล้วแค่ไหน (เก็บไว้ตอนสร้างแท็บเดิมใหม่ จะได้ไม่เด้งกลับขึ้นบนสุดทุกครั้งที่กดปุ่ม)
    public float Scrolled => content.anchoredPosition.y;
    public void ScrollBy(float delta) => Restore(Scrolled + delta); // เลื่อนด้วยคีย์บอร์ด (PageUp/PageDown)

    public void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i).gameObject;
            child.SetActive(false); // Destroy รอจบเฟรม ปิดไว้ก่อน layout จะได้ไม่นับ
            UnityEngine.Object.Destroy(child);
        }
    }

    public void Restore(float scrolled)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float max = Mathf.Max(0f, content.rect.height - view.rect.height);
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(scrolled, 0f, max));
    }

    public void Section(string title)
    {
        var text = Text(content, title, 24f, AccentText, TextAlignmentOptions.BottomLeft);
        text.gameObject.AddComponent<LayoutElement>().minHeight = 38f;
    }

    public void Note(string text) => Text(content, text, 18f, NoteText, TextAlignmentOptions.TopLeft);

    // ตารางปุ่มกว้างเท่ากันทุกช่อง
    public RectTransform Grid(int columns, float height = 50f)
    {
        var grid = Rect("Grid", content);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.spacing = new Vector2(Gap, Gap);
        layout.cellSize = new Vector2((ContentWidth - Gap * (columns - 1)) / columns, height);
        return grid;
    }

    // แถวเดียว ความกว้างแต่ละช่องตาม flex ที่ส่งให้ Button/Label (เช่น ชื่อพรกว้าง ปุ่มระดับแคบ)
    public RectTransform Row(float height = 46f)
    {
        var row = Rect("Row", content);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = Gap;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.gameObject.AddComponent<LayoutElement>().minHeight = height;
        return row;
    }

    public Button Button(Transform parent, string label, Color color, Action onClick, Color? textColor = null, float flex = 1f)
    {
        var rect = Rect("Button", parent);
        var image = Fill(rect, color);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None }; // ลูกศร/Space ไม่ไปกดปุ่มในคอนโซล
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            onClick?.Invoke();
        });
        rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = flex;

        var text = Text(rect, label, 22f, textColor ?? Color.white, TextAlignmentOptions.Center);
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = 22f;
        Stretch(text.rectTransform, 6f, 2f, 6f, 2f);
        return button;
    }

    // ช่องข้อความเฉย ๆ หน้าตาเหมือนปุ่มแต่กดไม่ได้
    public void Label(Transform parent, string label, float flex = 1f, Color? textColor = null)
    {
        var rect = Rect("Label", parent);
        Fill(rect, MutedColor).raycastTarget = false;
        rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = flex;
        var text = Text(rect, label, 20f, textColor ?? Color.white, TextAlignmentOptions.MidlineLeft);
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = 20f;
        Stretch(text.rectTransform, 10f, 2f, 6f, 2f);
    }

    // ---------- ตัวช่วยสร้างชิ้นส่วน ----------

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Strip(string name, RectTransform panel, float top, float height)
    {
        var rect = Rect(name, panel);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, -top);
        return rect;
    }

    static Image Fill(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    TextMeshProUGUI Text(Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var rect = Rect("Text", parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }
}
