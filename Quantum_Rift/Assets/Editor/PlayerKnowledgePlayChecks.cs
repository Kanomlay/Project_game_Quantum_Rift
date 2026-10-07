using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// ทดสอบ UI จริงใน Play Mode เก็บและคืนภาษา/ความคืบหน้าเดิม ไม่บันทึกผลทดสอบเป็นรอบเล่น
[InitializeOnLoad]
public static class PlayerKnowledgePlayChecks
{
    const string Key="QuantumRift.PlayerKnowledgeChecks";
    static int step,index,checks;
    static double wait,deadline;
    static MonsterCollectionWindow collection;
    static GameHelpWindow help;
    static CharacterSelectionManager selection;
    static MapManager manager;
    static MiniMapHUD mini;
    static PlayerStats hero;
    static readonly string[] StagePaths={"Assets/Data/Map/MapData_1_1.asset","Assets/Data/Map/MapData_1_4.asset","Assets/Data/Map/MapData_2_3.asset"};
    static PlayerKnowledgePlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Setup(){if(!SessionState.GetBool(Key,false))return;GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");MapManager.startOverride=AssetDatabase.LoadAssetAtPath<MapData>(StagePaths[0]);}
    public static void Run()
    {
        SessionState.SetInt(Key+".lang",(int)LanguageSettings.Current);SessionState.SetFloat(Key+".volume",AudioListener.volume);
        SessionState.SetBool(Key+".watchedExists",PlayerPrefs.HasKey(CinematicProgress.WatchedKey));SessionState.SetInt(Key+".watched",PlayerPrefs.GetInt(CinematicProgress.WatchedKey));PlayerPrefs.SetInt(CinematicProgress.WatchedKey,1);PlayerPrefs.Save();
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;step=index=checks=0;deadline=EditorApplication.timeSinceStartup+180;wait=EditorApplication.timeSinceStartup+1;AudioListener.volume=0;EditorApplication.update+=Tick;
    }
    static void Check(bool valid,string label){if(!valid)throw new Exception("KNOWLEDGE_PLAY_FAILED "+label);checks++;Debug.Log("KNOWLEDGE_PLAY_PASS "+label);}
    static void Next(int s,double delay=.2){step=s;wait=EditorApplication.timeSinceStartup+delay;}
    static Camera Camera()=>Object.FindFirstObjectByType<Camera>();
    static void Fits(GameObject root,string context)
    {
        Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,context+" text fits "+text.name);}
    }
    static void ReceivesClick(Button button,string label)
    {
        Canvas.ForceUpdateCanvases();var canvas=button.GetComponentInParent<Canvas>();var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,button.transform.position);
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        if(hits.Count==0||hits[0].gameObject!=button.gameObject)Debug.LogWarning("KNOWLEDGE_RAY "+label+" point="+point+" hits="+string.Join(",",hits.Take(4).Select(h=>h.gameObject.name)));
        Check(hits.Count>0&&hits[0].gameObject==button.gameObject,label);
    }
    static void Tick()
    {
        if(!Application.isPlaying||EditorApplication.timeSinceStartup<wait)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Knowledge timeout step="+step);
            switch(step)
            {
                case 0:
                    collection=Object.FindFirstObjectByType<MonsterCollectionWindow>();help=Object.FindFirstObjectByType<GameHelpWindow>();selection=Object.FindFirstObjectByType<CharacterSelectionManager>();
                    Check(collection!=null&&!MonsterCollectionWindow.IsOpen,"collection closed at menu");Check(collection.library.entries.Length==13,"all ten monsters and three bosses listed");
                    ReceivesClick(collection.entryButton,"menu entry receives mouse click without overlapping help");
                    collection.entryButton.onClick.Invoke();Check(MonsterCollectionWindow.IsOpen&&Time.timeScale==1,"menu button opens without pausing menu");
                    for(int lang=0;lang<2;lang++)
                    {
                        LanguageSettings.Current=(GameLanguage)lang;
                        for(int i=0;i<collection.rows.Length;i++)
                        {
                            collection.rows[i].onClick.Invoke();var e=collection.library.entries[i];Check(collection.SelectedIndex==i&&collection.portrait.sprite==e.portrait&&collection.nameLabel.text==e.name,"row selects actual portrait "+e.name);
                            Check(collection.healthLabel.text.Contains(e.Health.ToString("0.##"))&&collection.damageLabel.text.Contains(e.Damage.ToString("0.##")),"stats match source "+e.name);Fits(collection.panel,"collection "+i+" lang "+lang);
                        }
                    }
                    collection.list.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Check(collection.list.content.anchoredPosition.y>0,"list scrolls to final entries");Next(10,.3);break;
                case 10:
                    // รอเฟรมถัดไปให้ RectMask2D อัปเดตการตัดภาพหลังเลื่อน ก่อนจำลองคลิกแถวที่เพิ่งเห็น
                    ReceivesClick(collection.rows.Last(),"final boss row reachable by scrolling");ReceivesClick(collection.closeButton,"close receives mouse click above help");
                    collection.Select(12);PlayerKnowledgeInstaller.Capture(Camera(),"collection-play.png",1280,720);collection.list.verticalNormalizedPosition=1;
                    Check(!help.CanOpen(),"help cannot overlap collection");collection.Close();Check(!MonsterCollectionWindow.IsOpen&&GameHelpWindow.BlocksGameplayInput,"closing frame blocks click-through");Next(1);break;
                case 1:
                    Check(!GameHelpWindow.BlocksGameplayInput,"input restores next frame");ReceivesClick(help.entryButton,"existing help button still receives mouse click");help.Open();collection.Open();Check(!MonsterCollectionWindow.IsOpen,"collection does not overlap help");help.Close();
                    var menu=Object.FindFirstObjectByType<MainMenuController>();menu.OnNewGameClicked();Next(2);break;
                case 2:
                    for(int lang=0;lang<2;lang++)
                    {
                        LanguageSettings.Current=(GameLanguage)lang;
                        for(int i=0;i<selection.allCharacters.Count;i++)
                        {
                            var card=selection.characterContainer.GetComponentsInChildren<CharacterBoxUI>().Single();var data=selection.allCharacters[i];var weapon=data.characterPrefab.GetComponent<PlayerStats>().weapon1;
                            Check(card.StarterWeapon==weapon&&card.starterWeaponIcon.sprite==weapon.weaponIcon,"starter weapon correct "+data.name);Check(card.starterWeaponName.text==weapon.weaponName,"starter weapon name correct "+data.name);Fits(card.gameObject,"hero "+data.name+" lang "+lang);selection.NextCharacter();
                        }
                    }
                    LanguageSettings.Current=GameLanguage.Thai;PlayerKnowledgeInstaller.Capture(Camera(),"starter-play.png",1280,720);
                    SceneManager.LoadScene("GameScene");index=0;Next(3,1);break;
                case 3:
                    manager=MapManager.instance;if(manager==null||manager.IsLoading)return;hero=Object.FindFirstObjectByType<PlayerStats>();mini=Object.FindFirstObjectByType<MiniMapHUD>();collection=Object.FindFirstObjectByType<MonsterCollectionWindow>();help=Object.FindFirstObjectByType<GameHelpWindow>();
                    Check(mini!=null&&mini.Markers.Count==mini.Graph.nodes.Count,"room icon per node stage "+index);
                    Check(mini.GetComponentsInChildren<TMP_Text>().All(t=>t.text!="?"),"no question marks stage "+index);
                    Check(mini.Markers.All(m=>m.GetComponent<Image>()==null&&!m.raycastTarget),"real glyphs not square Images stage "+index);
                    Check(mini.Markers.Any(m=>m.kind==MapRoomGraph.RoomKind.Combat)&&mini.Markers.Any(m=>m.kind==MapRoomGraph.RoomKind.Shop),"combat and shop icons visible before discovery stage "+index);
                    Check(mini.Markers.Any(m=>m.exitBadge),"exit is visible before discovery stage "+index);
                    int visit=mini.Graph.nodes.FindIndex(n=>n.kind==MapRoomGraph.RoomKind.Shop);hero.transform.position=mini.Graph.nodes[visit].center;Physics2D.SyncTransforms();mini.Refresh();Check(mini.Markers[visit].visited&&mini.Markers[visit].current,"visiting room preserves glyph and marks visit stage "+index);
                    PlayerKnowledgeInstaller.Capture(Camera(),"minimap-play-"+index+".png",1280,720);
                    if(++index<StagePaths.Length){manager.LoadMap(AssetDatabase.LoadAssetAtPath<MapData>(StagePaths[index]));Next(3,1);}else Next(4);break;
                case 4:
                    ReceivesClick(collection.entryButton,"in-game collection button receives mouse click");
                    float hp=hero.currentHP;var at=hero.transform.position;collection.Open();Check(MonsterCollectionWindow.IsOpen&&Time.timeScale==0&&PauseManager.isGamePaused,"collection pauses gameplay");
                    Check(GameHelpWindow.BlocksGameplayInput&&!help.CanOpen(),"collection blocks combat and help");SessionState.SetFloat(Key+".hp",hp);SessionState.SetString(Key+".pos",JsonUtility.ToJson(new Position{p=at}));PlayerKnowledgeInstaller.Capture(Camera(),"collection-in-game.png",1280,720);Next(5,.6);break;
                case 5:
                    Check(Mathf.Approximately(hero.currentHP,SessionState.GetFloat(Key+".hp",0)),"HP unchanged while reading");var p=JsonUtility.FromJson<Position>(SessionState.GetString(Key+".pos","")).p;Check(Vector3.Distance(hero.transform.position,p)<.001f,"player frozen while reading");collection.Close();Check(Time.timeScale==1&&!PauseManager.isGamePaused,"time and pause restore");Next(6);break;
                case 6:
                    PauseManager.isGamePaused=true;Time.timeScale=0;collection.Open();collection.Close();Check(Time.timeScale==0&&PauseManager.isGamePaused,"pre-existing pause stays paused");PauseManager.isGamePaused=false;Time.timeScale=1;
                    Debug.Log("PLAYER_KNOWLEDGE_PLAY_COMPLETE checks="+checks);Finish(0);break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    [Serializable] sealed class Position{public Vector3 p;}
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);LanguageSettings.Current=(GameLanguage)SessionState.GetInt(Key+".lang",0);AudioListener.volume=SessionState.GetFloat(Key+".volume",1);
        if(SessionState.GetBool(Key+".watchedExists",false))PlayerPrefs.SetInt(CinematicProgress.WatchedKey,SessionState.GetInt(Key+".watched",0));else PlayerPrefs.DeleteKey(CinematicProgress.WatchedKey);PlayerPrefs.Save();Time.timeScale=1;PauseManager.isGamePaused=false;EditorApplication.Exit(code);
    }
}
