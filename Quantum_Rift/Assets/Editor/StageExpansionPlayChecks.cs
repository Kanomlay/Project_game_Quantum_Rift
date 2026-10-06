using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class StageExpansionPlayChecks
{
    const string Key="QuantumRift.StageExpansionChecks";
    static int step,index,checks,pick;
    static double wait,deadline;
    static MapManager manager;
    static TutorialDirector tutorial;
    static PlayerStats hero;
    static WeaponData startingWeapon,reserveWeapon;
    static LootPickup[] pickups;
    static StageExpansionPlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetupHero()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(SessionState.GetBool(Key+".tutorialOnly",false)){GameManager.selectedCharacter=null;MapManager.startOverride=null;return;}
        GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        MapManager.startOverride=AssetDatabase.LoadAssetAtPath<MapData>(StageExpansionInstaller.MapPath(StageExpansionInstaller.NewMaps[0]));
    }
    public static void Run()
    {
        StartChecks(false);
    }
    public static void RunTutorial(){StartChecks(true);}
    static void StartChecks(bool tutorialOnly)
    {
        SessionState.SetBool(Key+".tutorialOnly",tutorialOnly);
        StageExpansionInstaller.Verify();
        foreach(var k in new[]{CinematicProgress.WatchedKey,CinematicProgress.CompletedKey})
        {
            SessionState.SetBool(Key+k+".exists",PlayerPrefs.HasKey(k));SessionState.SetInt(Key+k,PlayerPrefs.GetInt(k));PlayerPrefs.DeleteKey(k);
        }
        PlayerPrefs.Save();SessionState.SetFloat(Key+".volume",AudioListener.volume);SessionState.SetInt(Key+".lang",(int)LanguageSettings.Current);
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(tutorialOnly?StageExpansionInstaller.TutorialScene:"Assets/Scenes/GameScene.unity");EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
        step=SessionState.GetBool(Key+".tutorialOnly",false)?2:0;index=checks=pick=0;deadline=EditorApplication.timeSinceStartup+180;wait=0;AudioListener.volume=0;EditorApplication.update+=Tick;
    }
    static void Check(bool valid,string name){if(!valid)throw new Exception("STAGE_CHECK_FAILED "+name);checks++;Debug.Log("STAGE_CHECK_PASS "+name);}
    static void Next(int s,float delay=.2f){step=s;wait=EditorApplication.timeSinceStartup+delay;}
    static void Move(Vector2 p){hero.transform.position=p;var rb=hero.GetComponent<Rigidbody2D>();if(rb!=null)rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    static void Tick()
    {
        if(!Application.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Stage timeout step="+step);
            if(EditorApplication.timeSinceStartup<wait)return;
            switch(step)
            {
                case 0:
                    manager=MapManager.instance;if(manager==null||manager.IsLoading)return;
                    hero=Object.FindFirstObjectByType<PlayerStats>();
                    Check(hero!=null,"selected hero exists");Check(manager.CurrentMap.name=="MapData_1_4","first new stage loads");
                    startingWeapon=hero.weapon1;reserveWeapon=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapon/Old Rifle.asset");
                    Check(startingWeapon!=null&&reserveWeapon!=null&&hero.weapon2==null,"initial and reserve test weapons valid");
                    Check(hero.PickUpWeapon(reserveWeapon)==null&&hero.weapon2==reserveWeapon&&hero.weaponController.currentWeaponData==reserveWeapon,"pickup equips second weapon without losing starter");
                    typeof(PlayerStats).GetMethod("SwapWeapon",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hero,null);
                    Check(hero.weaponController.currentWeaponData==startingWeapon,"R swap returns to original weapon");
                    typeof(PlayerStats).GetMethod("SwapWeapon",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hero,null);
                    Check(hero.weaponController.currentWeaponData==reserveWeapon,"R swap returns to reserve weapon");
                    Next(1);break;
                case 1:
                    if(manager.IsLoading)return;
                    var root=manager.CurrentMapRoot;var layout=root.GetComponent<MapLayoutRandomizer>();var active=layout.layouts.Count(l=>l.activeSelf);
                    Check(active==1&&layout.CurrentLayout>=0,"exactly one random layout active "+index);
                    Check(root.GetComponentsInChildren<RoomController>().Length>=5,"playable rooms present "+index);
                    var floor=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");Check(floor.HasTile(floor.WorldToCell(hero.transform.position)),"hero spawns on floor "+index);
                    Check(root.GetComponentsInChildren<MapPortal>().Length==1,"one exit in active layout "+index);
                    Check(root.GetComponentsInChildren<ShopClickable>().Count(s=>s.isBuffShop)==1,"buff vendor accompanies shop "+index);
                    Check(manager.GetComponent<MapBackgroundMusic>().PlaybackSource.clip==manager.CurrentMap.backgroundMusic,"new stage uses correct theme music "+index);
                    Check(!CinematicDirector.IsOpen&&!BlessingManager.IsChoosing,"ordinary stage does not open boss cinematic "+index);
                    Check(hero.weapon1==startingWeapon&&hero.weapon2==reserveWeapon&&hero.weaponController.currentWeaponData==reserveWeapon,"both weapons and equipped reserve persist through stage load "+index);
                    Check(hero.weaponController.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.sprite!=null),"reserve weapon has visible sprite "+index);
                    index++;if(index<StageExpansionInstaller.NewMaps.Length){manager.LoadMap(AssetDatabase.LoadAssetAtPath<MapData>(StageExpansionInstaller.MapPath(StageExpansionInstaller.NewMaps[index])));Next(1,3.8f);}else{GameManager.selectedCharacter=null;MapManager.startOverride=null;SceneManager.LoadScene("TutorialScene");Next(2,4);}
                    break;
                case 2:
                    manager=MapManager.instance;tutorial=Object.FindFirstObjectByType<TutorialDirector>();hero=Object.FindFirstObjectByType<PlayerStats>();if(manager==null||manager.IsLoading||tutorial==null||!tutorial.Ready)return;
                    Check(manager.CurrentMap.isTutorial&&hero!=null,"tutorial bootstrap supplies default hero without menu selection");
                    Check(!CinematicDirector.IsOpen&&PlayerPrefs.GetInt(CinematicProgress.WatchedKey)==0&&PlayerPrefs.GetInt(CinematicProgress.CompletedKey)==0,"training does not play or consume first-run story");
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Move&&!tutorial.TryUseExit(),"training starts at movement and blocks early exit");
                    var solid=manager.CurrentMapRoot.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Bulkheads_Unified64");Check(solid.GetComponent<TilemapCollider2D>()!=null&&solid.GetTilesBlock(solid.cellBounds).OfType<Tile>().All(t=>t.colliderType==Tile.ColliderType.Grid),"training walls have physical tile collision");
                    Check(!tutorial.combatRoom.HasStarted,"training combat waits for lessons");
                    var graph=new MapRoomGraph(manager.CurrentMapRoot.gameObject);Check(graph.nodes.Count==4&&graph.edges.Count==3,"tutorial minimap links all four training stations");
                    var help=Object.FindFirstObjectByType<GameHelpWindow>();help.Open();Check(GameHelpWindow.IsOpen&&Time.timeScale==0,"guide available during training");help.Close();
                    Move((Vector2)hero.transform.position+Vector2.right*4.5f);Next(3,.4f);break;
                case 3:
                    Capture("tutorial-attack.png");
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Attack&&tutorial.Dummy!=null&&tutorial.Dummy.Frozen,"actual movement unlocks stationary training dummy");
                    tutorial.Dummy.TakeDamage(1);Next(4,.3f);break;
                case 4:
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Skills,"landing damage advances attack lesson");
                    typeof(PlayerStats).GetMethod("UseSkillQ",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hero,null);
                    typeof(PlayerStats).GetMethod("UseSkillE",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hero,null);Next(5,.4f);break;
                case 5:
                    Check(tutorial.UsedQ&&tutorial.UsedE&&tutorial.CurrentLesson==TutorialDirector.Lesson.Crate,"both successful skills unlock crate lesson");
                    Check(tutorial.Crate!=null,"training crate exists");tutorial.Crate.TakeDamage(999);Next(6,.3f);break;
                case 6:
                    Capture("tutorial-pickups.png");
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Pickups&&hero.currentHP<hero.maxHP&&hero.currentEnergy<hero.maxEnergy,"crate destruction prepares genuine healing and energy pickups");
                    pickups=tutorial.GetComponentsInChildren<LootPickup>();Check(pickups.Length==3,"one coin and two guaranteed potions");pick=0;Move(pickups[pick].transform.position);Next(7,.3f);break;
                case 7:
                    Check(pickups[pick]==null,"walking over item collects it "+pick);pick++;if(pick<pickups.Length){Move(pickups[pick].transform.position);Next(7,.3f);}else Next(8,.3f);break;
                case 8:
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Combat&&hero.currentCurrency==5,"all pickups unlock combat and update currency");Move(tutorial.combatRoom.transform.position);Next(9,1.6f);break;
                case 9:
                    Check(tutorial.combatRoom.HasStarted&&tutorial.combatRoom.AliveMonstersCount==2,"entering practice room spawns two enemies after warning");
                    Check(tutorial.combatRoom.doors.All(d=>d.GetComponent<AnimatedRoomGate>().IsClosed),"practice room doors lock during combat");
                    foreach(var monster in tutorial.combatRoom.GetComponentsInChildren<MonsterController>())monster.TakeDamage(99999);Next(10,.6f);break;
                case 10:
                    Check(tutorial.combatRoom.IsCleared&&tutorial.CurrentLesson==TutorialDirector.Lesson.Chest,"defeating all enemies advances to reward lesson");
                    Check(tutorial.combatRoom.doors.All(d=>!d.GetComponent<AnimatedRoomGate>().IsClosed),"practice gates reopen");var chest=tutorial.combatRoom.GetComponentInChildren<TreasureChest>();Check(chest!=null,"clear produces real reward chest");Move(chest.transform.position);Next(11,1.4f);break;
                case 11:
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Exit&&tutorial.portalVisual.activeSelf,"opening chest reveals training exit");Move(tutorial.exitPoint.position);Check(tutorial.TryUseExit(),"exit requires completed lessons and proximity");Next(12);break;
                case 12:
                    Check(TutorialDirector.IsCompleting&&Time.timeScale==0&&GameHelpWindow.BlocksGameplayInput,"completion freezes combat and blocks click-through");
                    Check(PlayerPrefs.GetInt(CinematicProgress.WatchedKey)==0&&PlayerPrefs.GetInt(CinematicProgress.CompletedKey)==0,"finishing training does not mark campaign completed");
                    tutorial.StartRealGame();Next(13,4.5f);break;
                case 13:
                    manager=MapManager.instance;hero=Object.FindFirstObjectByType<PlayerStats>();if(manager==null||manager.IsLoading)return;
                    Check(manager.CurrentMap.name=="MapData_1_1"&&hero.currentCurrency==0&&hero.runDamageBonus==0&&BlessingManager.Instance.Owned.Count==0,"real campaign starts fresh with no training gains");
                    Check(CinematicDirector.IsOpen&&CinematicDirector.Instance.IsStory,"first campaign story remains available after tutorial");CinematicDirector.Instance.Skip();Next(14,.2f);break;
                case 14:
                    SceneManager.LoadScene("MainMenu");Next(15,.8f);break;
                case 15:
                    var menu=Object.FindFirstObjectByType<MainMenuController>();var button=Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.name=="TutorialButton");Check(button.onClick.GetPersistentEventCount()>0,"tutorial menu button connected");menu.OnTutorialClicked();Next(16,4);break;
                case 16:
                    tutorial=Object.FindFirstObjectByType<TutorialDirector>();if(tutorial==null||!tutorial.Ready)return;
                    Check(tutorial.CurrentLesson==TutorialDirector.Lesson.Move,"tutorial can be replayed from menu");tutorial.ReturnToMenu();Next(17,.8f);break;
                case 17:
                    Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1&&Object.FindFirstObjectByType<TutorialDirector>()==null,"leaving training cleans up controller and pause");
                    Debug.Log("STAGE_PLAY_CHECKS_COMPLETE checks="+checks);Finish(0);break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        foreach(var k in new[]{CinematicProgress.WatchedKey,CinematicProgress.CompletedKey}){if(SessionState.GetBool(Key+k+".exists",false))PlayerPrefs.SetInt(k,SessionState.GetInt(Key+k,0));else PlayerPrefs.DeleteKey(k);}
        PlayerPrefs.Save();LanguageSettings.Current=(GameLanguage)SessionState.GetInt(Key+".lang",0);AudioListener.volume=SessionState.GetFloat(Key+".volume",1);Time.timeScale=1;PauseManager.isGamePaused=false;EditorApplication.Exit(code);
    }
    static void Capture(string name)
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-stageOutput");
        if(i<0)return;System.IO.Directory.CreateDirectory(args[i+1]);
        var camera=Camera.main;var previousTarget=camera.targetTexture;
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var layers=canvases.Select(c=>c.sortingLayerID).ToArray();
        var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
        try
        {
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.6f;canvas.sortingLayerID=SortingLayer.layers.OrderBy(l=>l.value).Last().id;}
            Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(args[i+1],name),texture.EncodeToPNG());RenderTexture.active=old;Object.Destroy(texture);
        }
        finally
        {
            for(int c=0;c<canvases.Length;c++){canvases[c].renderMode=RenderMode.ScreenSpaceOverlay;canvases[c].worldCamera=null;canvases[c].sortingLayerID=layers[c];}
            camera.targetTexture=previousTarget;Object.Destroy(target);Canvas.ForceUpdateCanvases();
        }
    }
}
