using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// เมนูหลักเมื่อมีเซฟค้าง (RunSave): เพิ่มปุ่ม "เล่นต่อ" เหนือปุ่มเริ่มเกม และถามยืนยันก่อนเริ่มเกมใหม่ทับเซฟ
// สร้างตอนเริ่มฉากโดยโคลนปุ่มเริ่มเกมเดิม ไม่ต้องแก้ฉาก ไม่มีเซฟ = เมนูหน้าตาเหมือนเดิมทุกอย่าง
// เกาะอยู่บนกล่องปุ่มของเมนู: ทุกครั้งที่หน้าเมนูหลักกลับมาเปิด (OnEnable) จะเช็คเซฟใหม่ ลบเซฟไปแล้วปุ่มก็หาย
public sealed class ContinueMenu : MonoBehaviour
{
    const float ShiftDown = 30f; // มีปุ่มที่ 4 แล้วกล่องปุ่มเลื่อนลงนิดหนึ่ง ปุ่มบนสุดจะได้ไม่ชนโลโก้

    GameObject continueButton;
    RectTransform container;
    Vector2 restingPosition;
    static TMP_Text fontSource;

    public static void Attach(MainMenuController menu)
    {
        if (menu == null || menu.mainMenuUI == null) return;
        var source = menu.mainMenuUI.transform.Find("MenuButtonContainer/Button_Start") as RectTransform;
        var start = source != null ? source.GetComponent<Button>() : null;
        if (start == null || source.parent.GetComponent<ContinueMenu>() != null) return;
        fontSource = source.GetComponentInChildren<TMP_Text>(true);

        var clone = Instantiate(source, source.parent);
        clone.name = "Button_Continue";
        clone.SetSiblingIndex(source.GetSiblingIndex()); // เหนือปุ่มเริ่มเกม
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent(); // ตัดคำสั่งเริ่มเกมที่ติดมากับโคลน
        button.onClick.AddListener(Continue);
        var label = clone.GetComponentInChildren<LocalizedText>(true);
        if (label != null)
        {
            label.englishText = "CONTINUE";
            label.thaiText = "เล่นต่อ";
            label.Apply();
        }

        // ปุ่มเริ่มเกม: ถามก่อนถ้ามีเซฟ (ตัวฟังก์ชัน OnNewGameClicked ยังเปิดหน้าเลือกอาชีพตรง ๆ เหมือนเดิม)
        for (int i = 0; i < start.onClick.GetPersistentEventCount(); i++)
            start.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        start.onClick.AddListener(() => { if (!AskBeforeNewGame(menu.OnNewGameClicked)) menu.OnNewGameClicked(); });

        var view = source.parent.gameObject.AddComponent<ContinueMenu>();
        view.continueButton = clone.gameObject;
        view.container = (RectTransform)source.parent;
        view.restingPosition = view.container.anchoredPosition;
        view.Refresh();
    }

    void OnEnable() { Refresh(); }

    void Refresh()
    {
        if (continueButton == null) return;
        bool saved = RunSave.HasSave;
        continueButton.SetActive(saved);
        container.anchoredPosition = restingPosition + (saved ? Vector2.down * ShiftDown : Vector2.zero);
    }

    static void Continue()
    {
        if (!RunSave.BeginResume())
        {
            // เซฟหายหรือใช้ไม่ได้แล้วระหว่างที่อยู่หน้าเมนู ปุ่มก็หายตาม
            var view = FindFirstObjectByType<ContinueMenu>();
            if (view != null) view.Refresh();
            return;
        }
        MapManager.startOverride = null;
        Time.timeScale = 1f;
        PauseManager.isGamePaused = false;
        SceneManager.LoadScene("GameScene");
    }

    // มีเซฟค้าง = เปิดหน้าต่างยืนยันแล้วคืน true (ตกลง: ลบเซฟแล้วเรียก start) ไม่มีเซฟ = คืน false ให้คนเรียกไปต่อเอง
    public static bool AskBeforeNewGame(Action start)
    {
        if (!RunSave.TryDescribe(out string where)) return false;
        NewGameConfirm.Show(where, fontSource, () =>
        {
            RunSave.Delete();
            start?.Invoke();
        });
        return true;
    }
}

// หน้าต่างยืนยันเริ่มเกมใหม่ทับเซฟ สร้างจากโค้ดทั้งหมด (แบบเดียวกับ CreditsWindow) ESC = ยกเลิก
sealed class NewGameConfirm : MonoBehaviour
{
    static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.8f);
    static readonly Color Warning = new Color(1f, 0.62f, 0.5f);
    static NewGameConfirm open;

    public static void Show(string where, TMP_Text like, Action confirmed)
    {
        if (open != null) return;
        var go = new GameObject("NewGameConfirm", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        open = go.AddComponent<NewGameConfirm>();
        open.Build(where, like, confirmed);
    }

    void Build(string where, TMP_Text like, Action confirmed)
    {
        bool thai = LanguageSettings.IsThai;
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var backdrop = Rect("Backdrop", transform, Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.gameObject.AddComponent<Image>().color = Backdrop; // กันคลิกทะลุไปโดนเมนูข้างหลัง

        var panel = Rect("Panel", transform, Vector2.zero, new Vector2(860f, 420f));
        panel.gameObject.AddComponent<Image>().color = QuantumUiSkin.Ink;
        QuantumUiSkin.Frame(panel, 24f);

        Text(panel, like, thai ? "เริ่มเกมใหม่?" : "START A NEW GAME?", 46f, QuantumUiSkin.Cyan, new Vector2(0f, 140f), new Vector2(780f, 70f));
        Text(panel, like, (thai ? "มีเกมที่เล่นค้างไว้: " : "Run in progress: ") + where, 30f, Color.white, new Vector2(0f, 58f), new Vector2(780f, 50f));
        Text(panel, like, thai ? "ถ้าเริ่มเกมใหม่ ความคืบหน้านี้จะถูกลบ" : "Starting a new game will erase it.", 28f, Warning,
             new Vector2(0f, 6f), new Vector2(780f, 46f));

        Choice(panel, like, thai ? "เริ่มเกมใหม่" : "NEW GAME", new Vector2(-190f, -120f), () => { Close(); confirmed?.Invoke(); });
        Choice(panel, like, thai ? "ยกเลิก" : "CANCEL", new Vector2(190f, -120f), Close);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Close()
    {
        if (open == this) open = null;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (open == this) open = null; // เปลี่ยนฉากระหว่างเปิดอยู่
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static TMP_Text Text(Transform parent, TMP_Text like, string value, float size, Color color, Vector2 position, Vector2 box)
    {
        var text = Rect("Text", parent, position, box).gameObject.AddComponent<TextMeshProUGUI>();
        // ตัวอักษรชุดเดียวกับปุ่มเมนูหลัก (มีภาษาไทย)
        if (like != null) { text.font = like.font; text.fontSharedMaterial = like.fontSharedMaterial; }
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = size;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void Choice(Transform parent, TMP_Text like, string label, Vector2 position, UnityAction clicked)
    {
        var rect = Rect("Button", parent, position, new Vector2(340f, 84f));
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(clicked);
        var text = Text(rect, like, label, 32f, Color.white, Vector2.zero, new Vector2(310f, 64f));
        QuantumUiSkin.Button(button, 10f);
        text.transform.SetAsLastSibling(); // ข้อความอยู่เหนือกรอบปุ่ม
    }
}
