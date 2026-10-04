using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class QuantumUiSkinInstaller
{
    const string SpritePath="Assets/Resources/QuantumUI/QuantumFrame.png";
    static string Out { get { var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-skinOutput");var path=i>=0?args[i+1]:Path.GetFullPath(Application.dataPath+"/../Temp/UiSkin");Directory.CreateDirectory(path);return path;} }
    sealed class Layout
    {
        public RectTransform rect;
        public Vector2 min,max,pivot,pos,size;
        public Vector3 scale;
        public Quaternion rotation;
        public Layout(RectTransform r){rect=r;min=r.anchorMin;max=r.anchorMax;pivot=r.pivot;pos=r.anchoredPosition;size=r.sizeDelta;scale=r.localScale;rotation=r.localRotation;}
        public void Verify(){if(rect==null||rect.anchorMin!=min||rect.anchorMax!=max||rect.pivot!=pivot||rect.anchoredPosition!=pos||rect.sizeDelta!=size||rect.localScale!=scale||rect.localRotation!=rotation)throw new Exception("UI moved: "+(rect!=null?rect.name:"deleted"));}
    }
    static List<Layout> Snapshot(IEnumerable<GameObject> roots)=>roots.SelectMany(x=>x.GetComponentsInChildren<RectTransform>(true)).Where(r=>!r.name.StartsWith("__")).Select(r=>new Layout(r)).ToList();
    [MenuItem("Tools/Quantum Rift/Apply Quantum UI Skin (Keep Layout)")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        var importer=(TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.spriteBorder=new Vector4(180,180,180,180);
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        int checkedRects=0;
        var prefab=PrefabUtility.LoadPrefabContents(GameHelpInstaller.PrefabPath);
        try
        {
            var before=Snapshot(new[]{prefab});QuantumUiSkin.Help(prefab.GetComponent<GameHelpWindow>());
            foreach(var rect in before)rect.Verify();checkedRects+=before.Count;
            PrefabUtility.SaveAsPrefabAsset(prefab,GameHelpInstaller.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/GameScene.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();var before=Snapshot(roots);
            foreach(var root in roots)
            {
                foreach(var help in root.GetComponentsInChildren<GameHelpWindow>(true))QuantumUiSkin.Help(help);
                foreach(var hud in root.GetComponentsInChildren<HUDManager>(true))QuantumUiSkin.Hud(hud);
                foreach(var blessing in root.GetComponentsInChildren<BlessingWindow>(true))QuantumUiSkin.Blessings(blessing);
            }
            foreach(var rect in before)rect.Verify();checkedRects+=before.Count;
            // จดเฉพาะค่ารูปแบบของ prefab instance; ไม่ถอดความเชื่อมโยง Help เดิม
            foreach(var root in roots)foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();Debug.Log("UI_SKIN_LAYOUT_PRESERVED rects="+checkedRects);
        Preview();
        Debug.Log("UI_SKIN_INSTALL_COMPLETE");
    }
    public static void Preview()
    {
        var saved=LanguageSettings.Current;int overflow=0;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            foreach(var x in Object.FindObjectsByType<SettingsMenu>(FindObjectsSortMode.None))x.Close();
            foreach(var x in Object.FindObjectsByType<PauseManager>(FindObjectsSortMode.None))x.pauseMenuPanel.SetActive(false);
            foreach(var x in Object.FindObjectsByType<SummaryManager>(FindObjectsSortMode.None))x.summaryPanel.SetActive(false);
            var blessing=Object.FindFirstObjectByType<BlessingWindow>();blessing.panel.SetActive(false);
            var help=Object.FindFirstObjectByType<GameHelpWindow>();help.panel.SetActive(false);
            var hud=Object.FindFirstObjectByType<HUDManager>();hud.transitionCanvas.gameObject.SetActive(false);
            hud.UpdateHP(8,8);hud.UpdateEnergy(150,150);hud.UpdateCurrency(28);QuantumUiSkin.Hud(hud);
            var map=AssetDatabase.LoadAssetAtPath<MapData>("Assets/Data/Map/MapData_1_1.asset");
            var mapObject=Object.Instantiate(map.mapPrefab);var hero=new GameObject("Preview hero");hero.transform.position=map.spawnPosition;
            var cam=Camera.main;cam.orthographic=true;cam.orthographicSize=9;cam.transform.position=new Vector3(map.spawnPosition.x,map.spawnPosition.y,-10);
            MiniMapHUD.Show(hud,mapObject,hero.transform);
            var weapon=AssetDatabase.FindAssets("t:WeaponData").Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).First(w=>w.weaponIcon!=null);
            hud.UpdateWeapon(weapon);
            var icons=AssetDatabase.FindAssets("t:SkillData").Select(g=>AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var skillIcons=icons.Where(s=>s.skillIcon!=null).Select(s=>s.skillIcon).Take(2).ToArray();
            if(skillIcons.Length==2)hud.SetupSkillIcons(skillIcons[0],skillIcons[1]);
            LanguageSettings.Current=GameLanguage.Thai;help.Refresh();
            Capture(cam,"hud-1280.png",1280,720);Capture(cam,"hud-1920.png",1920,1080);
            var manager=Object.FindFirstObjectByType<BlessingManager>();
            var offers=manager.all.Take(3).Select(b=>new BlessingOffer(b,1,null)).ToList();
            blessing.Open(offers,new List<BlessingData>(),4,_=>{});
            foreach(var c in blessing.cards){c.transform.localScale=Vector3.one;c.GetComponent<CanvasGroup>().alpha=1;}
            QuantumUiSkin.Blessings(blessing);
            blessing.cards[1].GetComponent<QuantumUiButtonState>().OnPointerEnter(new PointerEventData(EventSystem.current));
            Capture(cam,"blessings-1280.png",1280,720);overflow+=Overflow(blessing.panel,"blessings");
            blessing.panel.SetActive(false);
            help.Open();
            foreach(var language in new[]{GameLanguage.Thai,GameLanguage.English})
            {
                LanguageSettings.Current=language;
                for(int i=0;i<help.pages.Length;i++)
                {
                    help.SelectPage(i);Capture(cam,$"help-{language}-{i+1}.png",1280,720);
                    overflow+=Overflow(help.panel,$"help {language} {i}");
                }
            }
            LanguageSettings.Current=GameLanguage.Thai;help.SelectPage(0);Capture(cam,"help-1024.png",1024,768);
            help.Close();
            var state=help.entryButton.GetComponent<QuantumUiButtonState>();
            state.OnPointerEnter(new PointerEventData(EventSystem.current));Capture(cam,"help-button-hover.png",1280,720);
            state.OnPointerDown(new PointerEventData(EventSystem.current));Capture(cam,"help-button-pressed.png",1280,720);
            Debug.Log("UI_SKIN_PREVIEWS_COMPLETE overflow="+overflow);
        }
        finally{LanguageSettings.Current=saved;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        if(overflow>0)throw new Exception("Skin text overflow: "+overflow);
    }
    static int Overflow(GameObject root,string context)
    {
        int count=0;foreach(var t in root.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing){count++;Debug.LogWarning("SKIN_OVERFLOW "+context+" "+t.name);}}return count;
    }
    static void Capture(Camera cam,string file,int w,int h)
    {
        foreach(var t in Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))t.Apply();
        var target=new RenderTexture(w,h,24);cam.targetTexture=target;
        foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=c.GetComponent<GameHelpWindow>()!=null?.3f:1f;
            // เฉพาะภาพตรวจงาน: screen-space camera ต้องอยู่เหนือ sorting layer ของ Tilemap ด้วย
            c.overrideSorting=true;c.sortingLayerID=SortingLayer.layers.OrderBy(l=>l.value).Last().id;c.sortingOrder=c.GetComponent<GameHelpWindow>()!=null?30000:29000;
            var s=c.GetComponent<CanvasScaler>();if(s==null||s.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize)continue;
            float x=w/s.referenceResolution.x,y=h/s.referenceResolution.y;c.scaleFactor=s.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(x,y):s.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(x,y):Mathf.Pow(2,Mathf.Lerp(Mathf.Log(x,2),Mathf.Log(y,2),s.matchWidthOrHeight));
        }
        Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Out,file),texture.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
    }
}
