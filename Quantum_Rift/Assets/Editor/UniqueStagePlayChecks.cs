using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

// ตรวจทุกผังจริงโดยสร้างแม่แบบชั่วคราวใต้ parent ที่ปิดอยู่ ไม่แก้ prefab/data ระหว่างทดสอบ
[InitializeOnLoad]
public static class UniqueStagePlayChecks
{
    const string Key="QuantumRift.UniqueStagePlayChecks";
    static readonly string[] Maps={"1_4","1_5","2_3","2_4","2_5"};
    static int mapIndex,variant,step,checks,killed;
    static double deadline,wait;
    static GameObject templates,prefab;
    static MapData data;
    static PlayerStats hero;
    static RoomController room;
    static MapManager manager;
    static UniqueStagePlayChecks(){EditorApplication.playModeStateChanged+=Changed;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Setup()
    {
        if(!SessionState.GetBool(Key,false))return;
        GameManager.selectedCharacter=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        MapManager.startOverride=AssetDatabase.LoadAssetAtPath<MapData>("Assets/Data/Map/MapData_1_4.asset");
    }
    public static void Run()
    {
        ExpandedStageLayoutInstaller.Verify();SessionState.SetBool(Key,true);SessionState.SetFloat(Key+".volume",AudioListener.volume);
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
        mapIndex=variant=step=checks=killed=0;deadline=EditorApplication.timeSinceStartup+600;wait=0;AudioListener.volume=0;
        templates=new GameObject("InactiveTestTemplates");templates.SetActive(false);EditorApplication.update+=Tick;
    }
    static void Check(bool valid,string label){if(!valid)throw new Exception("UNIQUE_PLAY_FAILED "+Maps[mapIndex]+"/"+variant+" "+label);checks++;Debug.Log("UNIQUE_PLAY_PASS "+Maps[mapIndex]+"/"+variant+" "+label);}
    static void Next(int s,float delay=.2f){step=s;wait=EditorApplication.timeSinceStartup+delay;}
    static void Tick()
    {
        if(!Application.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Unique layout timeout step="+step);
            if(EditorApplication.timeSinceStartup<wait)return;
            switch(step)
            {
                case 0:
                    manager=MapManager.instance;if(manager==null||manager.IsLoading)return;
                    hero=Object.FindFirstObjectByType<PlayerStats>();hero.GrantInvincibility(600);Next(1);break;
                case 1:
                    if(prefab!=null)Object.Destroy(prefab);if(data!=null)Object.Destroy(data);
                    data=Object.Instantiate(AssetDatabase.LoadAssetAtPath<MapData>("Assets/Data/Map/MapData_"+Maps[mapIndex]+".asset"));
                    prefab=Object.Instantiate(data.mapPrefab,templates.transform);
                    var random=prefab.GetComponent<MapLayoutRandomizer>();
                    for(int i=0;i<random.layouts.Length;i++)random.layouts[i].SetActive(i==variant);
                    random.layouts=new[]{random.layouts[variant]};data.mapPrefab=prefab;
                    manager.LoadMap(data);Next(2,4);break;
                case 2:
                    if(manager.IsLoading)return;
                    var root=manager.CurrentMapRoot;var layout=root.GetComponent<MapLayoutRandomizer>();
                    Check(layout.CurrentLayout==0&&layout.layouts[0].activeInHierarchy,"forced layout is active");
                    var floor=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");var walls=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Bulkheads_Unified64");
                    Check(floor.HasTile(floor.WorldToCell(hero.transform.position)),"spawn has floor");
                    Check(root.GetComponentsInChildren<ShopClickable>().Count(s=>s.isBuffShop)==1,"weapon shop has buff companion");
                    Check(root.GetComponentsInChildren<BreakableProp>().Length>0,"random breakable crates generated");
                    Check(root.GetComponent<RandomRoomWalls>().PlacedCells>0,"random wall shapes generated");
                    var graph=new MapRoomGraph(root.gameObject);Check(graph.nodes.Count>=8&&graph.edges.Count>=8,"minimap rooms and links");
                    Check(graph.nodes.Count(n=>n.hasExitPortal)==1,"one discoverable exit room");
                    room=root.GetComponentsInChildren<RoomController>().Where(r=>!r.IsSafeRoom).OrderBy(r=>Vector2.Distance(hero.transform.position,r.transform.position)).First();
                    hero.transform.position=room.transform.position;hero.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Next(3,1.5f);break;
                case 3:
                    if(CinematicDirector.IsOpen){Next(3,1);break;}
                    if(room.AliveMonstersCount==0){Next(3,.3f);break;}
                    Check(room.HasStarted&&room.AliveMonstersCount>0,"entering room spawns combat");
                    Check(room.doors.All(d=>d.GetComponent<AnimatedRoomGate>().IsClosed&&d.GetComponent<BoxCollider2D>().enabled),"every doorway locks with collision");
                    Check(room.GetComponent<RoomEntryTrapSpawner>().HasRolled,"room traps roll on first visit");
                    var monsters=room.GetComponentsInChildren<MonsterController>().Where(m=>m.IsAlive).ToArray();
                    Check(monsters.All(m=>floorFor(root:manager.CurrentMapRoot).HasTile(floorFor(manager.CurrentMapRoot).WorldToCell(m.transform.position))),"enemies spawn on floor");
                    if(variant==0)typeof(StageExpansionPlayChecks).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"Map_"+Maps[mapIndex]+"_play.png"});
                    killed=0;Next(4);break;
                case 4:
                    if(CinematicDirector.IsOpen){Next(4,1);break;}
                    foreach(var monster in room.GetComponentsInChildren<MonsterController>().Where(m=>m.IsAlive)){monster.TakeDamage(99999);killed++;}
                    if(!room.IsCleared){Next(4,.3f);break;}
                    Check(killed>0&&room.IsCleared,"all waves can be cleared");
                    Check(room.doors.All(d=>!d.GetComponent<AnimatedRoomGate>().IsClosed),"gates reopen after clear");
                    Check(room.GetComponentInChildren<TreasureChest>()!=null,"reward chest appears");
                    Debug.Log("UNIQUE_PLAY_LAYOUT_COMPLETE "+Maps[mapIndex]+"/"+variant+" kills="+killed);
                    variant++;if(variant==3){variant=0;mapIndex++;}
                    if(mapIndex==Maps.Length){Debug.Log("UNIQUE_STAGE_PLAY_COMPLETE layouts=15 checks="+checks);Finish(0);}else Next(1,.5f);
                    break;
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static Tilemap floorFor(Transform root)=>root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);AudioListener.volume=SessionState.GetFloat(Key+".volume",1);
        Time.timeScale=1;PauseManager.isGamePaused=false;EditorApplication.Exit(code);
    }
}
