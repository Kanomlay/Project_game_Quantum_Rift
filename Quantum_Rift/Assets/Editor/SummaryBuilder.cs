using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// สร้างหน้าสรุปผล (ตาย / ผ่านด่าน) ในฉากเกม แล้วต่อช่องทั้งหมดเข้ากับ SummaryManager ให้เสร็จในตัว
// หน้าตาใช้หน้าต่างชุดเดียวกับ Pause ผ่าน MenuWindowUI
//
// สั่งซ้ำได้: ตำแหน่ง/ขนาด/สี ตั้งให้เฉพาะชิ้นที่เพิ่งสร้างใหม่ ของเดิมที่ปรับเองไว้จะไม่โดนทับ
public static class SummaryBuilder
{
    const string PanelName = "Summary_Panel";

    static readonly Vector2 WindowSize = new Vector2(640f, 700f);
    const float ButtonWidth = 420f;
    const float RowWidth = 440f;   // ความกว้างของแถวสถิติในหน้าต่าง
    const float LabelSize = 30f;

    [MenuItem("Tools/Quantum Rift/Build Summary Screen")]
    public static void Build()
    {
        var scene = GameSceneUI.OpenGameScene();
        var canvas = GameSceneUI.FindCanvas();
        var ui = new MenuWindowUI("Build Summary Screen");

        var panel = ui.EnsureBackdrop(canvas.transform, PanelName);
        var window = ui.EnsureWindow(panel.transform, "SummaryWindow", WindowSize);

        // หัวเรื่อง SummaryManager เปลี่ยนข้อความเองตอนแสดง (Game Over! / Stage Cleared!)
        var title = ui.EnsureText(window.transform, "Summary_Title", "Game Over!", 56f);
        if (ui.IsNew(title)) MenuWindowUI.Place(title.rectTransform, new Vector2(0f, 270f), new Vector2(WindowSize.x, 80f));

        // สามแถวสถิติ ป้ายชื่อชิดซ้าย ตัวเลขชิดขวา
        var time = EnsureStatRow(ui, window.transform, "Row_Time", "TIME", "เวลาที่ใช้", "00:00", 170f);
        var enemies = EnsureStatRow(ui, window.transform, "Row_Enemies", "ENEMIES", "ศัตรูที่กำจัด", "0", 110f);
        var reward = EnsureStatRow(ui, window.transform, "Row_Crystals", "CRYSTALS", "คริสตัล", "0", 50f);

        // กล่องบอกด่านถัดไป โผล่เฉพาะตอนผ่านด่าน
        var nextBox = ui.EnsureRect(window.transform, "NextSector_Box");
        if (ui.IsNew(nextBox)) MenuWindowUI.Place(nextBox, new Vector2(0f, -30f), new Vector2(RowWidth, 90f));
        var nextLabel = ui.EnsureText(nextBox, "NextSector_Label", "NEXT SECTOR", "ด่านถัดไป", LabelSize);
        if (ui.IsNew(nextLabel)) MenuWindowUI.Place(nextLabel.rectTransform, new Vector2(0f, 25f), new Vector2(RowWidth, 40f));
        var nextName = ui.EnsureText(nextBox, "NextSector_Text", "-", 34f);
        if (ui.IsNew(nextName)) MenuWindowUI.Place(nextName.rectTransform, new Vector2(0f, -20f), new Vector2(RowWidth, 44f));

        var continueButton = ui.EnsureMenuButton(window.transform, "Button_Continue", "Continue", -140f, ButtonWidth);
        // ปุ่มนี้พากลับเมนูหลัก ไม่ใช่ปิดเกม จึงใช้ภาพ Back ไม่ใช่ Quit
        var endButton = ui.EnsureMenuButton(window.transform, "Button_End", "Back", -260f, ButtonWidth);

        var manager = GameSceneUI.EnsureManager<SummaryManager>("SummaryManager", "Build Summary Screen");
        manager.summaryPanel = panel.gameObject;
        manager.nextSectorBox = nextBox.gameObject;
        manager.continueButton = continueButton.gameObject;
        manager.titleText = title;
        manager.timeText = time;
        manager.enemiesDefeatedText = enemies;
        manager.rewardText = reward;
        manager.nextSectorText = nextName;
        EditorUtility.SetDirty(manager);

        MenuWindowUI.WireButton(continueButton, manager.OnClickContinue);
        MenuWindowUI.WireButton(endButton, manager.OnClickEnd);

        GameSceneUI.KeepTransitionOnTop(canvas.transform, panel.transform);

        // เปิดค้างไว้ให้เห็นใน Scene view จะได้ลากปรับต่อได้เลย
        // ตอนเล่นจริง SummaryManager.Start() ปิดให้เองอยู่แล้ว
        panel.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("สร้างหน้าสรุปผลเสร็จแล้ว ลากปรับใน Scene view ได้เลย " +
                  "(ซ่อน/โชว์ด้วย Tools > Quantum Rift > Toggle Summary Preview) แล้วกด Ctrl+S");
    }

    [MenuItem("Tools/Quantum Rift/Toggle Summary Preview")]
    public static void TogglePreview()
    {
        var manager = UnityEngine.Object.FindObjectOfType<SummaryManager>(true);
        var panel = manager != null ? manager.summaryPanel : null;
        if (panel == null)
        {
            Debug.LogWarning("ยังไม่มีหน้าสรุปผลในฉาก สั่ง Build Summary Screen ก่อน");
            return;
        }

        Undo.RecordObject(panel, "Toggle Summary Preview");
        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(panel.scene);
    }

    // คืนค่าเป็นช่องตัวเลข เพราะ SummaryManager เขียนทับเฉพาะตัวเลข ส่วนป้ายชื่อเป็นข้อความตายตัว
    static TextMeshProUGUI EnsureStatRow(MenuWindowUI ui, Transform window, string name, string label, string labelThai, string value, float y)
    {
        var row = ui.EnsureRect(window, name);
        if (ui.IsNew(row)) MenuWindowUI.Place(row, new Vector2(0f, y), new Vector2(RowWidth, 44f));

        var labelText = ui.EnsureText(row, "Label", label, labelThai, LabelSize, TextAlignmentOptions.Left);
        if (ui.IsNew(labelText)) MenuWindowUI.Stretch(labelText.rectTransform);

        var valueText = ui.EnsureText(row, "Value", value, LabelSize, TextAlignmentOptions.Right);
        if (ui.IsNew(valueText)) MenuWindowUI.Stretch(valueText.rectTransform);

        return valueText;
    }
}
