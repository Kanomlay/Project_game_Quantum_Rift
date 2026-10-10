using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// สร้างเฉพาะในเมนูหลัก ใช้กรอบและฟอนต์จาก START เดิม ไม่ต้องแก้หรือบันทึกฉาก
public sealed class RunHistoryWindow : MonoBehaviour
{
    const int PageSize = 6; // แบ่งหน้าแทนการบีบข้อความ รองรับทั้งจอ 16:9 และ 4:3
    static RunHistoryWindow open;
    public static bool IsOpen => open != null;
    TMP_FontAsset font;
    RectTransform panel;
    RunHistory.Data data;
    int page;
    GameObject returnSelection;

    public static void Attach(MainMenuController menu)
    {
        if (menu == null || menu.mainMenuUI == null || menu.mainMenuUI.transform.Find("Button_RunHistory") != null) return;
        var source = menu.mainMenuUI.transform.Find("MenuButtonContainer/Button_Start") as RectTransform;
        if (source == null) return;
        var clone = Instantiate(source, menu.mainMenuUI.transform);
        clone.name = "Button_RunHistory";
        clone.anchorMin = clone.anchorMax = clone.pivot = Vector2.zero;
        clone.anchoredPosition = new Vector2(42, 38);
        clone.sizeDelta = new Vector2(370, 88);
        clone.localScale = Vector3.one;
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(26, 8);
        label.rectTransform.offsetMax = new Vector2(-26, -8);
        label.enableAutoSizing = true;
        label.fontSizeMin = 27;
        label.fontSizeMax = 34;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        var localized = label.GetComponent<LocalizedText>() ?? label.gameObject.AddComponent<LocalizedText>();
        localized.thaiText = "ประวัติการเล่น";
        localized.englishText = "RUN HISTORY";
        localized.Apply();
        var feedback = button.GetComponent<MenuButtonLabelFeedback>();
        if (feedback != null) { feedback.button = button; feedback.label = label; feedback.restingPosition = label.rectTransform.anchoredPosition; }
        button.onClick.AddListener(() => Show(label.font, button.gameObject));
    }

    public static void Show(TMP_FontAsset like, GameObject returnTo = null)
    {
        if (IsOpen || GameHelpWindow.IsOpen || MonsterCollectionWindow.IsOpen || CreditsWindow.BlocksEscape
            || (SettingsMenu.instance != null && SettingsMenu.instance.IsOpen)) return;
        var go = new GameObject("RunHistoryWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        open = go.AddComponent<RunHistoryWindow>();
        open.font = like != null ? like : TMP_Settings.defaultFontAsset;
        open.returnSelection = returnTo;
        open.data = RunHistory.Load();
        open.Build();
    }

    void OnEnable() { LanguageSettings.Changed += Refresh; }
    void OnDisable() { LanguageSettings.Changed -= Refresh; }
    void OnDestroy() { if (open == this) open = null; }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (Input.GetKeyDown(KeyCode.PageDown)) ChangePage(1);
        else if (Input.GetKeyDown(KeyCode.PageUp)) ChangePage(-1);
    }

    void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000; // คู่มือเดิมอยู่ชั้น 30000 ต้องกันคลิกและคีย์บอร์ดเหนือเมนูทุกส่วน
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var backdrop = Rect("Backdrop", transform, Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .86f);
        panel = Rect("Window", transform, Vector2.zero, new Vector2(1480, 780));
        panel.gameObject.AddComponent<Image>().color = QuantumUiSkin.Ink;
        QuantumUiSkin.Frame(panel, 24);
        Refresh();
    }

    int PageCount => Mathf.Max(1, (data.entries.Count + PageSize - 1) / PageSize);
    public void ChangePage(int direction)
    {
        page = Mathf.Clamp(page + direction, 0, PageCount - 1);
        Refresh();
    }

    void Refresh()
    {
        if (panel == null || data == null) return;
        // ปิดชิ้นเก่าทันที ก่อน Destroy ท้ายเฟรม ไม่ให้รับคลิกหรือทับข้อความชุดใหม่
        foreach (Transform child in panel)
            if (child.name != "__CleanFrame") { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        bool thai = LanguageSettings.IsThai;
        Text("Title", panel, thai ? "ประวัติการเล่น" : "RUN HISTORY", 40, QuantumUiSkin.Cyan, new Vector2(-170, 324), new Vector2(1040, 64));
        Text("Description", panel, thai ? "20 รอบล่าสุด · บันทึกเมื่อแพ้หรือเคลียร์เกม · ไม่รวมสนามฝึกและโหมดทดสอบ" : "Latest 20 completed runs · Defeats and game clears · Training and test runs excluded",
             23, new Color32(187, 181, 208, 255), new Vector2(0, 268), new Vector2(1380, 44));
        var close = Button("Close", panel, thai ? "ปิด [Esc]" : "CLOSE [Esc]", new Vector2(596, 324), new Vector2(210, 62), Close);
        Stat("Total", -460, thai ? "รอบที่เล่นจบทั้งหมด" : "COMPLETED RUNS", data.totalRuns.ToString());
        Stat("Wins", 0, thai ? "เคลียร์เกมแล้ว" : "GAME CLEARS", data.totalWins.ToString());
        Stat("Best", 460, thai ? "เวลาชนะที่เร็วที่สุด" : "FASTEST CLEAR", data.bestWinSeconds >= 0 ? FormatTime(data.bestWinSeconds) : "—");
        string[] headings = thai ? new[] { "วันที่ / เวลา", "ผลการเล่น", "คลาส", "ด่านที่ไปถึง", "เวลาเล่น", "กำจัดศัตรู", "เหรียญเหลือ" }
                                 : new[] { "DATE / TIME", "RESULT", "CLASS", "STAGE REACHED", "PLAY TIME", "DEFEATED", "COINS LEFT" };
        float[] xs = { -572, -386, -214, 35, 273, 451, 619 };
        float[] widths = { 206, 150, 174, 290, 140, 164, 138 };
        for (int i = 0; i < headings.Length; i++) Text("Heading" + i, panel, headings[i], 23, QuantumUiSkin.Cyan, new Vector2(xs[i], 130), new Vector2(widths[i], 38));
        if (data.entries.Count == 0)
        {
            Text("Empty", panel, thai ? "ยังไม่มีประวัติการเล่น\nเล่นรอบจริงจนแพ้หรือเคลียร์เกม แล้วผลจะถูกบันทึกที่นี่" : "NO RUNS RECORDED YET\nFinish a normal run with a defeat or game clear to record it here.",
                 30, Color.white, new Vector2(0, -50), new Vector2(1280, 180));
        }
        for (int i = 0; i < PageSize && page * PageSize + i < data.entries.Count; i++)
        {
            var entry = data.entries[page * PageSize + i];
            var row = Rect("Row" + i, panel, new Vector2(0, 78 - i * 70), new Vector2(1390, 64));
            row.gameObject.AddComponent<Image>().color = i % 2 == 0 ? QuantumUiSkin.Surface : new Color32(23, 23, 40, 255);
            string date = DateTimeOffset.TryParse(entry.finishedUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
                ? timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "—";
            string className = thai ? entry.classThai : entry.classEnglish;
            string[] cells = { date, entry.won ? (thai ? "เคลียร์เกม" : "CLEARED") : (thai ? "แพ้" : "DEFEAT"), className, entry.stage,
                               FormatTime(entry.seconds), entry.kills.ToString(), entry.remainingCoins.ToString() };
            for (int j = 0; j < cells.Length; j++)
            {
                Color color = j == 1 ? (entry.won ? new Color32(131, 239, 179, 255) : new Color32(249, 143, 153, 255)) : Color.white;
                var cell = Text("Cell" + j, row, cells[j], j == 0 ? 22 : 26, color, new Vector2(xs[j], 0), new Vector2(widths[j] - 8, 60));
                cell.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
        Text("Footer", panel, thai ? "เหรียญเป็นยอดที่เหลือตอนจบรอบ ไม่ใช่เงินสะสมข้ามรอบ" : "Coins show the amount left at run end, not a persistent wallet.",
             22, new Color32(187, 181, 208, 255), new Vector2(-220, -346), new Vector2(920, 42));
        Button("Previous", panel, "<", new Vector2(366, -346), new Vector2(70, 48), () => ChangePage(-1)).interactable = page > 0;
        Text("Page", panel, (page + 1) + " / " + PageCount, 26, Color.white, new Vector2(498, -346), new Vector2(174, 42));
        Button("Next", panel, ">", new Vector2(630, -346), new Vector2(70, 48), () => ChangePage(1)).interactable = page + 1 < PageCount;
        // ย้ายการเลือกคีย์บอร์ดเข้าในหน้าต่าง ป้องกัน Space กด START ที่ถูกบังอยู่
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
    }

    void Stat(string name, float x, string heading, string value)
    {
        var card = Rect(name, panel, new Vector2(x, 204), new Vector2(432, 82));
        card.gameObject.AddComponent<Image>().color = QuantumUiSkin.Surface;
        QuantumUiSkin.Frame(card, 6);
        Text("Label", card, heading, 22, QuantumUiSkin.Cyan, new Vector2(0, 19), new Vector2(410, 34));
        Text("Value", card, value, 31, Color.white, new Vector2(0, -19), new Vector2(410, 42));
    }

    public static string FormatTime(float seconds)
    {
        int whole = Mathf.FloorToInt(Mathf.Max(0, seconds));
        return whole >= 3600 ? string.Format("{0:00}:{1:00}:{2:00}", whole / 3600, whole / 60 % 60, whole % 60)
                             : string.Format("{0:00}:{1:00}", whole / 60, whole % 60);
    }

    public void Close()
    {
        if (open == this) open = null;
        gameObject.SetActive(false);
        if (EventSystem.current != null && returnSelection != null && returnSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(returnSelection);
        Destroy(gameObject);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    TMP_Text Text(string name, Transform parent, string value, float size, Color color, Vector2 position, Vector2 box)
    {
        var text = Rect(name, parent, position, box).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) { text.font = font; text.fontSharedMaterial = font.material; }
        text.text = value;
        text.fontStyle = FontStyles.Normal;
        text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(size, 20); text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = color; text.raycastTarget = false;
        return text;
    }

    Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        QuantumUiSkin.Button(button, 8);
        Text("Label", rect, label, 26, Color.white, Vector2.zero, size - new Vector2(12, 8));
        return button;
    }
}
