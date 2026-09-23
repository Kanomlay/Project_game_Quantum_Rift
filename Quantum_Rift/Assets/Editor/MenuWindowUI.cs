using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ตัวช่วยสร้างหน้าต่างเมนูในเกม (Pause, หน้าสรุป, ตั้งค่า) ให้หน้าตาเหมือนกันทุกหน้า
// ใช้สีจาก MenuUIPack และภาพปุ่มชุด v3 ชุดเดียวกับที่เพื่อนใช้ในเมนูหลัก
//
// จำไว้ว่าชิ้นไหนเพิ่งสร้างใหม่ในรอบนี้ คนเรียกจึงตั้งตำแหน่ง/ขนาด/สี ให้เฉพาะของใหม่ได้
// ของที่มีอยู่แล้วจะไม่โดนทับ ผู้ใช้ลากปรับเองใน Scene view ได้โดยสั่ง build ซ้ำกี่ครั้งก็ไม่หาย
public sealed class MenuWindowUI
{
    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    const float BorderThickness = 6f;

    readonly HashSet<GameObject> created = new HashSet<GameObject>();
    readonly string undoName;

    public MenuWindowUI(string undoName)
    {
        this.undoName = undoName;
    }

    public bool IsNew(Component component) => created.Contains(component.gameObject);

    // แผ่นคลุมทั้งจอ กันเมาส์ทะลุไปโดนของหลังฉากตอนหน้าต่างเปิดอยู่
    public Image EnsureBackdrop(Transform canvas, string name)
    {
        var panel = EnsureImage(canvas, name);
        if (IsNew(panel))
        {
            Stretch(panel.rectTransform);
            panel.color = MenuUIPack.Backdrop;
        }
        panel.raycastTarget = true;
        return panel;
    }

    // ตัวหน้าต่าง ทำเป็นกรอบม่วงซ้อนผิวเข้ม เพราะแพ็กมีแต่ภาพปุ่ม ไม่มีภาพกรอบหน้าต่างมาให้
    public Image EnsureWindow(Transform panel, string name, Vector2 size)
    {
        var window = EnsureImage(panel, name);
        if (IsNew(window))
        {
            Place(window.rectTransform, Vector2.zero, size);
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

        return window;
    }

    // คำบนปุ่มตามที่เพื่อนใช้ในฉากจริง (Code-Game) ให้ปุ่มที่สร้างใหม่พูดคำเดียวกับปุ่มเดิม
    static readonly Dictionary<string, (string english, string thai)> ButtonLabels = new Dictionary<string, (string, string)>
    {
        { "Start", ("START", "เริ่มเกม") },
        { "Continue", ("CONTINUE", "ดำเนินการต่อ") },
        { "Settings", ("SETTINGS", "ตั้งค่า") },
        { "Resume", ("RESUME", "เล่นต่อ") },
        { "Back", ("BACK", "ย้อนกลับ") },
        { "Quit", ("QUIT", "ออกจากเกม") },
    };

    public Button EnsureMenuButton(Transform parent, string name, string word, float y, float width)
    {
        var image = EnsureImage(parent, name);
        var button = EnsureComponent<Button>(image.gameObject);
        button.targetGraphic = image;

        // ปุ่มที่มีอยู่แล้วห้ามแตะหน้าตา: เพื่อนเปลี่ยนเป็นปุ่มชุด v3 + ป้าย TMP ไว้ในฉากแล้ว
        // ถ้าเขียนทับด้วยภาพแบบเก่าจะได้ตัวหนังสือซ้อนสองชั้นและสถานะชี้/กดหาย
        // คืนปุ่มไปให้ต่อสาย onClick อย่างเดียวพอ
        if (!IsNew(image)) return button;

        ApplyApprovedButtonStyle(image, button, word, y, width);
        return button;
    }

    // สร้างปุ่มใหม่ให้หน้าตาเหมือนที่ MainMenuColorStatesBuilder ของเพื่อนทำ:
    // ภาพปุ่มเปล่า 4 สถานะแบบ Sprite Swap + ป้ายข้อความแยกที่สลับภาษาได้
    void ApplyApprovedButtonStyle(Image image, Button button, string word, float y, float width)
    {
        var normal = LoadButtonState("Normal");
        image.sprite = normal;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        float aspect = normal.rect.height / Mathf.Max(1f, normal.rect.width);
        Place(image.rectTransform, new Vector2(0f, y), new Vector2(width, width * aspect));

        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = LoadButtonState("Hover"),
            selectedSprite = LoadButtonState("Hover"),
            pressedSprite = LoadButtonState("Pressed"),
            disabledSprite = LoadButtonState("Disabled"),
        };

        (string english, string thai) words = ButtonLabels.TryGetValue(word, out var pair) ? pair : (word.ToUpperInvariant(), word);
        var label = EnsureText(image.transform, "StateLabel", words.english, words.thai, 42f);
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true; // คำไทยยาวกว่าอังกฤษ ปล่อยให้ย่อเองไม่ล้นปุ่ม
        label.fontSizeMin = 14f;
        label.fontSizeMax = 42f;
        Place(label.rectTransform, Vector2.zero, new Vector2(width * 0.61f, width * aspect * 0.46f));

        var feedback = EnsureComponent<MenuButtonLabelFeedback>(image.gameObject);
        feedback.button = button;
        feedback.label = label;
        feedback.restingPosition = Vector2.zero;
    }

    static Sprite LoadButtonState(string state)
    {
        string path = $"{MainMenuColorStatesBuilder.Folder}/Button-{state}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.InvalidOperationException($"ไม่เจอภาพปุ่ม {path}");
        return sprite;
    }

    // ป้ายที่มีคำแปลไทย จะติด LocalizedText ให้ด้วย เปลี่ยนภาษาในหน้าตั้งค่าแล้วสลับเอง
    public TextMeshProUGUI EnsureText(Transform parent, string name, string english, string thai, float fontSize, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var text = EnsureText(parent, name, english, fontSize, align);

        var localized = EnsureComponent<LocalizedText>(text.gameObject);
        localized.englishText = english;
        localized.thaiText = thai;
        return text;
    }

    public TextMeshProUGUI EnsureText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var text = EnsureComponent<TextMeshProUGUI>(EnsureRect(parent, name).gameObject);

        // ข้อความ/ขนาดฟอนต์/สี ตั้งให้ตอนสร้างครั้งแรกพอ หลังจากนั้นเป็นของผู้ใช้
        if (IsNew(text))
        {
            if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = MenuUIPack.TextMain;
        }
        text.raycastTarget = false;
        return text;
    }

    public Image EnsureImage(Transform parent, string name) => EnsureComponent<Image>(EnsureRect(parent, name).gameObject);

    public RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, undoName);
            go.transform.SetParent(parent, false);
            created.Add(go);
            return (RectTransform)go.transform;
        }

        // เผื่อกรณีที่เคยสร้างมือด้วย Create Empty ซึ่งได้ Transform ธรรมดามา ไม่ใช่ RectTransform
        return existing as RectTransform ?? Undo.AddComponent<RectTransform>(existing.gameObject);
    }

    public T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    public static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // ต่อ onClick แบบ persistent (เห็นใน Inspector เหมือนลากเอง) ล้างของเดิมก่อนกันสั่งซ้ำแล้วได้สายซ้อน
    public static void WireButton(Button button, UnityAction call)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, call);
        EditorUtility.SetDirty(button);
    }
}
