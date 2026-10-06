using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class StageExpansionInstaller
{
    public const string TutorialScene="Assets/Scenes/TutorialScene.unity";
    public const string TutorialMap="Assets/Data/Map/MapData_Tutorial.asset";
    public const string TutorialPrefab="Assets/Prefab/Tutorial/TrainingMap.prefab";
    public static readonly string[] NewMaps={"1_4","1_5","2_3","2_4","2_5"};
    public static string MapPath(string key)=>"Assets/Data/Map/MapData_"+key+".asset";
    public static string PrefabPath(string key)=>"Assets/Prefab/ExpandedStages/Map_"+key+".prefab";
    static T Load<T>(string path) where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new Exception("Missing "+path);
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
    static T Copy<T>(string from,string to) where T:Object
    {
        var found=AssetDatabase.LoadAssetAtPath<T>(to);if(found!=null)return found;
        Folder(System.IO.Path.GetDirectoryName(to).Replace('\\','/'));
        if(!AssetDatabase.CopyAsset(from,to))throw new Exception("Copy failed "+to);return Load<T>(to);
    }
    [MenuItem("Tools/Quantum Rift/Add Five Stages and Tutorial")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(var key in NewMaps)CreateStage(key);
            Chain("1_3","1_4");Chain("1_4","1_5");Chain("1_5","1_bossroom");
            Chain("2_2","2_3");Chain("2_3","2_4");Chain("2_4","2_5");Chain("2_5","2_boss");
            CreateTutorial();ConnectMenu();DevCatalogBuilder.Refresh(true);AssetDatabase.SaveAssets();Verify();
            Debug.Log("STAGE_EXPANSION_INSTALLED stages=5 tutorial=1 progression=13");
        }
        finally
        {
            if(previous.Length>0&&previous.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    static void Chain(string a,string b){var map=Load<MapData>(MapPath(a));map.nextMap=Load<MapData>(MapPath(b));EditorUtility.SetDirty(map);}
    static void CreateStage(string key)
    {
        int theme=int.Parse(key.Substring(0,1)),stage=int.Parse(key.Substring(2));
        string source=theme==1?"1_3":"2_2";
        var sourceMap=Load<MapData>(MapPath(source));
        var map=Copy<MapData>(MapPath(source),MapPath(key));map.mapName="Map "+key.Replace('_','-');map.isTutorial=false;map.isBossRoom=false;
        map.mapPrefab=Copy<GameObject>(AssetDatabase.GetAssetPath(sourceMap.mapPrefab),PrefabPath(key));
        var root=PrefabUtility.LoadPrefabContents(PrefabPath(key));
        try
        {
            root.name="Map_"+key;
            var encounterMap=new Dictionary<RoomEncounterData,RoomEncounterData>();
            foreach(var room in root.GetComponentsInChildren<RoomController>(true))
            {
                if(room.roomData==null)continue;
                var old=room.roomData;
                if(!encounterMap.TryGetValue(old,out var data))
                {
                    string suffix=old.name.Contains("Exit")?"Exit":"Room";
                    data=Copy<RoomEncounterData>(AssetDatabase.GetAssetPath(old),$"Assets/Data/Map/RoomData/Map {key.Replace('_','-')} - {suffix}.asset");
                    data.minMonsters=stage>=5?4:3;data.maxMonsters=stage>=4?5:4;
                    data.minWaves=data.maxWaves=3;
                    data.leaderChance=suffix=="Exit"?1:Mathf.Min(.55f,.35f+.1f*(stage-3));
                    EditorUtility.SetDirty(data);encounterMap.Add(old,data);
                }
                room.roomData=data;
            }
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(key));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        map.backgroundMusic=sourceMap.backgroundMusic;EditorUtility.SetDirty(map);
    }
    static Transform Point(Transform parent,string name,Vector2 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
    static void CreateTutorial()
    {
        Folder("Assets/Prefab/Tutorial");
        var map=AssetDatabase.LoadAssetAtPath<MapData>(TutorialMap);
        if(map==null){map=ScriptableObject.CreateInstance<MapData>();AssetDatabase.CreateAsset(map,TutorialMap);}
        map.mapName="Tutorial / สนามฝึก";map.isTutorial=true;map.isBossRoom=false;map.nextMap=null;map.possibleEvents=Array.Empty<MapEventData>();
        var source=Load<MapData>(MapPath("1_1"));map.chestLoot=source.chestLoot;map.backgroundMusic=source.backgroundMusic;
        var floor=Load<Tile>("Assets/Data/Map/Map1-Unified-v2/FloorPlain.asset");
        var conduit=Load<Tile>("Assets/Data/Map/Map1-Unified-v2/FloorConduit.asset");
        var wall=Load<Tile>("Assets/Data/Map/Map1-Unified-v2/WallPlain.asset");
        const string wallPath="Assets/Data/Map/TutorialWall.asset";
        var solidWall=AssetDatabase.LoadAssetAtPath<Tile>(wallPath);
        if(solidWall==null){solidWall=Object.Instantiate(wall);AssetDatabase.CreateAsset(solidWall,wallPath);}
        solidWall.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(solidWall);wall=solidWall;
        float cell=floor.sprite.bounds.size.x;
        var root=new GameObject("TutorialTrainingMap",typeof(Grid));root.GetComponent<Grid>().cellSize=new Vector3(cell,cell,1);
        var ground=Tilemap(root.transform,"Floor_Unified64","ground",false);
        var walls=Tilemap(root.transform,"Bulkheads_Unified64","object",true);
        var walk=new HashSet<Vector3Int>();
        for(int bay=0;bay<4;bay++)for(int x=-4;x<=4;x++)for(int y=-4;y<=4;y++)walk.Add(new Vector3Int(bay*12+x,y,0));
        for(int x=4;x<=32;x++)for(int y=-1;y<=1;y++)walk.Add(new Vector3Int(x,y,0));
        foreach(var p in walk)ground.SetTile(p,p.y==0&&p.x%3==0?conduit:floor);
        foreach(var p in walk)foreach(var offset in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right})if(!walk.Contains(p+offset))walls.SetTile(p+offset,wall);
        var tutorial=root.AddComponent<TutorialDirector>();tutorial.loot=map.chestLoot;
        tutorial.dummyPoint=Point(root.transform,"TrainingDummy",new Vector2(cell,cell));
        tutorial.cratePoint=Point(root.transform,"SupplyCrate",new Vector2(11*cell,cell));
        tutorial.pickupPoint=Point(root.transform,"PotionsAndCoin",new Vector2(14*cell,cell));
        tutorial.exitPoint=Point(root.transform,"RiftExit",new Vector2(36*cell,0));
        tutorial.cratePrefab=Load<GameObject>("Assets/Prefab/MapObjects/Spaceship/BreakableWall.prefab");
        if(tutorial.cratePrefab.GetComponent<BreakableProp>()==null)throw new Exception("Training crate has no BreakableProp");
        var worker=AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data"}).Select(g=>Load<MonsterData>(AssetDatabase.GUIDToAssetPath(g))).First(m=>m.monsterPrefab!=null&&m.monsterPrefab.name.StartsWith("Rift-Drained"));
        const string dummyPath="Assets/Data/Map/TutorialDummy.asset";
        tutorial.dummyData=Copy<MonsterData>(AssetDatabase.GetAssetPath(worker),dummyPath);tutorial.dummyData.maxHealth=80;tutorial.dummyData.moveSpeed=0;tutorial.dummyData.attackDamage=0;EditorUtility.SetDirty(tutorial.dummyData);
        const string practicePath="Assets/Data/Map/TutorialEnemy.asset";
        var practice=Copy<MonsterData>(AssetDatabase.GetAssetPath(worker),practicePath);practice.maxHealth=12;practice.attackDamage=.25f;practice.moveSpeed=.8f;EditorUtility.SetDirty(practice);
        const string roomPath="Assets/Data/Map/RoomData/Tutorial Combat.asset";
        var encounter=AssetDatabase.LoadAssetAtPath<RoomEncounterData>(roomPath);if(encounter==null){encounter=ScriptableObject.CreateInstance<RoomEncounterData>();AssetDatabase.CreateAsset(encounter,roomPath);}
        encounter.monstersToSpawn=new[]{practice,practice};EditorUtility.SetDirty(encounter);
        var room=Point(root.transform,"PracticeCombatRoom",new Vector2(24*cell,0)).gameObject;
        tutorial.combatRoom=room.AddComponent<RoomController>();tutorial.combatRoom.roomData=encounter;tutorial.combatRoom.canHostEvent=false;
        var zone=room.AddComponent<BoxCollider2D>();zone.isTrigger=true;zone.size=Vector2.one*cell*7;zone.enabled=false;
        tutorial.combatRoom.monsterSpawnPoints=new[]{Point(room.transform,"Spawn_A",new Vector2(0,cell)),Point(room.transform,"Spawn_B",new Vector2(2*cell,-cell))};
        // โซนปลอดภัยและชื่อจุดเชื่อม ให้มินิแมพรู้เส้นทาง 0→1→2→3 (จุด 0 คือ PlayerSpawn)
        foreach(int id in new[]{1,3})
        {
            var safe=Point(root.transform,"TrainingZone_"+id,new Vector2(id*12*cell,0)).gameObject;
            var controller=safe.AddComponent<RoomController>();var area=safe.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=Vector2.one*cell*7;
            var links=new List<GameObject>();foreach(int neighbor in id==1?new[]{0,2}:new[]{2})links.Add(Point(safe.transform,$"Gate_{id}_To_{neighbor}",Vector2.zero).gameObject);
            controller.doors=links.ToArray();
        }
        var template=source.mapPrefab.GetComponentInChildren<AnimatedRoomGate>(true);
        var doors=new List<GameObject>();
        foreach(int side in new[]{-1,1})
        {
            var gate=Object.Instantiate(template.gameObject,room.transform);gate.name=side<0?"Gate_2_To_1":"Gate_2_To_3";gate.transform.localPosition=new Vector3(side*4.5f*cell,.5f*cell,0);gate.transform.rotation=Quaternion.Euler(0,0,90);gate.GetComponent<AnimatedRoomGate>().initiallyClosed=false;doors.Add(gate);
            var gateScale=gate.transform.lossyScale;var blocker=gate.GetComponent<BoxCollider2D>();blocker.offset=Vector2.zero;blocker.size=new Vector2(3*cell/Mathf.Abs(gateScale.x),.45f/Mathf.Abs(gateScale.y));
        }
        tutorial.combatRoom.doors=doors.ToArray();
        var portal=source.mapPrefab.GetComponentInChildren<MapPortal>(true);
        tutorial.portalVisual=Object.Instantiate(portal.gameObject,root.transform);tutorial.portalVisual.name="TutorialRiftExit";tutorial.portalVisual.transform.position=tutorial.exitPoint.position;Object.DestroyImmediate(tutorial.portalVisual.GetComponent<MapPortal>());tutorial.portalVisual.SetActive(false);
        var bgTemplate=source.mapPrefab.GetComponentInChildren<WorldFlowBackdrop>(true);
        if(bgTemplate!=null)Object.Instantiate(bgTemplate.gameObject,root.transform);
        string[] signs={"01 · MOVEMENT / ATTACK","02 · SUPPLIES","03 · COMBAT","04 · RIFT EXIT"};
        for(int i=0;i<4;i++)
        {
            var sign=Point(root.transform,"StationSign_"+i,new Vector2(i*12*cell,3.1f*cell)).gameObject.AddComponent<TextMeshPro>();sign.text=signs[i];sign.fontSize=2.1f;sign.alignment=TextAlignmentOptions.Center;sign.rectTransform.sizeDelta=new Vector2(10*cell,2);sign.color=QuantumUiSkin.Cyan;sign.GetComponent<MeshRenderer>().sortingLayerName="Effect";
        }
        map.spawnPosition=new Vector2(-2*cell,0);Point(root.transform,"PlayerSpawn",map.spawnPosition);
        map.mapPrefab=PrefabUtility.SaveAsPrefabAsset(root,TutorialPrefab);Object.DestroyImmediate(root);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
        if(!System.IO.File.Exists(TutorialScene))AssetDatabase.CopyAsset("Assets/Scenes/GameScene.unity",TutorialScene);
        var scene=EditorSceneManager.OpenScene(TutorialScene);
        var manager=Object.FindFirstObjectByType<MapManager>();manager.firstMap=map;
        var bootstrap=Object.FindFirstObjectByType<TutorialBootstrap>()??new GameObject("TutorialBootstrap").AddComponent<TutorialBootstrap>();bootstrap.defaultCharacter=Load<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=TutorialScene).ToList();scenes.Add(new EditorBuildSettingsScene(TutorialScene,true));EditorBuildSettings.scenes=scenes.ToArray();
    }
    static Tilemap Tilemap(Transform parent,string name,string layer,bool collision)
    {
        var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingLayerName=layer;renderer.sortingOrder=collision?-1:0;if(collision)go.AddComponent<TilemapCollider2D>();return go.GetComponent<Tilemap>();
    }
    static void ConnectMenu()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var menu=Object.FindFirstObjectByType<MainMenuController>();
        var existing=Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="TutorialButton");
        if(existing==null)
        {
            // ปุ่มแยก ไม่ย้ายปุ่มเมนูเดิมหรือคู่มือ
            var canvas=menu.mainMenuUI.GetComponentInParent<Canvas>();
            var go=new GameObject("TutorialButton",typeof(RectTransform),typeof(Image),typeof(Button));go.layer=5;go.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-34,-32);rect.sizeDelta=new Vector2(260,65);
            var image=go.GetComponent<Image>();image.color=QuantumUiSkin.Surface;existing=go.GetComponent<Button>();existing.targetGraphic=image;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.layer=5;label.transform.SetParent(go.transform,false);var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
            var text=label.GetComponent<TextMeshProUGUI>();text.fontSize=28;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;
            var localized=label.AddComponent<LocalizedText>();localized.thaiText="สนามฝึกสอน";localized.englishText="TUTORIAL";localized.Apply();
            QuantumUiSkin.Button(existing,10);
        }
        bool wired=false;for(int i=0;i<existing.onClick.GetPersistentEventCount();i++)if(existing.onClick.GetPersistentMethodName(i)==nameof(MainMenuController.OnTutorialClicked))wired=true;
        if(!wired)UnityEventTools.AddPersistentListener(existing.onClick,menu.OnTutorialClicked);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void Verify()
    {
        var chain=new List<MapData>();for(var m=Load<MapData>(MapPath("1_1"));m!=null;m=m.nextMap){if(chain.Contains(m))throw new Exception("Progression cycle");chain.Add(m);}
        string[] expected={"1_1","1_2","1_3","1_4","1_5","1_bossroom","2_1","2_2","2_3","2_4","2_5","2_boss","boss"};
        if(!chain.Select(m=>m.name).SequenceEqual(expected.Select(k=>"MapData_"+k)))throw new Exception("Incorrect progression");
        foreach(var key in NewMaps)
        {
            var map=Load<MapData>(MapPath(key));var root=map.mapPrefab;
            if(map.isBossRoom||map.isTutorial||map.backgroundMusic==null||map.chestLoot==null||map.possibleEvents.Length==0)throw new Exception("Incomplete stage "+key);
            var layout=root.GetComponent<MapLayoutRandomizer>();if(layout==null||layout.layouts.Length<2)throw new Exception("Random layouts missing "+key);
            foreach(var room in root.GetComponentsInChildren<RoomController>(true))if(room.roomData!=null&&!room.roomData.name.Contains(key.Replace('_','-')))throw new Exception("Shared old encounter "+key);
        }
        var tutorial=Load<MapData>(TutorialMap);if(!tutorial.isTutorial||tutorial.mapPrefab.GetComponent<TutorialDirector>()==null)throw new Exception("Tutorial data missing");
        if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==TutorialScene))throw new Exception("Tutorial excluded from build");
        Debug.Log("STAGE_ASSET_CHECKS_COMPLETE chain=13 newMaps=5 tutorial=true");
    }
    // ทดสอบทุกห้องร้านของทุกผัง ไม่พึ่งผลสุ่มรอบเดียว รวม SporeGlade ที่ใช้ trigger ทางเข้าขนาดเล็ก
    public static void VerifyVendorSpaces()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);int count=0;
        foreach(string key in new[]{"1_5","2_5"})
        {
            var map=Load<MapData>(MapPath(key));var root=Object.Instantiate(map.mapPrefab);var layout=root.GetComponent<MapLayoutRandomizer>();
            try
            {
                for(int i=0;i<layout.layouts.Length;i++)
                {
                    for(int j=0;j<layout.layouts.Length;j++)layout.layouts[j].SetActive(i==j);
                    foreach(var gate in root.GetComponentsInChildren<AnimatedRoomGate>())gate.SetClosed(false,true);
                    Physics2D.SyncTransforms();
                    foreach(var room in root.GetComponentsInChildren<RoomController>().Where(r=>r.canHostEvent))
                    {
                        var shop=Object.Instantiate(map.possibleEvents[0].eventPrefab,room.transform.position,Quaternion.identity,room.transform);
                        Physics2D.SyncTransforms();var companion=shop.GetComponent<BuffShopCompanion>();
                        var vendor=companion.Spawn();
                        if(vendor==null)throw new Exception("Vendor space missing: "+key+" layout="+i+" room="+room.name);
                        count++;Object.DestroyImmediate(vendor);Object.DestroyImmediate(shop);Physics2D.SyncTransforms();
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        Debug.Log("STAGE_VENDOR_SPACES_COMPLETE rooms="+count);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
}
