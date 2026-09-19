using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// สร้างหน้าตั้งค่า (ความดังเสียง + ภาษา) แล้วต่อเข้ากับ SettingsMenu ให้เสร็จในตัว
// ต้องมีทั้งในฉากเมนูหลัก (เปิดจากปุ่ม Setting) และฉากเกม (เปิดจากหน้า Pause) จึงแยกเป็นสองคำสั่ง
// ทำทีละฉากเพราะการเปิดฉากใหม่ทับจะทำให้ของที่ยังไม่เซฟในฉากเดิมหายไป
//
// สั่งซ้ำได้: ตำแหน่ง/ขนาด/สี ตั้งให้เฉพาะชิ้นที่เพิ่งสร้างใหม่ ของเดิมที่ปรับเองไว้จะไม่โดนทับ
public static class SettingsBuilder
{
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string MainMenuCanvasName = "Canvas";
    const string PanelName = "Settings_Panel";

    static readonly Vector2 WindowSize = new Vector2(620f, 520f);
    static readonly Vector2 RowSize = new Vector2(460f, 44f);
    const float CloseButtonWidth = 380f;
    const float LabelSize = 30f;

    [MenuItem("Tools/Quantum Rift/Build Settings Screen/Game Scene")]
    public static void BuildInGameScene()
    {
        var scene = GameSceneUI.OpenGameScene();
        var canvas = GameSceneUI.FindCanvas();

        var panel = BuildWindow(canvas.transform);
        GameSceneUI.KeepTransitionOnTop(canvas.transform, panel.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("สร้างหน้าตั้งค่าในฉากเกมแล้ว เปิดได้จากปุ่ม Settings ในหน้า Pause แล้วกด Ctrl+S");
    }

    [MenuItem("Tools/Quantum Rift/Build Settings Screen/Main Menu")]
    public static void BuildInMainMenu()
    {
        var scene = GameSceneUI.OpenScene(MainMenuScenePath);
        var canvas = GameSceneUI.FindCanvas(MainMenuCanvasName);

        BuildWindow(canvas.transform);
        LocalizeMenuLabels(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("สร้างหน้าตั้งค่าในเมนูหลักแล้ว เปิดได้จากปุ่ม Setting แล้วกด Ctrl+S");
    }

    static Image BuildWindow(Transform canvas)
    {
        var ui = new MenuWindowUI("Build Settings Screen");

        var panel = ui.EnsureBackdrop(canvas, PanelName);
        var window = ui.EnsureWindow(panel.transform, "SettingsWindow", WindowSize);

        var title = ui.EnsureText(window.transform, "Settings_Title", "SETTINGS", "ตั้งค่า", 48f);
        if (ui.IsNew(title)) MenuWindowUI.Place(title.rectTransform, new Vector2(0f, 190f), new Vector2(WindowSize.x, 70f));

        // แถวเสียง: ป้ายชื่อซ้าย ตัวเลขเปอร์เซ็นต์ขวา แล้วมีแถบเลื่อนอยู่ใต้แถว
        var volumeValue = EnsureRow(ui, window.transform, "Row_Volume", "VOLUME", "ความดังเสียง", "100%", 110f);
        var slider = EnsureVolumeSlider(ui, window.transform, 55f);

        // แถวภาษา: กดที่กล่องขวามือเพื่อสลับ EN / TH
        EnsureRow(ui, window.transform, "Row_Language", "LANGUAGE", "ภาษา", "", -40f);
        var languageButton = EnsureLanguageButton(ui, window.transform, -40f, out TextMeshProUGUI languageValue);

        // ปุ่มปิดใช้ภาพ Back เพราะเป็นการย้อนกลับไปหน้าเดิม ไม่ใช่ออกจากเกม
        var close = ui.EnsureMenuButton(window.transform, "Button_CloseSettings", "Back", -170f, CloseButtonWidth);

        var manager = GameSceneUI.EnsureManager<SettingsMenu>("SettingsMenu", "Build Settings Screen");
        manager.settingsPanel = panel.gameObject;
        manager.volumeSlider = slider;
        manager.volumeValueText = volumeValue;
        manager.languageValueText = languageValue;
        EditorUtility.SetDirty(manager);

        MenuWindowUI.WireButton(languageButton, manager.ToggleLanguage);
        MenuWindowUI.WireButton(close, manager.Close);

        // เปิดค้างไว้ให้เห็นใน Scene view จะได้ลากปรับต่อได้เลย
        // ตอนเล่นจริง SettingsMenu.Start() ปิดให้เองอยู่แล้ว
        panel.transform.SetAsLastSibling();
        panel.gameObject.SetActive(true);
        return panel;
    }

    // คืนค่าเป็นช่องค่าทางขวา เพราะ SettingsMenu เขียนทับเฉพาะค่า ส่วนป้ายชื่อเป็นข้อความตายตัว
    static TextMeshProUGUI EnsureRow(MenuWindowUI ui, Transform window, string name, string label, string labelThai, string value, float y)
    {
        var row = ui.EnsureRect(window, name);
        if (ui.IsNew(row)) MenuWindowUI.Place(row, new Vector2(0f, y), RowSize);

        var labelText = ui.EnsureText(row, "Label", label, labelThai, LabelSize, TextAlignmentOptions.Left);
        if (ui.IsNew(labelText)) MenuWindowUI.Stretch(labelText.rectTransform);

        var valueText = ui.EnsureText(row, "Value", value, LabelSize, TextAlignmentOptions.Right);
        if (ui.IsNew(valueText)) MenuWindowUI.Stretch(valueText.rectTransform);

        return valueText;
    }

    // ประกอบ Slider เองจากภาพสี่เหลี่ยม เพราะโปรเจกต์ยังไม่มีภาพรางกับปุ่มเลื่อน
    static Slider EnsureVolumeSlider(MenuWindowUI ui, Transform window, float y)
    {
        var root = ui.EnsureRect(window, "Slider_Volume");
        if (ui.IsNew(root)) MenuWindowUI.Place(root, new Vector2(0f, y), new Vector2(RowSize.x, 28f));

        var background = ui.EnsureImage(root, "Background");
        if (ui.IsNew(background))
        {
            MenuWindowUI.Stretch(background.rectTransform);
            background.color = MenuUIPack.Sunken;
        }

        // เว้นซ้ายขวาไว้เท่าครึ่งความกว้างปุ่มเลื่อน ปุ่มจะได้ไม่ล้นออกนอกราง
        var fillArea = ui.EnsureRect(root, "Fill Area");
        if (ui.IsNew(fillArea)) Inset(fillArea, 11f, 6f);
        var fill = ui.EnsureImage(fillArea, "Fill");
        if (ui.IsNew(fill))
        {
            MenuWindowUI.Stretch(fill.rectTransform);
            fill.color = MenuUIPack.Accent;
        }

        var handleArea = ui.EnsureRect(root, "Handle Slide Area");
        if (ui.IsNew(handleArea)) Inset(handleArea, 11f, 0f);
        var handle = ui.EnsureImage(handleArea, "Handle");
        if (ui.IsNew(handle))
        {
            // Slider เขียน anchor ของปุ่มเลื่อนเองตามค่าที่เลือก เหลือให้เรากำหนดแค่ว่าใหญ่กว่ารางเท่าไหร่
            MenuWindowUI.Stretch(handle.rectTransform);
            handle.rectTransform.sizeDelta = new Vector2(22f, 6f);
            handle.color = MenuUIPack.TextMain;
        }

        var slider = ui.EnsureComponent<Slider>(root.gameObject);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        return slider;
    }

    static void Inset(RectTransform rect, float x, float y)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = MenuWindowUI.Center;
        rect.offsetMin = new Vector2(x, y);
        rect.offsetMax = new Vector2(-x, -y);
    }

    static Button EnsureLanguageButton(MenuWindowUI ui, Transform window, float y, out TextMeshProUGUI valueText)
    {
        var box = ui.EnsureImage(window, "Button_Language");
        if (ui.IsNew(box))
        {
            MenuWindowUI.Place(box.rectTransform, new Vector2(RowSize.x / 2f - 70f, y), new Vector2(140f, 52f));
            box.color = MenuUIPack.Border;
        }

        valueText = ui.EnsureText(box.transform, "Value", "EN", 30f);
        if (ui.IsNew(valueText)) MenuWindowUI.Stretch(valueText.rectTransform);

        var button = ui.EnsureComponent<Button>(box.gameObject);
        button.targetGraphic = box;
        button.transition = Selectable.Transition.ColorTint;
        return button;
    }

    // ปุ่มเมนูหลักเป็นปุ่มเปล่า + ตัวหนังสือ TMP (ไม่ใช่ภาพที่มีตัวอักษรในตัว) จึงต้องสลับที่ข้อความแทนภาพ
    static void LocalizeMenuLabels(Transform canvas)
    {
        Localize(canvas, "Button_Start", "START", "เริ่มเกม");
        Localize(canvas, "Start", "START", "เริ่มเกม"); // ปุ่มยืนยันในหน้าเลือกตัวละคร คนละตัวกับปุ่มเมนูหลัก
        Localize(canvas, "Button_Setting", "SETTINGS", "ตั้งค่า");
        Localize(canvas, "Button_Exit", "QUIT", "ออกจากเกม");
        Localize(canvas, "BACK", "BACK", "ย้อนกลับ");
    }

    static void Localize(Transform canvas, string buttonName, string english, string thai)
    {
        var button = FindDeep(canvas, buttonName);
        if (button == null)
        {
            Debug.LogWarning($"ไม่เจอปุ่ม {buttonName} ในเมนูหลัก ข้ามการใส่ข้อความสองภาษา");
            return;
        }

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            Debug.LogWarning($"ปุ่ม {buttonName} ไม่มีตัวหนังสือ TMP ข้ามไป");
            return;
        }

        var localized = label.GetComponent<LocalizedText>();
        if (localized == null) localized = Undo.AddComponent<LocalizedText>(label.gameObject);
        localized.englishText = english;
        localized.thaiText = thai;
        EditorUtility.SetDirty(localized);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
