using TMPro;
using UnityEngine;
using UnityEngine.UI;

// แถวเปิด/ปิดคอนโซลทดสอบในหน้าตั้งค่า (ทั้งเมนูหลักและในเกม)
// โคลนแถวภาษาที่มีอยู่แล้วตอนเริ่มฉาก หน้าตาจะได้เหมือนแถวอื่นเป๊ะ ไม่ต้องแก้ฉากหรือสั่ง Builder ใหม่
// หน้าต่างสูงขึ้นหนึ่งแถว: ของเดิมเลื่อนขึ้นครึ่งแถว แถวใหม่อยู่ใต้แถวภาษา ปุ่ม Back เลื่อนลง
public sealed class DevConsoleSettingsRow : MonoBehaviour
{
    const float Step = 70f;

    TMP_Text value;

    public static void Attach(SettingsMenu menu)
    {
        if (menu == null || menu.settingsPanel == null) return;
        var panel = menu.settingsPanel.transform;
        var languageRow = FindDeep(panel, "Row_Language") as RectTransform;
        var languageBox = FindDeep(panel, "Button_Language") as RectTransform;
        var window = languageRow != null ? languageRow.parent as RectTransform : null;
        if (window == null || languageBox == null)
        {
            Debug.LogWarning("หน้าตั้งค่าไม่มีแถวภาษา (Row_Language / Button_Language) เลยไม่ได้ใส่แถวคอนโซลทดสอบ");
            return;
        }
        if (window.Find("Row_DevConsole") != null) return;

        foreach (RectTransform child in window)
            if (child.anchorMin == child.anchorMax) child.anchoredPosition += Vector2.up * Step * 0.5f; // พื้นหลังที่ยืดเต็มหน้าต่างไม่ต้องเลื่อน
        window.sizeDelta += Vector2.up * Step;

        var row = Instantiate(languageRow, window);
        row.name = "Row_DevConsole";
        row.anchoredPosition = languageRow.anchoredPosition + Vector2.down * Step;
        var box = Instantiate(languageBox, window);
        box.name = "Button_DevConsole";
        box.anchoredPosition = languageBox.anchoredPosition + Vector2.down * Step;
        var close = window.Find("Button_CloseSettings") as RectTransform;
        if (close != null) close.anchoredPosition += Vector2.down * Step;

        var label = row.Find("Label") != null ? row.Find("Label").GetComponent<TMP_Text>() : null;
        var localized = label != null ? label.GetComponent<LocalizedText>() : null;
        if (localized != null)
        {
            localized.englishText = "DEV CONSOLE (F2)";
            localized.thaiText = "คอนโซลทดสอบ (F2)";
            localized.Apply();
        }
        else if (label != null) label.text = "คอนโซลทดสอบ (F2)";

        var toggle = box.gameObject.AddComponent<DevConsoleSettingsRow>();
        toggle.value = box.Find("Value") != null ? box.Find("Value").GetComponent<TMP_Text>() : null;
        var button = box.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent(); // ตัดปุ่มสลับภาษาที่ติดมากับโคลนออก
            button.onClick.AddListener(toggle.Flip);
        }
        toggle.Refresh();
    }

    void OnEnable()
    {
        LanguageSettings.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        LanguageSettings.Changed -= Refresh;
    }

    void Flip()
    {
        DevConsole.Enabled = !DevConsole.Enabled;
        Refresh();
    }

    void Refresh()
    {
        if (value == null) return;
        bool on = DevConsole.Enabled;
        value.text = LanguageSettings.IsThai ? (on ? "เปิด" : "ปิด") : (on ? "ON" : "OFF");
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
