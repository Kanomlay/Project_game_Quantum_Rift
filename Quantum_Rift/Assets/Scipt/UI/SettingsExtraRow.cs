using TMPro;
using UnityEngine;
using UnityEngine.UI;

// เพิ่มแถวในหน้าตั้งค่าตอนเริ่มฉาก (ทั้งเมนูหลักและในเกม) โดยโคลนแถวภาษาที่มีอยู่แล้ว หน้าตาจะได้เหมือนแถวอื่นเป๊ะ
// ไม่ต้องแก้ฉากหรือสั่ง Builder ใหม่ ทุกแถวที่เพิ่ม หน้าต่างสูงขึ้นหนึ่งแถว: ของเดิมเลื่อนขึ้นครึ่งแถว ปุ่ม Back เลื่อนลง
// ใช้โดยแถวคอนโซลทดสอบ (DevConsoleSettingsRow) และแถวเครดิต (CreditsWindow)
public static class SettingsExtraRow
{
    const float Step = 70f;
    const string Prefix = "Row_Extra_";

    // คืนปุ่มทางขวาของแถวใหม่ (ล้างของที่ติดมากับโคลนแล้ว) กับข้อความบนปุ่ม คืน null ถ้าเพิ่มไม่ได้หรือมีแถวนี้อยู่แล้ว
    public static Button Add(SettingsMenu menu, string id, string english, string thai, out TMP_Text value)
    {
        value = null;
        if (menu == null || menu.settingsPanel == null) return null;
        var panel = menu.settingsPanel.transform;
        var languageRow = FindDeep(panel, "Row_Language") as RectTransform;
        var languageBox = FindDeep(panel, "Button_Language") as RectTransform;
        var window = languageRow != null ? languageRow.parent as RectTransform : null;
        if (window == null || languageBox == null)
        {
            Debug.LogWarning("หน้าตั้งค่าไม่มีแถวภาษา (Row_Language / Button_Language) เลยเพิ่มแถว " + id + " ไม่ได้");
            return null;
        }
        if (window.Find(Prefix + id) != null) return null;

        int added = 0;
        foreach (RectTransform child in window)
        {
            if (child.name.StartsWith(Prefix)) added++;
            if (child.anchorMin == child.anchorMax) child.anchoredPosition += Vector2.up * Step * 0.5f; // พื้นหลังที่ยืดเต็มหน้าต่างไม่ต้องเลื่อน
        }
        window.sizeDelta += Vector2.up * Step;

        var row = Object.Instantiate(languageRow, window);
        row.name = Prefix + id;
        row.anchoredPosition = languageRow.anchoredPosition + Vector2.down * Step * (added + 1);
        var box = Object.Instantiate(languageBox, window);
        box.name = "Button_Extra_" + id;
        box.anchoredPosition = languageBox.anchoredPosition + Vector2.down * Step * (added + 1);
        var close = window.Find("Button_CloseSettings") as RectTransform;
        if (close != null) close.anchoredPosition += Vector2.down * Step;

        var labelNode = row.Find("Label");
        var label = labelNode != null ? labelNode.GetComponent<TMP_Text>() : null;
        var localized = label != null ? label.GetComponent<LocalizedText>() : null;
        if (localized != null)
        {
            localized.englishText = english;
            localized.thaiText = thai;
            localized.Apply();
        }
        else if (label != null) label.text = thai;

        var valueNode = box.Find("Value");
        value = valueNode != null ? valueNode.GetComponent<TMP_Text>() : null;
        var button = box.GetComponent<Button>();
        if (button != null) button.onClick = new Button.ButtonClickedEvent(); // ตัดปุ่มสลับภาษาที่ติดมากับโคลนออก
        return button;
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
