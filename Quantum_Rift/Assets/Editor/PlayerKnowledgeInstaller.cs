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

public static class PlayerKnowledgeInstaller
{
    public const string LibraryPath="Assets/Data/Monster/MonsterCollection.asset";
    public const string PrefabPath="Assets/Prefab/UI/Help/MonsterCollection.prefab";
    static TMP_FontAsset font;
    static readonly Color Ink=new Color32(14,17,31,255),Surface=new Color32(28,28,48,255),White=new Color32(234,237,247,255),Muted=new Color32(173,182,207,255);
    public static string Output
    {
        get{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-knowledgeOutput");string path=i>=0?args[i+1]:Path.GetFullPath("../KnowledgePreviews");Directory.CreateDirectory(path);return path;}
    }
    [MenuItem("Tools/Quantum Rift/Install Room Icons and Monster Collection")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/IBMPlexSansThaiLooped/IBM Plex UI SDF.asset");
        if(font==null)throw new Exception("Missing readable UI font");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var library=BuildLibrary();var root=BuildCollection(library);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Object.DestroyImmediate(root);
            var menu=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var manager=menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CharacterSelectionManager>(true)).First();
            string cardPath=AssetDatabase.GetAssetPath(manager.characterBoxPrefab);var card=PrefabUtility.LoadPrefabContents(cardPath);
            try{StarterCard(card);PrefabUtility.SaveAsPrefabAsset(card,cardPath);}finally{PrefabUtility.UnloadPrefabContents(card);}
            foreach(string scenePath in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/GameScene.unity","Assets/Scenes/TutorialScene.unity"})
            {
                var scene=EditorSceneManager.OpenScene(scenePath);
                foreach(var old in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonsterCollectionWindow>(true)).ToArray())Object.DestroyImmediate(old.gameObject);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);var window=instance.GetComponent<MonsterCollectionWindow>();
                window.pauseGameplay=!scenePath.EndsWith("MainMenu.unity");
                // คู่มือเดิมใช้ฐาน 1600 แต่หน้าคอลเลกชันใช้ 1920 จึงแปลงพิกัดเพื่อไม่ให้ปุ่มทับกัน
                var button=(RectTransform)window.entryButton.transform;button.anchorMin=button.anchorMax=button.pivot=Vector2.zero;button.anchoredPosition=new Vector2(window.pauseGameplay?132:370,48);button.sizeDelta=new Vector2(window.pauseGameplay?226:300,68);
                var entryRect=window.entryLabel.rectTransform;entryRect.anchoredPosition=new Vector2(18,0);entryRect.sizeDelta=new Vector2(button.sizeDelta.x-58,62);window.entryLabel.fontSize=25;
                Place(button.Find("SkullIcon"),new Vector2(-button.sizeDelta.x*.5f+30,0),new Vector2(32,32));
                window.panel.SetActive(false);window.Refresh();PrefabUtility.RecordPrefabInstancePropertyModifications(window);
                foreach(var r in instance.GetComponentsInChildren<RectTransform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                foreach(var t in instance.GetComponentsInChildren<TMP_Text>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Verify();Preview();Debug.Log("PLAYER_KNOWLEDGE_INSTALLED scenes=3 entries="+library.entries.Length);
        }
        finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
    static T[] Assets<T>(string folder) where T:Object=>AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{folder}).Select(g=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(o=>o!=null).ToArray();
    static MonsterCollectionLibrary BuildLibrary()
    {
        var library=AssetDatabase.LoadAssetAtPath<MonsterCollectionLibrary>(LibraryPath);
        if(library==null){library=ScriptableObject.CreateInstance<MonsterCollectionLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
        var maps=Assets<MapData>("Assets/Data/Map").Where(m=>m.mapPrefab!=null&&!m.isTutorial).OrderBy(m=>m.name).ToArray();
        var entries=Assets<MonsterData>("Assets/Data/Monster").Where(m=>m.monsterPrefab!=null).OrderBy(m=>m.monsterName).Select(data=>
        {
            var first=maps.FirstOrDefault(map=>map.mapPrefab.GetComponentsInChildren<RoomController>(true).Any(r=>r.roomData!=null&&new[]{r.roomData.monstersToSpawn,r.roomData.possibleMonsters,r.roomData.leaderMonsters}.Any(a=>a!=null&&a.Contains(data))));
            bool boss=data.monsterPrefab.GetComponent<EchoCommanderBoss>()!=null||data.monsterPrefab.GetComponent<AncientEntbornBoss>()!=null;
            var sprite=data.monsterPrefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
            return new MonsterCollectionLibrary.Entry{data=data,prefab=data.monsterPrefab,portrait=sprite,name=data.monsterName,boss=boss,stageThai=first!=null?first.mapName:"หลายด่าน",stageEnglish=first!=null?first.mapName:"Multiple stages"};
        }).ToList();
        var finalMap=maps.FirstOrDefault(m=>m.mapPrefab.GetComponentInChildren<ArchitectBossHealth>(true)!=null);
        if(finalMap==null)throw new Exception("Final boss map missing from collection");
        var architect=finalMap.mapPrefab.GetComponentInChildren<ArchitectBossHealth>(true);
        var cinematic=AssetDatabase.LoadAssetAtPath<CinematicLibrary>("Assets/Resources/CinematicLibrary.asset");var profile=cinematic?.bosses?.FirstOrDefault(b=>b.id=="architect");
        entries.Add(new MonsterCollectionLibrary.Entry{prefab=architect.gameObject,portrait=profile!=null?profile.portrait:architect.GetComponentInChildren<SpriteRenderer>(true)?.sprite,name="The Architect of Collapse",boss=true,stageThai=finalMap.mapName,stageEnglish=finalMap.mapName});
        library.entries=entries.OrderBy(e=>e.boss).ThenBy(e=>e.stageEnglish).ThenBy(e=>e.name).ToArray();EditorUtility.SetDirty(library);return library;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Box(Transform p,string name,Vector2 pos,Vector2 size,Color color)
    {
        var r=Rect(p,name,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;
    }
    static TextMeshProUGUI Label(Transform p,string n,Vector2 pos,Vector2 size,string value,float fs,TextAlignmentOptions align=TextAlignmentOptions.Left)
    {
        size.y=Mathf.Max(size.y,fs*1.8f);var r=Rect(p,n,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSharedMaterial=font.material;t.fontSize=fs;t.text=value;t.color=White;t.alignment=align;t.raycastTarget=false;t.extraPadding=true;return t;
    }
    static Button Button(Transform p,string n,Vector2 pos,Vector2 size,string text,float fs)
    {
        var im=Box(p,n,pos,size,Surface);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;Label(im.transform,"Label",Vector2.zero,size-new Vector2(22,6),text,fs,TextAlignmentOptions.Center);QuantumUiSkin.Button(b,10);return b;
    }
    static void Place(Transform t,Vector2 pos,Vector2 size){var r=(RectTransform)t;r.anchoredPosition=pos;r.sizeDelta=size;}
    static void StarterCard(GameObject root)
    {
        var card=root.GetComponent<CharacterBoxUI>();
        // สกิลเดิมอยู่ครบ ปรับช่องให้มีแถบอาวุธเพิ่มโดยไม่ย่อรูปฮีโร่หรือสเตตัส
        for(int i=0;i<2;i++)
        {
            var p=root.transform.Find("SkillPanel"+i);Place(p,new Vector2(215,i==0?215:15),new Vector2(780,190));
            Place(p.Find("Key"),new Vector2(-337,64),new Vector2(56,60));Place(p.Find("Icon"),new Vector2(-329,-12),new Vector2(82,82));
            Place(p.Find("Name"),new Vector2(21,64),new Vector2(624,52));p.Find("Name").GetComponent<TMP_Text>().fontSize=28;
            Place(p.Find("Description"),new Vector2(53,-7),new Vector2(574,86));var desc=p.Find("Description").GetComponent<TMP_Text>();desc.fontSizeMin=23;desc.fontSizeMax=24;desc.fontSize=24;
            Place(p.Find("Cost"),new Vector2(20,-71),new Vector2(684,40));p.Find("Cost").GetComponent<TMP_Text>().fontSize=21;
        }
        var old=root.transform.Find("StartingWeapon");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var panel=Box(root.transform,"StartingWeapon",new Vector2(215,-215),new Vector2(780,170),Surface);QuantumUiSkin.Frame(panel.transform,12);
        card.starterWeaponIcon=Box(panel.transform,"WeaponIcon",new Vector2(-280,-10),new Vector2(168,110),Color.white);card.starterWeaponIcon.preserveAspect=true;
        card.starterWeaponTitle=Label(panel.transform,"Heading",new Vector2(65,52),new Vector2(530,38),"",24);card.starterWeaponTitle.color=QuantumUiSkin.Cyan;
        card.starterWeaponName=Label(panel.transform,"WeaponName",new Vector2(65,5),new Vector2(530,46),"",31);
        card.starterWeaponStats=Label(panel.transform,"WeaponStats",new Vector2(65,-47),new Vector2(530,36),"",24);card.starterWeaponStats.color=Muted;
    }
    static GameObject BuildCollection(MonsterCollectionLibrary library)
    {
        var root=new GameObject("MonsterCollection",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(MonsterCollectionWindow));root.layer=5;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30010;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var w=root.GetComponent<MonsterCollectionWindow>();w.library=library;
        w.entryButton=Button(root.transform,"OpenCollection",Vector2.zero,new Vector2(300,68),"",25);w.entryLabel=w.entryButton.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(w.entryButton.onClick,w.Open);
        var badge=Rect(w.entryButton.transform,"SkullIcon",new Vector2(-117,0),new Vector2(32,32)).gameObject.AddComponent<MapRoomIcon>();badge.kind=MapRoomGraph.RoomKind.Combat;badge.raycastTarget=false;
        // แผ่นมืดเต็มจอรับคลิกไว้ ไม่ให้ซื้อของหรือโจมตีทะลุหน้าคอลเลกชัน
        var shade=Box(root.transform,"CollectionPanel",Vector2.zero,Vector2.zero,new Color32(0,0,0,205));shade.raycastTarget=true;var shadeRect=shade.rectTransform;shadeRect.anchorMin=Vector2.zero;shadeRect.anchorMax=Vector2.one;shadeRect.offsetMin=shadeRect.offsetMax=Vector2.zero;w.panel=shade.gameObject;
        var window=Box(shade.transform,"CollectionWindow",Vector2.zero,new Vector2(1540,900),Ink);QuantumUiSkin.Frame(window.transform,28);
        w.title=Label(window.transform,"Title",new Vector2(-170,389),new Vector2(1120,66),"",43);
        w.subtitle=Label(window.transform,"Subtitle",new Vector2(-170,337),new Vector2(1120,40),"",24);w.subtitle.color=Muted;
        w.closeButton=Button(window.transform,"Close",new Vector2(632,388),new Vector2(208,62),"",25);w.closeLabel=w.closeButton.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(w.closeButton.onClick,w.Close);
        var side=Box(window.transform,"ListPanel",new Vector2(-526,-28),new Vector2(426,688),Surface);QuantumUiSkin.Frame(side.transform,8);
        var viewport=Box(side.transform,"Viewport",Vector2.zero,new Vector2(410,668),Color.clear);viewport.gameObject.AddComponent<RectMask2D>();viewport.raycastTarget=true;
        var content=Rect(viewport.transform,"Content",Vector2.zero,new Vector2(410,library.entries.Length*76));content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,library.entries.Length*76);
        w.list=side.gameObject.AddComponent<ScrollRect>();w.list.viewport=viewport.rectTransform;w.list.content=content;w.list.horizontal=false;w.list.movementType=ScrollRect.MovementType.Clamped;w.list.scrollSensitivity=36;
        var rail=Box(side.transform,"Scrollbar",new Vector2(207,0),new Vector2(8,666),new Color32(49,48,71,255));rail.raycastTarget=true;
        var thumb=Box(rail.transform,"Handle",Vector2.zero,new Vector2(8,100),new Color32(112,190,211,255));thumb.raycastTarget=true;
        var scrollbar=rail.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=thumb.rectTransform;scrollbar.targetGraphic=thumb;scrollbar.direction=Scrollbar.Direction.BottomToTop;w.list.verticalScrollbar=scrollbar;
        w.scrollHint=Label(window.transform,"ScrollHint",new Vector2(-526,-378),new Vector2(426,32),"",17,TextAlignmentOptions.Center);w.scrollHint.color=Muted;
        w.rows=new Button[library.entries.Length];w.rowNames=new TMP_Text[library.entries.Length];
        for(int i=0;i<library.entries.Length;i++)
        {
            var b=Button(content,"MonsterRow_"+i,new Vector2(0,-38-i*76),new Vector2(400,70),"",23);var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=Vector2.one*.5f;
            var label=b.GetComponentInChildren<TMP_Text>();Place(label.transform,new Vector2(34,0),new Vector2(315,62));label.alignment=TextAlignmentOptions.Left;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=23;
            var icon=Box(b.transform,"Portrait",new Vector2(-161,0),new Vector2(58,58),Color.white);icon.sprite=library.entries[i].portrait;icon.preserveAspect=true;
            w.rows[i]=b;w.rowNames[i]=label;UnityEventTools.AddIntPersistentListener(b.onClick,w.Select,i);
        }
        w.nameLabel=Label(window.transform,"MonsterName",new Vector2(220,279),new Vector2(970,76),"",39);w.nameLabel.enableAutoSizing=true;w.nameLabel.fontSizeMin=29;w.nameLabel.fontSizeMax=39;
        w.stageLabel=Label(window.transform,"Stage",new Vector2(220,224),new Vector2(970,42),"",24);w.stageLabel.color=QuantumUiSkin.Cyan;
        var art=Box(window.transform,"PortraitPanel",new Vector2(-113,52),new Vector2(302,278),Surface);QuantumUiSkin.Frame(art.transform,12);
        w.portrait=Box(art.transform,"Portrait",Vector2.zero,new Vector2(280,258),Color.white);w.portrait.preserveAspect=true;
        var values=new TMP_Text[4];
        for(int i=0;i<4;i++){var tile=Box(window.transform,"Stat"+i,new Vector2(i%2==0?265:525,i<2?133:-7),new Vector2(230,124),Surface);QuantumUiSkin.Frame(tile.transform,8);values[i]=Label(tile.transform,"Value",Vector2.zero,new Vector2(214,112),"",29,TextAlignmentOptions.Center);values[i].lineSpacing=8;}
        w.healthLabel=values[0];w.damageLabel=values[1];w.speedLabel=values[2];w.cooldownLabel=values[3];
        w.abilitiesTitle=Label(window.transform,"AbilitiesHeading",new Vector2(220,-112),new Vector2(970,42),"",27);w.abilitiesTitle.color=QuantumUiSkin.Cyan;
        var body=Box(window.transform,"AbilitiesPanel",new Vector2(220,-248),new Vector2(970,230),Surface);QuantumUiSkin.Frame(body.transform,10);
        var abilityViewport=Box(body.transform,"Viewport",Vector2.zero,new Vector2(928,208),Color.clear);abilityViewport.raycastTarget=true;abilityViewport.gameObject.AddComponent<RectMask2D>();
        w.abilities=Label(abilityViewport.transform,"Abilities",Vector2.zero,new Vector2(928,208),"",25,TextAlignmentOptions.TopLeft);w.abilities.rectTransform.anchorMin=w.abilities.rectTransform.anchorMax=new Vector2(.5f,1);w.abilities.rectTransform.pivot=new Vector2(.5f,1);
        w.abilityScroll=body.gameObject.AddComponent<ScrollRect>();w.abilityScroll.viewport=abilityViewport.rectTransform;w.abilityScroll.content=w.abilities.rectTransform;w.abilityScroll.horizontal=false;w.abilityScroll.movementType=ScrollRect.MovementType.Clamped;w.abilityScroll.scrollSensitivity=35;
        w.footer=Label(window.transform,"Footer",new Vector2(60,-410),new Vector2(1350,50),"",21);w.footer.color=Muted;
        w.languageButton=Button(window.transform,"Language",new Vector2(-634,-410),new Vector2(196,50),"TH / EN",22);UnityEventTools.AddPersistentListener(w.languageButton.onClick,w.ToggleLanguage);Place(w.footer.transform,new Vector2(110,-410),new Vector2(1200,50));
        w.Refresh();w.panel.SetActive(false);return root;
    }
    public static void Verify()
    {
        var library=AssetDatabase.LoadAssetAtPath<MonsterCollectionLibrary>(LibraryPath);if(library==null||library.entries.Length<13)throw new Exception("Missing collection entries");
        foreach(var e in library.entries){if(e.prefab==null||e.portrait==null||e.Health<=0||e.Damage<0||string.IsNullOrEmpty(e.Abilities(true))||string.IsNullOrEmpty(e.Abilities(false)))throw new Exception("Invalid collection "+e.name);}
        var menu=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var manager=menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CharacterSelectionManager>(true)).First();var card=manager.characterBoxPrefab.GetComponent<CharacterBoxUI>();
        if(card.starterWeaponIcon==null||card.starterWeaponName==null)throw new Exception("Card missing starter weapon UI");
        foreach(var hero in manager.allCharacters){var weapon=hero.characterPrefab.GetComponent<PlayerStats>()?.weapon1;if(weapon==null||weapon.weaponIcon==null)throw new Exception("Missing starter weapon "+hero.name);}
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/GameScene.unity","Assets/Scenes/TutorialScene.unity"}){var scene=EditorSceneManager.OpenScene(path);var windows=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonsterCollectionWindow>(true)).ToArray();if(windows.Length!=1||windows[0].entryButton.onClick.GetPersistentEventCount()==0||windows[0].panel.activeSelf)throw new Exception("Invalid collection scene "+path);}
        Debug.Log("PLAYER_KNOWLEDGE_VERIFIED heroes="+manager.allCharacters.Count+" entries="+library.entries.Length+" scenes=3");
        VerifyMapIcons();
    }
    public static void VerifyMapIcons()
    {
        int variants=0,rooms=0;
        foreach(var map in Assets<MapData>("Assets/Data/Map").Where(m=>m.mapPrefab!=null))
        {
            var root=Object.Instantiate(map.mapPrefab);
            try
            {
                var random=root.GetComponent<MapLayoutRandomizer>();var layouts=random!=null?random.layouts:new[]{root};
                for(int v=0;v<layouts.Length;v++)
                {
                    for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(i==v);
                    var graph=new MapRoomGraph(root);if(graph.nodes.Count==0)throw new Exception("No minimap rooms "+map.name);
                    if(map.isBossRoom&&!graph.nodes.Any(n=>n.kind==MapRoomGraph.RoomKind.Boss))throw new Exception("Missing boss glyph "+map.name);
                    foreach(var n in graph.nodes){if(n.hasExitPortal&&n.kind!=MapRoomGraph.RoomKind.Exit&&n.kind!=MapRoomGraph.RoomKind.Boss)throw new Exception("Missing exit glyph "+map.name);rooms++;}
                    variants++;
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        Debug.Log("ROOM_ICON_GRAPHS_VERIFIED layouts="+variants+" rooms="+rooms+" bosses=true exits=true");
    }
    public static void Preview()
    {
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/IBMPlexSansThaiLooped/IBM Plex UI SDF.asset");var language=LanguageSettings.Current;
        try
        {
            LanguageSettings.Current=GameLanguage.Thai;var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var roots=scene.GetRootGameObjects();var menu=roots.SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).First();var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();
            menu.OnNewGameClicked();var selection=roots.SelectMany(r=>r.GetComponentsInChildren<CharacterSelectionManager>(true)).First();
            foreach(var hero in selection.allCharacters){var card=Object.Instantiate(selection.characterBoxPrefab,selection.characterContainer);card.GetComponent<CharacterBoxUI>().SetupBox(hero);Capture(camera,"starter-"+hero.name+".png",1280,720);Object.DestroyImmediate(card);}
            menu.OnBackClicked();var collection=roots.SelectMany(r=>r.GetComponentsInChildren<MonsterCollectionWindow>(true)).First();collection.Open();
            for(int i=0;i<collection.library.entries.Length;i++){collection.Select(i);Capture(camera,"monster-"+i+".png",1280,720);}
            Capture(camera,"collection-1024.png",1024,768);LanguageSettings.Current=GameLanguage.English;collection.Refresh();Capture(camera,"collection-english.png",1280,720);collection.Close();
        }
        finally{LanguageSettings.Current=language;}
    }
    public static void Capture(Camera camera,string name,int width,int height)
    {
        var target=new RenderTexture(width,height,24);camera.targetTexture=target;
        foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.4f;canvas.overrideSorting=true;canvas.sortingLayerID=SortingLayer.layers.OrderBy(l=>l.value).Last().id;
            var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null&&scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize)canvas.scaleFactor=Mathf.Min(width/scaler.referenceResolution.x,height/scaler.referenceResolution.y);
        }
        Canvas.ForceUpdateCanvases();foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){text.ForceMeshUpdate();if(text.isTextOverflowing)Debug.LogWarning("KNOWLEDGE_OVERFLOW "+name+" "+text.name);}
        camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);
    }
}
