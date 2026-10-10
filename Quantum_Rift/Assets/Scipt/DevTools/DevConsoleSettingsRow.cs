using TMPro;
using UnityEngine;

// เฝ้าดูสถานะจากตัวเมนู ไม่สร้างแถวหรือขยายหน้าต่างจนกว่าจะคลิกโลโก้ครบ 5 ครั้ง
public sealed class DevConsoleSettingsRow : MonoBehaviour
{
    TMP_Text value;
    SettingsMenu menu;

    public static void Attach(SettingsMenu menu)
    {
        if (menu == null) return;
        var toggle = menu.GetComponent<DevConsoleSettingsRow>();
        if (toggle == null) toggle = menu.gameObject.AddComponent<DevConsoleSettingsRow>();
        toggle.menu = menu;
        toggle.Refresh();
    }

    void OnEnable()
    {
        LanguageSettings.Changed += Refresh;
        DevConsole.AccessChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        LanguageSettings.Changed -= Refresh;
        DevConsole.AccessChanged -= Refresh;
    }

    void Flip()
    {
        if (!DevConsole.Unlocked) return;
        DevConsole.Enabled = !DevConsole.Enabled;
        Refresh();
    }

    void Refresh()
    {
        if (value == null && menu != null && DevConsole.Unlocked)
        {
            // เพิ่มเพียงครั้งเดียว และใช้งานได้ทันทีโดยไม่ต้องโหลดฉากใหม่
            var button = SettingsExtraRow.Add(menu, "DevConsole", "DEV CONSOLE (F2)", "คอนโซลทดสอบ (F2)", out TMP_Text text);
            if (button != null)
            {
                value = text;
                button.onClick.AddListener(Flip);
            }
        }
        if (value == null) return;
        bool on = DevConsole.Enabled;
        value.text = LanguageSettings.IsThai ? (on ? "เปิด" : "ปิด") : (on ? "ON" : "OFF");
    }
}
