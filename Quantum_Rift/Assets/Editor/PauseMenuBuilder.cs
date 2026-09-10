using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// สร้างหน้าต่าง Pause (กด ESC) ในฉากเกม แล้วต่อปุ่มเข้ากับ PauseManager ให้เสร็จในตัว
// ใช้ภาพปุ่มจากชุด QuantumRift-Menu-Logo-TH-EN-v1 ชุดเดียวกับเมนูหลัก
//
// สั่งซ้ำได้: ตำแหน่ง/ขนาด/สี จะถูกตั้งให้เฉพาะชิ้นที่เพิ่งสร้างใหม่เท่านั้น
// ของที่มีอยู่แล้วจะไม่โดนทับ จะได้ลากปรับเองใน Scene view โดยไม่ต้องกลัวว่าสั่งซ้ำแล้วค่าที่ปรับหาย
public static class PauseMenuBuilder
{
    const string ScenePath = "Assets/Scenes/GameScene.unity";
    const string CanvasName = "UI";
    const string PanelName = "PauseMenu_Panel";

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 WindowSize = new Vector2(560f, 560f);
    const float ButtonWidth = 420f;
    const float BorderThickness = 6f;

    // ชิ้นที่สร้างใหม่ในรอบนี้ มีแค่ชิ้นพวกนี้ที่จะถูกจัดวางให้ ที่เหลือปล่อยไว้ตามที่ผู้ใช้ปรับ
    static readonly HashSet<GameObject> freshlyCreated = new HashSet<GameObject>();

    [MenuItem("Tools/Quantum Rift/Build Pause Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้างหน้าต่าง Pause");

        freshlyCreated.Clear();
        MenuUIPack.FixImportSettings();

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("เซฟฉากที่เปิดอยู่ก่อน แล้วค่อยสั่งสร้างหน้าต่าง Pause");
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        var canvas = GameObject.Find(CanvasName);
        if (canvas == null || canvas.GetComponent<Canvas>() == null)
            throw new InvalidOperationException($"ไม่เจอ Canvas ชื่อ {CanvasName} ใน {ScenePath}");

        // 1) แผ่นคลุมทั้งจอ กันเมาส์ทะลุไปโดนของหลังฉากตอนพักเกม
        var panel = EnsureImage(canvas.transform, PanelName);
        if (IsNew(panel))
        {
            Stretch(panel.rectTransform);
            panel.color = MenuUIPack.Backdrop;
        }
        panel.raycastTarget = true;

        // 2) ตัวหน้าต่าง ทำเป็นกรอบม่วงซ้อนผิวเข้ม เพราะแพ็กมีแต่ภาพปุ่ม ไม่มีภาพกรอบหน้าต่างมาให้
        var window = EnsureImage(panel.transform, "PauseWindow");
        if (IsNew(window))
        {
            Place(window.rectTransform, Center, Vector2.zero, WindowSize);
            window.color = MenuUIPack.Border;
        }

        var inner = EnsureImage(window.transform, "WindowInner");
        if (IsNew(inner))
        {
            Stretch(inner.rectTransform, BorderThickness);
            inner.color = MenuUIPack.Surface;
            inner.transform.SetAsFirstSibling(); // เป็นพื้นหลัง ต้องวาดก่อนตัวหนังสือกับปุ่ม
        }
        inner.raycastTarget = false;

        // 3) หัวหน้าต่าง: ไอคอนพักเกมจากแพ็ก + ข้อความ
        var icon = EnsureImage(window.transform, "Pause_Icon");
        if (IsNew(icon)) Place(icon.rectTransform, Center, new Vector2(0f, 210f), new Vector2(72f, 72f));
        icon.sprite = MenuUIPack.Load($"{MenuUIPack.SpriteFolder}/Icon-Pause-Focused.png");
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var title = EnsureText(window.transform, "Pause_Title", "PAUSED", 56f);
        if (IsNew(title)) Place(title.rectTransform, Center, new Vector2(0f, 140f), new Vector2(WindowSize.x, 70f));

        // 4) ปุ่มสามตัว ใช้ภาพปุ่มที่มีตัวหนังสือในตัว
        var resume = EnsureMenuButton(window.transform, "Button_Resume", "Resume", 50f);
        var settings = EnsureMenuButton(window.transform, "Button_Settings", "Settings", -80f);
        var quit = EnsureMenuButton(window.transform, "Button_Quit", "Quit", -210f);

        // 5) หา (หรือสร้าง) ตัวคุมแล้วต่อสายให้ครบ
        var manager = UnityEngine.Object.FindObjectOfType<PauseManager>(true);
        if (manager == null)
        {
            var host = new GameObject("PauseManager");
            Undo.RegisterCreatedObjectUndo(host, "Build Pause Menu");
            manager = Undo.AddComponent<PauseManager>(host);
        }
        manager.pauseMenuPanel = panel.gameObject;
        EditorUtility.SetDirty(manager);

        WireButton(resume, manager.ResumeGame);
        WireButton(settings, manager.OpenSettings);
        WireButton(quit, manager.ExitToMainMenu);

        // จอดำเปลี่ยนด่านต้องอยู่บนสุดเสมอ ไม่งั้นตอนวาปข้ามด่านจะเห็นฉากโผล่ใต้จอดำ
        panel.transform.SetAsLastSibling();
        var transition = canvas.transform.Find("TransitionScreen");
        if (transition != null) transition.SetAsLastSibling();

        // เปิดค้างไว้ให้เห็นใน Scene view จะได้ลากปรับต่อได้เลย
        // ตอนเล่นจริง PauseManager.Start() ปิดให้เองอยู่แล้ว ไม่ต้องกลัวว่าจะค้างหน้าจอ
        panel.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("สร้างหน้าต่าง Pause เสร็จแล้ว ลากปรับใน Scene view ได้เลย " +
                  "(ซ่อน/โชว์ด้วย Tools > Quantum Rift > Toggle Pause Menu Preview) แล้วกด Ctrl+S");
    }

    // เปิด/ปิดหน้าต่างชั่วคราวตอนแก้ฉาก จะได้ไม่ต้องไล่ติ๊ก checkbox เอง
    // จะปิดหรือเปิดค้างไว้ก็ไม่มีผลกับเกมจริง เพราะ PauseManager.Start() สั่งปิดให้ทุกครั้งที่เริ่มฉาก
    [MenuItem("Tools/Quantum Rift/Toggle Pause Menu Preview")]
    public static void TogglePreview()
    {
        var panel = FindPanel();
        if (panel == null)
        {
            Debug.LogWarning("ยังไม่มีหน้าต่าง Pause ในฉาก สั่ง Build Pause Menu ก่อน");
            return;
        }

        Undo.RecordObject(panel, "Toggle Pause Menu Preview");
        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(panel.scene);
    }

    static GameObject FindPanel()
    {
        // ถาม PauseManager ก่อน เพราะ GameObject.Find หาของที่ถูกปิดอยู่ไม่เจอ
        var manager = UnityEngine.Object.FindObjectOfType<PauseManager>(true);
        if (manager != null && manager.pauseMenuPanel != null) return manager.pauseMenuPanel;

        var canvas = GameObject.Find(CanvasName);
        var panel = canvas != null ? canvas.transform.Find(PanelName) : null;
        return panel != null ? panel.gameObject : null;
    }

    static Button EnsureMenuButton(Transform parent, string name, string word, float y)
    {
        var image = EnsureImage(parent, name);

        // ภาพปุ่มผูกกับค่า Language ในแพ็ก ตรงนี้จึงเซ็ตทุกครั้ง สลับ EN/TH แล้วสั่งซ้ำได้เลย
        image.sprite = MenuUIPack.LoadButton(word);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        // คำนวณความสูงจากสัดส่วนภาพ ปุ่มจะได้ไม่ยืดเพี้ยน
        if (IsNew(image))
            Place(image.rectTransform, Center, new Vector2(0f, y), new Vector2(ButtonWidth, ButtonWidth / MenuUIPack.ButtonAspect));

        var button = EnsureComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        // ภาพปุ่มมีตัวหนังสือในตัว ถ้าใช้ Sprite Swap ไปปุ่มเปล่าตอน hover ตัวหนังสือจะหายไป จึงใช้ไล่สีแทน
        button.transition = Selectable.Transition.ColorTint;
        return button;
    }

    // ต่อ onClick แบบ persistent (เห็นใน Inspector เหมือนลากเอง) ล้างของเดิมก่อนกันสั่งซ้ำแล้วได้สายซ้อน
    static void WireButton(Button button, UnityAction call)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, call);
        EditorUtility.SetDirty(button);
    }

    static bool IsNew(Component component) => freshlyCreated.Contains(component.gameObject);

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build Pause Menu");
            go.transform.SetParent(parent, false);
            freshlyCreated.Add(go);
            return (RectTransform)go.transform;
        }

        // เผื่อกรณีที่เคยสร้างมือด้วย Create Empty ซึ่งได้ Transform ธรรมดามา ไม่ใช่ RectTransform
        return existing as RectTransform ?? Undo.AddComponent<RectTransform>(existing.gameObject);
    }

    static Image EnsureImage(Transform parent, string name)
    {
        return EnsureComponent<Image>(EnsureRect(parent, name).gameObject);
    }

    static TextMeshProUGUI EnsureText(Transform parent, string name, string content, float fontSize)
    {
        var rect = EnsureRect(parent, name);
        var text = EnsureComponent<TextMeshProUGUI>(rect.gameObject);

        // ข้อความ/ขนาดฟอนต์/สี ตั้งให้ตอนสร้างครั้งแรกพอ หลังจากนั้นเป็นของผู้ใช้
        if (IsNew(text))
        {
            if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = MenuUIPack.TextMain;
        }
        text.raycastTarget = false;
        return text;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
