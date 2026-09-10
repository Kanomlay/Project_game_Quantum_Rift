using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;

public static class ShopIdleBuilder
{
    const string Art = "Assets/image/Shop/QuantumRift-ShopIdle-UI-v3";
    const string Clips = "Assets/Animation/Shop";
    const string Prefabs = "Assets/Prefab/Shop";
    const string ScenePath = "Assets/Scenes/Animation.unity";
    static readonly string[] Themes = { "Forest", "Spaceship" };
    static string SheetPath(string theme) => $"{Art}/{theme}-Idle-7Frames.png";
    static string ClipPath(string theme) => $"{Clips}/Shop{theme}_Idle.anim";
    static string ControllerPath(string theme) => $"{Clips}/Shop{theme}.controller";
    static string PrefabPath(string theme) => $"{Prefabs}/Shop{theme}.prefab";

    [MenuItem("Tools/Quantum Rift/Build Shop Idle")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Stop Play/Animation preview before building shops.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the current scene before building shops.");
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        EnsureFolder(Clips); EnsureFolder(Prefabs);
        var prefabs = new List<GameObject>();
        foreach (string theme in Themes)
        {
            ImportSheet(theme);
            var sprites = LoadSprites(theme);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(theme));
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath(theme)); }
            clip.name = $"Shop{theme}_Idle"; clip.frameRate = 6;
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                sprites.Select((s,i) => new ObjectReferenceKeyframe { time = i/6f, value = s }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true; settings.stopTime = 7/6f;
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(theme));
            // รักษา controller และ prefab ที่ผู้ใช้ปรับไว้เมื่อสร้างซ้ำ
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath(theme));
                var state = controller.layers[0].stateMachine.AddState("Idle");
                state.motion = clip; state.writeDefaultValues = false;
                controller.layers[0].stateMachine.defaultState = state;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(theme));
            if (prefab == null)
            {
                var go = new GameObject("Shop" + theme);
                try
                {
                    // ใช้สเกลเดียวกับ Hero ใหม่; หน้าร้านกว้างกว่าตัวละครตามขนาดเคาน์เตอร์
                    go.transform.localScale = new Vector3(0.5f, 0.5f, 1);
                    var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[0];
                    renderer.sortingOrder = 5; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                    go.AddComponent<Animator>().runtimeAnimatorController = controller;
                    prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(theme));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            prefabs.Add(prefab);
        }
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Shop Idle Preview");
        if (root == null)
        {
            root = new GameObject("Shop Idle Preview"); Undo.RegisterCreatedObjectUndo(root, "Add shop previews");
            for (int i = 0; i < prefabs.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
                instance.transform.SetParent(root.transform);
                instance.transform.position = new Vector3(3 + i*3.5f, -10, 0);
            }
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Validate();
        Selection.activeGameObject = root.transform.GetChild(0).gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(4.75f,-9,0),new Vector3(9,4,1)),false);
        Debug.Log("Shop Idle ready: Forest + Spaceship, 14 frames, 2 looping clips and 2 prefabs in Animation scene.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static Sprite[] LoadSprites(string theme) => AssetDatabase.LoadAllAssetsAtPath(SheetPath(theme)).OfType<Sprite>().OrderBy(s => s.name).ToArray();

    static void ImportSheet(string theme)
    {
        var importer = AssetImporter.GetAtPath(SheetPath(theme)) as TextureImporter;
        if (importer == null) throw new Exception("Missing shop sheet: " + theme);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name);
        // Pivot ต้นฉบับ (224,416) จากบนซ้าย เป็น (224,32) จากล่างซ้ายใน Unity
        // ทุกเฟรมใช้จุดยึดเดียวกันเพื่อรักษาเคาน์เตอร์ให้นิ่งขณะพ่อค้าลอยขึ้นลง
        var rects = Enumerable.Range(0,7).Select(i => {
            string name = $"Idle_{i:00}";
            return new SpriteRect { name = name, rect = new Rect(i*448,0,448,448),
                alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f,32f/448),
                spriteID = existing.TryGetValue(name,out var old) ? old.spriteID : GUID.Generate() };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }

    [MenuItem("Tools/Quantum Rift/Validate Shop Idle")]
    public static void Validate()
    {
        var report = new List<string>();
        foreach (string theme in Themes)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath(theme));
            var sprites = LoadSprites(theme);
            if (texture == null || texture.width != 3136 || texture.height != 448 || sprites.Length != 7) throw new Exception("Shop import: " + theme);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(theme));
            if (clip == null || !AnimationUtility.GetAnimationClipSettings(clip).loopTime || Mathf.Abs(clip.length-7/6f)>0.001f) throw new Exception("Shop clip timing: " + theme);
            var keys = AnimationUtility.GetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"));
            if (keys == null || keys.Length != 7) throw new Exception("Shop frame count: " + theme);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(theme));
            if (prefab == null || prefab.GetComponent<SpriteRenderer>()?.sprite != sprites[0] ||
                prefab.GetComponent<Animator>()?.runtimeAnimatorController != AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(theme))) throw new Exception("Shop prefab: " + theme);
            var sample = new GameObject("ShopValidation",typeof(SpriteRenderer),typeof(Animator));
            var graph = PlayableGraph.Create("ShopValidation");
            try
            {
                for (int i=0;i<7;i++)
                {
                    if (sprites[i].rect != new Rect(i*448,0,448,448) || Vector2.Distance(sprites[i].pivot,new Vector2(224,32))>0.01f || keys[i].value != sprites[i] || Mathf.Abs(keys[i].time-i/6f)>0.001f) throw new Exception("Shop frame/pivot: " + theme);
                    clip.SampleAnimation(sample,i/6f+0.001f);
                    if (sample.GetComponent<SpriteRenderer>().sprite != sprites[i]) throw new Exception("Shop sample: " + theme);
                }
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimatorControllerPlayable.Create(graph,prefab.GetComponent<Animator>().runtimeAnimatorController);
                var output = AnimationPlayableOutput.Create(graph,"Sprite",sample.GetComponent<Animator>());
                output.SetSourcePlayable(playable); graph.Play();
                for (int i=0;i<160;i++) graph.Evaluate(1f/60f);
                var state = playable.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName("Idle") || state.normalizedTime<2 || sample.GetComponent<SpriteRenderer>().sprite == null) throw new Exception("Shop loop: " + theme);
            }
            finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(sample); }
            report.Add($"PASS {theme}: 7 ordered frames, original 3136x448, pivot (224,32), 6 FPS, 1.167s loop, prefab and Animator loop playback");
        }
        File.WriteAllLines("Library/ShopIdleValidation.txt",report); Debug.Log(string.Join("\n",report));
    }
}
