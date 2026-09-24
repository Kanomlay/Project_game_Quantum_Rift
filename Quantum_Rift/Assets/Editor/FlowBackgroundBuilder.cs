using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>ติดตั้งพื้นหลังเคลื่อนไหวไว้ใต้พื้นแมพ ไม่เปลี่ยนผังห้องหรือระบบต่อสู้</summary>
public static class FlowBackgroundBuilder
{
    const string Art = "Assets/image/Map/FlowBackgrounds-v1";
    const string Node = "QuantumFlowBackground";
    const float Rate = 5f;
    static readonly string[] Themes = { "Map1-Spaceship", "Map2-LivingForest", "Map3-OrganicHive" };
    static readonly string[][] Maps = {
        new[] { "map_1", "map_1_2", "map_1_3", "map_1_bossroom" },
        new[] { "Map_2", "Map_2_2", "Map_2_boss" },
        new[] { "Map_boss" }
    };
    [Serializable] sealed class Entry
    {
        public string prefab;
        public int backgrounds, rooms, colliders, tilemaps;
        public bool gameplayStructureUnchanged;
    }
    [Serializable] sealed class Report
    {
        public int themes = 3, framesPerTheme = 7;
        public float fps = Rate, loopSeconds = 1.4f;
        public List<Entry> maps = new List<Entry>();
        public bool animationSamplingPassed;
    }
    static string Output
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-qrOutput");
            return i >= 0 && i+1 < args.Length ? args[i+1] : Path.GetFullPath("../FlowBackgroundReport");
        }
    }

    [MenuItem("Tools/Quantum Rift/Backgrounds/Install Flow Backgrounds (7 Frames)")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนติดตั้งพื้นหลัง");
        Directory.CreateDirectory(Output);
        var report = new Report();
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) throw new Exception("Missing URP 2D sprite unlit shader");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art+"/FlowBackground-Unlit.mat");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, Art+"/FlowBackground-Unlit.mat");
        }
        for (int theme = 0; theme < Themes.Length; theme++)
        {
            string folder = Art+"/"+Themes[theme];
            var sprites = ImportFrames(folder);
            var clip = BuildClip(folder, sprites);
            var controller = BuildController(folder, clip);
            AssetDatabase.SaveAssets();
            VerifyClip(clip, sprites, controller);
            foreach (string name in Maps[theme])
            {
                string path = "Assets/Prefab/"+name+".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string before = GameplaySignature(root);
                    var rooms = root.GetComponentsInChildren<RoomController>(true);
                    // ขอบเขตห้องรวม trigger ทุกชิ้น รองรับห้องทรง L/T/ห้องตัดมุม
                    if (rooms.Length > 0)
                    {
                        foreach (var room in rooms)
                            AddBackground(room.transform, RoomBounds(room), sprites[0], controller, material);
                    }
                    else
                    {
                        var floors = root.GetComponentsInChildren<Tilemap>(true)
                            .Where(t => t.GetComponent<TilemapCollider2D>() == null && t.cellBounds.size.x > 4)
                            .OrderByDescending(t => t.cellBounds.size.x*t.cellBounds.size.y).ToArray();
                        if (floors.Length == 0) throw new Exception("No room/floor bounds: "+path);
                        var floor = floors[0];
                        var b = floor.localBounds;
                        var center = root.transform.InverseTransformPoint(floor.transform.TransformPoint(b.center));
                        var scale = floor.transform.lossyScale;
                        AddBackground(root.transform, new Bounds(center, Vector3.Scale(b.size,scale)), sprites[0], controller, material);
                    }
                    if (GameplaySignature(root) != before) throw new Exception("Gameplay geometry changed: "+path);
                    var nodes = root.GetComponentsInChildren<Animator>(true).Where(a => a.name == Node).ToArray();
                    if (nodes.Length != Math.Max(1,rooms.Length)) throw new Exception("Background count mismatch: "+path);
                    foreach (var node in nodes)
                    {
                        if (node.runtimeAnimatorController != controller || node.GetComponent<Collider2D>() != null)
                            throw new Exception("Incorrect background setup: "+path);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                    report.maps.Add(new Entry { prefab=path, backgrounds=nodes.Length, rooms=rooms.Length,
                        colliders=root.GetComponentsInChildren<Collider2D>(true).Length,
                        tilemaps=root.GetComponentsInChildren<Tilemap>(true).Length, gameplayStructureUnchanged=true });
                    Debug.Log("FLOW_INSTALLED "+path+" backgrounds="+nodes.Length);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        report.animationSamplingPassed = true;
        AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Output,"installation-report.json"),JsonUtility.ToJson(report,true));
        CapturePreviews();
        Debug.Log("FLOW_BACKGROUND_INSTALLATION_OK");
    }

    static Sprite[] ImportFrames(string folder)
    {
        var frames = new Sprite[7];
        for (int i=0;i<7;i++)
        {
            string path = folder+"/Background-"+(i+1).ToString("00")+".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Missing image: "+path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null || frames[i].rect.width != 1200 || frames[i].rect.height != 800)
                throw new Exception("Unexpected imported dimensions: "+path);
        }
        return frames;
    }
    static AnimationClip BuildClip(string folder, Sprite[] frames)
    {
        string path=folder+"/Flow-7Frames.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null) { clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,path); }
        clip.frameRate=Rate;
        var keys=new ObjectReferenceKeyframe[7];
        for(int i=0;i<7;i++) keys[i]=new ObjectReferenceKeyframe {time=i/Rate,value=frames[i]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=true; settings.stopTime=7f/Rate;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }
    static AnimatorController BuildController(string folder,AnimationClip clip)
    {
        string path=folder+"/Flow-Loop.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(controller.layers.Length==0) controller.AddLayer("Base Layer");
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Flow") ?? machine.AddState("Flow");
        state.motion=clip; state.writeDefaultValues=false;
        machine.defaultState=state;
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }
    static Bounds RoomBounds(RoomController room)
    {
        var boxes=room.GetComponents<BoxCollider2D>();
        if(boxes.Length==0) throw new Exception("Room lacks box geometry: "+room.name);
        var bounds=new Bounds((Vector3)boxes[0].offset,(Vector3)boxes[0].size);
        foreach(var box in boxes.Skip(1)) bounds.Encapsulate(new Bounds((Vector3)box.offset,(Vector3)box.size));
        return bounds;
    }
    static void AddBackground(Transform parent,Bounds room,Sprite sprite,RuntimeAnimatorController controller,Material material)
    {
        var child=parent.Find(Node);
        if(child==null) { child=new GameObject(Node).transform; child.SetParent(parent,false); }
        child.localPosition=new Vector3(room.center.x,room.center.y,2);
        child.localRotation=Quaternion.identity;
        // เพิ่มพื้นที่รอบห้อง ไม่เปลี่ยนขนาดพื้นหรือ collider ของห้อง
        var size=new Vector2(Mathf.Max(12,room.size.x+10),Mathf.Max(10,room.size.y+10));
        child.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        var renderer=child.GetComponent<SpriteRenderer>();
        if(renderer==null) renderer=child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite=sprite; renderer.sharedMaterial=material;
        renderer.color=new Color(.68f,.68f,.68f,1);
        renderer.sortingLayerName="Default"; renderer.sortingOrder=-100;
        var animator=child.GetComponent<Animator>();
        if(animator==null) animator=child.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController=controller;
        animator.cullingMode=AnimatorCullingMode.CullCompletely;
        animator.updateMode=AnimatorUpdateMode.Normal;
    }
    static string GameplaySignature(GameObject root)
    {
        var pieces=new List<string>();
        foreach(var room in root.GetComponentsInChildren<RoomController>(true)) pieces.Add(EditorJsonUtility.ToJson(room));
        foreach(var col in root.GetComponentsInChildren<Collider2D>(true)) pieces.Add(EditorJsonUtility.ToJson(col));
        foreach(var tile in root.GetComponentsInChildren<Tilemap>(true)) pieces.Add(EditorJsonUtility.ToJson(tile));
        return string.Join("\n",pieces);
    }
    static void VerifyClip(AnimationClip clip,Sprite[] frames, RuntimeAnimatorController controller)
    {
        var go=new GameObject("FlowClipVerification",typeof(SpriteRenderer),typeof(Animator));
        try
        {
            var animator=go.GetComponent<Animator>();
            animator.runtimeAnimatorController=controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
            for(int i=0;i<7;i++)
            {
                clip.SampleAnimation(go,(i+.5f)/Rate);
                if(go.GetComponent<SpriteRenderer>().sprite!=frames[i]) throw new Exception("Bad frame timing: "+i+" actual="+go.GetComponent<SpriteRenderer>().sprite+" expected="+frames[i]);
            }
            Debug.Log("FLOW_CLIP_DURATION "+clip.length+" settings="+AnimationUtility.GetAnimationClipSettings(clip).stopTime);
            if(Mathf.Abs(clip.length-1.4f)>.001f) throw new Exception("Bad loop duration: "+clip.length);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    static void CapturePreviews()
    {
        for(int theme=0;theme<3;theme++)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            string path="Assets/Prefab/"+Maps[theme][0]+".prefab";
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var random=root.GetComponentInChildren<MapLayoutRandomizer>(true);
            if(random!=null) for(int i=0;i<random.layouts.Length;i++) if(random.layouts[i]!=null) random.layouts[i].SetActive(i==0);
            var backgrounds=root.GetComponentsInChildren<Animator>().Where(a=>a.name==Node).ToArray();
            var focus=backgrounds.OrderBy(a=>a.transform.position.sqrMagnitude).First();
            var bounds=focus.GetComponent<SpriteRenderer>().bounds;
            var camera=new GameObject("FlowPreviewCamera",typeof(Camera)).GetComponent<Camera>();
            camera.orthographic=true;
            camera.orthographicSize=bounds.size.y*.6f;
            camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-50);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.03f,.04f);
            camera.nearClipPlane=.1f; camera.farClipPlane=200;
            var light=new GameObject("PreviewGlobalLight",typeof(Light2D)).GetComponent<Light2D>();
            light.lightType=Light2D.LightType.Global; light.intensity=1;
            var render=new RenderTexture(1200,800,24);
            camera.targetTexture=render;
            for(int frame=0;frame<7;frame++)
            {
                foreach(var animator in backgrounds)
                {
                    var clip=animator.runtimeAnimatorController.animationClips[0];
                    clip.SampleAnimation(animator.gameObject,(frame+.5f)/Rate);
                }
                camera.Render();
                var previous=RenderTexture.active; RenderTexture.active=render;
                var tex=new Texture2D(1200,800,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,1200,800),0,0); tex.Apply();
                File.WriteAllBytes(Path.Combine(Output,Themes[theme]+"-Unity-"+(frame+1).ToString("00")+".png"),tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex); RenderTexture.active=previous;
            }
            camera.targetTexture=null; render.Release(); UnityEngine.Object.DestroyImmediate(render);
        }
    }
}
