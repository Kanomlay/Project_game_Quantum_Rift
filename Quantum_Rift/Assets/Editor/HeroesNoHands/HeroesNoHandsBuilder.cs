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

public static class HeroesNoHandsBuilder
{
    const string Set = "Heroes-BlondeStyle-NoHands-v2";
    const string Art = "Assets/image/hero/" + Set;
    const string Clips = "Assets/Animation/Hero/" + Set;
    const string Prefabs = "Assets/Prefab/Hero/" + Set;
    const string ScenePath = "Assets/Scenes/Animation.unity";
    static readonly string[] Heroes = {"Hero01_Knight", "Hero02_Blonde", "Hero03_BlueHair", "Hero04_WhiteHair"};
    static readonly string[] Actions = {"Idle", "Walk", "Death"};
    static string SheetPath(string hero) => $"{Art}/{hero}-21Frames.png";
    static string ClipPath(string hero, string action) => $"{Clips}/{hero}/{hero}_{action}.anim";
    static string ControllerPath(string hero) => $"{Clips}/{hero}/{hero}.controller";
    static string PrefabPath(string hero) => $"{Prefabs}/{hero}.prefab";
    static float Fps(string action) => action == "Walk" ? 10 : 8;

    [MenuItem("Tools/Quantum Rift/Build Heroes NoHands v2 %#h")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Stop Play/Animation preview before building Heroes.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the current scene before building Heroes.");
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        EnsureFolder(Prefabs);
        var prefabs = new List<GameObject>();
        foreach (var hero in Heroes)
        {
            ImportSheet(hero);
            EnsureFolder($"{Clips}/{hero}");
            var sprites = LoadSprites(hero);
            foreach (var action in Actions) MakeClip(hero, action, sprites);
            prefabs.Add(MakePrefab(hero, sprites["Idle_00"], MakeController(hero)));
        }
        // เพิ่มกลุ่มใหม่ในฉาก Animation โดยรักษาตัวละครและบอสที่มีอยู่
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Heroes NoHands v2 Preview");
        if (root == null)
        {
            root = new GameObject("Heroes NoHands v2 Preview");
            Undo.RegisterCreatedObjectUndo(root, "Add Heroes preview");
            for (int i = 0; i < prefabs.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
                instance.transform.SetParent(root.transform);
                instance.transform.position = new Vector3(3 + i * 3.5f, -6, 0);
            }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Selection.activeGameObject = root.transform.GetChild(0).gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(8.25f, -4.8f, 0), new Vector3(16, 5, 1)), false);
        Debug.Log("Heroes NoHands v2 ready: 4 heroes, 84 sprites, 12 clips, 4 prefabs in Animation scene.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    static Dictionary<string, Sprite> LoadSprites(string hero) =>
        AssetDatabase.LoadAllAssetsAtPath(SheetPath(hero)).OfType<Sprite>().ToDictionary(s => s.name);

    static void ImportSheet(string hero)
    {
        var importer = AssetImporter.GetAtPath(SheetPath(hero)) as TextureImporter;
        if (importer == null) throw new Exception("Missing source sheet: " + hero);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name);
        var rects = new List<SpriteRect>();
        for (int row = 0; row < 3; row++) for (int col = 0; col < 7; col++)
        {
            string name = $"{Actions[row]}_{col:00}";
            // Unity วัดจากล่างซ้าย; ต้นฉบับวัด pivot (160,272) จากบนซ้าย
            rects.Add(new SpriteRect {name = name, rect = new Rect(col * 320, (2-row) * 320, 320, 320),
                alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 0.15f),
                spriteID = existing.TryGetValue(name, out var old) ? old.spriteID : GUID.Generate()});
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }

    static void MakeClip(string hero, string action, Dictionary<string, Sprite> sprites)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(hero, action));
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath(hero, action)); }
        clip.name = hero + "_" + action; clip.frameRate = Fps(action);
        var keys = Enumerable.Range(0, 7).Select(i => new ObjectReferenceKeyframe {time = i/Fps(action), value = sprites[$"{action}_{i:00}"]}).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = action != "Death"; settings.stopTime = 7/Fps(action);
        AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
    }

    static AnimatorController MakeController(string hero)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(hero));
        if (controller != null) return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath(hero));
        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var states = new Dictionary<string, AnimatorState>();
        for (int i = 0; i < Actions.Length; i++)
        {
            var state = machine.AddState(Actions[i], new Vector3(280 + i * 260, 150));
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(hero, Actions[i]));
            state.writeDefaultValues = false; states[Actions[i]] = state;
        }
        machine.defaultState = states["Idle"];
        var death = machine.AddAnyStateTransition(states["Death"]);
        Configure(death); death.canTransitionToSelf = false;
        // ไม่เปลี่ยนกลับเข้า Death ซ้ำทุกเฟรม เพื่อให้คลิปเล่นถึงเฟรมสุดท้ายได้
        death.AddCondition(AnimatorConditionMode.If, 0, "isDead");
        var walk = states["Idle"].AddTransition(states["Walk"]); Configure(walk);
        walk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        walk.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
        var idle = states["Walk"].AddTransition(states["Idle"]); Configure(idle);
        idle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        idle.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
        var revive = states["Death"].AddTransition(states["Idle"]); Configure(revive);
        revive.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
        return controller;
    }

    static void Configure(AnimatorStateTransition transition)
    { transition.hasExitTime = false; transition.duration = 0; transition.hasFixedDuration = true; }

    static GameObject MakePrefab(string hero, Sprite sprite, RuntimeAnimatorController controller)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(hero));
        if (existing != null) return existing;
        var go = new GameObject(hero);
        // ลดขนาดภาพครึ่งหนึ่งให้ใกล้เคียงตัวละครเดิม โดยคงสัดส่วนและ pivot ของทุกคลิป
        // ใช้เป็นค่าเริ่มต้นของ prefab ใหม่; การรันซ้ำรักษา prefab ที่ผู้ใช้ปรับแล้ว
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot; renderer.sortingOrder = 5;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            go.AddComponent<HeroNoHandsAnimation>();
            return PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(hero));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [MenuItem("Tools/Quantum Rift/Validate Heroes NoHands v2")]
    public static void Validate()
    {
        var report = new List<string>();
        foreach (var hero in Heroes)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath(hero));
            var sprites = LoadSprites(hero);
            if (texture == null || texture.width != 2240 || texture.height != 960 || sprites.Count != 21)
                throw new Exception("Invalid texture dimensions or sprite count: " + hero);
            for (int row = 0; row < 3; row++) for (int col = 0; col < 7; col++)
            {
                var s = sprites[$"{Actions[row]}_{col:00}"];
                if (s.rect != new Rect(col*320, (2-row)*320, 320, 320) || Vector2.Distance(s.pivot, new Vector2(160,48)) > 0.01f)
                    throw new Exception("Incorrect slice or pivot: " + hero + s.name);
            }
            report.Add($"PASS {hero}: full resolution, 21 sprites, rects and pivots");
            foreach (var action in Actions)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(hero,action));
                if (clip == null) throw new Exception("Missing clip: " + hero + action);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings.Length != 1) throw new Exception("Invalid sprite binding: " + clip.name);
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                if (keys.Length != 7 || Mathf.Abs(clip.length - 7/Fps(action)) > 0.001f || AnimationUtility.GetAnimationClipSettings(clip).loopTime != (action != "Death"))
                    throw new Exception("Invalid timing: " + clip.name);
                var sample = new GameObject("HeroFrameValidation", typeof(SpriteRenderer), typeof(Animator));
                try
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var expected = sprites[$"{action}_{i:00}"];
                        if (keys[i].value != expected || Mathf.Abs(keys[i].time - i/Fps(action)) > 0.001f) throw new Exception("Frame order: " + clip.name);
                        clip.SampleAnimation(sample, keys[i].time + 0.001f);
                        if (sample.GetComponent<SpriteRenderer>().sprite != expected) throw new Exception("Frame sample: " + clip.name);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(sample); }
                report.Add($"PASS {clip.name}: 7 ordered frames sampled, {clip.length:F3}s, loop={action != "Death"}");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(hero));
            if (prefab == null || prefab.GetComponent<SpriteRenderer>()?.sprite != sprites["Idle_00"] ||
                prefab.GetComponent<Animator>()?.runtimeAnimatorController != AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(hero)) ||
                prefab.GetComponent<HeroNoHandsAnimation>() == null || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) != 0)
                throw new Exception("Incomplete prefab: " + hero);
            ValidateController(hero, sprites["Death_06"]);
            report.Add($"PASS {hero} prefab + Idle/Walk/Death transitions, death hold and explicit revive");
        }
        File.WriteAllLines("Library/HeroesNoHandsValidation.txt", report);
        Debug.Log(string.Join("\n", report));
    }

    static void ValidateController(string hero, Sprite finalDeath)
    {
        var go = new GameObject("HeroControllerValidation", typeof(SpriteRenderer), typeof(Animator));
        var graph = PlayableGraph.Create("HeroControllerValidation");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimatorControllerPlayable.Create(graph, AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(hero)));
            var output = AnimationPlayableOutput.Create(graph, "Sprite", go.GetComponent<Animator>());
            output.SetSourcePlayable(playable); graph.Play();
            void Tick(int count) { for (int i = 0; i < count; i++) graph.Evaluate(1f/60f); }
            void Expect(string name)
            { if (!playable.GetCurrentAnimatorStateInfo(0).IsName(name)) throw new Exception(hero + " did not reach " + name); }
            Tick(3); Expect("Idle");
            playable.SetBool("isWalking", true); Tick(3); Expect("Walk");
            playable.SetBool("isWalking", false); Tick(3); Expect("Idle");
            foreach (bool walking in new[]{false, true})
            {
                playable.SetBool("isWalking", walking); Tick(3); Expect(walking ? "Walk" : "Idle");
                playable.SetBool("isDead", true); Tick(3); Expect("Death");
                Tick(120); Expect("Death");
                if (go.GetComponent<SpriteRenderer>().sprite != finalDeath) throw new Exception(hero + " death did not hold last frame");
                playable.SetBool("isWalking", !walking); Tick(3); Expect("Death");
                playable.SetBool("isWalking", false); playable.SetBool("isDead", false); Tick(3); Expect("Idle");
            }
        }
        finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(go); }
    }
}
