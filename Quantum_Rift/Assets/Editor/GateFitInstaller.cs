using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using Object=UnityEngine.Object;

// จัดภาพประตูและพื้นที่ชนให้พอดีช่องทางเดิน โดยรักษาขนาดเสาปลายและอนิเมชันเดิม
public static class GateFitInstaller
{
    const float PostScale=.4f;
    static readonly string[] Keys={"1_4","1_5","2_3","2_4","2_5"};
    static string Output {get{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-gateOutput");return i<0?Path.GetFullPath("../GateFitPreviews"):args[i+1];}}
    static Sprite[] Frames(bool forest)
    {
        const string folder="Assets/Data/Map/FittedRoomGates-v1";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Data/Map","FittedRoomGates-v1");
        var result=new Sprite[7];
        for(int i=0;i<7;i++)
        {
            string path=folder+"/"+(forest?"Forest":"Space")+"-"+i+".asset";
            result[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(result[i]!=null)continue;
            string original=forest?$"Assets/image/Map/Map2-Forest-v1/Gate{i}.png":$"Assets/image/Map/RoomGates-v1/Space/Space-Gate-{i+1:00}.png";
            var source=AssetDatabase.LoadAssetAtPath<Sprite>(original);
            if(source==null)throw new Exception("Missing original gate "+original);
            float border=forest?96:72;
            // แบ่ง 9-slice จาก texture เดิม เสาปลายคงขนาด ยืดเฉพาะช่องกลาง ไม่แก้ไฟล์ภาพต้นฉบับ
            result[i]=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(border,0,border,0));
            result[i].name=(forest?"Forest":"Space")+"GateFit_"+i;
            AssetDatabase.CreateAsset(result[i],path);
        }
        return result;
    }
    // ใช้ร่วมกับตัวสร้างแมพ เพื่อไม่กลับไปขยายเสาทั้งภาพเมื่อสร้าง prefab ซ้ำ
    public static void Fit(AnimatedRoomGate gate,Vector3 center,float passageWidth,bool forest)
    {
        gate.frames=Frames(forest);gate.transform.position=center;
        var parent=gate.transform.parent!=null?gate.transform.parent.lossyScale:Vector3.one;
        gate.transform.localScale=new Vector3(PostScale/Mathf.Abs(parent.x),PostScale/Mathf.Abs(parent.y),1);
        var display=gate.GetComponent<SpriteRenderer>();display.drawMode=SpriteDrawMode.Sliced;
        // เสาประตูอยู่บนขอบกำแพง ภาพต้องวาดเหนือไทล์กำแพง ไม่ถูกไทล์รากไม้บังจนเหลือเพียงปลายเถา
        display.sortingLayerName="object";display.sortingOrder=2;
        float border=(gate.frames[0].border.x+gate.frames[0].border.z)/gate.frames[0].pixelsPerUnit;
        display.size=new Vector2(passageWidth/PostScale+border,gate.frames[0].rect.height/gate.frames[0].pixelsPerUnit);
        // collider กั้นเฉพาะช่องเดิน ไม่รวมเสาที่ทับบนกำแพง และใช้จุดศูนย์กลางเดียวกับภาพ
        var box=gate.GetComponent<BoxCollider2D>();box.offset=Vector2.zero;box.size=new Vector2(passageWidth/PostScale,.55f/PostScale);
        gate.SetClosed(gate.initiallyClosed,true);
    }
    [MenuItem("Tools/Quantum Rift/Fit New Stage and Tutorial Doors")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Directory.CreateDirectory(Output);int count=0;
        foreach(string key in Keys.Concat(new[]{"Tutorial"}))
        {
            string path=key=="Tutorial"?"Assets/Prefab/Tutorial/TrainingMap.prefab":"Assets/Prefab/ExpandedStages/Map_"+key+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var gate in root.GetComponentsInChildren<AnimatedRoomGate>(true))
                {
                    Vector3 center=gate.transform.position;
                    // เก่าชี้ pivot ล่างและวางบางบานทับช่องสุดท้ายของห้อง ให้ไปอยู่กลางช่องทางเดินจริง
                    bool fitted=gate.frames[0].border.x>0;
                    if(!fitted)
                    {
                        if(key=="Tutorial"){if(gate.name=="Gate_2_To_3")center+=Vector3.right*1.25f;}
                        else
                        {
                            var room=gate.GetComponentInParent<RoomController>(true);Vector3 normal=gate.transform.up;
                            float direction=Mathf.Sign(Vector3.Dot(center-room.transform.position,normal));center+=normal*(direction*.625f);
                        }
                    }
                    if(gate.GetComponent<BoxCollider2D>().size.x*Mathf.Abs(gate.transform.lossyScale.x)>3f)
                        center+=(Mathf.Abs(gate.transform.right.y)>.5f?Vector3.up:Vector3.right)*.625f;
                    Fit(gate,center,2*1.25f,key.StartsWith("2_"));count++;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Preview(path,key);
        }
        AssetDatabase.SaveAssets();Verify();Debug.Log("GATE_FIT_INSTALLED gates="+count);
    }
    public static void Verify()
    {
        int count=0;
        foreach(string key in Keys.Concat(new[]{"Tutorial"}))
        {
            string path=key=="Tutorial"?"Assets/Prefab/Tutorial/TrainingMap.prefab":"Assets/Prefab/ExpandedStages/Map_"+key+".prefab";
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var layout=root.GetComponent<MapLayoutRandomizer>();int variants=layout!=null?layout.layouts.Length:1;
                for(int v=0;v<variants;v++)
                {
                    if(layout!=null)for(int i=0;i<variants;i++)layout.layouts[i].SetActive(i==v);
                    Physics2D.SyncTransforms();var tiles=root.GetComponentsInChildren<Tilemap>();var floor=tiles.First(t=>t.name=="Floor_Unified64");var walls=tiles.First(t=>t.name=="Bulkheads_Unified64");
                    foreach(var gate in root.GetComponentsInChildren<AnimatedRoomGate>())
                    {
                        var r=gate.GetComponent<SpriteRenderer>();var box=gate.GetComponent<BoxCollider2D>();
                        if(gate.frames.Length!=7||gate.frames.Any(s=>s==null||s.pivot!=s.rect.size*.5f||s.border.x==0))throw new Exception("Invalid sliced frames "+gate.name);
                        if(r.drawMode!=SpriteDrawMode.Sliced||Mathf.Abs(gate.transform.lossyScale.x-PostScale)>.001f)throw new Exception("Post scale changed");
                        if(Mathf.Abs(box.size.x*PostScale-2.5f)>.001f||box.offset!=Vector2.zero)throw new Exception("Blocker does not span two-cell opening");
                        if(!floor.HasTile(floor.WorldToCell(gate.transform.position)))throw new Exception("Gate is not on corridor floor");
                        foreach(float side in new[]{-1f,1f})
                        {
                            Vector3 post=gate.transform.position+gate.transform.right*(side*(2.5f*.5f+.625f));
                            if(!walls.HasTile(walls.WorldToCell(post)))throw new Exception("Post not attached to wall: "+key+" "+gate.name+" "+post);
                        }
                        gate.SetClosed(true,true);if(!box.enabled)throw new Exception("Closed gate missing collision");
                        gate.SetClosed(false,true);if(box.enabled)throw new Exception("Open gate still blocks");count++;
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        Debug.Log("GATE_FIT_VERIFIED gates="+count+" wallAttached=true frames=7 blockerAligned=true");
    }
    public static void Preview(string path,string key)
    {
        Directory.CreateDirectory(Output);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var layout=root.GetComponent<MapLayoutRandomizer>();if(layout!=null)for(int i=0;i<layout.layouts.Length;i++)layout.layouts[i].SetActive(i==0);
        foreach(var backdrop in root.GetComponentsInChildren<WorldFlowBackdrop>())backdrop.gameObject.SetActive(false);
        var gates=root.GetComponentsInChildren<AnimatedRoomGate>();var a=gates.First(g=>key=="Tutorial"?g.name=="Gate_2_To_1":g.name=="Gate_1_To_2");
        var b=key=="Tutorial"?gates.First(g=>g.name=="Gate_2_To_3"):gates.First(g=>g.name=="Gate_2_To_1");
        var camera=new GameObject("GatePreviewCamera").AddComponent<Camera>();camera.orthographic=true;camera.aspect=1.8f;camera.orthographicSize=4.7f;camera.backgroundColor=new Color(.08f,.1f,.13f);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.transform.position=(a.transform.position+b.transform.position)*.5f+Vector3.back*10;
        var rt=new RenderTexture(1440,800,24);camera.targetTexture=rt;
        try
        {
            foreach(bool closed in new[]{false,true})
            {
                foreach(var gate in gates)gate.SetClosed(closed,true);camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var texture=new Texture2D(1440,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,800),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(Output,key+(closed?"-closed.png":"-open.png")),texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=previous;
            }
        }
        finally{camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);}
    }
    public static void Describe()
    {
        foreach(var path in new[]{"Assets/Prefab/map_1.prefab","Assets/Prefab/ExpandedStages/Map_1_4.prefab","Assets/Prefab/ExpandedStages/Map_2_3.prefab","Assets/Prefab/Tutorial/TrainingMap.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var gate in root.GetComponentsInChildren<AnimatedRoomGate>(true).Take(3))
            {
                var sprite=gate.frames[0];var closed=gate.frames[6];
                Debug.Log("GATE_FIT_SOURCE "+path+" name="+gate.name+" pos="+gate.transform.position+" rotation="+gate.transform.eulerAngles+" scale="+gate.transform.lossyScale+" sprite="+AssetDatabase.GetAssetPath(sprite)+" bounds="+sprite.bounds+" closed="+closed.bounds+" collider="+gate.GetComponent<BoxCollider2D>().size);
            }
        }
    }
}
