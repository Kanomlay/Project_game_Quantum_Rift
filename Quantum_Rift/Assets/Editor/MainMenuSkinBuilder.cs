using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// เอาชุดภาพเมนู QuantumRift-Menu-Logo-TH-EN-v1 มาใส่หน้า MainMenu:
// ซ่อมค่า import ของสไปรต์ในแพ็ก, วางโลโก้, และเปลี่ยนปุ่มเป็นภาพปุ่มที่มีตัวหนังสือในตัว
// สั่งซ้ำได้ ของที่มีอยู่แล้วจะถูกใช้ซ้ำ ไม่สร้างซ้อน
public static class MainMenuSkinBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Quantum Rift/Skin Main Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งแต่งเมนู");

        MenuUIPack.FixImportSettings();

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("เซฟฉากที่เปิดอยู่ก่อน แล้วค่อยสั่งแต่งเมนู");
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new InvalidOperationException($"ไม่เจอ Canvas ใน {ScenePath}");

        var background = canvas.transform.Find("Background");
        if (background == null) throw new InvalidOperationException("ไม่เจอ Background ใต้ Canvas");

        PlaceLogo(background);

        ApplyButtonSprite(canvas.transform, "Background/MenuButtonContainer/Button_Start", "Start");
        ApplyButtonSprite(canvas.transform, "Background/MenuButtonContainer/Button_Setting", "Settings");
        ApplyButtonSprite(canvas.transform, "Background/MenuButtonContainer/Button_Exit", "Quit");
        ApplyButtonSprite(canvas.transform, "CharacterSelect_Panel/BACK", "Back");

        MainMenuColorStatesBuilder.ApplyToScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("แต่งหน้าเมนูด้วยชุดภาพใหม่เรียบร้อย ตรวจใน Scene แล้วกด Ctrl+S เพื่อบันทึก");
    }

    static void PlaceLogo(Transform background)
    {
        var logo = EnsureRect(background, "Logo");
        var image = Ensure<Image>(logo.gameObject);
        image.sprite = MenuUIPack.Load($"{MenuUIPack.Folder}/Logo.png");
        image.preserveAspect = true;
        image.raycastTarget = false;

        logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 0.5f);
        logo.pivot = new Vector2(0.5f, 0.5f);
        logo.sizeDelta = new Vector2(480f, 320f);
        logo.anchoredPosition = new Vector2(0f, 340f); // วางเหนือแถวปุ่ม

        logo.SetAsFirstSibling(); // อยู่หลังปุ่ม ไม่บังการกด
    }

    static void ApplyButtonSprite(Transform canvas, string path, string word)
    {
        var target = canvas.Find(path);
        if (target == null)
        {
            Debug.LogWarning($"ไม่เจอปุ่ม {path} ข้ามไป");
            return;
        }

        MainMenuColorStatesBuilder.ApplyWideButton(target, word);
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Skin Main Menu");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        return existing as RectTransform ?? Undo.AddComponent<RectTransform>(existing.gameObject);
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }
}
