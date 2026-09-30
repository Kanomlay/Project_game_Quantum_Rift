using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class GameHelpInstaller
{
    public const string PrefabPath="Assets/Prefab/UI/Help/GameHelp.prefab";
    static readonly Color Ink=new Color32(17,15,29,255),Surface=new Color32(32,28,48,255),Border=new Color32(105,76,153,255),Cyan=new Color32(130,231,242,255),Muted=new Color32(179,170,202,255);
    static TMP_FontAsset font;
    static string Out
    {
        get {var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-helpOutput");string p=i>=0&&i+1<a.Length?a[i+1]:Path.GetFullPath(Application.dataPath+"/../Temp/HelpPreview");Directory.CreateDirectory(p);return p;}
    }
    [MenuItem("Tools/Quantum Rift/Install Game Help")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        font=TMP_Settings.defaultFontAsset;
        if(font==null)throw new Exception("Missing default game font");
        if(!AssetDatabase.IsValidFolder("Assets/Prefab/UI/Help"))AssetDatabase.CreateFolder("Assets/Prefab/UI","Help");
        var root=Build();
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        Object.DestroyImmediate(root);
        foreach(string scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/GameScene.unity"})
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            var existing=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameHelpWindow>(true)).FirstOrDefault();
            if(existing!=null)Object.DestroyImmediate(existing.gameObject);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            var help=go.GetComponent<GameHelpWindow>();
            help.pauseGameplay=scenePath.EndsWith("GameScene.unity");
            ConfigureEntry(help);help.Refresh();help.panel.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(help);
            foreach(var t in go.GetComponentsInChildren<RectTransform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            foreach(var t in go.GetComponentsInChildren<TMP_Text>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();Debug.Log("GAME_HELP_INSTALLED scenes=2 pages=8 languages=TH,EN");
        Preview();
    }
    static void ConfigureEntry(GameHelpWindow h)
    {
        var r=(RectTransform)h.entryButton.transform;
        r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;
        r.anchoredPosition=new Vector2(24,40);
        r.sizeDelta=h.pauseGameplay?new Vector2(68,68):new Vector2(264,68);
        h.entryLabel.fontSize=h.pauseGameplay?46:27;
        h.entryLabel.rectTransform.sizeDelta=h.pauseGameplay?new Vector2(66,80):new Vector2(252,64);
        h.entryHint.rectTransform.anchoredPosition=new Vector2(h.pauseGameplay?6:0,-54);
        h.entryHint.rectTransform.sizeDelta=new Vector2(180,36);
        h.entryHint.fontSize=19;
    }
    static GameObject Build()
    {
        var root=new GameObject("GameHelp",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.layer=5;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var h=root.AddComponent<GameHelpWindow>();h.pages=GameHelpContent.Create();
        h.entryButton=Button(root.transform,"OpenHelp",Vector2.zero,new Vector2(264,68),"",28);
        h.entryLabel=h.entryButton.GetComponentInChildren<TMP_Text>();
        h.entryHint=Label(h.entryButton.transform,"HelpShortcut",new Vector2(0,-54),new Vector2(180,36),"F1",19,Muted);
        UnityEventTools.AddPersistentListener(h.entryButton.onClick,h.Open);
        var backdrop=Box(root.transform,"HelpOverlay",Vector2.zero,Vector2.zero,new Color(0,0,0,.84f));Stretch(backdrop.rectTransform);backdrop.raycastTarget=true;h.panel=backdrop.gameObject;
        var window=Box(backdrop.transform,"GuideWindow",Vector2.zero,new Vector2(1480,810),Border);
        Box(window.transform,"Inner",Vector2.zero,new Vector2(1472,802),Ink);
        Box(window.transform,"AccentTop",new Vector2(0,402),new Vector2(160,4),Cyan);
        Box(window.transform,"Sidebar",new Vector2(-558,0),new Vector2(328,782),Surface);
        Label(window.transform,"Brand",new Vector2(-558,343),new Vector2(286,84),"QUANTUM RIFT",29,Cyan);
        Label(window.transform,"GuideCaption",new Vector2(-558,286),new Vector2(290,44),"FIELD GUIDE  /  01",18,Muted);
        h.title=Label(window.transform,"PageTitle",new Vector2(42,328),new Vector2(810,86),"",42,Color.white,TextAlignmentOptions.Left);
        h.summary=Label(window.transform,"PageSummary",new Vector2(154,263),new Vector2(1034,54),"",24,Muted,TextAlignmentOptions.Left);
        h.closeButton=Button(window.transform,"Close",new Vector2(610,356),new Vector2(198,58),"",24);
        h.closeLabel=h.closeButton.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(h.closeButton.onClick,h.Close);
        h.tabs=new Button[h.pages.Length];h.tabLabels=new TMP_Text[h.pages.Length];h.tabBackgrounds=new Image[h.pages.Length];
        for(int i=0;i<h.pages.Length;i++)
        {
            var b=Button(window.transform,"Topic"+i,new Vector2(-558,218-i*63),new Vector2(296,56),"",25);
            h.tabs[i]=b;h.tabLabels[i]=b.GetComponentInChildren<TMP_Text>();h.tabLabels[i].alignment=TextAlignmentOptions.Left;h.tabLabels[i].rectTransform.sizeDelta=new Vector2(266,54);
            h.tabBackgrounds[i]=b.GetComponent<Image>();UnityEventTools.AddIntPersistentListener(b.onClick,h.SelectPage,i);
        }
        h.sectionTitles=new TMP_Text[3];h.sectionBodies=new TMP_Text[3];h.sectionAccents=new Image[3];
        for(int i=0;i<3;i++)
        {
            var card=Box(window.transform,"Section"+i,new Vector2(161,143-i*186),new Vector2(1034,170),Surface);
            h.sectionAccents[i]=Box(card.transform,"Accent",new Vector2(-514,0),new Vector2(5,170),Cyan);
            h.sectionTitles[i]=Label(card.transform,"Heading",new Vector2(12,51),new Vector2(970,58),"",28,Cyan,TextAlignmentOptions.Left);
            h.sectionBodies[i]=Label(card.transform,"Body",new Vector2(12,-26),new Vector2(970,108),"",25,new Color32(234,230,242,255),TextAlignmentOptions.TopLeft);
            h.sectionBodies[i].lineSpacing=-3;
        }
        h.statusLabel=Label(window.transform,"ReadingStatus",new Vector2(-558,-286),new Vector2(282,70),"",20,Cyan);
        Box(window.transform,"FooterRule",new Vector2(161,-333),new Vector2(1034,2),Border);
        h.footer=Label(window.transform,"PageNumber",new Vector2(8,-364),new Vector2(724,50),"",22,Muted,TextAlignmentOptions.Left);
        h.previousButton=Button(window.transform,"Previous",new Vector2(525,-365),new Vector2(88,54),"<",30);
        h.nextButton=Button(window.transform,"Next",new Vector2(635,-365),new Vector2(88,54),">",30);
        UnityEventTools.AddPersistentListener(h.previousButton.onClick,h.Previous);UnityEventTools.AddPersistentListener(h.nextButton.onClick,h.Next);
        h.languageButton=Button(window.transform,"Language",new Vector2(-558,-357),new Vector2(274,54),"",24);h.languageLabel=h.languageButton.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(h.languageButton.onClick,h.ToggleLanguage);
        ConfigureEntry(h);h.Refresh();h.panel.SetActive(false);return root;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Image Box(Transform p,string n,Vector2 pos,Vector2 size,Color color){var im=Rect(p,n,pos,size).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
    static TMP_Text Label(Transform p,string n,Vector2 pos,Vector2 size,string value,float fs,Color color,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var t=Rect(p,n,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSharedMaterial=font.material;t.text=value;t.fontSize=fs;t.color=color;t.alignment=align;t.raycastTarget=false;t.extraPadding=true;t.textWrappingMode=TextWrappingModes.Normal;t.fontStyle=FontStyles.Normal;return t;
    }
    static Button Button(Transform p,string n,Vector2 pos,Vector2 size,string text,float fs)
    {
        var image=Box(p,n,pos,size,Border);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
        var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color32(176,237,255,255);c.selectedColor=c.highlightedColor;c.pressedColor=new Color32(104,164,211,255);c.disabledColor=new Color(.45f,.45f,.45f,.6f);c.fadeDuration=.08f;b.colors=c;
        b.navigation=new Navigation{mode=Navigation.Mode.Automatic};
        Label(image.transform,"Label",Vector2.zero,new Vector2(size.x-18,Mathf.Max(size.y,fs*1.8f)),text,fs,Color.white);return b;
    }

    public static void Preview()
    {
        var previousLanguage=LanguageSettings.Current;
        int overflows=0;
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var h=Object.FindFirstObjectByType<GameHelpWindow>();var cam=Camera.main;
            h.panel.SetActive(false);Capture(cam,1280,720,"main-menu-help.png");
            h.Open();
            foreach(var language in new[]{GameLanguage.Thai,GameLanguage.English})
            {
                LanguageSettings.Current=language;
                for(int i=0;i<h.pages.Length;i++)
                {
                    h.SelectPage(i);Capture(cam,1280,720,$"{language}-page-{i+1:00}.png");
                    foreach(var t in h.panel.GetComponentsInChildren<TMP_Text>())
                    {t.ForceMeshUpdate();if(t.isTextOverflowing){overflows++;Debug.LogWarning($"HELP_OVERFLOW {language} page={i+1} {t.name}: {t.text}");}}
                }
            }
            LanguageSettings.Current=GameLanguage.Thai;h.SelectPage(1);Capture(cam,1920,1080,"help-1920.png");Capture(cam,1024,768,"help-1024.png");h.Close();
            scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");cam=Camera.main;h=Object.FindFirstObjectByType<GameHelpWindow>();
            foreach(var settings in Object.FindObjectsByType<SettingsMenu>(FindObjectsSortMode.None))settings.Close();
            foreach(var pause in Object.FindObjectsByType<PauseManager>(FindObjectsSortMode.None))pause.pauseMenuPanel.SetActive(false);
            foreach(var s in Object.FindObjectsByType<SummaryManager>(FindObjectsSortMode.None))s.summaryPanel.SetActive(false);
            foreach(var b in Object.FindObjectsByType<BlessingWindow>(FindObjectsSortMode.None))b.panel.SetActive(false);
            var hud=Object.FindFirstObjectByType<HUDManager>();hud.transitionCanvas.gameObject.SetActive(false);hud.UpdateHP(8,13);hud.UpdateEnergy(100,150);hud.UpdateCurrency(230);
            Capture(cam,1280,720,"game-help-button.png");h.Open();h.SelectPage(6);Capture(cam,1280,720,"game-help-open.png");h.Close();
            Debug.Log($"HELP_PREVIEW_COMPLETE overflow={overflows}");
        }
        finally{LanguageSettings.Current=previousLanguage;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        if(overflows>0)throw new Exception("Help text overflow: "+overflows);
    }
    static void Capture(Camera cam,int w,int h,string name)
    {
        foreach(var label in Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))label.Apply();
        var target=new RenderTexture(w,h,24);cam.targetTexture=target;
        foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=c.GetComponent<GameHelpWindow>()!=null?.3f:1f;
            var s=c.GetComponent<CanvasScaler>();if(s==null||s.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize)continue;
            float x=w/s.referenceResolution.x,y=h/s.referenceResolution.y;c.scaleFactor=s.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(x,y):s.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(x,y):Mathf.Pow(2,Mathf.Lerp(Mathf.Log(x,2),Mathf.Log(y,2),s.matchWidthOrHeight));
        }
        Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(w,h,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,w,h),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Out,name),pixels.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);
    }
}
