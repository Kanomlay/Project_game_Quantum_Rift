using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// ตรวจปุ่มเมนูและการสลับหมวดจริง ไม่กดเริ่มรอบใหม่ ไม่เขียน/ลบเซฟที่ผู้เล่นมีอยู่
[InitializeOnLoad]
public static class UnifiedMenuPlayChecks
{
    const string Key="QuantumRift.UnifiedMenuChecks";
    static int step,checks;static double wait,deadline;static UnifiedHelpHub hub;static MainMenuController menu;static Button training,source;
    static UnifiedMenuPlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    static string SaveHash()
    {
        return string.Join("|",new[]{"run-save.json","run-save.test.json"}.Select(name=>{string path=Path.Combine(Application.persistentDataPath,name);if(!File.Exists(path))return name+":missing";using(var sha=SHA256.Create())return name+":"+Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}));
    }
    public static void Run()
    {
        SessionState.SetInt(Key+".lang",(int)LanguageSettings.Current);SessionState.SetFloat(Key+".volume",AudioListener.volume);SessionState.SetString(Key+".save",SaveHash());SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false)||s!=PlayModeStateChange.EnteredPlayMode)return;step=checks=0;deadline=EditorApplication.timeSinceStartup+100;wait=EditorApplication.timeSinceStartup+1;AudioListener.volume=0;EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception("UNIFIED_MENU_PLAY_FAILED "+message);checks++;Debug.Log("UNIFIED_MENU_PLAY_PASS "+message);}
    static void Next(int value,double delay=.2){step=value;wait=EditorApplication.timeSinceStartup+delay;}
    static Camera Cam()=>Object.FindFirstObjectByType<Camera>();
    static void Clickable(Button b,string context)
    {
        Canvas.ForceUpdateCanvases();var c=b.GetComponentInParent<Canvas>();var rect=(RectTransform)b.transform;
        // ปุ่มมุมจอมี pivot อยู่ขอบ ต้องคลิกกลางรูปจริง ไม่ใช่จุด pivot นอกมุมภาพ
        var pos=RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pos},hits);if(hits.Count==0||hits[0].gameObject!=b.gameObject)Debug.LogWarning("UNIFIED_RAY "+context+" point="+pos+" screen="+Screen.width+"x"+Screen.height+" hits="+string.Join(",",hits.Take(4).Select(h=>h.gameObject.name)));Check(hits.Count>0&&hits[0].gameObject==b.gameObject,context+" receives click");
    }
    static void Fits(GameObject root,string label){Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Check(!t.isTextOverflowing,label+" text fits "+t.name);}}
    static void MenuBounds()
    {
        var group=(RectTransform)source.transform.parent;LayoutRebuilder.ForceRebuildLayoutImmediate(group);Canvas.ForceUpdateCanvases();
        var canvas=group.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var visible=group.GetComponentsInChildren<Button>().OrderByDescending(b=>b.transform.position.y).ToArray();
        for(int i=0;i<visible.Length;i++)
        {
            var rect=(RectTransform)visible[i].transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);var bottom=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var top=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);Check(bottom.y>=0&&top.y<=Screen.height,"center button in screen "+visible[i].name);
            if(i>0){var previous=new Vector3[4];((RectTransform)visible[i-1].transform).GetWorldCorners(previous);Check(RectTransformUtility.WorldToScreenPoint(camera,previous[0]).y>top.y,"buttons do not overlap "+visible[i].name);}
        }
    }
    static void Tick()
    {
        if(!Application.isPlaying||EditorApplication.timeSinceStartup<wait)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Unified menu timeout step="+step);
            switch(step)
            {
                case 0:
                    hub=Object.FindFirstObjectByType<UnifiedHelpHub>();menu=Object.FindFirstObjectByType<MainMenuController>();source=menu.mainMenuUI.transform.Find("MenuButtonContainer/Button_Start").GetComponent<Button>();training=source.transform.parent.Find("TutorialButton").GetComponent<Button>();
                    Check(hub!=null&&!hub.monsters.entryButton.gameObject.activeInHierarchy,"one guide entry, no standalone monster entry");Check(training.transform.parent==source.transform.parent,"training in central menu");
                    Check(hub.guide.entryButton.GetComponent<Image>().sprite==source.GetComponent<Image>().sprite&&training.GetComponent<Image>().sprite==source.GetComponent<Image>().sprite,"new buttons match existing purple sprite");
                    Check(hub.guide.entryButton.spriteState.pressedSprite==source.spriteState.pressedSprite&&training.spriteState.highlightedSprite==source.spriteState.highlightedSprite,"pressed and hover sprites match");
                    foreach(var lang in new[]{GameLanguage.Thai,GameLanguage.English})
                    {
                        LanguageSettings.Current=lang;Check(hub.guide.entryLabel.text==(lang==GameLanguage.Thai?"คู่มือการเล่น":"HOW TO PLAY"),"entry label correct "+lang);Fits(hub.guide.entryButton.gameObject,"guide entry "+lang);Fits(training.gameObject,"training "+lang);MenuBounds();UnifiedMenuInstaller.Capture(Cam(),"menu-play-"+lang+".png",1280,720);
                    }
                    Next(8);break;
                case 8:
                    // กลับจากการถ่ายภาพให้ CanvasScaler อัปเดตพิกัดหน้าจอจริงก่อนตรวจการคลิก
                    Clickable(training,"training");Clickable(hub.guide.entryButton,"top-right guide");hub.guide.entryButton.onClick.Invoke();Check(GameHelpWindow.IsOpen&&!MonsterCollectionWindow.IsOpen,"single entry opens guide");Next(1);break;
                case 1:
                    Clickable(hub.monsterTabs[0],"guide monster tab");hub.monsterTabs[0].onClick.Invoke();Check(MonsterCollectionWindow.IsOpen&&!GameHelpWindow.IsOpen,"monster tab switches without overlapping windows");Next(2);break;
                case 2:
                    Clickable(hub.guideTabs[1],"monster guide tab");
                    foreach(var lang in new[]{GameLanguage.Thai,GameLanguage.English}){LanguageSettings.Current=lang;hub.Refresh();Fits(hub.monsters.panel,"collection "+lang);}
                    var last=hub.monsters.rows.Last();hub.monsters.Select(hub.monsters.rows.Length-1);hub.monsters.list.verticalNormalizedPosition=0;Next(3);break;
                case 3:
                    Clickable(hub.monsters.rows.Last(),"last monster row");hub.guideTabs[1].onClick.Invoke();Check(GameHelpWindow.IsOpen&&!MonsterCollectionWindow.IsOpen,"return to guide");
                    foreach(var lang in new[]{GameLanguage.Thai,GameLanguage.English}){LanguageSettings.Current=lang;hub.Refresh();Fits(hub.guide.panel,"guide "+lang);}
                    hub.guide.closeButton.onClick.Invoke();Check(!GameHelpWindow.IsOpen&&!MonsterCollectionWindow.IsOpen&&Time.timeScale==1,"close returns menu without paused time");
                    var resume=source.transform.parent.Find("Button_Continue");Check(resume!=null,"continue workflow retained");resume.gameObject.SetActive(true);((RectTransform)source.transform.parent).anchoredPosition+=Vector2.down*30;MenuBounds();UnifiedMenuInstaller.Capture(Cam(),"menu-five-buttons.png",1280,720);menu.OnNewGameClicked();Next(4);break;
                case 4:
                    Check(!training.gameObject.activeInHierarchy&&menu.characterSelectUI.activeInHierarchy,"training hides with main menu during class selection");menu.OnBackClicked();Next(5);break;
                case 5:
                    Check(training.gameObject.activeInHierarchy,"training returns with menu");training.onClick.Invoke();Next(6,1);break;
                case 6:
                    Check(SceneManager.GetActiveScene().name=="TutorialScene"&&Object.FindFirstObjectByType<TutorialDirector>()!=null,"training click loads tutorial scene");
                    Check(!Object.FindFirstObjectByType<GameHelpWindow>().menuButtonSkin,"in-game help skin unchanged");Check(SaveHash()==SessionState.GetString(Key+".save",""),"training preserves player save files");SceneManager.LoadScene("MainMenu");Next(7,.7);break;
                case 7:
                    Check(Object.FindFirstObjectByType<UnifiedHelpHub>()!=null&&Time.timeScale==1,"returning menu keeps unified UI and live time");Check(SaveHash()==SessionState.GetString(Key+".save",""),"menu tests leave saves unchanged");Debug.Log("UNIFIED_MENU_PLAY_COMPLETE checks="+checks);Finish(0);break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool(Key,false);LanguageSettings.Current=(GameLanguage)SessionState.GetInt(Key+".lang",0);AudioListener.volume=SessionState.GetFloat(Key+".volume",1);Time.timeScale=1;PauseManager.isGamePaused=false;EditorApplication.Exit(code);}
}
