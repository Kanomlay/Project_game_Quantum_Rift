using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// ทดสอบ Help ด้วย scene และ prefab จริง โดยไม่แก้ข้อมูลที่บันทึกไว้ในเกม
[InitializeOnLoad]
public static class QuantumUiSkinPlayChecks
{
    const string Active="QuantumRift.UiSkinChecks.Active",OriginalLanguage="QuantumRift.UiSkinChecks.Language";
    static int choices;
    static BlessingWindow blessingWindow;
    static int step,checks;
    static double deadline,waitUntil;
    static float pausedAt,hp;
    static int energy;
    static Vector3 playerAt;
    static GameHelpWindow help;
    static PlayerStats player;
    static QuantumUiSkinPlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        SessionState.SetInt(OriginalLanguage,(int)LanguageSettings.Current);
        SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Active,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            step=0;checks=0;deadline=EditorApplication.timeSinceStartup+75;waitUntil=EditorApplication.timeSinceStartup+1;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
    }
    static void Check(bool condition,string name)
    {
        if(!condition)throw new Exception("HELP_CHECK_FAILED "+name);
        checks++;Debug.Log("HELP_CHECK_PASS "+name);
    }
    static void Tick()
    {
        if(!Application.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Help checks timed out at step "+step);
            if(EditorApplication.timeSinceStartup<waitUntil)return;
            switch(step)
            {
                case 0:
                    help=Object.FindFirstObjectByType<GameHelpWindow>();
                    Check(help!=null&&!GameHelpWindow.IsOpen&&!help.pauseGameplay,"menu starts with closed manual");
                    help.entryButton.onClick.Invoke();Check(GameHelpWindow.IsOpen&&Time.timeScale==1,"menu help opens without pausing");
                    for(int i=0;i<help.tabs.Length;i++){help.tabs[i].onClick.Invoke();Check(help.CurrentPage==i,"topic button "+i);}
                    help.nextButton.onClick.Invoke();Check(help.CurrentPage==7,"last-page boundary");
                    help.SelectPage(0);help.previousButton.onClick.Invoke();Check(help.CurrentPage==0,"first-page boundary");
                    LanguageSettings.Current=GameLanguage.Thai;help.languageButton.onClick.Invoke();Check(!LanguageSettings.IsThai&&help.title.text==help.pages[0].title.english,"language changes actual page text");
                    step=10;waitUntil=EditorApplication.timeSinceStartup+.2;break;
                case 10:
                    CheckOverlayRaycast();help.closeButton.onClick.Invoke();Check(!GameHelpWindow.IsOpen&&Time.timeScale==1,"menu close");
                    var heroPath=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/Data"})[0]);
                    GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>(heroPath);
                    SceneManager.LoadScene("GameScene");step=1;waitUntil=EditorApplication.timeSinceStartup+3;break;
                case 1:
                    help=Object.FindFirstObjectByType<GameHelpWindow>();player=Object.FindFirstObjectByType<PlayerStats>();
                    if(help==null||player==null||!help.CanOpen())return;
                    Check(help.pauseGameplay&&!GameHelpWindow.IsOpen,"game has question-mark entry");
                    CheckEntryRaycast();help.entryButton.onClick.Invoke();
                    Check(GameHelpWindow.IsOpen&&PauseManager.isGamePaused&&Time.timeScale==0,"help pauses gameplay");
                    pausedAt=Time.time;hp=player.currentHP;energy=player.currentEnergy;playerAt=player.transform.position;
                    step=2;waitUntil=EditorApplication.timeSinceStartup+.6;break;
                case 2:
                    Check(Time.time==pausedAt&&player.currentHP==hp&&player.currentEnergy==energy&&player.transform.position==playerAt,"game time player position HP and energy stay frozen");
                    help.tabs[6].onClick.Invoke();Check(help.CurrentPage==6,"page buttons work at timeScale zero");
                    CheckOverlayRaycast();help.closeButton.onClick.Invoke();
                    Check(Time.timeScale==1&&!PauseManager.isGamePaused&&!GameHelpWindow.IsOpen,"closing resumes live game");
                    Check(GameHelpWindow.BlocksGameplayInput,"close frame blocks click-through and Escape reuse");
                    step=3;waitUntil=EditorApplication.timeSinceStartup+.1;break;
                case 3:
                    Check(!GameHelpWindow.BlocksGameplayInput,"input lock clears on following frames");
                    var pause=Object.FindFirstObjectByType<PauseManager>();pause.PauseGame();help.Open();help.Close();
                    Check(PauseManager.isGamePaused&&Time.timeScale==0&&pause.pauseMenuPanel.activeSelf,"closing help preserves existing pause menu");pause.ResumeGame();
                    Time.timeScale=.5f;help.Open();help.Close();Check(Time.timeScale==.5f,"custom time scale restored");Time.timeScale=1;
                    var settings=Object.FindFirstObjectByType<SettingsMenu>();settings.Open();help.Open();Check(!GameHelpWindow.IsOpen,"help does not overlap settings");settings.Close();
                    var summary=Object.FindFirstObjectByType<SummaryManager>();summary.summaryPanel.SetActive(true);help.Open();Check(!GameHelpWindow.IsOpen,"help does not override game-over summary");summary.summaryPanel.SetActive(false);
                    var blessing=Object.FindFirstObjectByType<BlessingManager>();blessing.window.panel.SetActive(true);help.Open();Check(!GameHelpWindow.IsOpen,"help does not interrupt blessing choice");blessing.window.panel.SetActive(false);
                    help.Open();help.gameObject.SetActive(false);Check(Time.timeScale==1&&!PauseManager.isGamePaused&&!GameHelpWindow.IsOpen,"disabling open guide restores time and clears singleton");help.gameObject.SetActive(true);
                    CheckSkin();step=30;waitUntil=EditorApplication.timeSinceStartup+.6;break;
                case 30:
                    blessingWindow.cards[0].button.onClick.Invoke();blessingWindow.cards[0].button.onClick.Invoke();
                    Check(choices==1&&!blessingWindow.IsOpen,"skinned blessing card chooses exactly once");
                    var mini=Object.FindFirstObjectByType<MiniMapHUD>();
                    Check(mini!=null&&mini.transform.Cast<Transform>().Count(t=>t.name=="__CleanFrame"&&t.gameObject.activeSelf)==1,"minimap rebuild leaves one visible clean frame");
                    CheckShop(false);step=20;waitUntil=EditorApplication.timeSinceStartup+.2;break;
                case 20:
                    CheckOverlayRaycast();ShopWindow.Active.Close();
                    Check(!GameHelpWindow.IsOpen&&Time.timeScale==1&&!ShopWindow.IsOpen,"closing shop also closes its guide and restores time");
                    CheckShop(true);step=21;waitUntil=EditorApplication.timeSinceStartup+.2;break;
                case 21:
                    CheckOverlayRaycast();ShopWindow.Active.Close();
                    Check(!GameHelpWindow.IsOpen&&Time.timeScale==1&&help.pages.Length==8,"buff shop cleanup restores main manual");
                    help.Open();SceneManager.LoadScene("MainMenu");step=4;waitUntil=EditorApplication.timeSinceStartup+.5;break;
                case 4:
                    help=Object.FindFirstObjectByType<GameHelpWindow>();Check(help!=null&&!help.pauseGameplay&&!GameHelpWindow.IsOpen&&Time.timeScale==1&&!PauseManager.isGamePaused,"scene change while reading does not leave time paused");
                    Debug.Log("UI_SKIN_PLAY_CHECKS_COMPLETE checks="+checks);Finish(0);break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void CheckSkin()
    {
        Check(help.entryButton.GetComponentInChildren<QuantumUiFrame>()!=null,"help renders scalable geometry frame");
        var state=help.entryButton.GetComponent<QuantumUiButtonState>();
        Check(state!=null&&state.border!=null,"question button uses new frame");
        var rect=(RectTransform)help.entryButton.transform;var size=rect.sizeDelta;var position=rect.anchoredPosition;
        state.OnDeselect(null);state.OnPointerExit(null);var normal=state.surface.color;
        var pointer=new PointerEventData(EventSystem.current);state.OnPointerEnter(pointer);var hover=state.surface.color;
        state.OnPointerDown(pointer);var pressed=state.surface.color;
        Check(normal!=hover&&hover!=pressed&&normal!=pressed,"normal hover pressed are distinct");
        Check(rect.sizeDelta==size&&rect.anchoredPosition==position,"interaction does not move or resize help button");
        state.OnPointerUp(pointer);state.OnPointerExit(pointer);
        var decorations=Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(i=>i.name.StartsWith("__"));
        Check(decorations.All(i=>!i.raycastTarget),"decorative frames never intercept clicks");
        var hud=Object.FindFirstObjectByType<HUDManager>();var map=MapManager.instance;
        MiniMapHUD.Show(hud,map.CurrentMapRoot.gameObject,player.transform);
        MiniMapHUD.Show(hud,map.CurrentMapRoot.gameObject,player.transform);
        blessingWindow=Object.FindFirstObjectByType<BlessingWindow>();
        var manager=Object.FindFirstObjectByType<BlessingManager>();choices=0;
        blessingWindow.Open(manager.all.Take(3).Select(b=>new BlessingOffer(b,1,null)).ToList(),new List<BlessingData>(),4,_=>choices++);
        QuantumUiSkin.Blessings(blessingWindow);QuantumUiSkin.Blessings(blessingWindow);
        Check(blessingWindow.cards.All(c=>c.categoryText.transform.parent.Find("__Plate_"+c.categoryText.name).GetSiblingIndex()<c.categoryText.transform.GetSiblingIndex()&&c.limitText.transform.parent.Find("__Plate_"+c.limitText.name).GetSiblingIndex()<c.limitText.transform.GetSiblingIndex()),"reapplying skin keeps plates behind text");
        Check(blessingWindow.ownedIcons.Length==4&&blessingWindow.ownedIcons.All(i=>i.transform.Find("__CleanFrame")!=null),"four blessing sockets retain individual clean frames");
    }
    static void CheckShop(bool red)
    {
        LanguageSettings.Current=GameLanguage.Thai;
        var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(red?"Assets/Prefab/Shop/ShopBuffRed.prefab":"Assets/Prefab/Shop/ShopSpaceship.prefab")).GetComponent<ShopClickable>();
        source.transform.position=new Vector3(1000,1000,0);
        var prefab=AssetDatabase.LoadAssetAtPath<ShopWindow>("Assets/Prefab/Shop/ShopWindow.prefab");
        ShopWindow.Open(prefab,source);var window=ShopWindow.Active;
        var stock=window.HelpStock;var sold=stock.Select(x=>x.sold).ToArray();
        int money=player.currentCurrency,energyBefore=player.currentEnergy;float hpBefore=player.currentHP;
        var header=window.GetComponentsInChildren<Button>().Single(b=>b.name=="ShopHelp");
        header.onClick.Invoke();
        Check(GameHelpWindow.IsOpen&&ShopWindow.IsOpen&&help.CurrentPage==0&&help.pages.Length==(red?5:6),"header opens correct shop overview "+red);
        Check(Time.timeScale==0&&PauseManager.isGamePaused,"shop guide pauses game "+red);
        window.Buy(0);
        Check(player.currentCurrency==money&&player.currentHP==hpBefore&&player.currentEnergy==energyBefore&&stock.Select(x=>x.sold).SequenceEqual(sold),"reading prevents purchases and stat changes "+red);
        help.closeButton.onClick.Invoke();
        Check(!GameHelpWindow.IsOpen&&ShopWindow.Active==window&&ReferenceEquals(stock,window.HelpStock)&&help.pages.Length==8&&Time.timeScale==1,"return keeps original shop stock and restores general guide "+red);
        int index=red?3:1;
        var item=red?window.GetComponentInChildren<BuffShopView>().transform.Find("Offer3/ItemHelp").GetComponent<Button>():window.slots[1].transform.Find("ItemHelp").GetComponent<Button>();
        item.onClick.Invoke();
        Check(GameHelpWindow.IsOpen&&help.CurrentPage==index+1&&help.title.text==stock[index].DisplayName,"item question opens exact offer "+red);
        if(red)
        {
            var offer=stock[index];string all=string.Join("\n",help.sectionBodies.Select(t=>t.text));
            Check(offer.risky&&new[]{offer.hpDelta,offer.energyDelta,offer.damageDelta}.Where(v=>v!=0).All(v=>all.Contains(v.ToString("+0;-0"))),"risky buff displays actual benefits and penalties");
        }
        else
        {
            Check(help.sectionBodies[0].text.Contains("+"+window.HelpPool.energyRestore),"energy potion uses actual restoration value");
            var offer=stock[2];help.SelectPage(3);
            Check(help.title.text==offer.DisplayName&&help.sectionBodies[1].text.Contains(offer.weapon.energyCost.ToString()),"weapon displays current offer energy requirement");
        }
    }
    static void CheckOverlayRaycast()
    {
        Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.7f,Screen.height*.5f)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Debug.Log("HELP_RAYCAST screen="+Screen.width+"x"+Screen.height+" hits="+string.Join(",",hits.Select(x=>x.gameObject.name+":"+x.sortingOrder)));
        Check(hits.Count>0&&(hits[0].gameObject==help.panel||hits[0].gameObject.transform.IsChildOf(help.panel.transform)),"guide backdrop intercepts pointer");
    }
    static void CheckEntryRaycast()
    {
        Canvas.ForceUpdateCanvases();var r=(RectTransform)help.entryButton.transform;var pos=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pos},hits);
        Check(hits.Count>0&&hits[0].gameObject==help.entryButton.gameObject,"question mark receives clicks above gameplay HUD");
    }
    static void Finish(int exitCode)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Active,false);
        LanguageSettings.Current=(GameLanguage)SessionState.GetInt(OriginalLanguage,0);
        Time.timeScale=1;PauseManager.isGamePaused=false;
        EditorApplication.Exit(exitCode);
    }
}
