using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class UnifiedMenuInstaller
{
    const string ScenePath="Assets/Scenes/MainMenu.unity";
    static TMP_FontAsset font;
    [MenuItem("Tools/Quantum Rift/Unify Menu Guide and Training Buttons")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var roots=scene.GetRootGameObjects();
            var menu=roots.SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).Single();
            var help=roots.SelectMany(r=>r.GetComponentsInChildren<GameHelpWindow>(true)).Single();
            var monsters=roots.SelectMany(r=>r.GetComponentsInChildren<MonsterCollectionWindow>(true)).Single();
            var source=menu.mainMenuUI.transform.Find("MenuButtonContainer/Button_Start").GetComponent<Button>();font=source.GetComponentInChildren<TMP_Text>(true).font;
            var group=(RectTransform)source.transform.parent;group.anchoredPosition=new Vector2(0,-132);group.sizeDelta=new Vector2(600,750);
            var training=roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)).Single(b=>b.name=="TutorialButton");training.transform.SetParent(group,false);training.transform.SetSiblingIndex(source.transform.GetSiblingIndex()+1);
            var tr=(RectTransform)training.transform;tr.anchorMin=tr.anchorMax=tr.pivot=Vector2.one*.5f;tr.sizeDelta=((RectTransform)source.transform).sizeDelta;
            Skin(training,source,"สนามฝึกซ้อม","TRAINING",40);
            help.menuButtonSkin=true;Skin(help.entryButton,source,"คู่มือการเล่น","HOW TO PLAY",34);
            var entry=(RectTransform)help.entryButton.transform;entry.anchorMin=entry.anchorMax=entry.pivot=Vector2.one;entry.anchoredPosition=new Vector2(-24,-24);entry.sizeDelta=new Vector2(360,88);
            Place(help.entryLabel.rectTransform,Vector2.zero,new Vector2(284,72));help.entryLabel.fontSize=34;help.entryLabel.enableAutoSizing=true;help.entryLabel.fontSizeMin=29;help.entryLabel.fontSizeMax=34;
            help.entryHint.gameObject.SetActive(false);monsters.entryButton.gameObject.SetActive(false);
            var hub=help.GetComponent<UnifiedHelpHub>()??help.gameObject.AddComponent<UnifiedHelpHub>();hub.guide=help;hub.monsters=monsters;
            var guideWindow=help.panel.transform.Find("GuideWindow");var caption=guideWindow.Find("GuideCaption");if(caption!=null)caption.gameObject.SetActive(false);
            var guideTab=Tab(guideWindow,"GuideCategory",new Vector2(-638,286),new Vector2(148,52),hub.ShowGuide);
            var monTab=Tab(guideWindow,"MonsterCategory",new Vector2(-477,286),new Vector2(158,52),hub.ShowMonsters);
            var collectionWindow=monsters.panel.transform.Find("CollectionWindow");var side=collectionWindow.Find("ListPanel");
            var collectionGuide=Tab(side,"GuideCategory",new Vector2(-102,315),new Vector2(196,50),hub.ShowGuide);
            var collectionMon=Tab(side,"MonsterCategory",new Vector2(102,315),new Vector2(196,50),hub.ShowMonsters);
            Place(monsters.list.viewport,new Vector2(0,-36),new Vector2(410,590));
            var scrollbar=side.Find("Scrollbar");if(scrollbar!=null)Place((RectTransform)scrollbar,new Vector2(207,-36),new Vector2(8,588));
            hub.guideTabs=new[]{guideTab,collectionGuide};hub.monsterTabs=new[]{monTab,collectionMon};hub.Refresh();help.Refresh();help.panel.SetActive(false);monsters.panel.SetActive(false);
            Record(help.gameObject);Record(monsters.gameObject);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Verify();Preview();Debug.Log("UNIFIED_MENU_INSTALLED singleGuide=true trainingCentered=true preservedSave=true");
        }
        finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
    }
    static void Record(GameObject root){foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    static void Place(RectTransform r,Vector2 p,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=size;r.localScale=Vector3.one;}
    static void Skin(Button button,Button source,string thai,string english,float size)
    {
        foreach(var state in button.GetComponents<QuantumUiButtonState>())Object.DestroyImmediate(state);
        foreach(Transform c in button.transform.Cast<Transform>().ToArray())if(c.name=="__CleanFrame"||c.name=="__QuantumFrame")Object.DestroyImmediate(c.gameObject);
        var im=button.GetComponent<Image>();var src=source.GetComponent<Image>();im.sprite=src.sprite;im.overrideSprite=null;im.color=Color.white;im.type=Image.Type.Simple;im.preserveAspect=false;im.raycastTarget=true;
        button.targetGraphic=im;button.transition=Selectable.Transition.SpriteSwap;button.spriteState=source.spriteState;
        var text=button.GetComponentInChildren<TMP_Text>(true);text.font=font;text.fontSharedMaterial=font.material;text.fontStyle=FontStyles.Normal;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size-4;text.fontSizeMax=size;text.color=Color.white;text.raycastTarget=false;
        Place(text.rectTransform,Vector2.zero,new Vector2(((RectTransform)source.transform).sizeDelta.x*.76f,88));
        var localized=text.GetComponent<LocalizedText>()??text.gameObject.AddComponent<LocalizedText>();localized.thaiText=thai;localized.englishText=english;localized.Apply();
        var feedback=button.GetComponent<MenuButtonLabelFeedback>()??button.gameObject.AddComponent<MenuButtonLabelFeedback>();feedback.button=button;feedback.label=text;feedback.restingPosition=Vector2.zero;feedback.pressedOffset=2;feedback.Refresh();
    }
    static Button Tab(Transform parent,string name,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.layer=5;go.transform.SetParent(parent,false);Place((RectTransform)go.transform,pos,size);
        var im=go.GetComponent<Image>();im.color=QuantumUiSkin.Surface;var b=go.GetComponent<Button>();b.targetGraphic=im;
        var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.layer=5;label.transform.SetParent(go.transform,false);Place((RectTransform)label.transform,Vector2.zero,size-new Vector2(12,4));var t=label.GetComponent<TextMeshProUGUI>();t.font=font;t.fontSharedMaterial=font.material;t.fontSize=22;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.color=Color.white;
        QuantumUiSkin.Button(b,8);UnityEventTools.AddPersistentListener(b.onClick,action);return b;
    }
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);var roots=scene.GetRootGameObjects();var help=roots.SelectMany(r=>r.GetComponentsInChildren<GameHelpWindow>(true)).Single();var hub=help.GetComponent<UnifiedHelpHub>();
        if(hub==null||hub.guideTabs.Length!=2||hub.monsterTabs.Length!=2||hub.monsters.entryButton.gameObject.activeSelf)throw new Exception("Unified navigation missing");
        var menu=roots.SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).Single();var source=menu.mainMenuUI.transform.Find("MenuButtonContainer/Button_Start").GetComponent<Button>();var training=source.transform.parent.Find("TutorialButton").GetComponent<Button>();
        if(training.GetComponent<Image>().sprite!=source.GetComponent<Image>().sprite||help.entryButton.GetComponent<Image>().sprite!=source.GetComponent<Image>().sprite||training.spriteState.pressedSprite!=source.spriteState.pressedSprite)throw new Exception("New buttons do not match menu skin");
        if(((RectTransform)help.entryButton.transform).anchorMax!=Vector2.one||!help.menuButtonSkin||source.transform.parent.childCount!=4)throw new Exception("Wrong menu positions");
        if(!Enumerable.Range(0,training.onClick.GetPersistentEventCount()).Any(i=>training.onClick.GetPersistentMethodName(i)==nameof(MainMenuController.OnTutorialClicked)))throw new Exception("Training not wired");
        Debug.Log("UNIFIED_MENU_VERIFIED buttons=4 categories=2 sameSprites=true topRight=true");
    }
    public static void Preview()
    {
        var language=LanguageSettings.Current;
        try
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var roots=scene.GetRootGameObjects();var cam=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();var hub=roots.SelectMany(r=>r.GetComponentsInChildren<UnifiedHelpHub>(true)).Single();
            foreach(var lang in new[]{GameLanguage.Thai,GameLanguage.English}){LanguageSettings.Current=lang;hub.guide.Refresh();hub.Refresh();Capture(cam,"menu-"+lang+".png",1280,720);}
            LanguageSettings.Current=GameLanguage.Thai;hub.ShowGuide();hub.guide.Refresh();hub.Refresh();Capture(cam,"guide-categories.png",1280,720);hub.ShowMonsters();hub.monsters.Refresh();hub.Refresh();Capture(cam,"monster-categories.png",1280,720);hub.monsters.Close();Capture(cam,"menu-1024.png",1024,768);
        }
        finally{LanguageSettings.Current=language;}
    }
    public static void Capture(Camera camera,string file,int width,int height)
    {
        foreach(var label in Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))label.Apply();
        PlayerKnowledgeInstaller.Capture(camera,file,width,height);
    }
}
