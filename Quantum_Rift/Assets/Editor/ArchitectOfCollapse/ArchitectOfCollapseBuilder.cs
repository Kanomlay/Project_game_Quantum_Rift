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

public static class ArchitectOfCollapseBuilder
{
    public const string Art = "Assets/image/Boss/ArchitectOfCollapse";
    public const string Clips = "Assets/Animation/Boss/ArchitectOfCollapse";
    public const string Prefabs = "Assets/Prefab/Boss/ArchitectOfCollapse";
    public const string ScenePath = "Assets/Scenes/Animation.unity";
    [Serializable] public class Frame { public string name; public int x,y,width,height; public float pivotX,pivotY; }
    [Serializable] public class Sheet { public string key,file; public int width,height; public float ppu; public Frame[] frames; }
    [Serializable] public class SliceData { public Sheet[] sheets; }
    class ClipSpec
    {
        public int phase; public string name,sheet; public float fps; public bool loop;
        public ClipSpec(int phase, string name, string sheet, float fps, bool loop = false)
        { this.phase=phase; this.name=name; this.sheet=sheet; this.fps=fps; this.loop=loop; }
    }
    static readonly ClipSpec[] Specs = {
        new ClipSpec(1,"Idle","Phase1Body",8,true), new ClipSpec(1,"BulletPose","Phase1Body",10),
        new ClipSpec(1,"LaserPose","Phase1Body",8), new ClipSpec(1,"MixedPose","Phase1Body",10),
        new ClipSpec(1,"Phase2Charge","Phase1Body",8), new ClipSpec(1,"BulletSpin","Phase1Bullet",12,true),
        new ClipSpec(1,"LaserBeam","Phase1Laser",12),
        new ClipSpec(2,"Idle","Phase2Body",8,true), new ClipSpec(2,"Float","Phase2Body",10,true),
        new ClipSpec(2,"AuraDash","Phase2Body",12), new ClipSpec(2,"ReturnSlash","Phase2Body",12),
        new ClipSpec(2,"LaserPose","Phase2Body",8), new ClipSpec(2,"ScytheSlash","Phase2Body",12),
        new ClipSpec(2,"ScytheWave","Phase2Wave",12,true), new ClipSpec(2,"LaserBeam","Phase2Laser",12)
    };
    static string ClipPath(int phase,string name) => $"{Clips}/Phase{phase}/ArchitectPhase{phase}_{name}.anim";
    static string ControllerPath(int phase) => $"{Clips}/Phase{phase}/ArchitectPhase{phase}.controller";
    static string BossPath(int phase) => $"{Prefabs}/Phase{phase}/ArchitectOfCollapsePhase{phase}.prefab";
    static string EffectPath(int phase,string name) => $"{Prefabs}/Phase{phase}/ArchitectPhase{phase}{name}.prefab";

    [MenuItem("Tools/Quantum Rift/Build Architect Of Collapse Both Phases %#k")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Stop Play/Animation preview before building.");
        var scene=SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the current scene before building Architect Of Collapse.");
        if (scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath);
        var data=JsonUtility.FromJson<SliceData>(File.ReadAllText(Art+"/ArchitectOfCollapseSlices.json"));
        var sprites=new Dictionary<string,Dictionary<string,Sprite>>();
        foreach (var sheet in data.sheets)
        {
            ImportSheet(sheet);
            sprites[sheet.key]=AssetDatabase.LoadAllAssetsAtPath(Art+"/"+sheet.file).OfType<Sprite>().ToDictionary(s=>s.name);
        }
        foreach(int phase in new[]{1,2}) { EnsureFolder($"{Clips}/Phase{phase}"); EnsureFolder($"{Prefabs}/Phase{phase}"); }
        foreach(var spec in Specs)
            MakeClip(spec,Enumerable.Range(0,7).Select(i=>sprites[spec.sheet][$"{spec.name}_{i:00}"]).ToArray());
        var phase1=MakeBossController(1);
        var phase2=MakeBossController(2);
        var boss1=MakePrefab(BossPath(1),"ArchitectOfCollapsePhase1",sprites["Phase1Body"]["Idle_00"],phase1,1);
        var boss2=MakePrefab(BossPath(2),"ArchitectOfCollapsePhase2",sprites["Phase2Body"]["Idle_00"],phase2,2);
        var bullet=MakeEffect(1,"Bullet","BulletSpin",sprites["Phase1Bullet"]["BulletSpin_00"]);
        var laser1=MakeEffect(1,"Laser","LaserBeam",sprites["Phase1Laser"]["LaserBeam_03"]);
        var wave=MakeEffect(2,"ScytheWave","ScytheWave",sprites["Phase2Wave"]["ScytheWave_00"]);
        var laser2=MakeEffect(2,"Laser","LaserBeam",sprites["Phase2Laser"]["LaserBeam_03"]);
        // แยกกลุ่มพรีวิวสองเฟส และเพิ่มเฉพาะเมื่อยังไม่มี เพื่อรักษาฉากและงานบอสเดิม
        var first=AddPreview(scene,"ArchitectOfCollapse Phase1 Preview",new[]{boss1,bullet,laser1},
            new[]{new Vector3(15,2.6f,0),new Vector3(17.8f,2.6f,0),new Vector3(14,6,0)});
        var second=AddPreview(scene,"ArchitectOfCollapse Phase2 Preview",new[]{boss2,wave,laser2},
            new[]{new Vector3(22,1,0),new Vector3(25.5f,2.5f,0),new Vector3(21,6,0)});
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Selection.activeGameObject=second.transform.GetChild(0).gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(20,3,0),new Vector3(17,8,1)),false);
        Debug.Log("Architect Of Collapse ready: Phase 1 + Phase 2, 105 sprites, 15 clips, 6 prefabs in Animation scene.");
    }

    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }

    static void ImportSheet(Sheet sheet)
    {
        var importer=AssetImporter.GetAtPath(Art+"/"+sheet.file) as TextureImporter;
        if(importer==null) throw new Exception("Missing sheet: "+sheet.file);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=sheet.ppu;
        importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
        importer.npotScale=TextureImporterNPOTScale.None;
        // แผ่น Phase 2 กว้าง 4480 px ต้องใช้ขีดจำกัด 8192 เพื่อไม่ให้ Unity ย่อภาพ
        importer.maxTextureSize=Mathf.NextPowerOfTwo(Mathf.Max(sheet.width,sheet.height));
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape=false;
        importer.SetTextureSettings(settings);
        var factory=new SpriteDataProviderFactories(); factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var existing=provider.GetSpriteRects().ToDictionary(r=>r.name);
        // รักษา GUID รายสไปรต์เมื่อรันซ้ำ เพื่อรักษา references ของคลิปและ prefab
        var rects=sheet.frames.Select(f=>new SpriteRect { name=f.name,rect=new Rect(f.x,f.y,f.width,f.height),
            alignment=SpriteAlignment.Custom,pivot=new Vector2(f.pivotX,f.pivotY),
            spriteID=existing.TryGetValue(f.name,out var old)?old.spriteID:GUID.Generate() }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }

    static void MakeClip(ClipSpec spec,Sprite[] sprites)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(spec.phase,spec.name));
        if(clip==null) {clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,ClipPath(spec.phase,spec.name));}
        clip.name=$"ArchitectPhase{spec.phase}_{spec.name}"; clip.frameRate=spec.fps;
        var keys=sprites.Select((s,i)=>new ObjectReferenceKeyframe{time=i/spec.fps,value=s}).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=spec.loop; settings.stopTime=sprites.Length/spec.fps;
        AnimationUtility.SetAnimationClipSettings(clip,settings); EditorUtility.SetDirty(clip);
    }

    static AnimatorController MakeBossController(int phase)
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(phase));
        // ไม่เขียนทับ Animator ที่ผู้ใช้อาจปรับไว้หลังสร้างครั้งแรก
        if(controller!=null) return controller;
        controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath(phase));
        var actions=phase==1?new[]{"BulletPose","LaserPose","MixedPose","Phase2Charge"}:new[]{"AuraDash","ReturnSlash","LaserPose","ScytheSlash"};
        foreach(var action in actions) controller.AddParameter(action,AnimatorControllerParameterType.Trigger);
        if(phase==2) controller.AddParameter("isFloating",AnimatorControllerParameterType.Bool);
        var machine=controller.layers[0].stateMachine;
        var states=new Dictionary<string,AnimatorState>(); int index=0;
        foreach(var spec in Specs.Where(s=>s.phase==phase && s.sheet==$"Phase{phase}Body"))
        {
            var state=machine.AddState(spec.name,new Vector3(260+index%2*280,80+index/2*110)); index++;
            state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(phase,spec.name));
            state.writeDefaultValues=false; states[spec.name]=state;
        }
        machine.defaultState=states["Idle"];
        foreach(var action in actions)
        {
            Transition(states["Idle"],states[action],false,action,true);
            if(phase==2) Transition(states["Float"],states[action],false,action,true);
            // Phase2Charge ค้างปลายคลิปไว้รอระบบเกมสลับเฟส
            if(phase==1) {if(action!="Phase2Charge") Transition(states[action],states["Idle"],true);}
            else if(action=="AuraDash") Transition(states[action],states["ReturnSlash"],true);
            else
            {
                Transition(states[action],states["Float"],true,"isFloating",true);
                Transition(states[action],states["Idle"],true,"isFloating",false);
            }
        }
        if(phase==2)
        {
            Transition(states["Idle"],states["Float"],false,"isFloating",true);
            Transition(states["Float"],states["Idle"],false,"isFloating",false);
        }
        return controller;
    }

    static void Transition(AnimatorState from,AnimatorState to,bool exit,string condition=null,bool positive=true)
    {
        var transition=from.AddTransition(to); transition.hasExitTime=exit; transition.exitTime=1;
        transition.duration=0; transition.hasFixedDuration=true;
        if(condition!=null) transition.AddCondition(positive?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,condition);
    }

    static GameObject MakeEffect(int phase,string name,string clipName,Sprite sprite)
    {
        string path=$"{Clips}/Phase{phase}/ArchitectPhase{phase}{name}.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var state=controller.layers[0].stateMachine.AddState(clipName);
            state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(phase,clipName));
            controller.layers[0].stateMachine.defaultState=state;
        }
        return MakePrefab(EffectPath(phase,name),$"ArchitectPhase{phase}{name}",sprite,controller,0);
    }

    static GameObject MakePrefab(string path,string name,Sprite sprite,RuntimeAnimatorController controller,int phase)
    {
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
        var go=new GameObject(name);
        try
        {
            var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
            renderer.spriteSortPoint=SpriteSortPoint.Pivot; renderer.sortingOrder=5;
            go.AddComponent<Animator>().runtimeAnimatorController=controller;
            if(phase==1) go.AddComponent<ArchitectPhase1Animation>();
            if(phase==2) go.AddComponent<ArchitectPhase2Animation>();
            return PrefabUtility.SaveAsPrefabAsset(go,path);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static GameObject AddPreview(Scene scene,string name,GameObject[] prefabs,Vector3[] positions)
    {
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==name);
        if(root!=null) return root;
        root=new GameObject(name); Undo.RegisterCreatedObjectUndo(root,"Add Architect preview");
        for(int i=0;i<prefabs.Length;i++)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i],scene);
            instance.transform.SetParent(root.transform); instance.transform.position=positions[i];
        }
        return root;
    }

    [MenuItem("Tools/Quantum Rift/Validate Architect Of Collapse Both Phases")]
    public static void Validate()
    {
        var report=new List<string>();
        var data=JsonUtility.FromJson<SliceData>(File.ReadAllText(Art+"/ArchitectOfCollapseSlices.json"));
        foreach(var sheet in data.sheets)
        {
            string path=Art+"/"+sheet.file;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null || texture.width!=sheet.width || texture.height!=sheet.height) throw new Exception("Texture resized: "+path);
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
            if(sprites.Count!=sheet.frames.Length) throw new Exception("Sprite count: "+path);
            foreach(var frame in sheet.frames)
            {
                if(!sprites.TryGetValue(frame.name,out var sprite) || sprite.rect!=new Rect(frame.x,frame.y,frame.width,frame.height)) throw new Exception("Sprite rect: "+frame.name);
                if(Vector2.Distance(sprite.pivot,new Vector2(frame.pivotX*frame.width,frame.pivotY*frame.height))>0.01f) throw new Exception("Sprite pivot: "+frame.name);
            }
            report.Add($"PASS {sheet.key}: {sprites.Count} frames, full {texture.width}x{texture.height}, rects and pivots match");
        }
        foreach(var spec in Specs)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(spec.phase,spec.name));
            if(clip==null) throw new Exception("Missing clip: "+spec.name);
            var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if(bindings.Length!=1) throw new Exception("Missing sprite curve: "+spec.name);
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,bindings[0]);
            if(keys.Length!=7 || keys.Any(k=>k.value==null) || keys.Select(k=>k.value).Distinct().Count()!=7) throw new Exception("Invalid frames: "+spec.name);
            if(AnimationUtility.GetAnimationClipSettings(clip).loopTime!=spec.loop || Mathf.Abs(clip.length-7/spec.fps)>0.001f) throw new Exception("Clip timing: "+spec.name);
            var go=new GameObject("ArchitectFrameValidation",typeof(SpriteRenderer),typeof(Animator));
            try
            {
                foreach(var key in keys)
                {
                    clip.SampleAnimation(go,key.time+0.001f);
                    if(go.GetComponent<SpriteRenderer>().sprite!=key.value) throw new Exception("Sprite sample: "+spec.name);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            report.Add($"PASS Phase{spec.phase} {spec.name}: 7 frames sampled, {clip.length:F3}s, loop={spec.loop}");
        }
        foreach(var path in new[]{BossPath(1),BossPath(2),EffectPath(1,"Bullet"),EffectPath(1,"Laser"),EffectPath(2,"ScytheWave"),EffectPath(2,"Laser")})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null || prefab.GetComponent<SpriteRenderer>()?.sprite==null || prefab.GetComponent<Animator>()?.runtimeAnimatorController==null || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab)>0) throw new Exception("Incomplete prefab: "+path);
            report.Add("PASS prefab: "+path);
        }
        ValidateController(1,report); ValidateController(2,report);
        File.WriteAllLines("Library/ArchitectOfCollapseValidation.txt",report);
        Debug.Log(string.Join("\n",report));
    }

    // ตรวจเส้นทางจริงด้วย PlayableGraph โดยไม่ต้องสลับฉากไป Play Mode
    static void ValidateController(int phase,List<string> report)
    {
        var go=new GameObject("ArchitectControllerValidation",typeof(SpriteRenderer),typeof(Animator));
        var graph=PlayableGraph.Create("ArchitectControllerValidation");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable=AnimatorControllerPlayable.Create(graph,AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(phase)));
            var output=AnimationPlayableOutput.Create(graph,"Sprite",go.GetComponent<Animator>());
            output.SetSourcePlayable(playable); graph.Play();
            void Tick(int frames) {for(int i=0;i<frames;i++) graph.Evaluate(1f/60f);}
            void Expect(string name)
            {
                if(!playable.GetCurrentAnimatorStateInfo(0).IsName(name) || go.GetComponent<SpriteRenderer>().sprite==null) throw new Exception($"Phase{phase} did not reach {name}");
            }
            Tick(3); Expect("Idle");
            if(phase==1)
            {
                foreach(var action in new[]{"BulletPose","LaserPose","MixedPose"})
                {
                    playable.SetTrigger(action); Tick(3); Expect(action); Tick(80); Expect("Idle");
                    report.Add($"PASS Phase1 Idle -> {action} -> Idle");
                }
                playable.SetTrigger("Phase2Charge"); Tick(3); Expect("Phase2Charge"); Tick(120); Expect("Phase2Charge");
                var last=AssetDatabase.LoadAllAssetsAtPath(Art+"/Phase1/Architect-Body-35Frames.png").OfType<Sprite>().Single(s=>s.name=="Phase2Charge_06");
                if(go.GetComponent<SpriteRenderer>().sprite!=last) throw new Exception("Phase2Charge did not hold its last frame");
                playable.Play("Idle",0,0); Tick(3); Expect("Idle");
                report.Add("PASS Phase1 Phase2Charge plays once and holds last frame; explicit Idle reset works");
            }
            else foreach(bool floating in new[]{false,true})
            {
                playable.SetBool("isFloating",floating); Tick(3); string basis=floating?"Float":"Idle"; Expect(basis);
                foreach(var action in new[]{"LaserPose","ScytheSlash","ReturnSlash"})
                {
                    playable.SetTrigger(action); Tick(3); Expect(action); Tick(80); Expect(basis);
                    report.Add($"PASS Phase2 {basis} -> {action} -> {basis}");
                }
                playable.SetTrigger("AuraDash"); Tick(3); Expect("AuraDash"); Tick(38); Expect("ReturnSlash"); Tick(80); Expect(basis);
                report.Add($"PASS Phase2 {basis} -> AuraDash -> ReturnSlash -> {basis}");
            }
        }
        finally { graph.Destroy(); UnityEngine.Object.DestroyImmediate(go); }
    }
}
