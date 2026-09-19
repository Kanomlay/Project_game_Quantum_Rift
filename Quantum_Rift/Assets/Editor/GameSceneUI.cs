using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ของใช้ร่วมสำหรับ builder ที่สร้าง UI ในฉากเกม
public static class GameSceneUI
{
    public const string ScenePath = "Assets/Scenes/GameScene.unity";
    public const string CanvasName = "UI";

    public static Scene OpenGameScene() => OpenScene(ScenePath);

    public static Scene OpenScene(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้าง UI");

        MenuUIPack.FixImportSettings();

        var scene = SceneManager.GetActiveScene();
        if (scene.path == path) return scene;

        // เปิดฉากอื่นทับจะทำให้ของที่ยังไม่เซฟหายไปเงียบๆ เลยต้องกันไว้ก่อน
        if (scene.isDirty)
            throw new InvalidOperationException("เซฟฉากที่เปิดอยู่ก่อน แล้วค่อยสั่งสร้าง UI");
        return EditorSceneManager.OpenScene(path);
    }

    public static GameObject FindCanvas() => FindCanvas(CanvasName);

    public static GameObject FindCanvas(string name)
    {
        var canvas = GameObject.Find(name);
        if (canvas == null || canvas.GetComponent<Canvas>() == null)
            throw new InvalidOperationException($"ไม่เจอ Canvas ชื่อ {name} ในฉากนี้");
        return canvas;
    }

    public static T EnsureManager<T>(string hostName, string undoName) where T : Component
    {
        var manager = UnityEngine.Object.FindObjectOfType<T>(true);
        if (manager != null) return manager;

        var host = new GameObject(hostName);
        Undo.RegisterCreatedObjectUndo(host, undoName);
        return Undo.AddComponent<T>(host);
    }

    // จอดำเปลี่ยนด่านต้องอยู่บนสุดเสมอ ไม่งั้นตอนวาปข้ามด่านจะเห็นฉากโผล่ใต้จอดำ
    public static void KeepTransitionOnTop(Transform canvas, Transform panel)
    {
        panel.SetAsLastSibling();
        var transition = canvas.Find("TransitionScreen");
        if (transition != null) transition.SetAsLastSibling();
    }
}
