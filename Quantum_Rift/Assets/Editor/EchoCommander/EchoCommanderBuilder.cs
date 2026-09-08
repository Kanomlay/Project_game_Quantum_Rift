using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public static class EchoCommanderBuilder
{
    public const string Art = "Assets/image/Boss/EchoCommander";
    public const string Clips = "Assets/Animation/Boss/EchoCommander";
    public const string Prefabs = "Assets/Prefab/Boss/EchoCommander";
    public const string ScenePath = "Assets/Scenes/Animation.unity";
    public const string BossPath = Prefabs + "/EchoCommander.prefab";
    public const string BulletPath = Prefabs + "/EchoCommanderProjectile.prefab";
    const string ActionTexture = Art + "/EchoCommander-Actions-28Frames-v2.png";
    const string BulletTexture = Art + "/EchoCommander-Projectile-7Frames.png";

    // rect อ้างอิงมุมล่างซ้ายของ texture; pivot เป็นสัดส่วน 0..1 ภายใน rect
    [Serializable] public class Frame { public string name; public int x, y, width, height; public float pivotX, pivotY; }
    [Serializable] public class SliceData { public Frame[] actions, projectile; }

    [MenuItem("Tools/Quantum Rift/Build Echo Commander %#e")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Stop Play/Animation preview before building.");
        var active = SceneManager.GetActiveScene();
        if (active.isDirty) throw new InvalidOperationException("Save the current scene before building Echo Commander.");
        var scene = active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath);
        EnsureFolder(Clips);
        EnsureFolder(Prefabs);
        var data = JsonUtility.FromJson<SliceData>(File.ReadAllText(Art + "/EchoCommanderSlices.json"));
        ImportSheet(ActionTexture, data.actions, 100);
        ImportSheet(BulletTexture, data.projectile, 300);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(ActionTexture).OfType<Sprite>().ToDictionary(s => s.name);
        var bullets = AssetDatabase.LoadAllAssetsAtPath(BulletTexture).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        var clips = new Dictionary<string, AnimationClip>();
        // ไม่มีแถวยืนแยกในต้นฉบับ จึงใช้ภาพแรกของท่ายิงเป็น Idle
        clips["Idle"] = CreateClip("Idle", new[] { sprites["Shoot_00"] }, 1, true);
        foreach (var name in new[] { "Walk", "Summon", "Shoot", "Melee" })
        {
            var frames = Enumerable.Range(0, 7).Select(i => sprites[$"{name}_{i:00}"]).ToArray();
            clips[name] = CreateClip(name, frames, name == "Summon" ? 8 : name == "Melee" ? 12 : 10, name == "Walk", name == "Summon" ? 3 : 0);
        }
        var spin = CreateClip("ProjectileSpin", bullets, 12, true);
        var controller = CreateBossController(clips);
        var bulletController = GetController(Clips + "/EchoCommanderProjectile.controller", out bool newBullet);
        if (newBullet)
        {
            var state = bulletController.layers[0].stateMachine.AddState("Spin");
            state.motion = spin;
            bulletController.layers[0].stateMachine.defaultState = state;
        }
        var bossPrefab = CreatePrefab(BossPath, "EchoCommander", sprites["Shoot_00"], controller, true);
        var bulletPrefab = CreatePrefab(BulletPath, "EchoCommanderProjectile", bullets[0], bulletController, false);
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "EchoCommander Preview");
        if (root == null)
        {
            root = new GameObject("EchoCommander Preview");
            Undo.RegisterCreatedObjectUndo(root, "Add Echo Commander preview");
            var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, scene);
            boss.transform.SetParent(root.transform);
            boss.transform.position = new Vector3(1f, 0.5f, 0);
            var bullet = (GameObject)PrefabUtility.InstantiatePrefab(bulletPrefab, scene);
            bullet.transform.SetParent(root.transform);
            bullet.transform.position = new Vector3(3.5f, 1.5f, 0);
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Selection.activeGameObject = root.transform.GetChild(0).gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(1.8f, 1.5f), new Vector3(6, 4, 1)), false);
        Debug.Log("Echo Commander ready: 28 action sprites, 7 projectile sprites, 6 clips, 2 prefabs in Assets/Scenes/Animation.unity.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void ImportSheet(string path, Frame[] frames, float ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing sprite sheet: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        // รักษา spriteID เดิมเมื่อสร้างซ้ำ เพื่อไม่ให้คลิปและ prefab สูญเสีย references
        var old = provider.GetSpriteRects().ToDictionary(r => r.name);
        var rects = frames.Select(f => new SpriteRect {
            name = f.name, rect = new Rect(f.x, f.y, f.width, f.height),
            alignment = SpriteAlignment.Custom, pivot = new Vector2(f.pivotX, f.pivotY),
            spriteID = old.TryGetValue(f.name, out var previous) ? previous.spriteID : GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    static AnimationClip CreateClip(string name, Sprite[] sprites, float fps, bool loop, int hold = 0)
    {
        string path = Clips + "/EchoCommander_" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.name = "EchoCommander_" + name;
        clip.frameRate = fps;
        var keys = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < sprites.Length; i++) keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] });
        // hold เพิ่มเวลาค้างภาพสุดท้าย และ stopTime กำหนดความยาวคลิปให้พอดี
        if (hold > 0) keys.Add(new ObjectReferenceKeyframe { time = (sprites.Length + hold - 1) / fps, value = sprites[sprites.Length - 1] });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = (sprites.Length + hold) / fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorController GetController(string path, out bool created)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        created = controller == null;
        return controller != null ? controller : AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static AnimatorController CreateBossController(Dictionary<string, AnimationClip> clips)
    {
        var controller = GetController(Clips + "/EchoCommander.controller", out bool created);
        // รักษา controller เดิม รวมถึง transition ที่ผู้ใช้ปรับด้วยมือภายหลัง
        if (!created) return controller;
        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        foreach (string name in new[] { "Summon", "Shoot", "Attack" }) controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var states = new Dictionary<string, AnimatorState>();
        int index = 0;
        foreach (var pair in clips)
        {
            var state = machine.AddState(pair.Key, new Vector3(260 + index % 2 * 270, 80 + index / 2 * 100));
            state.motion = pair.Value;
            state.writeDefaultValues = false;
            states.Add(pair.Key, state);
            index++;
        }
        machine.defaultState = states["Idle"];
        Transition(states["Idle"], states["Walk"], false, "isWalking", true);
        Transition(states["Walk"], states["Idle"], false, "isWalking", false);
        foreach (string name in new[] { "Summon", "Shoot", "Melee" })
        {
            // เริ่มจาก Idle/Walk และรอท่าเล่นครบก่อนกลับตามค่า isWalking
            foreach (string from in new[] { "Idle", "Walk" })
                Transition(states[from], states[name], false, name == "Melee" ? "Attack" : name, true);
            Transition(states[name], states["Walk"], true, "isWalking", true);
            Transition(states[name], states["Idle"], true, "isWalking", false);
        }
        return controller;
    }

    static void Transition(AnimatorState from, AnimatorState to, bool exit, string condition, bool positive)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = exit;
        transition.exitTime = 1;
        transition.duration = 0;
        transition.hasFixedDuration = true;
        transition.AddCondition(positive ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition);
    }

    static GameObject CreatePrefab(string path, string name, Sprite sprite, RuntimeAnimatorController controller, bool boss)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var go = new GameObject(name);
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sortingOrder = 5;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            if (boss) go.AddComponent<EchoCommanderAnimation>();
            return PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [MenuItem("Tools/Quantum Rift/Validate Echo Commander")]
    public static void Validate()
    {
        var messages = new List<string>();
        foreach (var entry in new[] { (ActionTexture, 28), (BulletTexture, 7) })
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(entry.Item1).OfType<Sprite>().ToArray();
            if (sprites.Length != entry.Item2) throw new Exception("Incorrect sprite count: " + entry.Item1);
            if (sprites.Any(s => s.rect.width <= 0 || s.rect.height <= 0 || s.pivot.x < 0 || s.pivot.y < 0 || s.pivot.x > s.rect.width || s.pivot.y > s.rect.height))
                throw new Exception("Invalid sprite bounds or pivot.");
            messages.Add($"PASS {sprites.Length} valid sprites: {entry.Item1}");
        }
        foreach (string name in new[] { "Idle", "Walk", "Summon", "Shoot", "Melee", "ProjectileSpin" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "/EchoCommander_" + name + ".anim");
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (bindings.Length != 1) throw new Exception("Missing sprite curve: " + name);
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            if (keys.Any(k => k.value == null) || keys.Select(k => k.value).Distinct().Count() != (name == "Idle" ? 1 : 7))
                throw new Exception("Invalid animation frames: " + name);
            var go = new GameObject("EchoValidation", typeof(SpriteRenderer), typeof(Animator));
            try
            {
                foreach (var key in keys)
                {
                    clip.SampleAnimation(go, key.time + 0.001f);
                    if (go.GetComponent<SpriteRenderer>().sprite != key.value) throw new Exception($"Frame sample mismatch: {name} at {key.time}, expected {key.value.name}, actual {go.GetComponent<SpriteRenderer>().sprite?.name ?? "null"}");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            messages.Add($"PASS {name}: {keys.Length} keys, {clip.length:F3}s, all sprite samples match");
        }
        foreach (string path in new[] { BossPath, BulletPath })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<SpriteRenderer>().sprite == null || prefab.GetComponent<Animator>().runtimeAnimatorController == null)
                throw new Exception("Incomplete prefab: " + path);
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) > 0) throw new Exception("Missing prefab script: " + path);
            messages.Add("PASS prefab: " + path);
        }
        ValidateTransitions(messages);
        File.WriteAllLines("Library/EchoCommanderValidation.txt", messages);
        Debug.Log(string.Join("\n", messages));
    }

    // ทดสอบ Animator จริงบนวัตถุชั่วคราวด้วยเวลาที่ควบคุมเอง และล้างทิ้งเมื่อจบ
    static void ValidateTransitions(List<string> messages)
    {
        var go = new GameObject("EchoTransitionValidation", typeof(SpriteRenderer), typeof(Animator));
        var graph = UnityEngine.Playables.PlayableGraph.Create("EchoCommanderValidation");
        try
        {
            graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Clips + "/EchoCommander.controller");
            var playable = UnityEngine.Animations.AnimatorControllerPlayable.Create(graph, controller);
            var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Sprite", go.GetComponent<Animator>());
            output.SetSourcePlayable(playable);
            graph.Play();
            void Tick(int count) { for (int i = 0; i < count; i++) graph.Evaluate(1f / 60f); }
            void Expect(string name)
            {
                if (!playable.GetCurrentAnimatorStateInfo(0).IsName(name)) throw new Exception("Animator did not reach " + name);
                if (go.GetComponent<SpriteRenderer>().sprite == null) throw new Exception("Animator rendered a missing sprite in " + name);
            }
            Tick(3); Expect("Idle");
            playable.SetBool("isWalking", true); Tick(3); Expect("Walk");
            playable.SetBool("isWalking", false); Tick(3); Expect("Idle");
            foreach (bool walking in new[] { false, true })
            {
                playable.SetBool("isWalking", walking); Tick(3);
                foreach (string action in new[] { "Summon", "Shoot", "Melee" })
                {
                    playable.SetTrigger(action == "Melee" ? "Attack" : action);
                    Tick(3); Expect(action);
                    Tick(100); Expect(walking ? "Walk" : "Idle");
                    messages.Add($"PASS Animator: {(walking ? "Walk" : "Idle")} -> {action} -> {(walking ? "Walk" : "Idle")}");
                }
            }
        }
        finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(go); }
    }
}
