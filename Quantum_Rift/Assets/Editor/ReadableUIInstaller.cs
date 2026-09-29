using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// เปลี่ยนฟอนต์และขนาดข้อความจริง ไม่วาดตัวอักษรลงภาพ จึงสลับภาษาได้และคมทุกความละเอียด
public static class ReadableUIInstaller
{
    const string FontPath="Assets/Fonts/IBMPlexSansThaiLooped/IBM Plex UI SDF.asset";
    const string SourceFontPath="Assets/Fonts/IBMPlexSansThaiLooped/IBMPlexSansThaiLooped-Medium.ttf";
    static string previewPrefix="";
    static string Out
    {get{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-readableUIOutput");var path=i>=0&&i+1<args.Length?args[i+1]:Path.GetFullPath(Application.dataPath+"/../Temp/ReadableUI-v1");Directory.CreateDirectory(path);return path;}}
    static TMP_FontAsset font;
    static readonly Color Ink=new Color32(19,17,36,255),Panel=new Color32(33,28,53,255),Purple=new Color32(117,83,183,255),White=new Color32(247,245,255,255);
    static readonly StringBuilder report=new StringBuilder();
    // เปลี่ยนเฉพาะฟอนต์ทั้งเกม โดยรักษาขนาดและการจัดวาง UI ที่ปรับไว้แล้ว
    [MenuItem("Tools/Quantum Rift/Change All Game Fonts")]
    public static void ChangeAllGameFonts()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        font=MakeFont();DefaultFont();
        var source=AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        int labels=0,prefabs=0,scenes=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            if(path.StartsWith("Assets/TextMesh Pro/")||path.StartsWith("Assets/Settings/"))continue;
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentsInChildren<TMP_Text>(true).Any()&&!asset.GetComponentsInChildren<Text>(true).Any())continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{int count=ReplaceFont(root,source);labels+=count;if(count>0){PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;}}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            var scene=EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));int count=0;
            foreach(var root in scene.GetRootGameObjects())count+=ReplaceFont(root,source);
            labels+=count;if(count>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;}
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"GAME_FONT_INSTALLED font={font.name} labels={labels} prefabs={prefabs} scenes={scenes}");
        // ฟอนต์เริ่มต้นครอบคลุมป้ายที่สร้างระหว่างเล่น เช่น ดาเมจ ร้านบัพ และมินิแมพ
        var probe=new GameObject("Runtime font probe").AddComponent<TextMeshProUGUI>();
        if(probe.font!=font)throw new Exception("Runtime default font mismatch");
        Object.DestroyImmediate(probe.gameObject);
        var original=LanguageSettings.Current;
        try{foreach(var lang in new[]{GameLanguage.Thai,GameLanguage.English}){LanguageSettings.Current=lang;previewPrefix=lang==GameLanguage.Thai?"th-":"en-";Preview();}}
        finally{LanguageSettings.Current=original;previewPrefix="";}
    }
    static int ReplaceFont(GameObject root,Font source)
    {
        int count=0;
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            t.font=font;t.fontSharedMaterial=font.material;t.extraPadding=true;
            if(t is TextMeshProUGUI && t.rectTransform.anchorMin.y==t.rectTransform.anchorMax.y)
            {
                var rect=t.rectTransform;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(rect.rect.height,t.fontSize*1.75f));
            }
            EditorUtility.SetDirty(t);count++;
        }
        var shop=root.GetComponent<ShopWindow>();
        if(shop!=null)foreach(var slot in shop.slots)
        {
            var t=slot.nameText;t.rectTransform.sizeDelta=new Vector2(154,80);
            t.fontSizeMin=20;t.fontSizeMax=22;t.fontSize=22;t.enableAutoSizing=true;
        }
        foreach(var t in root.GetComponentsInChildren<Text>(true))
        {if(t.font==source)continue;t.font=source;EditorUtility.SetDirty(t);count++;}
        return count;
    }
    [MenuItem("Tools/Quantum Rift/Apply Readable UI")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        font=MakeFont();DefaultFont();
        int prefabs=0,scenes=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefab"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentsInChildren<TMP_Text>(true).Any()&&!asset.GetComponentsInChildren<Text>(true).Any())continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))Typography(t,path);foreach(var t in root.GetComponentsInChildren<Text>(true))Legacy(t);
                if(root.GetComponent<CharacterBoxUI>()!=null)CharacterCard(root);
                if(root.GetComponent<ShopWindow>()!=null)ShopLayout(root.GetComponent<ShopWindow>());
                var architect=root.GetComponent<ArchitectBossHud>();
                if(architect!=null)
                {
                    var label=architect.phaseLabel;label.textWrappingMode=TextWrappingModes.NoWrap;label.enableAutoSizing=false;label.fontSize=22;label.rectTransform.anchoredPosition=new Vector2(789,-1);label.rectTransform.sizeDelta=new Vector2(132,32);
                    var badge=(RectTransform)label.transform.parent.Find("PhaseBadgeBackground");badge.anchoredPosition=new Vector2(784,2);badge.sizeDelta=new Vector2(142,38);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var scene=EditorSceneManager.OpenScene(path);
            var roots=scene.GetRootGameObjects();bool changed=false;
            foreach(var root in roots)
            {
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){Typography(t,path);changed=true;}
                foreach(var t in root.GetComponentsInChildren<Text>(true)){Legacy(t);changed=true;}
            }
            if(path.EndsWith("MainMenu.unity")){MainMenu(scene);changed=true;}
            if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;}
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Out+"/typography-report.txt",report.ToString());
        Debug.Log($"READABLE_UI_INSTALLED prefabs={prefabs} scenes={scenes}");
        Preview();
    }
    static TMP_FontAsset MakeFont()
    {
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);if(existing!=null){AddSymbols(existing);return existing;}
        var source=AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        var f=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
        f.name="IBM Plex UI SDF";AssetDatabase.CreateAsset(f,FontPath);
        AssetDatabase.AddObjectToAsset(f.atlasTexture,f);AssetDatabase.AddObjectToAsset(f.material,f);
        var chars=Enumerable.Range(32,95).Concat(Enumerable.Range(0xE01,0xE3A-0xE01+1)).Concat(Enumerable.Range(0xE3F,0xE5B-0xE3F+1)).Select(i=>(uint)i).ToArray();
        if(!f.TryAddCharacters(chars,out uint[] missing))throw new Exception("Missing Thai/English glyphs: "+string.Join(",",missing));
        AddSymbols(f);f.atlasTexture.filterMode=FilterMode.Bilinear;EditorUtility.SetDirty(f);EditorUtility.SetDirty(f.atlasTexture);
        AssetDatabase.SaveAssets();return f;
    }
    static void AddSymbols(TMP_FontAsset f)
    {
        f.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        f.TryAddCharacters(Enumerable.Range(160,96).Concat(Enumerable.Range(0x2000,112)).Concat(new[]{0x2190,0x2192,0x2212,0x25A1}).Select(i=>(uint)i).ToArray(),out uint[] unused);
        f.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(f);EditorUtility.SetDirty(f.atlasTexture);
    }
    static void DefaultFont()
    {
        var settings=Resources.Load<TMP_Settings>("TMP Settings");var s=new SerializedObject(settings);
        s.FindProperty("m_defaultFontAsset").objectReferenceValue=font;s.FindProperty("m_defaultFontSize").floatValue=28;s.ApplyModifiedProperties();EditorUtility.SetDirty(settings);
    }
    static void Typography(TMP_Text t,string path)
    {
        if(t.font==font){TextSpace(t);return;}
        float old=t.fontSize;t.font=font;t.fontSharedMaterial=font.material;
        bool world=t is TextMeshPro;float size=world?Mathf.Max(2.8f,old*1.16f):Mathf.Max(24,old*1.15f);
        t.fontSize=size;t.fontStyle&=~FontStyles.Bold;t.characterSpacing=0;t.lineSpacing=4;
        if(t.enableAutoSizing){t.fontSizeMax=Mathf.Max(size,t.fontSizeMax);t.fontSizeMin=world?Mathf.Max(2.6f,t.fontSizeMin):Mathf.Min(t.fontSizeMax,Mathf.Max(22,t.fontSizeMin));}
        t.extraPadding=true;TextSpace(t);EditorUtility.SetDirty(t);
        report.AppendLine($"{path} | {t.transform.parent?.name}/{t.name} | {old} -> {size} | {t.rectTransform.rect.size}");
    }
    static void TextSpace(TMP_Text t)
    {
        if(t is TextMeshPro)return;
        var r=t.rectTransform;
        // เว้นที่ให้สระบน/ล่างครบ ไม่ให้เพิ่มขนาดแล้วชนขอบหน้ากาก
        if(r.anchorMin.y==r.anchorMax.y && r.rect.height<t.fontSize*1.55f)r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,t.fontSize*1.55f);
    }
    static void ShopLayout(ShopWindow shop)
    {
        foreach(var slot in shop.slots)
        {
            var t=slot.nameText;t.textWrappingMode=TextWrappingModes.Normal;t.rectTransform.sizeDelta=new Vector2(146,72);t.rectTransform.anchoredPosition=new Vector2(0,-95);t.enableAutoSizing=true;t.fontSizeMin=22;t.fontSizeMax=24;
            slot.icon.rectTransform.anchoredPosition=new Vector2(0,8);
            var border=Rect(t.transform.parent,"ReadableNameBorder",t.rectTransform.anchoredPosition,new Vector2(160,76),Purple);border.SetSiblingIndex(t.transform.GetSiblingIndex());
            var fill=Rect(border,"Fill",Vector2.zero,new Vector2(154,70),Panel);
            t.transform.SetAsLastSibling();
        }
        shop.messageText.rectTransform.sizeDelta=new Vector2(650,88);
        var message=shop.messageText;
        var back=Rect(message.transform.parent,"ReadableMessageBorder",message.rectTransform.anchoredPosition,new Vector2(674,92),Purple);back.SetSiblingIndex(message.transform.GetSiblingIndex());Rect(back,"Fill",Vector2.zero,new Vector2(668,86),Panel);message.transform.SetAsLastSibling();
    }
    static void Legacy(Text t){t.font=AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);t.fontSize=Mathf.Max(24,Mathf.RoundToInt(t.fontSize*1.15f));EditorUtility.SetDirty(t);}
    static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size,Color? color=null)
    {
        var found=parent.Find(name);var r=found!=null?(RectTransform)found:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,pos,size);
        if(color.HasValue){var image=r.GetComponent<Image>();if(image==null)image=r.gameObject.AddComponent<Image>();image.sprite=null;image.color=color.Value;image.raycastTarget=false;}
        return r;
    }
    static void Place(RectTransform r,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=pos;r.sizeDelta=size;}
    static TextMeshProUGUI Label(Transform parent,string name,Vector2 pos,Vector2 size,string value,float fontSize,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var r=Rect(parent,name,pos,size);r.gameObject.SetActive(true);var t=r.GetComponent<TextMeshProUGUI>();if(t==null)t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSharedMaterial=font.material;t.text=value;t.fontSize=fontSize;t.enableAutoSizing=false;t.fontStyle=FontStyles.Normal;t.color=White;t.alignment=align;t.raycastTarget=false;t.extraPadding=true;return t;
    }
    static void Localize(TMP_Text t,string th,string en){var l=t.GetComponent<LocalizedText>();if(l==null)l=t.gameObject.AddComponent<LocalizedText>();l.thaiText=th;l.englishText=en;l.Apply();}
    static void CharacterCard(GameObject root)
    {
        var box=root.GetComponent<CharacterBoxUI>();foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        Place((RectTransform)root.transform,Vector2.zero,new Vector2(1280,670));root.GetComponent<Image>().color=Purple;root.GetComponent<Image>().sprite=null;
        Rect(root.transform,"Inset",Vector2.zero,new Vector2(1272,662),Ink);
        Rect(root.transform,"PortraitPanel",new Vector2(-425,0),new Vector2(388,626),Panel);
        box.classNameText=Label(root.transform,"HeroName",new Vector2(-425,255),new Vector2(366,74),"นักรบ",42);
        box.characterImage=Rect(root.transform,"HeroPortrait",new Vector2(-425,55),new Vector2(460,390)).gameObject.AddComponent<Image>();box.characterImage.preserveAspect=true;box.characterImage.raycastTarget=false;
        box.statsText=Label(root.transform,"HeroStats",new Vector2(-425,-203),new Vector2(330,186),"",32,TextAlignmentOptions.Left);box.statsText.lineSpacing=12;
        for(int i=0;i<2;i++)
        {
            var panel=Rect(root.transform,"SkillPanel"+i,new Vector2(215,i==0?161:-145),new Vector2(780,276),Panel);
            var key=Label(panel,"Key",new Vector2(-337,91),new Vector2(56,48),i==0?"Q":"E",32);key.color=new Color32(137,234,243,255);
            var icon=Rect(panel,"Icon",new Vector2(-329,10),new Vector2(90,90)).gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;
            var title=Label(panel,"Name",new Vector2(21,87),new Vector2(624,70),"",34,TextAlignmentOptions.Left);
            var info=Label(panel,"Cost",new Vector2(20,-85),new Vector2(684,46),"",24,TextAlignmentOptions.Left);info.color=new Color32(160,228,240,255);
            var desc=Label(panel,"Description",new Vector2(53,0),new Vector2(574,88),"",26,TextAlignmentOptions.Left);desc.enableAutoSizing=true;desc.fontSizeMin=24;desc.fontSizeMax=26;
            if(i==0){box.skill1Text=title;box.skill1Icon=icon;box.skill1Info=info;box.skill1Description=desc;}
            else{box.skill2Text=title;box.skill2Icon=icon;box.skill2Info=info;box.skill2Description=desc;}
        }
    }
    static void MainMenu(Scene scene)
    {
        var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).First(c=>c.name=="Canvas");
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var panel=canvas.transform.Find("CharacterSelect_Panel");var panelRect=(RectTransform)panel;panelRect.anchorMin=Vector2.zero;panelRect.anchorMax=Vector2.one;panelRect.offsetMin=panelRect.offsetMax=Vector2.zero;
        panel.GetComponent<Image>().color=Ink;panel.GetComponent<Image>().sprite=null;
        var backdrop=Rect(panel,"ReadableBackdrop",Vector2.zero,new Vector2(1920,1080),Ink);backdrop.SetAsFirstSibling();
        var title=Label(panel,"ReadableTitle",new Vector2(0,442),new Vector2(880,78),"เลือกตัวละคร",50);Localize(title,"เลือกตัวละคร","CHOOSE YOUR HERO");
        var hint=Label(panel,"ReadableHint",new Vector2(0,373),new Vector2(1200,46),"",26);hint.color=new Color32(185,179,208,255);Localize(hint,"เลือกฮีโร่ แล้วตรวจสอบค่าสถานะและสกิลก่อนเริ่มเกม","Choose a hero and review their stats and skills");
        var manager=canvas.GetComponent<CharacterSelectionManager>();if(manager==null)manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CharacterSelectionManager>(true)).First();
        var container=(RectTransform)manager.characterContainer;Place(container,new Vector2(0,-10),new Vector2(1280,670));
        foreach(var layout in container.GetComponents<LayoutGroup>())layout.enabled=false;
        foreach(var fit in container.GetComponents<ContentSizeFitter>())fit.enabled=false;
        foreach(var button in panel.GetComponentsInChildren<Button>(true))
        {
            if(button.name=="BACK"){Place((RectTransform)button.transform,new Vector2(-730,440),new Vector2(350,96));ButtonLabel(button,"ย้อนกลับ","BACK",34);}
            else if(button.name=="Start"){Place((RectTransform)button.transform,new Vector2(0,-441),new Vector2(510,114));ButtonLabel(button,"เริ่มเกม","START GAME",42);}
            else if(button.name=="<"||button.name==">")
            {
                // อ้างทิศจากคำสั่งที่ผูกไว้ เพราะชื่อวัตถุลูกศรเดิมสลับด้าน
                bool next=Enumerable.Range(0,button.onClick.GetPersistentEventCount()).Any(i=>button.onClick.GetPersistentMethodName(i)=="NextCharacter");
                Place((RectTransform)button.transform,new Vector2(next?740:-740,-10),new Vector2(116,116));button.GetComponent<Image>().sprite=null;button.GetComponent<Image>().color=Panel;
                foreach(var old in button.GetComponentsInChildren<TMP_Text>(true))old.gameObject.SetActive(false);
                Label(button.transform,"ReadableArrow",Vector2.zero,new Vector2(100,90),next?">":"<",60);
            }
        }
        foreach(var b in canvas.transform.Find("Background").GetComponentsInChildren<Button>(true))
        {var t=b.GetComponentInChildren<TMP_Text>(true);if(t==null)continue;t.font=font;t.fontSharedMaterial=font.material;t.fontSize=44;t.enableAutoSizing=false;t.fontStyle=FontStyles.Normal;Place(t.rectTransform,Vector2.zero,new Vector2(360,78));}
        foreach(var settings in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SettingsMenu>(true)))if(settings.settingsPanel!=null)settings.settingsPanel.SetActive(false);
        var obsolete=panel.Find("SkillDetail_Panel");if(obsolete!=null)obsolete.gameObject.SetActive(false);
        // หน้าเริ่มแสดงเมนูหลัก ส่วนการ์ดและรายละเอียดเปิดเมื่อกดเริ่มตามเดิม
        var menu=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).First();menu.mainMenuUI.SetActive(true);menu.characterSelectUI.SetActive(false);if(menu.characterDetailUI!=null)menu.characterDetailUI.SetActive(false);
    }
    static void ButtonLabel(Button b,string th,string en,float size)
    {
        var text=b.GetComponentInChildren<TMP_Text>(true);if(text==null)text=Label(b.transform,"ReadableLabel",Vector2.zero,new Vector2(360,78),th,size);
        text.gameObject.SetActive(true);text.font=font;text.fontSharedMaterial=font.material;text.fontStyle=FontStyles.Normal;text.enableAutoSizing=false;text.fontSize=size;
        Place(text.rectTransform,Vector2.zero,new Vector2(((RectTransform)b.transform).sizeDelta.x*.76f,78));Localize(text,th,en);
        var feedback=b.GetComponent<MenuButtonLabelFeedback>();if(feedback!=null)feedback.restingPosition=Vector2.zero;
    }
    public static void Preview()
    {
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var roots=scene.GetRootGameObjects();var menu=roots.SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).First();menu.OnNewGameClicked();
        foreach(var settings in roots.SelectMany(r=>r.GetComponentsInChildren<SettingsMenu>(true)))settings.Close();
        var obsolete=menu.characterSelectUI.transform.Find("SkillDetail_Panel");if(obsolete!=null)obsolete.gameObject.SetActive(false);
        var manager=roots.SelectMany(r=>r.GetComponentsInChildren<CharacterSelectionManager>(true)).First();
        var canvas=manager.GetComponent<Canvas>();if(canvas==null)canvas=manager.GetComponentInParent<Canvas>();
        var camera=Camera.main;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.orthographic=true;
        int index=0;
        foreach(var data in manager.allCharacters)
        {
            var card=Object.Instantiate(manager.characterBoxPrefab,manager.characterContainer);card.GetComponent<CharacterBoxUI>().SetupBox(data);
            Capture(camera,1280,720,"character-"+(index++)+"-1280.png");
            foreach(var t in card.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)Debug.LogWarning("READABLE_OVERFLOW "+data.name+"/"+t.name+" "+t.text);}
            Object.DestroyImmediate(card);
        }
        var first=Object.Instantiate(manager.characterBoxPrefab,manager.characterContainer);first.GetComponent<CharacterBoxUI>().SetupBox(manager.allCharacters[0]);Capture(camera,1920,1080,"character-1920.png");Capture(camera,1024,768,"character-1024.png");Object.DestroyImmediate(first);
        menu.OnBackClicked();Capture(camera,1280,720,"main-menu-1280.png");
        var menuSettings=roots.SelectMany(r=>r.GetComponentsInChildren<SettingsMenu>(true)).First();menuSettings.Open();Capture(camera,1280,720,"settings-1280.png");menuSettings.Close();
        AuditOtherUI();
        Debug.Log("READABLE_UI_PREVIEW_COMPLETE");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    static void AuditOtherUI()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("UI audit camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.backgroundColor=Ink;camera.clearFlags=CameraClearFlags.SolidColor;
        var window=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopWindow.prefab")).GetComponent<ShopWindow>();window.content.SetActive(true);
        var canvas=window.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopSpaceship.prefab").GetComponent<ShopClickable>();
        var weapons=AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).OrderByDescending(w=>w.weaponName.Length).ToArray();
        for(int i=0;i<window.slots.Length;i++){var offer=i<2?new ShopOffer(i==0?ShopOffer.Kind.HpPotion:ShopOffer.Kind.EnergyPotion,10):new ShopOffer(ShopOffer.Kind.Weapon,50,weapons[i-2]);window.slots[i].Show(offer,offer.Icon(source.itemPool),true);}
        window.currencyText.text="9999";window.messageText.text="คลิกที่ราคาเพื่อซื้อ · ซื้อได้ครั้งละ 1 ชิ้น";Capture(camera,1280,720,"shop-1280.png");
        foreach(var t in window.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)Debug.LogWarning("READABLE_OVERFLOW shop/"+t.name+" "+t.text);}
        foreach(Transform child in window.content.transform)child.gameObject.SetActive(false);
        var red=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopBuffRed.prefab")).GetComponent<ShopClickable>();
        foreach(var sprite in red.GetComponentsInChildren<SpriteRenderer>())sprite.enabled=false;
        var player=new GameObject("Audit player").AddComponent<PlayerStats>();player.currentCurrency=9999;
        var view=BuffShopView.Create(window,window.content.transform);view.Refresh(red,player);view.Say("บัพอยู่จนจบรอบ · เริ่มเกมใหม่จะรีเซ็ต",White);Capture(camera,1280,720,"buff-shop-1280.png");
        foreach(var t in view.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)Debug.LogWarning("READABLE_OVERFLOW buff/"+t.name+" "+t.text);}
        Object.DestroyImmediate(window.gameObject);Object.DestroyImmediate(red.gameObject);Object.DestroyImmediate(player.gameObject);
        var game=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");var roots=game.GetRootGameObjects();camera=Camera.main;
        foreach(var settings in roots.SelectMany(r=>r.GetComponentsInChildren<SettingsMenu>(true)))settings.Close();
        foreach(var pause in roots.SelectMany(r=>r.GetComponentsInChildren<PauseManager>(true)))pause.pauseMenuPanel.SetActive(false);
        foreach(var summary in roots.SelectMany(r=>r.GetComponentsInChildren<SummaryManager>(true)))summary.summaryPanel.SetActive(false);
        foreach(var blessings in roots.SelectMany(r=>r.GetComponentsInChildren<BlessingWindow>(true)))blessings.panel.SetActive(false);
        var hud=roots.SelectMany(r=>r.GetComponentsInChildren<HUDManager>(true)).First();hud.transitionCanvas.gameObject.SetActive(false);hud.UpdateHP(8,13);hud.UpdateEnergy(150,250);hud.UpdateCurrency(9999);
        foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true))){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
        Capture(camera,1280,720,"hud-1280.png");
        foreach(string boss in new[]{"EchoCommander","AncientEntborn","Architect"})
        {
            var bar=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/UI/BossHealth-v1/{boss}-HealthHUD.prefab"));var b=bar.GetComponent<BossHealthHud>();if(b==null)b=bar.GetComponentInChildren<BossHealthHud>();b.Show(1234,2000);
            foreach(var c in bar.GetComponentsInChildren<Canvas>()){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.5f;}
            Capture(camera,1280,720,"boss-"+boss+"-1280.png");Object.DestroyImmediate(bar);
        }
    }
    static void Capture(Camera camera,int w,int h,string name)
    {
        name=previewPrefix+name;
        foreach(var label in Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))label.Apply();
        var target=new RenderTexture(w,h,24);camera.targetTexture=target;
        // คำนวณสเกลสำหรับขนาดภาพทดสอบ ไม่เรียก Update ของ Behaviour ใน Edit Mode
        foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var s=c.GetComponent<CanvasScaler>();if(s==null||s.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize)continue;
            float x=w/s.referenceResolution.x,y=h/s.referenceResolution.y;
            c.scaleFactor=s.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(x,y):s.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(x,y):Mathf.Pow(2,Mathf.Lerp(Mathf.Log(x,2),Mathf.Log(y,2),s.matchWidthOrHeight));
        }
        Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(w,h,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,w,h),0,0);pixels.Apply();File.WriteAllBytes(Out+"/"+name,pixels.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);
    }
}
