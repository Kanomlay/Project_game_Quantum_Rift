using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ตัวช่วยสร้างหน้าต่างเมนูในเกม (Pause, หน้าสรุป, ตั้งค่า) ให้หน้าตาเหมือนกันทุกหน้า
// ใช้สีกับภาพปุ่มจาก MenuUIPack
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

    public Button EnsureMenuButton(Transform parent, string name, string word, float y, float width)
    {
        var image = EnsureImage(parent, name);

        // ภาพปุ่มผูกกับค่า Language ในแพ็ก ตรงนี้จึงเซ็ตทุกครั้ง สลับ EN/TH แล้วสั่งซ้ำได้เลย
        image.sprite = MenuUIPack.LoadButton(word);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        // คำนวณความสูงจากสัดส่วนภาพ ปุ่มจะได้ไม่ยืดเพี้ยน
        if (IsNew(image)) Place(image.rectTransform, new Vector2(0f, y), new Vector2(width, width / MenuUIPack.ButtonAspect));

        var button = EnsureComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        // ภาพปุ่มมีตัวหนังสือในตัว ถ้าใช้ Sprite Swap ไปปุ่มเปล่าตอน hover ตัวหนังสือจะหายไป จึงใช้ไล่สีแทน
        button.transition = Selectable.Transition.ColorTint;

        // เก็บภาพทั้งสองภาษาไว้ที่ปุ่ม ตอนเล่นจะสลับเองเมื่อผู้เล่นเปลี่ยนภาษาในหน้าตั้งค่า
        var localized = EnsureComponent<LocalizedImage>(image.gameObject);
        localized.englishSprite = MenuUIPack.LoadButton(word, "EN");
        localized.thaiSprite = MenuUIPack.LoadButton(word, "TH");

        return button;
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
