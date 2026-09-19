using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// สร้างหน้าต่าง Pause (กด ESC) ในฉากเกม แล้วต่อปุ่มเข้ากับ PauseManager ให้เสร็จในตัว
// ใช้ภาพปุ่มจากชุด QuantumRift-Menu-Logo-TH-EN-v1 ชุดเดียวกับเมนูหลัก
//
// สั่งซ้ำได้: ตำแหน่ง/ขนาด/สี จะถูกตั้งให้เฉพาะชิ้นที่เพิ่งสร้างใหม่เท่านั้น
// ของที่มีอยู่แล้วจะไม่โดนทับ จะได้ลากปรับเองใน Scene view โดยไม่ต้องกลัวว่าสั่งซ้ำแล้วค่าที่ปรับหาย
public static class PauseMenuBuilder
{
    const string PanelName = "PauseMenu_Panel";

    static readonly Vector2 WindowSize = new Vector2(560f, 560f);
    const float ButtonWidth = 420f;

    [MenuItem("Tools/Quantum Rift/Build Pause Menu")]
    public static void Build()
    {
        var scene = GameSceneUI.OpenGameScene();
        var canvas = GameSceneUI.FindCanvas();
        var ui = new MenuWindowUI("Build Pause Menu");

        var panel = ui.EnsureBackdrop(canvas.transform, PanelName);
        var window = ui.EnsureWindow(panel.transform, "PauseWindow", WindowSize);

        // หัวหน้าต่าง: ไอคอนพักเกมจากแพ็ก + ข้อความ
        var icon = ui.EnsureImage(window.transform, "Pause_Icon");
        if (ui.IsNew(icon)) MenuWindowUI.Place(icon.rectTransform, new Vector2(0f, 210f), new Vector2(72f, 72f));
        icon.sprite = MenuUIPack.Load($"{MenuUIPack.SpriteFolder}/Icon-Pause-Focused.png");
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var title = ui.EnsureText(window.transform, "Pause_Title", "PAUSED", "หยุดเกม", 56f);
        if (ui.IsNew(title)) MenuWindowUI.Place(title.rectTransform, new Vector2(0f, 140f), new Vector2(WindowSize.x, 70f));

        var resume = ui.EnsureMenuButton(window.transform, "Button_Resume", "Resume", 50f, ButtonWidth);
        var settings = ui.EnsureMenuButton(window.transform, "Button_Settings", "Settings", -80f, ButtonWidth);
        var quit = ui.EnsureMenuButton(window.transform, "Button_Quit", "Quit", -210f, ButtonWidth);

        var manager = GameSceneUI.EnsureManager<PauseManager>("PauseManager", "Build Pause Menu");
        manager.pauseMenuPanel = panel.gameObject;
        EditorUtility.SetDirty(manager);

        MenuWindowUI.WireButton(resume, manager.ResumeGame);
        MenuWindowUI.WireButton(settings, manager.OpenSettings);
        MenuWindowUI.WireButton(quit, manager.ExitToMainMenu);

        GameSceneUI.KeepTransitionOnTop(canvas.transform, panel.transform);

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
        // ถาม PauseManager ก่อน เพราะ GameObject.Find หาของที่ถูกปิดอยู่ไม่เจอ
        var manager = UnityEngine.Object.FindObjectOfType<PauseManager>(true);
        var panel = manager != null ? manager.pauseMenuPanel : null;
        if (panel == null)
        {
            Debug.LogWarning("ยังไม่มีหน้าต่าง Pause ในฉาก สั่ง Build Pause Menu ก่อน");
            return;
        }

        Undo.RecordObject(panel, "Toggle Pause Menu Preview");
        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(panel.scene);
    }
}
