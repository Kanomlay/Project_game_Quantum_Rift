using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CinematicPlayChecks
{
    const string Key="QuantumRift.CinematicChecks.Active";
    static int step,checks,bossIndex,pageBefore;
    static double deadline,wait;
    static float frozenTime,hp,volume;
    static Vector3 playerPosition;
    static CinematicDirector director;
    static MapManager manager;
    static PlayerStats player;
    static RoomController room;
    static BossArenaEntry entry;
    static bool expectedPageChange;
    static readonly string[] Maps={"MapData_1_bossroom","MapData_2_boss"};
    static readonly string[] BossIds={"echo","entborn"};
    static CinematicPlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SelectTestCharacter()
    {
        if(!SessionState.GetBool(Key,false))return;
        var path=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/Data"})[0]);
        GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>(path);
    }
    public static void Run()
    {
        foreach(var key in new[]{CinematicProgress.WatchedKey,CinematicProgress.CompletedKey})
        {
            SessionState.SetBool(Key+key+".exists",PlayerPrefs.HasKey(key));
            SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key,0));PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();SessionState.SetInt(Key+".lang",(int)LanguageSettings.Current);
        SessionState.SetBool(Key,true);MapManager.startOverride=null;
        var path=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/Data"})[0]);
        GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
        step=checks=bossIndex=0;deadline=EditorApplication.timeSinceStartup+180;wait=0;
        volume=AudioListener.volume;AudioListener.volume=0;EditorApplication.update+=Tick;
    }
    static void Check(bool value,string label)
    {
        if(!value)throw new Exception("CINEMATIC_CHECK_FAILED "+label);
        checks++;Debug.Log("CINEMATIC_CHECK_PASS "+label);
    }
    static void Next(int next,float seconds=0){step=next;wait=EditorApplication.timeSinceStartup+seconds;}
    static void Tick()
    {
        if(!Application.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Cinematic checks timeout step="+step);
            if(EditorApplication.timeSinceStartup<wait)return;
            switch(step)
            {
                case 0:
                    director=Object.FindFirstObjectByType<CinematicDirector>();manager=MapManager.instance;
                    if(director==null||manager==null||manager.IsLoading||!CinematicDirector.IsOpen)return;
                    player=Object.FindFirstObjectByType<PlayerStats>();
                    Check(director.IsStory&&director.CurrentPage==0,"first play opens story after map is ready");
                    Check(Time.timeScale==0&&PauseManager.isGamePaused,"story freezes gameplay");
                    Check(director.library.opening.Length==3&&director.library.bosses.Length==3&&director.library.bosses.All(p=>p.portrait!=null),"all story pages and three real boss portraits present");
                    var guide=Object.FindFirstObjectByType<GameHelpWindow>();guide.Open();Check(!GameHelpWindow.IsOpen,"guide cannot overlap cinematic");
                    Check(GameHelpWindow.BlocksGameplayInput,"gameplay input locked during cinematic");
                    frozenTime=Time.time;hp=player.currentHP;playerPosition=player.transform.position;
                    Next(1,.4f);break;
                case 1:
                    Check(Time.time==frozenTime&&player.currentHP==hp&&player.transform.position==playerPosition,"story keeps time HP and player position frozen");
                    pageBefore=director.CurrentPage;expectedPageChange=director.Body.maxVisibleCharacters>=director.Body.textInfo.characterCount;
                    director.Advance();Next(2,.15f);break;
                case 2:
                    Check(expectedPageChange?director.CurrentPage==pageBefore+1:director.CurrentPage==pageBefore&&director.Body.maxVisibleCharacters>=director.Body.textInfo.characterCount,"next finishes typing before advancing a page");
                    director.Advance();Next(3,.15f);break;
                case 3:
                    Check(director.CurrentPage>0,"story next control works while paused");
                    director.Skip();Next(4,.15f);break;
                case 4:
                    Check(!CinematicDirector.IsOpen&&Time.timeScale==1&&!PauseManager.isGamePaused,"skipping story restores gameplay");
                    Check(!CinematicProgress.ShouldShowOpening&&PlayerPrefs.GetInt(CinematicProgress.WatchedKey)==1,"skipping story persists watched state");
                    SceneManager.LoadScene("GameScene");Next(5,4.5f);break;
                case 5:
                    manager=MapManager.instance;director=Object.FindFirstObjectByType<CinematicDirector>();player=Object.FindFirstObjectByType<PlayerStats>();
                    if(manager==null||manager.IsLoading)return;
                    Check(!CinematicDirector.IsOpen&&!CinematicProgress.ShouldShowOpening,"new run does not replay watched opening");
                    manager.LoadMap(Map(Maps[bossIndex]));Next(6,.1f);break;
                case 6:
                    if(manager.IsLoading)return;
                    room=manager.CurrentMapRoot.GetComponentsInChildren<RoomController>().First(r=>r.roomData!=null&&r.roomData.IsStaged);
                    room.StartEncounter();room.StartEncounter();Next(7,.25f);break;
                case 7:
                    Check(CinematicDirector.IsOpen&&!director.IsStory&&director.CurrentBossId==BossIds[bossIndex],"real boss room opens correct introduction "+BossIds[bossIndex]);
                    Check(room.AliveMonstersCount==0&&room.GetComponentsInChildren<MonsterController>().Length==0,"boss does not spawn before introduction finishes "+bossIndex);
                    Check(director.Portrait.sprite==director.library.bosses[bossIndex].portrait&&director.Title.text==director.library.bosses[bossIndex].displayName,"boss name and image match profile "+bossIndex);
                    Check(Time.timeScale==0&&PauseManager.isGamePaused,"boss cinematic freezes gameplay "+bossIndex);
                    Check(director.Overlay.transform.Find("Content/BossRevealViewport/BossReveal").GetComponent<RectTransform>().anchoredPosition.y<0,"boss portrait and name slide upward from below screen "+bossIndex);
                    director.Skip();Next(8,1.3f);break;
                case 8:
                    Check(!CinematicDirector.IsOpen&&room.AliveMonstersCount>0,"fight starts after cinematic and spawn warning "+bossIndex);
                    room.StartEncounter();Check(!CinematicDirector.IsOpen,"same room cannot replay cinematic "+bossIndex);
                    if(bossIndex==0)
                    {
                        PlayerPrefs.DeleteKey(CinematicProgress.CompletedKey);
                        SummaryManager.instance.ShowSummary(true,"next");
                        Check(PlayerPrefs.GetInt(CinematicProgress.CompletedKey,0)==0,"intermediate boss clear does not mark whole run finished");
                        SummaryManager.instance.summaryPanel.SetActive(false);Time.timeScale=1;PauseManager.isGamePaused=false;
                    }
                    bossIndex++;
                    if(bossIndex<Maps.Length){manager.LoadMap(Map(Maps[bossIndex]));Next(6);}
                    else{manager.LoadMap(Map("MapData_boss"));Next(9);}
                    break;
                case 9:
                    if(manager.IsLoading)return;
                    entry=manager.CurrentMapRoot.GetComponentInChildren<BossArenaEntry>(true);
                    entry.TryEnter(player);entry.TryEnter(player);Next(10,.25f);break;
                case 10:
                    Check(CinematicDirector.IsOpen&&director.CurrentBossId=="architect","final arena opens Architect introduction");
                    Check(!entry.arena.IsFighting,"Architect AI and arena hazards wait for cinematic");
                    Check(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Count(c=>c.name=="CinematicOverlay")==1,"repeated arena entry uses only one cinematic overlay");
                    Next(18,.95f);break;
                case 18:
                    Check(Mathf.Abs(director.Overlay.transform.Find("Content/BossRevealViewport/BossReveal").GetComponent<RectTransform>().anchoredPosition.y)<.01f&&Time.timeScale==0&&!entry.arena.IsFighting,"boss reveal reaches center using unscaled time while battle stays paused");
                    Next(11,3.9f);break;
                case 11:
                    Check(!CinematicDirector.IsOpen&&entry.arena.IsFighting&&Time.timeScale==1,"boss introduction completes automatically with unscaled time");
                    PlayerPrefs.DeleteKey(CinematicProgress.CompletedKey);
                    SummaryManager.instance.ShowSummary(true,"จบเกม!");
                    Check(PlayerPrefs.GetInt(CinematicProgress.CompletedKey,0)==1&&!SummaryManager.instance.continueButton.activeSelf,"final victory persists run completion and hides unavailable next-stage button");
                    SummaryManager.instance.summaryPanel.SetActive(false);Time.timeScale=1;PauseManager.isGamePaused=false;
                    PlayerPrefs.DeleteKey(CinematicProgress.CompletedKey);PlayerPrefs.DeleteKey(CinematicProgress.WatchedKey);
                    SummaryManager.instance.ShowSummary(false,"");
                    Check(PlayerPrefs.GetInt(CinematicProgress.CompletedKey,0)==1&&!CinematicProgress.ShouldShowOpening,"defeat also suppresses opening for later runs");
                    SummaryManager.instance.OnClickEnd();Next(12,.7f);break;
                case 12:
                    Check(Time.timeScale==1&&!CinematicDirector.IsOpen&&Object.FindFirstObjectByType<CinematicDirector>()==null,"return to menu clears cinematic and pause state");
                    var path=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/Data"})[0]);GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                    SceneManager.LoadScene("GameScene");Next(13,4.5f);break;
                case 13:
                    manager=MapManager.instance;director=Object.FindFirstObjectByType<CinematicDirector>();
                    if(manager.IsLoading)return;
                    Check(!CinematicDirector.IsOpen,"completed first run starts gameplay without story");
                    Time.timeScale=.5f;var echo=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Boss/EchoCommander/EchoCommander.prefab");director.StartCoroutine(director.PlayBoss(echo));Next(14,.3f);break;
                case 14:
                    Check(CinematicDirector.IsOpen,"boss introductions still play on subsequent runs");director.Skip();Next(15,.1f);break;
                case 15:
                    Check(Time.timeScale==.5f&&!PauseManager.isGamePaused,"cutscene restores previous custom time scale");Time.timeScale=1;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Boss/EchoCommander/EchoCommander.prefab");director.StartCoroutine(director.PlayBoss(prefab));Next(16,.3f);break;
                case 16:
                    SceneManager.LoadScene("MainMenu");Next(17,.5f);break;
                case 17:
                    Check(!CinematicDirector.IsOpen&&Time.timeScale==1&&!PauseManager.isGamePaused,"scene change during cinematic restores time");
                    Debug.Log("CINEMATIC_PLAY_CHECKS_COMPLETE checks="+checks);Finish(0);break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static MapData Map(string name)=>AssetDatabase.LoadAssetAtPath<MapData>("Assets/Data/Map/"+name+".asset");
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        foreach(var key in new[]{CinematicProgress.WatchedKey,CinematicProgress.CompletedKey})
        {
            if(SessionState.GetBool(Key+key+".exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();LanguageSettings.Current=(GameLanguage)SessionState.GetInt(Key+".lang",0);
        Time.timeScale=1;PauseManager.isGamePaused=false;AudioListener.volume=volume;EditorApplication.Exit(code);
    }
}
