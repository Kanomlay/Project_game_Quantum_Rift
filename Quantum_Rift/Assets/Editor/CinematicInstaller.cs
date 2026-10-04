using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class CinematicInstaller
{
    public const string LibraryPath="Assets/Resources/CinematicLibrary.asset";
    static string Out {get{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-cinematicOutput");var path=i>=0?args[i+1]:Path.GetFullPath(Application.dataPath+"/../Temp/Cinematics");Directory.CreateDirectory(path);return path;}}
    static Sprite[] Background(string theme)=>Enumerable.Range(1,7).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/image/Map/FlowBackgrounds-v1/{theme}/Background-{i:00}.png")).ToArray();
    [MenuItem("Tools/Quantum Rift/Cinematics/Create Story and Boss Introductions")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        var lib=AssetDatabase.LoadAssetAtPath<CinematicLibrary>(LibraryPath);
        if(lib==null){lib=ScriptableObject.CreateInstance<CinematicLibrary>();AssetDatabase.CreateAsset(lib,LibraryPath);}
        var ship=Background("Map1-Spaceship");var forest=Background("Map2-LivingForest");var hive=Background("Map3-OrganicHive");
        if(ship.Concat(forest).Concat(hive).Any(s=>s==null))throw new Exception("Cinematic backgrounds missing");
        if(lib.opening==null||lib.opening.Length==0)
            lib.opening=new[]{
                new CinematicLibrary.StoryPage{titleThai="สัญญาณจากซากยาน",titleEnglish="A SIGNAL IN THE WRECKAGE",bodyThai="รอยแยกควอนตัมฉีกเส้นทางระหว่างมิติ ซากยานอวกาศคือจุดเริ่มต้นของการเดินทาง\nคุณต้องฝ่าศัตรูที่เฝ้าอยู่ และค้นหาทางไปยังต้นตอของรอยแยก",bodyEnglish="A quantum rift has torn a path between dimensions. Your journey begins in the wreckage of a spaceship.\nFight past its defenders and find the way to the source of the rift.",backgrounds=ship,accent=new Color32(105,218,244,255)},
                new CinematicLibrary.StoryPage{titleThai="อีกฟากของรอยแยก",titleEnglish="BEYOND THE RIFT",bodyThai="เส้นทางพาคุณสู่มิติป่าที่เติบโตและเคลื่อนไหวได้ พืชพิษกับสิ่งมีชีวิตต่างมิติขวางทางอยู่\nเก็บอาวุธและพลังที่พบระหว่างทาง เพื่อเตรียมพร้อมเผชิญผู้พิทักษ์ของป่า",bodyEnglish="Beyond the wreckage lies a living forest dimension. Poisonous plants and creatures stand in your way.\nGather weapons and powers along the journey before facing the forest's guardian.",backgrounds=forest,accent=new Color32(119,235,196,255)},
                new CinematicLibrary.StoryPage{titleThai="ต้นตอแห่งการล่มสลาย",titleEnglish="THE SOURCE OF COLLAPSE",bodyThai="ปลายทางคือรังมิติ ที่ซึ่ง The Architect of Collapse รออยู่\nฝ่าบอสที่ขวางทาง ไปถึงใจกลางรัง และหยุดภัยจากรอยแยกก่อนทุกอย่างจะล่มสลาย",bodyEnglish="The path ends in the dimensional hive, where The Architect of Collapse awaits.\nOvercome the bosses in your way, reach the heart of the hive, and stop the rift before everything collapses.",backgrounds=hive,accent=new Color32(235,122,171,255)}
            };
        if(lib.bosses==null||lib.bosses.Length==0)
        {
            var echo=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Boss/EchoCommander/EchoCommander.prefab");
            var entborn=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Boss/AncientEntborn/AncientEntborn.prefab");
            var map=AssetDatabase.LoadAssetAtPath<MapData>("Assets/Data/Map/MapData_boss.asset");
            var architect=map.mapPrefab.GetComponentInChildren<ArchitectBossHealth>(true);
            lib.bosses=new[]{
                new CinematicLibrary.BossProfile{id="echo",displayName="ECHO COMMANDER",titleThai="ผู้บัญชาการแห่งซากยาน",titleEnglish="COMMANDER OF THE WRECKAGE",portrait=echo.GetComponent<SpriteRenderer>().sprite,backgrounds=ship,accent=new Color32(172,116,243,255)},
                new CinematicLibrary.BossProfile{id="entborn",displayName="ANCIENT ENTBORN",titleThai="ผู้พิทักษ์แห่งมิติป่า",titleEnglish="GUARDIAN OF THE LIVING FOREST",portrait=entborn.GetComponent<SpriteRenderer>().sprite,backgrounds=forest,accent=new Color32(110,232,169,255)},
                new CinematicLibrary.BossProfile{id="architect",displayName="THE ARCHITECT OF COLLAPSE",titleThai="ผู้อยู่ใจกลางรังมิติ",titleEnglish="AT THE HEART OF THE DIMENSIONAL HIVE",portrait=architect.phaseOne.GetComponentInChildren<SpriteRenderer>(true).sprite,backgrounds=hive,accent=new Color32(233,105,168,255)}
            };
        }
        if(lib.bosses.Any(p=>p.portrait==null))throw new Exception("Boss portrait missing");
        EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
        Debug.Log("CINEMATIC_LIBRARY_READY opening="+lib.opening.Length+" bosses="+lib.bosses.Length);
    }
    public static void InstallAndPreview(){Install();Preview();}
    public static void Preview()
    {
        var previousLanguage=LanguageSettings.Current;int overflow=0;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("PreviewCamera",typeof(Camera)).GetComponent<Camera>();cam.orthographic=true;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=Color.black;
            var director=new GameObject("PreviewDirector").AddComponent<CinematicDirector>();director.library=AssetDatabase.LoadAssetAtPath<CinematicLibrary>(LibraryPath);
            foreach(var language in new[]{GameLanguage.Thai,GameLanguage.English})
            {
                LanguageSettings.Current=language;
                for(int i=0;i<director.library.opening.Length;i++)
                {
                    director.SetStoryPage(i);SetupCanvas(director,cam,1280,720);director.SetStoryPage(i);director.Body.maxVisibleCharacters=int.MaxValue;
                    Capture(cam,$"opening-{language}-{i+1}.png",1280,720);overflow+=Overflow(director.Overlay);
                }
                foreach(var boss in director.library.bosses)
                {
                    director.SetBoss(boss);SetupCanvas(director,cam,1280,720);director.SetBoss(boss);
                    Capture(cam,$"boss-{language}-{boss.id}.png",1280,720);overflow+=Overflow(director.Overlay);
                }
            }
            LanguageSettings.Current=GameLanguage.Thai;director.SetBoss(director.library.bosses[2]);SetupCanvas(director,cam,1024,768);director.SetBoss(director.library.bosses[2]);Capture(cam,"boss-architect-1024.png",1024,768);overflow+=Overflow(director.Overlay);
            SetupCanvas(director,cam,1280,720);
            foreach(float age in new[]{0f,.3f,.9f})
            {
                director.SetBoss(director.library.bosses[0],age);
                Capture(cam,$"boss-slide-{Mathf.RoundToInt(age*100):00}.png",1280,720);
            }
            Debug.Log("CINEMATIC_PREVIEWS_COMPLETE overflow="+overflow);
        }
        finally{LanguageSettings.Current=previousLanguage;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        if(overflow>0)throw new Exception("Cinematic text overflow: "+overflow);
    }
    static void SetupCanvas(CinematicDirector director,Camera cam,int w,int h)
    {
        director.Overlay.SetActive(true);
        var canvas=director.Overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        canvas.overrideSorting=true;canvas.sortingLayerID=SortingLayer.layers.OrderBy(l=>l.value).Last().id;canvas.sortingOrder=31000;
        canvas.scaleFactor=Mathf.Min(w/1600f,h/900f);Canvas.ForceUpdateCanvases();
    }
    static int Overflow(GameObject root)
    {
        int n=0;foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing){n++;Debug.LogWarning("CINEMATIC_OVERFLOW "+text.name);}}return n;
    }
    static void Capture(Camera cam,string name,int w,int h)
    {
        var target=new RenderTexture(w,h,24);cam.targetTexture=target;Canvas.ForceUpdateCanvases();cam.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Out,name),texture.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
    }
}
