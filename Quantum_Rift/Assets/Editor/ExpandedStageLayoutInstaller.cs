using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// ผังเฉพาะด่านใหม่ ใช้ไทล์และภาพเดิม แต่สร้างห้อง/ทางเดิน/ประตูใหม่จริง
public static class ExpandedStageLayoutInstaller
{
    static readonly string[] Keys = { "1_4", "1_5", "2_3", "2_4", "2_5" };
    static string Output { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-layoutOutput"); return i >= 0 ? a[i + 1] : Path.GetFullPath("../StageLayoutPreviews"); } }
    static string MapPath(string key) => "Assets/Data/Map/MapData_" + key + ".asset";
    static string PrefabPath(string key) => "Assets/Prefab/ExpandedStages/Map_" + key + ".prefab";
    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception("Missing " + path);
    public static void Describe()
    {
        foreach (var key in new[] { "1_4", "2_3" })
        {
            var map = Load<MapData>(MapPath(key)); var root = map.mapPrefab;
            Debug.Log("LAYOUT_SOURCE " + key + " root=" + string.Join(",",root.GetComponents<Component>().Select(c=>c.GetType().Name)) + " spawn=" + map.spawnPosition);
            foreach(var layout in root.GetComponent<MapLayoutRandomizer>().layouts)
                Debug.Log("LAYOUT_ROOT " + layout.name + " pos=" + layout.transform.localPosition + " grid=" + layout.GetComponent<Grid>()?.cellSize);
            foreach(var t in root.GetComponentsInChildren<Tilemap>(true).Take(4))
                Debug.Log("LAYOUT_TILEMAP " + t.name + " position=" + t.transform.localPosition + " grid=" + t.layoutGrid.cellSize + " layer=" + t.GetComponent<TilemapRenderer>()?.sortingLayerName);
            var random = root.GetComponentInChildren<MapAssetRandomizer>(true);
            Debug.Log("LAYOUT_PROPS " + random.slots.Length + " sprite=" + random.slots[0].display.sprite.name + " scale=" + random.slots[0].display.transform.lossyScale);
            foreach(var c in root.GetComponentsInChildren<RoomController>(true).Take(2))
                Debug.Log("LAYOUT_ROOM " + c.name + " center=" + c.transform.position + " chest=" + AssetDatabase.GetAssetPath(c.chestPrefab) + " traps=" + c.GetComponent<RoomEntryTrapSpawner>()?.frames.Length);
        }
    }
    const float Cell = 1.25f;
    static readonly Vector3Int[] Sides = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
    sealed class Plan
    {
        public string name;
        public Vector2Int[] centers;
        public Vector2Int[] links;
        public Plan(string n, int[] xy, int[] edges)
        {
            name=n; centers=Enumerable.Range(0,xy.Length/2).Select(i=>new Vector2Int(xy[i*2],xy[i*2+1])).ToArray();
            links=Enumerable.Range(0,edges.Length/2).Select(i=>new Vector2Int(edges[i*2],edges[i*2+1])).ToArray();
        }
    }
    static readonly Plan[] Plans = {
        new Plan("ReactorRing", new[]{0,0, 0,24, -24,24, -24,48, 0,48, 24,48, 24,24, 0,76}, new[]{0,1, 1,2, 2,3, 3,4, 4,5, 5,6, 6,1, 4,7}),
        new Plan("CargoTwinLanes", new[]{0,0, 0,24, -28,24, 28,24, -28,52, 28,52, 0,52, -56,24, 0,82}, new[]{0,1, 1,2, 1,3, 2,4, 3,5, 4,6, 5,6, 1,6, 2,7, 6,8}),
        new Plan("ForkedGrove", new[]{0,0, 0,24, -28,24, 28,24, -28,52, 28,52, 56,24, 56,52}, new[]{0,1, 1,2, 1,3, 2,4, 3,5, 3,6, 5,7, 6,7}),
        new Plan("WindingCanopy", new[]{0,0, -26,0, -26,28, 0,28, 26,28, 26,56, 0,56, -26,56}, new[]{0,1, 1,2, 2,3, 3,4, 4,5, 5,6, 6,7, 3,6}),
        new Plan("RootHeartNetwork", new[]{0,0, 0,26, -30,26, 30,26, -30,58, 30,58, 0,58, -60,58, 0,90}, new[]{0,1, 1,2, 1,3, 2,4, 3,5, 4,6, 5,6, 4,7, 6,8})
    };
    sealed class Templates
    {
        public GameObject gate,portal,background;
        public GameObject chest;
        public MapAssetRandomizer.Slot[] props;
        public GameObject[] propObjects;
        public Sprite[] trapFrames;
        public Material floorMaterial,wallMaterial;
        public Tile[] floors,walls;
    }
    sealed class RoomShape
    {
        public int hx,hy,kind;
        public Vector2Int center;
        public RoomController room;
        public bool Includes(int x,int y,float inset=0)
        {
            float ax=Mathf.Abs(x),ay=Mathf.Abs(y),rx=hx-inset,ry=hy-inset;
            if(ax>rx||ay>ry)return false;
            if(kind==1)return ax+ay<=rx+ry-3;
            if(kind==2)return ax*ax/(rx*rx)+ay*ay/(ry*ry)<=1.05f;
            if(kind==3)return ax<=rx*.55f||ay<=ry*.55f;
            return true;
        }
    }
    static Transform Group(Transform parent,string name)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
    }
    static Vector2 World(Vector2Int p)=>new Vector2((p.x+.5f)*Cell,(p.y+.5f)*Cell);
    static Tilemap NewTiles(Transform parent,string name,string layer,Material material,bool solid)
    {
        var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);
        var r=go.GetComponent<TilemapRenderer>();r.sortingLayerName=layer;r.sortingOrder=0;r.sharedMaterial=material;
        if(solid)go.AddComponent<TilemapCollider2D>();return go.GetComponent<Tilemap>();
    }
    static Tile SolidTile(Tile source,string theme,int index)
    {
        const string folder="Assets/Data/Map/ExpandedStages";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Data/Map","ExpandedStages");
        string path=folder+"/"+theme+"Wall_"+index+".asset";
        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=Object.Instantiate(source);AssetDatabase.CreateAsset(tile,path);}
        tile.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(tile);return tile;
    }
    static Templates ReadTemplates(GameObject root,bool forest)
    {
        var t=new Templates();
        t.gate=Object.Instantiate(root.GetComponentInChildren<AnimatedRoomGate>(true).gameObject);t.gate.SetActive(false);
        t.portal=Object.Instantiate(root.GetComponentInChildren<MapPortal>(true).gameObject);t.portal.SetActive(false);
        t.background=Object.Instantiate(root.GetComponentInChildren<WorldFlowBackdrop>(true).gameObject);t.background.SetActive(false);
        var room=root.GetComponentsInChildren<RoomController>(true).First(r=>r.roomData!=null);t.chest=room.chestPrefab;
        t.trapFrames=root.GetComponentInChildren<RoomEntryTrapSpawner>(true).frames;
        var random=root.GetComponentsInChildren<MapAssetRandomizer>(true).First(r=>r.slots.Length>0);
        var usable=random.slots.Where(s=>s.display!=null).ToArray();
        t.propObjects=usable.Select(s=>Object.Instantiate(s.display.gameObject)).ToArray();
        t.props=usable.Select((s,i)=>new MapAssetRandomizer.Slot { display=t.propObjects[i].GetComponent<SpriteRenderer>(),variants=s.variants,presence=s.presence }).ToArray();
        foreach(var p in t.propObjects)p.SetActive(false);
        var maps=root.GetComponentsInChildren<Tilemap>(true);
        t.floorMaterial=maps.First(m=>m.name.StartsWith("Floor")&&m.GetComponent<TilemapRenderer>()!=null).GetComponent<TilemapRenderer>().sharedMaterial;
        t.wallMaterial=maps.First(m=>m.name.StartsWith("Bulkheads")&&m.GetComponent<TilemapRenderer>()!=null).GetComponent<TilemapRenderer>().sharedMaterial;
        string tileFolder="Assets/Data/Map/"+(forest?"Map2-Forest-v1/":"Map1-Unified-v2/");
        t.floors=(forest?new[]{"Soil","Moss","RootTrail","SporeTrail"}:new[]{"FloorPlain","FloorConduit","FloorCrack","FloorVent"}).Select(n=>Load<Tile>(tileFolder+n+".asset")).ToArray();
        t.walls=(forest?new[]{"RootWall","GlowRoots","RootKnot"}:new[]{"WallPlain","WallLight","WallVent"}).Select((n,i)=>SolidTile(Load<Tile>(tileFolder+n+".asset"),forest?"Forest":"Spaceship",i)).ToArray();
        return t;
    }
    static void DestroyTemplates(Templates t)
    {
        Object.DestroyImmediate(t.gate);Object.DestroyImmediate(t.portal);Object.DestroyImmediate(t.background);
        foreach(var p in t.propObjects)Object.DestroyImmediate(p);
    }
    [MenuItem("Tools/Quantum Rift/Rebuild Five Unique Stage Layouts")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=EditorSceneManager.GetSceneManagerSetup();Directory.CreateDirectory(Output);
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            for(int m=0;m<Keys.Length;m++)BuildStage(m);
            AssetDatabase.SaveAssets();Verify();ConnectPreviewScenes();
            Debug.Log("UNIQUE_STAGE_LAYOUTS_COMPLETE stages=5 layouts=15");
        }
        finally
        {
            if(previous.Length>0&&previous.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    public static void ConnectPreviewScenes()
    {
        for(int theme=1;theme<=2;theme++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Map/Map_"+theme+".unity");
            string name="AddedStages_"+theme;var group=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==name);
            if(group==null)group=new GameObject(name);
            int index=0;
            for(int m=0;m<Keys.Length;m++)
            {
                if(!Keys[m].StartsWith(theme+"_"))continue;
                var existing=group.transform.Find("Map_"+Keys[m]);
                if(existing==null)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(PrefabPath(Keys[m])),group.transform);
                    instance.name="Map_"+Keys[m];instance.transform.position=new Vector3(90+index*210,-170,0);
                    var floor=instance.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                    var label=Group(group.transform,"StageLabel_"+Keys[m]).gameObject.AddComponent<TMPro.TextMeshPro>();
                    label.text="MAP "+Keys[m].Replace('_','-')+"  /  "+Plans[m].name;label.fontSize=4;label.alignment=TMPro.TextAlignmentOptions.Center;
                    label.rectTransform.sizeDelta=new Vector2(140,6);label.color=theme==1?new Color(.5f,.9f,1):new Color(.6f,1,.8f);
                    label.transform.position=instance.transform.position+new Vector3(floor.localBounds.center.x,floor.localBounds.max.y+5,0);label.GetComponent<MeshRenderer>().sortingLayerName="Effect";
                }
                // RectTransform ของป้ายในฉากโลกบันทึกพิกัดผ่าน anchoredPosition ไม่ใช่ localPosition
                var instanceTransform=group.transform.Find("Map_"+Keys[m]);
                var stageLabel=group.transform.Find("StageLabel_"+Keys[m]);
                if(stageLabel!=null)
                {
                    var labelFloor=instanceTransform.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                    var position=instanceTransform.position+new Vector3(labelFloor.localBounds.center.x,labelFloor.localBounds.max.y+5,0);
                    stageLabel.GetComponent<TMPro.TextMeshPro>().rectTransform.anchoredPosition=group.transform.InverseTransformPoint(position);
                }
                index++;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("STAGE_PREVIEW_SCENE_CONNECTED theme="+theme+" added="+index);
        }
    }
    static void BuildStage(int m)
    {
        string key=Keys[m];bool forest=m>=2;var map=Load<MapData>(MapPath(key));
        var root=PrefabUtility.LoadPrefabContents(PrefabPath(key));Templates t=null;
        try
        {
            t=ReadTemplates(root,forest);
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            root.name="Map_"+key+"_"+Plans[m].name;
            var random=root.GetComponent<MapLayoutRandomizer>();random.themeKey="Stage_"+key;
            var layouts=new List<GameObject>();
            for(int v=0;v<3;v++)
            {
                var layout=BuildLayout(root.transform,t,map,m,v);layouts.Add(layout);
                for(int i=0;i<layouts.Count;i++)layouts[i].SetActive(i==v);
                Physics2D.SyncTransforms();CheckLayout(root,map,layout,m,v);Preview(layout,key,v);
            }
            random.layouts=layouts.ToArray();for(int i=0;i<layouts.Count;i++)layouts[i].SetActive(i==0);
            var background=Object.Instantiate(t.background,root.transform);background.name="WorldFlowBackground";background.SetActive(true);
            map.spawnPosition=World(Vector2Int.zero);EditorUtility.SetDirty(map);
            var saved=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(key));map.mapPrefab=saved;EditorUtility.SetDirty(map);
        }
        finally{if(t!=null)DestroyTemplates(t);PrefabUtility.UnloadPrefabContents(root);}
    }
    static GameObject BuildLayout(Transform parent,Templates t,MapData map,int m,int v)
    {
        var plan=Plans[m];bool forest=m>=2;var layout=Group(parent,$"Layout_{v}_{plan.name}_{(v==0?"Courtyards":v==1?"Galleries":"Crossroads")}").gameObject;
        layout.AddComponent<Grid>().cellSize=new Vector3(Cell,Cell,0);
        // ชั้นการวาดและสีพื้นเท่าแมพเดิมของธีมเดียวกัน: พื้นชั้น bg ย้อมหม่น กำแพงชั้น wall (ใต้มอน/หีบ/ของในห้องที่อยู่ชั้น object)
        var floor=NewTiles(layout.transform,"Floor_Unified64","bg",t.floorMaterial,false);
        floor.color=forest?new Color(.87f,.94f,.91f):new Color(.72f,.76f,.82f);
        var wall=NewTiles(layout.transform,"Bulkheads_Unified64","wall",t.wallMaterial,true);
        var centers=plan.centers.Select(p=>new Vector2Int(Mathf.RoundToInt(p.x*(v==1?1.12f:v==2?.95f:1)),Mathf.RoundToInt(p.y*(v==1?.97f:v==2?1.1f:1)))).ToArray();
        var shapes=new List<RoomShape>();var walk=new HashSet<Vector3Int>();
        for(int id=0;id<centers.Length;id++)
        {
            var shape=ShapeFor(m,v,id,centers[id]);
            shapes.Add(shape);
            for(int x=-shape.hx;x<=shape.hx;x++)for(int y=-shape.hy;y<=shape.hy;y++)
                if(shape.Includes(x,y))walk.Add(new Vector3Int(shape.center.x+x,shape.center.y+y,0));
            if(id==0){var spawn=Group(layout.transform,"PlayerSpawn");spawn.position=World(shape.center);continue;}
            var go=Group(layout.transform,$"Encounter_{id}_{plan.name}").gameObject;go.transform.position=World(shape.center);
            var room=go.AddComponent<RoomController>();shape.room=room;room.canHostEvent=id!=centers.Length-1;
            room.roomData=Load<RoomEncounterData>($"Assets/Data/Map/RoomData/Map {Keys[m].Replace('_','-')} - {(id==centers.Length-1?"Exit":"Room")}.asset");
            room.chestPrefab=t.chest;room.chestSpawnPoint=Group(go.transform,"RewardChest");room.chestSpawnPoint.localPosition=new Vector3(0,-2*Cell,0);
            room.eventAnchor=Group(go.transform,"VendorAnchor");
            var points=new List<Transform>();foreach(var p in new[]{new Vector2(-3,-2),new Vector2(3,-2),new Vector2(-3,2),new Vector2(3,2)}){var s=Group(go.transform,"MonsterSpawn_"+points.Count);s.localPosition=p*Cell;points.Add(s);}room.monsterSpawnPoints=points.ToArray();
            // ห้อง trigger ครอบพื้นที่ภายในจริง แต่ยุบเข้าจากประตูหนึ่งช่องเพื่อไม่ปิดประตูทับตัวผู้เล่น
            var area=go.AddComponent<PolygonCollider2D>();area.isTrigger=true;area.points=Outline(shape,1f);
            var traps=go.AddComponent<RoomEntryTrapSpawner>();traps.frames=t.trapFrames;traps.minCount=2;traps.count=3;traps.maxCells=6;
        }
        foreach(var edge in plan.links)
        {
            var a=centers[edge.x];var b=centers[edge.y];
            if(a.x!=b.x&&a.y!=b.y)throw new Exception("Non-orthogonal authored connection");
            for(int x=Mathf.Min(a.x,b.x);x<=Mathf.Max(a.x,b.x);x++)for(int y=Mathf.Min(a.y,b.y);y<=Mathf.Max(a.y,b.y);y++)
                for(int side=0;side<2;side++)walk.Add(new Vector3Int(x+(a.x==b.x?side:0),y+(a.y==b.y?side:0),0));
        }
        foreach(var p in walk)
        {
            int hash=Mathf.Abs(p.x*37+p.y*19+m*41+v*7);int choice=hash%29==0?3:hash%17==0?2:hash%13==0?1:0;
            if(!forest&&(Mathf.Abs(p.x)%24==0||Mathf.Abs(p.y)%24==0)&&hash%3==0)choice=1;
            floor.SetTile(p,t.floors[choice]);
        }
        foreach(var p in walk)foreach(var d in Sides)if(!walk.Contains(p+d))wall.SetTile(p+d,t.walls[Mathf.Abs((p.x+d.x)*11+(p.y+d.y)*7)%19==0?1:0]);
        foreach(var edge in plan.links)
        {
            AddGate(shapes[edge.x],shapes[edge.y],t,edge.x,edge.y);
            AddGate(shapes[edge.y],shapes[edge.x],t,edge.y,edge.x);
        }
        var exit=Object.Instantiate(t.portal,shapes.Last().room.transform);exit.name="StageExitPortal";exit.transform.localPosition=Vector3.zero;exit.SetActive(true);
        AddProps(layout,shapes,t,m,v);
        AddCorridorTraps(layout,shapes,plan.links,t,floor,wall,m,v);
        floor.CompressBounds();wall.CompressBounds();return layout;
    }
    static Vector2Int[] CentersFor(int m,int v)=>Plans[m].centers.Select(p=>new Vector2Int(Mathf.RoundToInt(p.x*(v==1?1.12f:v==2?.95f:1)),Mathf.RoundToInt(p.y*(v==1?.97f:v==2?1.1f:1)))).ToArray();
    static RoomShape ShapeFor(int m,int v,int id,Vector2Int center)
    {
        bool forest=m>=2;var s=new RoomShape{center=center,hx=8+(id+v)%3,hy=7+(id+v*2)%3,kind=forest?1+(id+v)%3:(id+v)%2};
        if(id==0){s.hx=s.hy=5;s.kind=forest?2:1;}
        if(m==1&&id==6){s.hx=11;s.hy=9;s.kind=v==2?3:1;}
        if(m==4&&id==6){s.hx=12;s.hy=11;s.kind=2;}
        if(v==1&&id%3==1){s.hx=6;s.hy=10;s.kind=forest?2:0;}
        if(v==2&&id%3==2){s.hx=10;s.hy=6;s.kind=3;}return s;
    }
    // ใช้ mask เดียวกับตัวสร้าง แต่แก้ไทล์ของ prefab เดิมได้โดยไม่สร้างห้อง/พร็อพใหม่
    public static HashSet<Vector3Int> TwoCellWalk(int m,int v)
    {
        var centers=CentersFor(m,v);var cells=new HashSet<Vector3Int>();
        for(int id=0;id<centers.Length;id++)
        {
            var s=ShapeFor(m,v,id,centers[id]);
            for(int x=-s.hx;x<=s.hx;x++)for(int y=-s.hy;y<=s.hy;y++)if(s.Includes(x,y))cells.Add(new Vector3Int(s.center.x+x,s.center.y+y,0));
        }
        foreach(var edge in Plans[m].links)
        {
            var a=centers[edge.x];var b=centers[edge.y];
            for(int x=Mathf.Min(a.x,b.x);x<=Mathf.Max(a.x,b.x);x++)for(int y=Mathf.Min(a.y,b.y);y<=Mathf.Max(a.y,b.y);y++)
                for(int side=0;side<2;side++)cells.Add(new Vector3Int(x+(a.x==b.x?side:0),y+(a.y==b.y?side:0),0));
        }
        return cells;
    }
    public static (Vector3Int cell,bool vertical)[] CorridorProbes(int m,int v)
    {
        var centers=CentersFor(m,v);return Plans[m].links.Select(e=>(new Vector3Int(Mathf.RoundToInt((centers[e.x].x+centers[e.y].x)*.5f),Mathf.RoundToInt((centers[e.x].y+centers[e.y].y)*.5f),0),centers[e.x].x==centers[e.y].x)).ToArray();
    }
    static Vector2[] Outline(RoomShape s,float inset)
    {
        float x=(s.hx+.5f-inset)*Cell,y=(s.hy+.5f-inset)*Cell;
        if(s.kind==2)return Enumerable.Range(0,24).Select(i=>new Vector2(Mathf.Cos(i*Mathf.PI/12)*x,Mathf.Sin(i*Mathf.PI/12)*y)).ToArray();
        if(s.kind==1){float cut=3*Cell;return new[]{new Vector2(-x,-y+cut),new Vector2(-x+cut,-y),new Vector2(x-cut,-y),new Vector2(x,-y+cut),new Vector2(x,y-cut),new Vector2(x-cut,y),new Vector2(-x+cut,y),new Vector2(-x,y-cut)};}
        if(s.kind==3){float a=x*.55f,b=y*.55f;return new[]{new Vector2(-a,-y),new Vector2(a,-y),new Vector2(a,-b),new Vector2(x,-b),new Vector2(x,b),new Vector2(a,b),new Vector2(a,y),new Vector2(-a,y),new Vector2(-a,b),new Vector2(-x,b),new Vector2(-x,-b),new Vector2(-a,-b)};}
        return new[]{new Vector2(-x,-y),new Vector2(x,-y),new Vector2(x,y),new Vector2(-x,y)};
    }
    static void AddGate(RoomShape from,RoomShape to,Templates t,int a,int b)
    {
        if(from.room==null)return;
        bool vertical=from.center.x!=to.center.x;int sign=vertical?Math.Sign(to.center.x-from.center.x):Math.Sign(to.center.y-from.center.y);
        var gate=Object.Instantiate(t.gate,from.room.transform);gate.name=$"Gate_{a}_To_{b}";
        gate.transform.localPosition=vertical?new Vector3(sign*(from.hx+1f)*Cell,0,0):new Vector3(0,sign*(from.hy+1f)*Cell,0);
        // ทางเดินใช้ช่องด้านข้าง 0 และ 1 จึงเลื่อนประตูครึ่งช่องให้อยู่กลางพื้นทั้งสองช่อง
        gate.transform.localPosition+=(vertical?Vector3.up:Vector3.right)*(Cell*.5f);
        gate.transform.localRotation=Quaternion.Euler(0,0,vertical?90:0);
        var anim=gate.GetComponent<AnimatedRoomGate>();anim.initiallyClosed=false;
        gate.SetActive(true);
        GateFitInstaller.Fit(anim,gate.transform.position,2*Cell,AssetDatabase.GetAssetPath(anim.frames[0].texture).Contains("Map2-Forest"));
        var list=(from.room.doors??Array.Empty<GameObject>()).ToList();list.Add(gate);from.room.doors=list.ToArray();
    }
    static void AddProps(GameObject layout,List<RoomShape> shapes,Templates t,int m,int v)
    {
        var random=layout.AddComponent<MapAssetRandomizer>();random.layoutKey="Stage_"+Keys[m]+"_"+v;var slots=new List<MapAssetRandomizer.Slot>();
        foreach(var shape in shapes.Where(s=>s.room!=null))for(int i=0;i<2;i++)
        {
            int source=(slots.Count+m*5+v*3)%t.propObjects.Length;
            var slot=t.props.Where(s=>s.display!=null).ElementAt(source);
            Vector2 local=new Vector2(i==0?-4:4,i==0?3:-3)*Cell;
            var prop=Object.Instantiate(t.propObjects[source],shape.room.transform);prop.name="RandomThemedProp_"+i;prop.transform.localPosition=local;prop.SetActive(true);
            var r=prop.GetComponent<SpriteRenderer>();
            // ปรับคงขนาดภาพตามต้นฉบับ ไม่ขยายตามขนาดห้อง
            prop.transform.localScale=t.propObjects[source].transform.localScale;
            slots.Add(new MapAssetRandomizer.Slot{display=r,variants=slot.variants,presence=.82f,blockers=prop.GetComponents<Collider2D>()});
        }
        random.slots=slots.ToArray();random.ApplySeed(m*100+v);
    }
    static void AddCorridorTraps(GameObject layout,List<RoomShape> shapes,Vector2Int[] links,Templates t,Tilemap floor,Tilemap walls,int m,int v)
    {
        var group=Group(layout.transform,"FixedCorridorTraps");int strips=0;
        foreach(var link in links.Where(e=>e.x!=0&&e.y!=0))
        {
            if(strips==3)break;var a=shapes[link.x];var b=shapes[link.y];
            var p=new Vector2Int(Mathf.RoundToInt((a.center.x+b.center.x)*.5f),Mathf.RoundToInt((a.center.y+b.center.y)*.5f));
            if(shapes.Any(s=>s.Includes(p.x-s.center.x,p.y-s.center.y)))continue;
            for(int side=0;side<1;side++) // กับดักหนึ่งช่อง อีกช่องยังเดินอ้อมได้
            {
                var cell=new Vector3Int(p.x+(a.center.x==b.center.x?side:0),p.y+(a.center.y==b.center.y?side:0),0);
                if(!floor.HasTile(cell)||walls.HasTile(cell))continue;
                var go=new GameObject("CorridorSpike_"+strips+"_"+side);go.transform.SetParent(group,false);go.transform.position=floor.GetCellCenterWorld(cell);
                var display=go.AddComponent<SpriteRenderer>();display.sprite=t.trapFrames[0];go.transform.localScale=Vector3.one*(Cell/display.sprite.bounds.size.x);
                var box=go.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=display.sprite.bounds.size;
                var trap=go.AddComponent<FixedSpikeTrap>();trap.animatedDisplay=display;trap.animationFrames=t.trapFrames;trap.damageArea=box;trap.cycleSeconds=10;trap.Restart((m+v+strips)%5);
            }
            strips++;
        }
    }
    static void Require(bool valid,string message){if(!valid)throw new Exception("LAYOUT_INVALID "+message);}
    static HashSet<Vector3Int> Cells(Tilemap floor)
    {
        var cells=new HashSet<Vector3Int>();foreach(var p in floor.cellBounds.allPositionsWithin)if(floor.HasTile(p))cells.Add(p);return cells;
    }
    static HashSet<Vector3Int> Flood(HashSet<Vector3Int> floor,Vector3Int start,HashSet<Vector3Int> blocked=null)
    {
        var visited=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();
        if(!floor.Contains(start)||(blocked!=null&&blocked.Contains(start)))return visited;
        visited.Add(start);queue.Enqueue(start);
        while(queue.Count>0){var p=queue.Dequeue();foreach(var d in Sides){var n=p+d;if(floor.Contains(n)&&(blocked==null||!blocked.Contains(n))&&visited.Add(n))queue.Enqueue(n);}}return visited;
    }
    static void CheckLayout(GameObject root,MapData map,GameObject layout,int m,int v)
    {
        var floor=layout.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
        var walls=layout.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Bulkheads_Unified64");var cells=Cells(floor);
        var seen=Flood(cells,floor.WorldToCell(World(Vector2Int.zero)));
        Require(seen.Count==cells.Count,$"{Keys[m]}/{v} disconnected floor {seen.Count}/{cells.Count}");
        Require(!cells.Any(p=>walls.HasTile(p)),"boundary walls cover walking floor");
        var rooms=layout.GetComponentsInChildren<RoomController>();Require(rooms.Length==Plans[m].centers.Length-1,"room count");
        foreach(var room in rooms)
        {
            Require(seen.Contains(floor.WorldToCell(room.transform.position)),"room center unreachable");
            Require(room.roomData!=null&&room.doors.Length>0&&map.chestLoot!=null,"missing encounter or gates or chest loot");
            foreach(var gate in room.doors){Require(gate.GetComponent<AnimatedRoomGate>().frames.Length==7,"gate frames");Require(floor.HasTile(floor.WorldToCell(gate.transform.position)),"gate not aligned to corridor floor");}
        }
        Require(layout.GetComponentsInChildren<MapPortal>().Length==1,"one exit required");
        Require(walls.GetTilesBlock(walls.cellBounds).OfType<Tile>().All(t=>t.colliderType==Tile.ColliderType.Grid),"solid walls required");
        Physics2D.SyncTransforms();
        foreach(var room in rooms.Where(r=>r.canHostEvent))
        {
            var shop=Object.Instantiate(map.possibleEvents[0].eventPrefab,room.eventAnchor.position,Quaternion.identity,room.transform);Physics2D.SyncTransforms();
            var vendor=shop.GetComponent<BuffShopCompanion>().Spawn();Require(vendor!=null,$"{Keys[m]}/{v} missing buff shop in {room.name}");
            Object.DestroyImmediate(vendor);Object.DestroyImmediate(shop);Physics2D.SyncTransforms();
        }
        Debug.Log($"UNIQUE_LAYOUT_CHECK {Keys[m]}/{v} rooms={rooms.Length+1} floor={cells.Count} links={Plans[m].links.Length} vendors=true");
    }
    public static void Verify()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var signatures=new HashSet<string>();int count=0;
        for(int m=0;m<Keys.Length;m++)
        {
            var map=Load<MapData>(MapPath(Keys[m]));var root=Object.Instantiate(map.mapPrefab);
            try
            {
                var random=root.GetComponent<MapLayoutRandomizer>();Require(random.layouts.Length==3,"three authored variants");
                for(int v=0;v<3;v++)
                {
                    for(int i=0;i<3;i++)random.layouts[i].SetActive(i==v);Physics2D.SyncTransforms();
                    var layout=random.layouts[v];CheckLayout(root,map,layout,m,v);
                    var graph=new MapRoomGraph(root);Require(graph.nodes.Count==Plans[m].centers.Length&&graph.edges.Count==Plans[m].links.Length,"minimap graph must match authored rooms and connections");
                    Require(graph.nodes.Count(n=>n.hasExitPortal)==1,"exit minimap marker");
                    var floor=layout.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                    string signature=string.Join(";",Cells(floor).OrderBy(p=>p.x).ThenBy(p=>p.y).Select(p=>p.x+","+p.y));Require(signatures.Add(signature),"layout duplicates another stage");
                    foreach(var c in layout.GetComponentsInChildren<Component>(true))Require(c!=null,"missing scripts");count++;
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        Debug.Log("UNIQUE_LAYOUT_VERIFY_COMPLETE count="+count+" differentGeometry=true connected=true gates=true vendors=true minimap=true");
    }
    static void Preview(GameObject layout,string key,int v)
    {
        var preview=Object.Instantiate(layout);preview.SetActive(true);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var floor=preview.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");var bounds=floor.localBounds;
        var go=new GameObject("LayoutPreview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.07f);
        camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);camera.orthographicSize=Mathf.Max(bounds.extents.y+4,(bounds.extents.x+4)*.625f);
        camera.aspect=1.6f;const int width=1440,height=900;var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
        try
        {
            camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,"Map_"+key+"_layout_"+v+".png"),image.EncodeToPNG());RenderTexture.active=old;Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);Object.DestroyImmediate(preview);}
    }
}
