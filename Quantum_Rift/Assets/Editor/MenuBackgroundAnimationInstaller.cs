using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ติดตั้งเฉพาะ BG ใน MainMenu ไม่แก้รูปเดิม ไม่ย้ายโลโก้ ปุ่ม หรือแก้ทางเข้าคู่มือ/สนามฝึก
public static class MenuBackgroundAnimationInstaller
{
    public const string Folder = "Assets/image/UI_Image/MainMenu-LivingPortal-v2";
    const string Scene = "Assets/Scenes/MainMenu.unity";
    [MenuItem("Tools/Quantum Rift/Install Living Menu Background")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("หยุด Play Mode ก่อนติดตั้ง BG");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var frames = new Sprite[7];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = Folder + "/MainMenu-LivingPortal-" + (i + 1).ToString("00") + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("ไม่พบภาพ BG: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.spritePivot = Vector2.one * .5f;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var scene = EditorSceneManager.OpenScene(Scene);
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var image = menu.mainMenuUI.GetComponent<Image>();
            if (image == null || image.name != "Background") throw new Exception("ไม่พบ BG เมนูที่ถูกต้อง");
            var animation = image.GetComponent<MenuBackgroundAnimation>() ?? image.gameObject.AddComponent<MenuBackgroundAnimation>();
            animation.display = image; animation.frames = frames; animation.frameSeconds = .18f;
            image.sprite = frames[0]; image.overrideSprite = null; image.raycastTarget = false;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Verify();
            Debug.Log("MENU_BG_INSTALLED frames=7 interval=.18 originalUiPreserved=true");
        }
        finally
        {
            if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    public static void Verify()
    {
        EditorSceneManager.OpenScene(Scene);
        var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        var animation = menu.mainMenuUI.GetComponent<MenuBackgroundAnimation>();
        if (animation == null || animation.frames == null || animation.frames.Length != 7 || animation.frames.Any(s => s == null)) throw new Exception("BG ไม่ครบ 7 เฟรม");
        if (animation.frames.Distinct().Count() != 7 || !Mathf.Approximately(animation.frameSeconds, .18f) || animation.display.raycastTarget) throw new Exception("ค่าภาพหรือการรับคลิกผิด");
        if (animation.display.sprite != animation.frames[0]) throw new Exception("เฟรมเริ่มต้นไม่ตรง");
        foreach (var frame in animation.frames)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame));
            if (frame.rect.width != 1672 || frame.rect.height != 941 || importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed) throw new Exception("คุณภาพภาพ BG ผิด: " + frame.name);
        }
        var rect = (RectTransform)menu.mainMenuUI.transform;
        if (rect.anchoredPosition != Vector2.zero || rect.sizeDelta != new Vector2(1980, 1114)) throw new Exception("กรอบเมนูถูกย้าย");
        var logo = menu.mainMenuUI.transform.Find("Logo") as RectTransform;
        if (logo == null || logo.anchoredPosition != new Vector2(0, 404) || logo.sizeDelta != new Vector2(480, 320)) throw new Exception("โลโก้ถูกย้าย");
        UnifiedMenuInstaller.Verify();
        Debug.Log("MENU_BG_VERIFIED frames=7 fullResolution=true uiUnchanged=true");
    }
}
