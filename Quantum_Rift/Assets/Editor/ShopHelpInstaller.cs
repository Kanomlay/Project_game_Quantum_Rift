using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ShopHelpInstaller
{
    const string PathToShop="Assets/Prefab/Shop/ShopWindow.prefab";
    static string Out {get{var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-shopHelpOutput");var p=i>=0&&i+1<a.Length?a[i+1]:Path.GetFullPath(Application.dataPath+"/../Temp/ShopHelp");Directory.CreateDirectory(p);return p;}}
    [MenuItem("Tools/Quantum Rift/Install Shop Help")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var root=PrefabUtility.LoadPrefabContents(PathToShop);
        try
        {
            var window=root.GetComponent<ShopWindow>();window.helpPrefab=AssetDatabase.LoadAssetAtPath<GameHelpWindow>(GameHelpInstaller.PrefabPath);
            if(window.helpPrefab==null)throw new Exception("Install Game Help first");
            var ui=root.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="ShopUI");
            var title=ui.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition=new Vector2(-95,title.rectTransform.anchoredPosition.y);title.rectTransform.sizeDelta=new Vector2(430,title.rectTransform.sizeDelta.y);
            var header=MakeButton(ui,"ShopHelp",new Vector2(178,212),new Vector2(52,52),32);
            UnityEventTools.AddPersistentListener(header.onClick,window.OpenHelp);
            for(int i=0;i<window.slots.Length;i++)
            {
                var b=MakeButton(window.slots[i].transform,"ItemHelp",new Vector2(52,51),new Vector2(38,40),24);
                UnityEventTools.AddIntPersistentListener(b.onClick,window.OpenOfferHelp,i);
            }
            PrefabUtility.SaveAsPrefabAsset(root,PathToShop);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("SHOP_HELP_INSTALLED");Preview();
    }
    static Button MakeButton(Transform parent,string name,Vector2 pos,Vector2 size,float fontSize)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;
        var im=go.GetComponent<Image>();im.color=new Color32(83,57,119,255);var b=go.GetComponent<Button>();b.targetGraphic=im;
        var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color32(145,230,255,255);c.selectedColor=c.highlightedColor;c.pressedColor=new Color32(93,151,191,255);b.colors=c;
        var text=new GameObject("Question",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();text.gameObject.layer=5;text.transform.SetParent(r,false);text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=Vector2.one*.5f;text.rectTransform.sizeDelta=new Vector2(size.x,Mathf.Max(size.y,fontSize*1.8f));text.font=TMP_Settings.defaultFontAsset;text.text="?";text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return b;
    }
    public static void Preview()
    {
        var savedLanguage=LanguageSettings.Current;var savedRandom=UnityEngine.Random.state;int overflow=0;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Shop help preview").AddComponent<Camera>();cam.transform.position=new Vector3(0,0,-10);cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color32(16,14,29,255);
            var player=new GameObject("Preview player").AddComponent<PlayerStats>();player.gameObject.tag="Player";player.currentCurrency=250;
            var prefab=AssetDatabase.LoadAssetAtPath<ShopWindow>(PathToShop);
            foreach(bool red in new[]{false,true})
            {
                var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(red?"Assets/Prefab/Shop/ShopBuffRed.prefab":"Assets/Prefab/Shop/ShopSpaceship.prefab")).GetComponent<ShopClickable>();
                foreach(var sprite in source.GetComponentsInChildren<SpriteRenderer>())sprite.enabled=false;
                ShopWindow.Open(prefab,source);var window=ShopWindow.Active;Capture(cam,red?"buff-shop-buttons.png":"weapon-shop-buttons.png");
                window.OpenHelp();var help=GameHelpWindow.Instance;
                foreach(var language in new[]{GameLanguage.Thai,GameLanguage.English})
                {
                    LanguageSettings.Current=language;
                    for(int i=0;i<help.pages.Length;i++)
                    {
                        help.SelectPage(i);Capture(cam,$"{(red?"buff":"shop")}-{language}-{i}.png");overflow+=CheckText(help,$"{red}/{language}/{i}");
                    }
                }
                help.Close();window.Close();Object.DestroyImmediate(source.gameObject);
            }
            var normal=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopSpaceship.prefab")).GetComponent<ShopClickable>();ShopWindow.Open(prefab,normal);ShopWindow.Active.OpenHelp();var guide=GameHelpWindow.Instance;
            foreach(var guid in AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/Data"}))
            {
                var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
                guide.pages=new[]{ShopHelpPages.Offer(new ShopOffer(ShopOffer.Kind.Weapon,50,weapon),normal.itemPool,2)};
                foreach(var language in new[]{GameLanguage.Thai,GameLanguage.English})
                {LanguageSettings.Current=language;guide.SelectPage(0);Canvas.ForceUpdateCanvases();overflow+=CheckText(guide,weapon.name+"/"+language);}
            }
            guide.Close();ShopWindow.Active.Close();
            Debug.Log("SHOP_HELP_PREVIEW_COMPLETE overflow="+overflow);
        }
        finally{LanguageSettings.Current=savedLanguage;UnityEngine.Random.state=savedRandom;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        if(overflow>0)throw new Exception("Shop help text overflow: "+overflow);
    }
    static int CheckText(GameHelpWindow guide,string context)
    {
        int n=0;foreach(var t in guide.panel.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing){n++;Debug.LogWarning("SHOP_HELP_OVERFLOW "+context+" "+t.name+": "+t.text);}}return n;
    }
    static void Capture(Camera cam,string name)
    {
        const int w=1280,h=720;var target=new RenderTexture(w,h,24);cam.targetTexture=target;
        foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=c.GetComponent<GameHelpWindow>()!=null?.3f:1f;
            var s=c.GetComponent<CanvasScaler>();if(s==null||s.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize)continue;
            float x=w/s.referenceResolution.x,y=h/s.referenceResolution.y;c.scaleFactor=s.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(x,y):s.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(x,y):Mathf.Pow(2,Mathf.Lerp(Mathf.Log(x,2),Mathf.Log(y,2),s.matchWidthOrHeight));
        }
        Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();File.WriteAllBytes(System.IO.Path.Combine(Out,name),texture.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
    }
}
