using TMPro;
using UnityEngine;

// แถวเปิด/ปิดคอนโซลทดสอบในหน้าตั้งค่า (ทั้งเมนูหลักและในเกม) สร้างตอนเริ่มฉากด้วย SettingsExtraRow
public sealed class DevConsoleSettingsRow : MonoBehaviour
{
    TMP_Text value;

    public static void Attach(SettingsMenu menu)
    {
        var button = SettingsExtraRow.Add(menu, "DevConsole", "DEV CONSOLE (F2)", "คอนโซลทดสอบ (F2)", out TMP_Text text);
        if (button == null) return;
        var toggle = button.gameObject.AddComponent<DevConsoleSettingsRow>();
        toggle.value = text;
        button.onClick.AddListener(toggle.Flip);
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
}
