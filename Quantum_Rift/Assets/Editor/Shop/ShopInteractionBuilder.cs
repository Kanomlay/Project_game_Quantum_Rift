using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class ShopInteractionBuilder
{
    const string Art = "Assets/image/Shop/QuantumRift-ShopIdle-UI-v3/ShopUI-5Items-Blank-Transparent.png";
    const string WindowPath = "Assets/Prefab/Shop/ShopWindow.prefab";

    [MenuItem("Tools/Quantum Rift/Build Shop Click Window")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Stop Play/Animation preview first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the scene before building shop interaction.");
        if (scene.path != "Assets/Scenes/Animation.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/Animation.unity");
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.SaveAndReimport();
        var savedWindow = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath);
        var window = savedWindow != null ? savedWindow.GetComponent<ShopWindow>() : null;
        if (window == null)
        {
            var root = new GameObject("ShopWindow",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ShopWindow));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 100;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0.5f;
                var content = Rect("ShopContent",root.transform); Stretch(content);
                var backdrop = content.gameObject.AddComponent<Image>(); backdrop.color = new Color(0,0,0,0.68f);
                var frame = Rect("BlankShopFrame",content); frame.sizeDelta = new Vector2(960,640);
                var frameImage = frame.gameObject.AddComponent<Image>(); frameImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art); frameImage.preserveAspect = true;
                var close = Rect("CloseButton",frame); close.sizeDelta = new Vector2(64,64); close.anchoredPosition = new Vector2(383,210);
                var hit = close.gameObject.AddComponent<Image>(); hit.color = new Color(0.1f,0.15f,0.25f,0.05f);
                var button = close.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                foreach (float angle in new[]{45f,-45f})
                {
                    var line = Rect("Cross",close); line.sizeDelta = new Vector2(29,4); line.localRotation = Quaternion.Euler(0,0,angle);
                    var image = line.gameObject.AddComponent<Image>(); image.color = new Color(0.7f,1,1); image.raycastTarget = false;
                }
                var component = root.GetComponent<ShopWindow>(); component.content = content.gameObject;
                UnityEventTools.AddPersistentListener(button.onClick,component.Close);
                content.gameObject.SetActive(false);
                window = PrefabUtility.SaveAsPrefabAsset(root,WindowPath).GetComponent<ShopWindow>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        foreach (string theme in new[]{"Forest","Spaceship"})
        {
            string path = $"Assets/Prefab/Shop/Shop{theme}.prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var collider = go.GetComponent<BoxCollider2D>();
                if (collider == null) collider = go.AddComponent<BoxCollider2D>();
                // ครอบเฉพาะบริเวณร้าน โดยหลบขอบว่างของ sprite และไม่กีดขวางการเดิน
                collider.size = new Vector2(3.8f,3.8f); collider.offset = new Vector2(0,1.8f); collider.isTrigger = true;
                var click = go.GetComponent<ShopClickable>();
                if (click == null) click = go.AddComponent<ShopClickable>();
                click.windowPrefab = window;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("Shop EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        // กล้องของฉากพรีวิวหันมาที่ร้านเพื่อทดลองคลิกใน Game/Play ได้ทันที
        var camera = Camera.main;
        if (camera != null) { camera.transform.position = new Vector3(4.75f,-9, -10); camera.orthographic = true; camera.orthographicSize = 3; }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath);
        Debug.Log("Shop click window ready: click either shop in Play Mode; X or Escape closes the empty shop.");
    }

    static RectTransform Rect(string name,Transform parent)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f); return rect;
    }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
