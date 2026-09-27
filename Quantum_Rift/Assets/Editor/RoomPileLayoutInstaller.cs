using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RoomPileLayoutInstaller
{
    static readonly string[] Maps={"map_1","map_1_2","map_1_3","Map_2","Map_2_2"};
    const float Step=1.25f;
    static readonly string[] ShapeNames={"Circle","Heart","Rectangle","LShape","Steps","TwinStacks","Compact"};
    static readonly string[][] Shapes={
        new[]{".###.","#...#","#...#","#...#",".###."},
        new[]{".#.#.","#####","#####",".###.","..#.."},
        new[]{"####","####","####"},
        new[]{"##..","##..","##..","####"},
        new[]{"..##",".###","###.","##.."},
        new[]{"##.##","##.##","##.##"},new[]{"###","###"}
    };
    static string Output { get { var a=Environment.GetCommandLineArgs(); int i=Array.IndexOf(a,"-qrOutput");return i>=0?a[i+1]:Path.GetFullPath("../RoomPilePreview"); } }
    static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
    sealed class Context
    {
        public GameObject root,layout,box,trap;
        public Tilemap floor,walls;
        public MapRoomGraph graph;
        public Vector2[] gates,portals,decor;
        public Collider2D[] solids;
        public List<Vector2> blocks=new List<Vector2>();
        public string key;
        public bool Walkable(Vector2 p,float half=.48f)
        {
            foreach(var d in new[]{Vector2.zero,new Vector2(-half,-half),new Vector2(half,-half),new Vector2(-half,half),new Vector2(half,half)})
                if(!floor.HasTile(floor.WorldToCell(p+d)) || walls.HasTile(walls.WorldToCell(p+d)))return false;
            var footprint=new Bounds(p,new Vector3(half*2,half*2,.5f));
            return !solids.Any(c=>c!=null && c.bounds.Intersects(footprint));
        }
    }
    [MenuItem("Tools/Quantum Rift/Maps/Arrange Fixed Room Piles And Corridor Spikes")]
    public static void Install(){Build(true);}
    public static void DryRun(){Build(false);}
    public static void VerifySaved()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        int total=0;
        foreach(string name in Maps)for(int v=0;v<3;v++)
        {
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/"+name+".prefab"));
            try
            {
                var layouts=map.GetComponent<MapLayoutRandomizer>().layouts;
                for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(i==v);
                Physics2D.SyncTransforms();var graph=new MapRoomGraph(map);
                var boxes=map.GetComponentsInChildren<BreakableProp>();var traps=map.GetComponentsInChildren<FixedSpikeTrap>();
                Require(graph.nodes.Count==7 && boxes.Length>=36,"Saved rooms/piles missing");
                foreach(var node in graph.nodes.Where(n=>n.room!=null))
                    Require(node.room.GetComponentsInChildren<BreakableProp>().Length>=6,"Room lost pile");
                foreach(var box in boxes)
                {
                    var col=box.GetComponent<BoxCollider2D>();var bounds=col.bounds;
                    foreach(var hit in Physics2D.OverlapBoxAll(bounds.center,(Vector2)bounds.size*.92f,0))
                        Require(!LootPlacement.IsSolid(hit) || hit.transform.IsChildOf(box.transform),"Saved block overlaps solid: "+name+"/"+v+" "+hit.name);
                }
                foreach(var trap in traps)
                {
                    Require(trap.room==null && trap.animationFrames.Length==7 && trap.cycleSeconds==10,"Saved trap reference/timing missing");
                    trap.Advance(8.9f);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Warning,"Warning missing");
                    trap.Advance(1.6f);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Raised && trap.animatedDisplay.sprite==trap.animationFrames[4],"Rise missing");
                    trap.Advance(2);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Retracted,"Retract missing");
                    trap.Advance(8);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Raised && trap.CompletedCycles==2,"Repeat missing");
                    foreach(var hit in Physics2D.OverlapBoxAll(trap.damageArea.bounds.center,(Vector2)trap.damageArea.bounds.size*.9f,0))
                        Require(!LootPlacement.IsSolid(hit),"Saved spike overlaps solid: "+name+"/"+v+" "+hit.name);
                }
                var positions=boxes.Select(b=>b.transform.position).ToArray();
                foreach(var randomizer in map.GetComponentsInChildren<MapAssetRandomizer>()){randomizer.ApplySeed(12);randomizer.ApplySeed(945);}
                Require(positions.SequenceEqual(boxes.Select(b=>b.transform.position)),"Piles moved after prop randomization");
                foreach(var component in map.GetComponentsInChildren<Component>(true))Require(component!=null,"Missing script after save");
                total++;Debug.Log("SAVED_PILE_LAYOUT_OK "+name+"/"+v);
            }
            finally{UnityEngine.Object.DestroyImmediate(map);}
        }
        Debug.Log("SAVED_PILES_VERIFIED layouts="+total+" collision=true spikeCycle=true fixedPositions=true missingScripts=0");
    }
    static void Build(bool save)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory(Output);
        int rooms=0,blocks=0,strips=0,traps=0;
        var loaded=new List<GameObject>();
        try
        {
            for(int m=0;m<Maps.Length;m++)
            {
                var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/"+Maps[m]+".prefab");loaded.Add(root);
                var layouts=root.GetComponent<MapLayoutRandomizer>().layouts;
                var states=layouts.Select(l=>l.activeSelf).ToArray();
                string theme=m<3?"Spaceship":"Forest";
                for(int v=0;v<layouts.Length;v++)
                {
                    for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(i==v);
                    var layout=layouts[v];
                    // แทนเฉพาะกล่อง/หนามชุดเดิม เก็บของตกแต่งและโครงห้องไว้ทั้งหมด
                    foreach(var t in layout.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FixedCorridorTraps").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                    foreach(var group in layout.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FixedGameplayObjects").ToArray())
                    {
                        foreach(var t in group.Cast<Transform>().Where(t=>t.name.StartsWith("Pile_") || t.GetComponent<BreakableProp>()!=null || t.GetComponent<FixedSpikeTrap>()!=null).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                    Physics2D.SyncTransforms();
                    var c=new Context {root=root,layout=layout,key=Maps[m]+"/"+v,
                        box=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/MapObjects/"+theme+"/BreakableWall.prefab"),
                        trap=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/MapObjects/"+theme+"/FloorSpikes.prefab"),
                        floor=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64"),
                        walls=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Bulkheads_Unified64"),graph=new MapRoomGraph(root),
                        gates=root.GetComponentsInChildren<AnimatedRoomGate>().Select(g=>(Vector2)g.transform.position).ToArray(),
                        portals=root.GetComponentsInChildren<MapPortal>(true).Where(p=>p.transform.IsChildOf(layout.transform)).Select(p=>(Vector2)p.transform.position).ToArray(),
                        decor=root.GetComponentsInChildren<MapAssetRandomizer>().SelectMany(r=>r.slots).Where(s=>s!=null && s.display!=null).Select(s=>(Vector2)s.display.transform.position).ToArray(),
                        solids=root.GetComponentsInChildren<Collider2D>().Where(s=>s.enabled && LootPlacement.IsSolid(s) && s.GetComponent<AnimatedRoomGate>()==null).ToArray()};
                    foreach(var node in c.graph.nodes.Where(n=>n.room!=null))
                    {
                        int shape=(node.id-1+v+m)%6;
                        Require(PlacePile(c,node,shape) || PlacePile(c,node,6),"No safe pile placement "+c.key+" room="+node.id);rooms++;
                    }
                    int lanes=PlaceCorridors(c);strips+=lanes;
                    Physics2D.SyncTransforms();
                    Verify(c);
                    int b=layout.GetComponentsInChildren<BreakableProp>().Length,trapCount=layout.GetComponentsInChildren<FixedSpikeTrap>().Length;
                    blocks+=b;traps+=trapCount;
                    Debug.Log("PILE_LAYOUT_OK "+c.key+" piles=6 blocks="+b+" corridorStrips="+lanes+" spikes="+trapCount);
                    if(v==0 && (m==0 || m==3))Preview(c,Maps[m]);
                }
                for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(states[i]);
            }
            // ตรวจครบทุกผังก่อนบันทึกจริง เพื่อไม่ทิ้งงานที่ตรวจผ่านเพียงบางแมพ
            if(save)
            {
                for(int m=0;m<Maps.Length;m++)PrefabUtility.SaveAsPrefabAsset(loaded[m],"Assets/Prefab/"+Maps[m]+".prefab");
                SavePilePrefabs();AssetDatabase.SaveAssets();
            }
            Debug.Log("ROOM_PILES_VERIFIED saved="+save+" layouts=15 rooms="+rooms+" blocks="+blocks+" corridorStrips="+strips+" spikes="+traps);
        }
        finally{foreach(var root in loaded)PrefabUtility.UnloadPrefabContents(root);}
    }
    static List<Vector2> Pattern(int shape)
    {
        var rows=Shapes[shape];var result=new List<Vector2>();
        for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)
            if(rows[y][x]=='#')result.Add(new Vector2(x-(rows[y].Length-1)*.5f,(rows.Length-1)*.5f-y)*Step);
        return result;
    }
    static Transform Group(Transform parent,string name)
    {
        var t=parent.Find(name);if(t!=null)return t;
        var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
    }
    static GameObject Copy(GameObject asset,Transform parent,Vector2 at)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        go.transform.position=at;return go;
    }
    static bool PlacePile(Context c,MapRoomGraph.Node node,int shape)
    {
        var bounds=node.areas[0].bounds;foreach(var area in node.areas)bounds.Encapsulate(area.bounds);
        var offsets=Pattern(shape);
        var candidates=new List<Vector2>();
        // คงตำแหน่งตายตัวใน Prefab ไม่สุ่มเมื่อเข้าเกม
        for(float y=bounds.min.y+1;y<bounds.max.y-1;y+=Step*.5f)
            for(float x=bounds.min.x+1;x<bounds.max.x-1;x+=Step*.5f)candidates.Add(new Vector2(x,y));
        var preferred=node.center+new Vector2(node.id%2==0?4.5f:-4.5f,node.id%3==0?-3f:3f);
        foreach(var anchor in candidates.OrderBy(p=>(p-preferred).sqrMagnitude))
        {
            var points=offsets.Select(o=>anchor+o).ToArray();
            if(points.Any(p=>!node.Contains(p) || !c.Walkable(p,.7f) || (p-node.center).sqrMagnitude<6.25f ||
                c.gates.Any(g=>(g-p).sqrMagnitude<10f) || c.portals.Any(g=>(g-p).sqrMagnitude<16f) ||
                c.decor.Any(g=>(g-p).sqrMagnitude<2.8f)))continue;
            // เว้นจุดเกิดที่วางไว้และจุดกิจกรรมกลางห้อง
            if(node.room.monsterSpawnPoints!=null && points.Any(p=>node.room.monsterSpawnPoints.Any(s=>s!=null && Vector2.Distance(s.position,p)<1.1f)))continue;
            c.blocks.AddRange(points);
            if(!Connected(c,node)){c.blocks.RemoveRange(c.blocks.Count-points.Length,points.Length);continue;}
            var parent=Group(node.room.transform,"FixedGameplayObjects");
            var pile=Group(parent,"Pile_"+ShapeNames[shape]);pile.position=anchor;
            foreach(var p in points)Copy(c.box,pile,p-Vector2.up*.55f);
            Debug.Log("PILE_PLACED "+c.key+" room="+node.id+" shape="+ShapeNames[shape]+" count="+points.Length);
            return true;
        }
        return false;
    }
    static bool Connected(Context c,MapRoomGraph.Node node)
    {
        // ทดสอบทางเดินด้วยระยะเผื่อตัวละคร: ยังเดินรอบกองจากทุกประตูถึงกลางห้องได้
        var bounds=node.areas[0].bounds;foreach(var a in node.areas)bounds.Encapsulate(a.bounds);bounds.Expand(8f);
        Vector2 origin=bounds.min;float step=.625f;
        int w=Mathf.CeilToInt(bounds.size.x/step)+1,h=Mathf.CeilToInt(bounds.size.y/step)+1;
        Func<Vector2,Vector2Int> cell=p=>new Vector2Int(Mathf.RoundToInt((p.x-origin.x)/step),Mathf.RoundToInt((p.y-origin.y)/step));
        Func<Vector2Int,Vector2> world=p=>origin+new Vector2(p.x,p.y)*step;
        var start=cell(node.center);var seen=new HashSet<Vector2Int>{start};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {
                var next=p+d;if(next.x<0||next.y<0||next.x>=w||next.y>=h||seen.Contains(next))continue;
                var q=world(next);
                if(!c.Walkable(q,.3f) || c.blocks.Any(b=>Mathf.Abs(b.x-q.x)<.9f && Mathf.Abs(b.y-q.y)<.72f))continue;
                seen.Add(next);queue.Enqueue(next);
            }
        }
        return node.room.doors.Where(g=>g!=null).All(g=>{
            Vector2 p=g.transform.position;var target=p+(node.center-p).normalized*2.5f;
            return seen.Any(s=>Vector2.Distance(world(s),target)<1.3f);
        });
    }
    static int PlaceCorridors(Context c)
    {
        var parent=Group(c.layout.transform,"FixedCorridorTraps");int count=0;
        var gates=c.root.GetComponentsInChildren<AnimatedRoomGate>();
        foreach(var edge in c.graph.edges)
        {
            var a=c.graph.nodes[edge.x];var b=c.graph.nodes[edge.y];
            var ga=gates.FirstOrDefault(g=>g.name=="Gate_"+a.id+"_To_"+b.id);
            var gb=gates.FirstOrDefault(g=>g.name=="Gate_"+b.id+"_To_"+a.id);
            Vector2 pa=ga!=null?(Vector2)ga.transform.position:a.center;
            Vector2 pb=gb!=null?(Vector2)gb.transform.position:b.center;
            bool horizontal=Mathf.Abs(pb.x-pa.x)>Mathf.Abs(pb.y-pa.y);
            Vector3Int across=horizontal?Vector3Int.up:Vector3Int.right;
            bool placed=false;
            foreach(float fraction in new[]{.5f,.4f,.6f,.3f,.7f})
            {
                var center=c.floor.WorldToCell(Vector2.Lerp(pa,pb,fraction));var cells=new List<Vector3Int>{center};
                for(int sign=-1;sign<=1;sign+=2)for(int k=1;k<9;k++)
                {var cell=center+across*(k*sign);if(!c.floor.HasTile(cell)||c.walls.HasTile(cell))break;cells.Add(cell);}
                var points=cells.Select(p=>(Vector2)c.floor.GetCellCenterWorld(p)).ToArray();
                if(points.Length<2 || points.Length>9 || points.Any(p=>!c.Walkable(p,.48f) ||
                    c.graph.nodes.Any(n=>n.Contains(p)) || c.gates.Any(g=>Vector2.Distance(g,p)<2.6f) ||
                    c.decor.Any(g=>Vector2.Distance(g,p)<1.5f)))continue;
                var strip=Group(parent,"SpikeStrip_"+a.id+"_To_"+b.id);strip.position=Vector2.Lerp(pa,pb,.5f);
                foreach(var p in points)
                {
                    var go=Copy(c.trap,strip,p);go.transform.localScale=Vector3.one*.625f;
                    var trap=go.GetComponent<FixedSpikeTrap>();trap.room=null;
                    trap.damageArea.size=new Vector2(1.95f,1.8f);trap.damageArea.offset=Vector2.zero;
                    trap.Advance(0);PrefabUtility.RecordPrefabInstancePropertyModifications(trap.damageArea);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                }
                count++;placed=true;break;
            }
            Require(placed,"No safe cross-corridor strip: "+c.key+" "+a.id+"->"+b.id);
        }
        return count;
    }
    static void Verify(Context c)
    {
        foreach(var node in c.graph.nodes.Where(n=>n.room!=null))Require(Connected(c,node),"Pile blocks room route "+c.key+"/"+node.id);
        var boxes=c.layout.GetComponentsInChildren<BreakableProp>();var traps=c.layout.GetComponentsInChildren<FixedSpikeTrap>();
        Require(boxes.Length>=36 && traps.Length>=c.graph.edges.Count*2,"Missing piles or corridor traps "+c.key);
        var positions=boxes.Select(b=>b.transform.position).ToArray();
        foreach(var rng in c.root.GetComponentsInChildren<MapAssetRandomizer>())
        {
            // ตรวจสมาชิกเท่านั้น ไม่แก้ seed/หน้าตา prop ของผู้ใช้ระหว่างบันทึกแมพ
            Require(!rng.slots.Any(s=>s!=null && s.display!=null && s.display.GetComponent<BreakableProp>()!=null),"Pile was randomized");
        }
        foreach(var trap in traps)
        {
            Require(trap.animationFrames.Length==7 && trap.cycleSeconds==10 && trap.room==null,"Invalid corridor spike");
            Require(trap.damageArea.isTrigger,"Spike blocks walkway while retracted");
            trap.Advance(8.9f);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Warning,"Missing warning");
            trap.Advance(1.2f);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Raised,"Not raised at 10s");
            trap.Advance(2f);Require(trap.CurrentPhase==FixedSpikeTrap.Phase.Retracted,"Not retracted");
            trap.Advance(8f);Require(trap.CompletedCycles==2 && trap.CurrentPhase==FixedSpikeTrap.Phase.Raised,"Not repeated at 20s");
            trap.Advance(2f);
        }
        Require(boxes.All(b=>b.GetComponent<BoxCollider2D>()!=null && b.GetComponent<CircleCollider2D>().isTrigger),"Missing box collision");
    }
    static void SavePilePrefabs()
    {
        foreach(var theme in new[]{"Spaceship","Forest"})
        {
            string path="Assets/Prefab/MapObjects/"+theme;
            if(!AssetDatabase.IsValidFolder(path+"/Piles"))AssetDatabase.CreateFolder(path,"Piles");
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path+"/BreakableWall.prefab");
            for(int s=0;s<Shapes.Length;s++)
            {
                var go=new GameObject("Pile_"+ShapeNames[s]);
                foreach(var p in Pattern(s))Copy(asset,go.transform,p-Vector2.up*.55f);
                PrefabUtility.SaveAsPrefabAsset(go,path+"/Piles/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
    static void Preview(Context c,string name)
    {
        // ภาพจาก Unity จริง ไม่เขียนกลับลง scene ที่ผู้ใช้กำลังจัดเอง
        var preview=UnityEngine.Object.Instantiate(c.root);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var go=new GameObject("PilePreviewCamera");
        var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.045f,.07f);
        foreach(int id in new[]{1,2,3})
        {
            var room=c.graph.nodes.First(n=>n.id==id);cam.transform.position=(Vector3)room.center+Vector3.back*10;cam.orthographicSize=10;
            Capture(cam,Path.Combine(Output,name+"-Room-"+id+".png"));
        }
        var strip=c.layout.transform.Find("FixedCorridorTraps").GetChild(0);cam.transform.position=strip.position+Vector3.back*10;cam.orthographicSize=7;
        Capture(cam,Path.Combine(Output,name+"-Corridor.png"));
        foreach(var trap in preview.GetComponentsInChildren<FixedSpikeTrap>())trap.Advance(10.5f);
        Capture(cam,Path.Combine(Output,name+"-Corridor-Raised.png"));
        UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(preview);
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.aspect=1.6f;camera.Render();
        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
    public static void Inspect()
    {
        foreach(var name in Maps)
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/"+name+".prefab");
            try
            {
                var layouts=root.GetComponent<MapLayoutRandomizer>().layouts;
                for(int v=0;v<layouts.Length;v++)
                {
                    for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(i==v);
                    Physics2D.SyncTransforms();
                    Debug.Log("MAP_INSPECT "+name+"/"+v+" root="+root.transform.lossyScale);
                    foreach(var tile in root.GetComponentsInChildren<Tilemap>())Debug.Log("TILE "+tile.name+" bounds="+tile.cellBounds+" cell="+tile.layoutGrid.cellSize+" scale="+tile.transform.lossyScale);
                    var graph=new MapRoomGraph(root);
                    foreach(var node in graph.nodes)
                        Debug.Log("ROOM "+node.id+" center="+node.center+" bounds="+string.Join(";",node.areas==null?new string[0]:node.areas.Select(c=>c.bounds.ToString())));
                    foreach(var gate in root.GetComponentsInChildren<AnimatedRoomGate>())Debug.Log("GATE "+gate.name+" pos="+gate.transform.position);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
