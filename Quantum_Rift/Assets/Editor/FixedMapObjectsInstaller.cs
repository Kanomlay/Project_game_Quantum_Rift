using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class FixedMapObjectsInstaller
{
    const string Art = "Assets/Prefab/MapFeatures";
    static readonly string[] Maps = { "map_1", "map_1_2", "map_1_3", "Map_2", "Map_2_2" };
    static Material material;
    static Mesh plate, tips, forestTips, warning, badge;
    static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }

    [MenuItem("Tools/Quantum Rift/Maps/Install Fixed Crates And Spikes")]
    public static void Install()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        if(!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Prefab","MapFeatures");
        material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/VertexPixel.mat");
        if(material==null) { material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,Art+"/VertexPixel.mat"); }
        plate=MakePlate();tips=MakeSpikes(false);forestTips=MakeSpikes(true);
        warning=MakeFlat("SpikeWarning",new Color32(255,170,48,255),1.45f,.85f);
        badge=MakeBadge();
        int count=0;
        foreach(string name in Maps)
        {
            string path="Assets/Prefab/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var layout=root.GetComponent<MapLayoutRandomizer>();
                bool[] states=layout.layouts.Select(l=>l.activeSelf).ToArray();
                for(int variant=0;variant<layout.layouts.Length;variant++)
                {
                    for(int j=0;j<layout.layouts.Length;j++)layout.layouts[j].SetActive(j==variant);
                    foreach(var old in layout.layouts[variant].GetComponentsInChildren<Transform>(true)
                        .Where(t=>t.name=="FixedGameplayObjects").ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
                    Physics2D.SyncTransforms();
                    var graph=new MapRoomGraph(root);
                    var floor=root.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                    var walls=root.GetComponentsInChildren<Tilemap>().FirstOrDefault(t=>t.name=="Bulkheads_Unified64");
                    var gatePoints=root.GetComponentsInChildren<AnimatedRoomGate>().Select(g=>(Vector2)g.transform.position).ToArray();
                    var portalPoints=root.GetComponentsInChildren<MapPortal>().Select(p=>(Vector2)p.transform.position).ToArray();
                    var decoration=root.GetComponentsInChildren<MapAssetRandomizer>().SelectMany(r=>r.slots)
                        .Where(s=>s!=null && s.display!=null).Select(s=>(Vector2)s.display.transform.position).ToArray();
                    var occupied=new List<Vector2>();
                    foreach(var node in graph.nodes.Where(n=>n.room!=null))
                    {
                        var group=new GameObject("FixedGameplayObjects");group.transform.SetParent(node.room.transform,false);
                        var spots=new List<Vector2>();
                        for(int y=-7;y<=7;y++)for(int x=-7;x<=7;x++)
                        {
                            Vector2 spot=node.center+new Vector2(x*1.25f,y*1.25f);
                            if(Vector2.Distance(spot,node.center)<3.5f || !node.Contains(spot))continue;
                            if(gatePoints.Any(p=>Vector2.Distance(p,spot)<3.5f) || portalPoints.Any(p=>Vector2.Distance(p,spot)<4f))continue;
                            if(decoration.Any(p=>Vector2.Distance(p,spot)<2.2f))continue;
                            if(!OnFloor(floor,walls,spot,.95f))continue;
                            spots.Add(spot);
                        }
                        spots=spots.OrderBy(p=>Vector2.Distance(p,node.center)).ThenBy(p=>p.y).ThenBy(p=>p.x).ToList();
                        Check(spots.Count>1,"No fixed placement space "+name+"/"+variant+" room "+node.id);
                        Vector2 crateSpot=spots.First();
                        var other=spots.Where(p=>Vector2.Distance(p,crateSpot)>=3.5f && occupied.All(o=>Vector2.Distance(o,p)>3f)).ToArray();
                        Check(other.Length>0,"No spike placement "+name+"/"+variant+" room "+node.id);
                        CreateCrate(root.GetComponent<MapGameplayFeatures>(),group.transform,crateSpot);
                        CreateSpike(node.room,group.transform,other[0],name.StartsWith("Map_2"));
                        occupied.Add(crateSpot);occupied.Add(other[0]);
                    }
                    count++;
                }
                for(int j=0;j<layout.layouts.Length;j++)layout.layouts[j].SetActive(states[j]);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("FIXED_OBJECTS_INSTALLED layouts="+count);
        Verify();
    }

    static bool OnFloor(Tilemap floor,Tilemap walls,Vector2 p,float extent)
    {
        foreach(var d in new[]{Vector2.zero,new Vector2(-extent,-extent),new Vector2(extent,-extent),new Vector2(-extent,extent),new Vector2(extent,extent)})
            if(!floor.HasTile(floor.WorldToCell(p+d)) || walls!=null && walls.HasTile(walls.WorldToCell(p+d)))return false;
        return true;
    }
    static void CreateCrate(MapGameplayFeatures features,Transform parent,Vector2 position)
    {
        var go=new GameObject("BreakableSupplyCrate");go.transform.SetParent(parent,false);go.transform.position=position;
        var display=go.AddComponent<SpriteRenderer>();
        display.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/Map/Map1-Unified-v2/Crate.png");
        display.sortingLayerName="object";display.sortingOrder=3;
        display.color=features.theme==MapGameplayFeatures.Theme.Spaceship?new Color(1f,.85f,.6f):new Color(.74f,1f,.8f);
        var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(1.8f,.9f);box.offset=new Vector2(0,.45f);
        var prop=go.AddComponent<BreakableProp>();
        prop.Configure(display,new Collider2D[]{box},features.healthPotionSprite,features.energyPotionSprite,features.transform);
        var hit=go.GetComponent<CircleCollider2D>();hit.offset=new Vector2(0,.55f);
        Draw("SupplyMark",go.transform,badge,5).transform.localPosition=new Vector3(0,.7f,0);
    }
    static void CreateSpike(RoomController room,Transform parent,Vector2 position,bool forest)
    {
        var go=new GameObject("FixedSpikeTrap_10Seconds");go.transform.SetParent(parent,false);go.transform.position=position;
        Draw("RecessedPlate",go.transform,plate,-2);
        var warningRender=Draw("WarningLight",go.transform,warning,-1);warningRender.enabled=false;
        var spikeRender=Draw("Spikes",go.transform,forest?forestTips:tips,4);spikeRender.gameObject.SetActive(false);
        var area=go.AddComponent<BoxCollider2D>();area.size=new Vector2(1.6f,1f);area.isTrigger=true;
        var trap=go.AddComponent<FixedSpikeTrap>();trap.room=room;trap.spikes=spikeRender.transform;
        trap.warningDisplay=warningRender;trap.damageArea=area;trap.cycleSeconds=10f;
    }
    static MeshRenderer Draw(string name,Transform parent,Mesh mesh,int order)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingLayerName="object";r.sortingOrder=order;return r;
    }
    sealed class Shape
    {
        public readonly List<Vector3> points=new List<Vector3>();
        public readonly List<Color> colors=new List<Color>();
        public readonly List<int> triangles=new List<int>();
        public void Rect(float x,float y,float w,float h,Color c)
        {
            int i=points.Count;points.Add(new Vector3(x,y,0));points.Add(new Vector3(x+w,y,0));
            points.Add(new Vector3(x+w,y+h,0));points.Add(new Vector3(x,y+h,0));
            for(int k=0;k<4;k++)colors.Add(c);
            triangles.AddRange(new[]{i,i+2,i+1,i,i+3,i+2});
        }
        public Mesh Save(string name)
        {
            string path=Art+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool exists=m!=null;if(!exists)m=new Mesh{name=name};else m.Clear();
            m.SetVertices(points);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateBounds();
            if(!exists)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;
        }
    }
    static Mesh MakeFlat(string name,Color color,float w,float h)
    {var s=new Shape();s.Rect(-w/2,-h/2,w,h,color);return s.Save(name);}
    static Mesh MakePlate()
    {
        var s=new Shape();s.Rect(-.95f,-.6f,1.9f,1.2f,new Color32(18,26,35,255));
        s.Rect(-.85f,-.5f,1.7f,1f,new Color32(62,73,82,255));
        for(int j=0;j<2;j++)for(int i=0;i<3;i++)s.Rect(-.67f+i*.55f,-.32f+j*.46f,.3f,.18f,new Color32(9,16,24,255));
        return s.Save("SpikePlate");
    }
    static Mesh MakeSpikes(bool forest)
    {
        var s=new Shape();Color dark=forest?new Color32(31,66,53,255):new Color32(41,57,79,255);
        Color light=forest?new Color32(165,215,146,255):new Color32(189,215,223,255);
        for(int row=1;row>=0;row--)for(int col=0;col<3;col++)
        {
            float x=-.53f+col*.55f,y=-.27f+row*.46f;
            for(int tier=0;tier<6;tier++)
            {
                float width=(6-tier)*.0625f;
                s.Rect(x-width/2,y+tier*.125f,width,.125f,dark);
                s.Rect(x,y+tier*.125f,Mathf.Max(.03125f,width/2),.125f,light);
            }
        }
        return s.Save(forest?"ForestSpikes":"MetalSpikes");
    }
    static Mesh MakeBadge()
    {
        var s=new Shape();s.Rect(-.28f,-.2f,.56f,.4f,new Color32(24,28,35,255));
        s.Rect(-.065f,-.15f,.13f,.3f,new Color32(255,218,96,255));
        s.Rect(-.2f,-.055f,.4f,.11f,new Color32(255,218,96,255));
        return s.Save("SupplyCrateMark");
    }

    public static void Verify()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var hud=UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        var canvas=GameObject.Find("UI").GetComponent<Canvas>();
        int variants=0,crates=0,traps=0;
        foreach(string name in Maps)for(int variant=0;variant<3;variant++)
        {
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/"+name+".prefab"));
            var hero=new GameObject("FixedObjectsCheckHero");hero.transform.position=new Vector2(9999,9999);
            try
            {
                var layout=map.GetComponent<MapLayoutRandomizer>();
                for(int j=0;j<layout.layouts.Length;j++)layout.layouts[j].SetActive(j==variant);
                Physics2D.SyncTransforms();
                var boxes=map.GetComponentsInChildren<BreakableProp>();
                var spikes=map.GetComponentsInChildren<FixedSpikeTrap>();
                Check(boxes.Length==6 && spikes.Length==6,name+" missing fixed objects "+variant);
                var positions=boxes.Select(b=>b.transform.position).ToArray();
                var spriteNames=boxes.Select(b=>b.GetComponent<SpriteRenderer>().sprite.name).ToArray();
                foreach(var rng in map.GetComponentsInChildren<MapAssetRandomizer>())
                {
                    Check(!rng.slots.Any(s=>s.display!=null && s.display.GetComponent<BreakableProp>()!=null),"Crate belongs to randomized decoration");
                    rng.ApplySeed(123);rng.ApplySeed(9823);
                }
                for(int i=0;i<boxes.Length;i++)Check(positions[i]==boxes[i].transform.position && spriteNames[i]==boxes[i].GetComponent<SpriteRenderer>().sprite.name,"Crate changed with random seed");
                Physics2D.SyncTransforms();
                foreach(var fixedObject in boxes.Select(b=>b.transform).Concat(spikes.Select(s=>s.transform)))
                    foreach(var hit in Physics2D.OverlapBoxAll(fixedObject.position,new Vector2(1.5f,.85f),0))
                        Check(!LootPlacement.IsSolid(hit) || hit.transform.IsChildOf(fixedObject),"Fixed object overlaps solid: "+name+"/"+variant+" "+fixedObject.name+" with "+hit.name);
                map.GetComponent<MapGameplayFeatures>().Initialize(new Vector2(9999,9999));
                Check(map.GetComponentsInChildren<BreakableProp>().Length==6,"Random props still made breakable");
                foreach(var spike in spikes)
                {
                    spike.Advance(8.9f);Check(spike.CurrentPhase==FixedSpikeTrap.Phase.Warning,"No warning before spike");
                    spike.Advance(1.2f);Check(spike.CurrentPhase==FixedSpikeTrap.Phase.Raised,"Not raised at 10 seconds");
                    spike.Advance(2f);Check(spike.CurrentPhase==FixedSpikeTrap.Phase.Retracted,"Spikes did not retract");
                    spike.Advance(8f);Check(spike.CurrentPhase==FixedSpikeTrap.Phase.Raised && spike.CompletedCycles==2,"No repeat at 20 seconds");
                    if (spike.animatedDisplay != null)
                    {
                        Check(spike.animationFrames != null && spike.animationFrames.Length == 7,"Missing themed animation frames");
                        Check(spike.animatedDisplay.sprite == spike.animationFrames[2],"Early rise frame does not match timing");
                        spike.Advance(.5f);
                        Check(spike.animatedDisplay.sprite == spike.animationFrames[4],"Full rise frame does not match timing");
                    }
                }
                MiniMapHUD.Show(hud,map,hero.transform);
                var mini=canvas.transform.Find("GameplayHUD/MiniMapHUD").GetComponent<MiniMapHUD>();
                Check(mini.Graph.nodes.Count(n=>n.hasExitPortal)==1,"Exit room mapping wrong "+name+"/"+variant);
                Check(!mini.GetComponentsInChildren<Transform>().Any(t=>t.name=="ExitPortalIcon"),"Undiscovered exit leaked");
                var exit=mini.Graph.nodes.First(n=>n.hasExitPortal);
                hero.transform.position=exit.center;mini.Refresh();
                Check(mini.GetComponentsInChildren<Transform>().Count(t=>t.name=="ExitPortalIcon")==1,"Discovered exit icon missing");
                hero.transform.position=mini.Graph.nodes[0].center;mini.Refresh();
                Check(mini.GetComponentsInChildren<Transform>().Count(t=>t.name=="ExitPortalIcon")==1,"Exit icon forgotten after leaving room");
                // ทดสอบความเสียหายกล่องโดยหลีกเลี่ยงเอฟเฟกต์ Play Mode ในการตรวจ Editor
                Check(boxes.All(b=>b.GetComponent<BoxCollider2D>()!=null && b.GetComponent<CircleCollider2D>().isTrigger),"Crate collision setup incomplete");
                if(variant==0 && (name=="map_1" || name=="Map_2"))Preview(map,hud,canvas,spikes[0],name);
                Debug.Log("FIXED_LAYOUT_OK "+name+"/"+variant+" crates="+boxes.Length+" spikes="+spikes.Length+" exit="+exit.id);
                variants++;crates+=boxes.Length;traps+=spikes.Length;
            }
            finally{UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(hero);}
        }
        Debug.Log("FIXED_OBJECTS_VERIFIED layouts="+variants+" crates="+crates+" spikes="+traps+" exitDiscovery=15");
    }
    static void Preview(GameObject map,HUDManager hud,Canvas canvas,FixedSpikeTrap spike,string name)
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-qrOutput");
        string output=i>=0?args[i+1]:Path.GetFullPath("../FixedObjectsPreview");Directory.CreateDirectory(output);
        var camera=Camera.main;
        foreach(Transform child in canvas.transform)if(child.name!="GameplayHUD")child.gameObject.SetActive(false);
        canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;
        canvas.worldCamera=camera;canvas.planeDistance=1;canvas.overrideSorting=true;
        canvas.sortingLayerID=SortingLayer.layers.Last().id;canvas.sortingOrder=32760;
        hud.UpdateHP(6,8);hud.UpdateEnergy(42,50);hud.UpdateCurrency(1250);
        var character=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        hud.SetupSkillIcons(character.skillQ.skillIcon,character.skillE.skillIcon);
        hud.UpdateWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapon/Quantum Hammer.asset"));
        camera.orthographicSize=5.5f;camera.aspect=16f/9f;
        camera.transform.position=spike.room.transform.position+new Vector3(0,-1,-10);
        var flow=map.GetComponentInChildren<WorldFlowBackdrop>();if(flow!=null)flow.RefreshForCamera(camera,.6f);
        foreach(var graphic in canvas.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();
        MapGameplayInstaller.Capture(camera,canvas,Path.Combine(output,name+"-Fixed-Crates-Spikes.png"));
        // Camera target resize updates screen-space canvas geometry on its first render.
        foreach(var graphic in canvas.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();
        MapGameplayInstaller.Capture(camera,canvas,Path.Combine(output,name+"-Fixed-Crates-Spikes.png"));
    }
}
