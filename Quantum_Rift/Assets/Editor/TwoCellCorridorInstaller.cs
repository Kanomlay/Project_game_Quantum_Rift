using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

// เครื่องมือ Editor ปรับ prefab แมพใหม่และ Tutorial โดยไม่เปลี่ยนห้อง จุดเกิด หรือบทเรียน
public static class TwoCellCorridorInstaller
{
    static readonly string[] Keys={"1_4","1_5","2_3","2_4","2_5","Tutorial"};
    static readonly Vector3Int[] Sides={Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right};
    static string PathFor(string key)=>key=="Tutorial"?"Assets/Prefab/Tutorial/TrainingMap.prefab":"Assets/Prefab/ExpandedStages/Map_"+key+".prefab";
    static HashSet<Vector3Int> TutorialWalk()
    {
        var cells=new HashSet<Vector3Int>();
        for(int bay=0;bay<4;bay++)for(int x=-4;x<=4;x++)for(int y=-4;y<=4;y++)cells.Add(new Vector3Int(bay*12+x,y,0));
        for(int x=4;x<=32;x++)for(int y=0;y<2;y++)cells.Add(new Vector3Int(x,y,0));return cells;
    }
    [MenuItem("Tools/Quantum Rift/Set New Map Corridors to Two Cells")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=EditorSceneManager.GetSceneManagerSetup();int removed=0,traps=0;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            for(int m=0;m<Keys.Length;m++)
            {
                var root=PrefabUtility.LoadPrefabContents(PathFor(Keys[m]));
                try
                {
                    var layouts=root.GetComponent<MapLayoutRandomizer>();var variants=layouts!=null?layouts.layouts:new[]{root};var states=variants.Select(g=>g.activeSelf).ToArray();
                    for(int v=0;v<variants.Length;v++)
                    {
                        for(int i=0;i<variants.Length;i++)variants[i].SetActive(i==v);
                        var floor=variants[v].GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                        var walls=variants[v].GetComponentsInChildren<Tilemap>().First(t=>t.name=="Bulkheads_Unified64");
                        var wanted=m==5?TutorialWalk():ExpandedStageLayoutInstaller.TwoCellWalk(m,v);
                        foreach(var p in wanted)if(!floor.HasTile(p))throw new Exception("Unexpected room geometry: "+Keys[m]+"/"+v+" "+p);
                        // ลบเฉพาะแถวที่สามนอกห้อง ไม่สร้างห้อง/จุดเกิด/ร้าน/พร็อพใหม่
                        foreach(var p in floor.cellBounds.allPositionsWithin)if(floor.HasTile(p)&&!wanted.Contains(p)){floor.SetTile(p,null);removed++;}
                        TileBase[] wallTiles=m==5?new[]{walls.GetTilesBlock(walls.cellBounds).First(t=>t!=null)}:
                            new TileBase[]{AssetDatabase.LoadAssetAtPath<Tile>("Assets/Data/Map/ExpandedStages/"+(m<2?"Spaceship":"Forest")+"Wall_0.asset"),AssetDatabase.LoadAssetAtPath<Tile>("Assets/Data/Map/ExpandedStages/"+(m<2?"Spaceship":"Forest")+"Wall_1.asset")};
                        // ขอบกำแพงต้องอิงพื้นสองช่องใหม่ เพื่อไม่เหลือช่องว่างจากแนวกำแพงเดิม
                        walls.ClearAllTiles();foreach(var p in wanted)foreach(var d in Sides)if(!wanted.Contains(p+d))walls.SetTile(p+d,wallTiles.Length==1?wallTiles[0]:wallTiles[Mathf.Abs((p.x+d.x)*11+(p.y+d.y)*7)%19==0?1:0]);
                        foreach(var gate in variants[v].GetComponentsInChildren<AnimatedRoomGate>())
                        {
                            var center=gate.transform.position;var box=gate.GetComponent<BoxCollider2D>();
                            // เลื่อนกลางประตูครึ่งช่องเฉพาะบานที่ยังกว้างสามช่อง รันซ้ำจึงไม่เลื่อนเพิ่ม
                            if(box.size.x*Mathf.Abs(gate.transform.lossyScale.x)>3f)center+=(Mathf.Abs(gate.transform.right.y)>.5f?Vector3.up:Vector3.right)*.625f;
                            GateFitInstaller.Fit(gate,center,2.5f,m>=2&&m<5);
                        }
                        // จุดกับดักที่อยู่ในแถวที่ตัดออกเอาออกด้วย อีกช่องของทางเดินยังใช้หลบได้
                        foreach(var trap in variants[v].GetComponentsInChildren<FixedSpikeTrap>().ToArray())if(!floor.HasTile(floor.WorldToCell(trap.transform.position))){Object.DestroyImmediate(trap.gameObject);traps++;}
                        floor.CompressBounds();walls.CompressBounds();
                    }
                    for(int i=0;i<variants.Length;i++)variants[i].SetActive(states[i]);
                    PrefabUtility.SaveAsPrefabAsset(root,PathFor(Keys[m]));
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                GateFitInstaller.Preview(PathFor(Keys[m]),Keys[m]);
            }
            AssetDatabase.SaveAssets();Verify();GateFitInstaller.Verify();
            Debug.Log("TWO_CELL_CORRIDORS_INSTALLED removedFloorCells="+removed+" removedOutsideTraps="+traps);
        }
        finally
        {
            if(previous.Length>0&&previous.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    public static void VerifyAll()
    {
        Verify();GateFitInstaller.Verify();
    }
    public static void Verify()
    {
        int count=0,links=0;
        for(int m=0;m<Keys.Length;m++)
        {
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(Keys[m])));
            try
            {
                var random=root.GetComponent<MapLayoutRandomizer>();var variants=random!=null?random.layouts:new[]{root};
                for(int v=0;v<variants.Length;v++)
                {
                    for(int i=0;i<variants.Length;i++)variants[i].SetActive(i==v);
                    var floor=variants[v].GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");var wanted=m==5?TutorialWalk():ExpandedStageLayoutInstaller.TwoCellWalk(m,v);
                    var actual=new HashSet<Vector3Int>();foreach(var p in floor.cellBounds.allPositionsWithin)if(floor.HasTile(p))actual.Add(p);
                    if(!actual.SetEquals(wanted))throw new Exception("Two-cell mask mismatch "+Keys[m]+"/"+v);
                    var probes=m==5?new[]{(new Vector3Int(6,0,0),false),(new Vector3Int(18,0,0),false),(new Vector3Int(30,0,0),false)}:ExpandedStageLayoutInstaller.CorridorProbes(m,v);
                    foreach(var probe in probes)
                    {
                        int width=0;for(int s=-3;s<=3;s++)if(floor.HasTile(probe.Item1+(probe.Item2?Vector3Int.right:Vector3Int.up)*s))width++;
                        if(width!=2)throw new Exception("Corridor width="+width+" "+Keys[m]+"/"+v+" "+probe.Item1);links++;
                    }
                    // เดินตรวจพื้นสี่ทิศด้วย BFS เพื่อยืนยันว่าการลดความกว้างไม่ตัดห้องออกจากกัน
                    var seen=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();var start=actual.First();seen.Add(start);queue.Enqueue(start);
                    while(queue.Count>0){var p=queue.Dequeue();foreach(var d in Sides)if(actual.Contains(p+d)&&seen.Add(p+d))queue.Enqueue(p+d);}
                    if(seen.Count!=actual.Count)throw new Exception("Disconnected rooms after narrowing");
                    foreach(var trap in variants[v].GetComponentsInChildren<FixedSpikeTrap>())if(!floor.HasTile(floor.WorldToCell(trap.transform.position)))throw new Exception("Trap on removed corridor cell");
                    count++;
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        Debug.Log("TWO_CELL_CORRIDORS_VERIFIED layouts="+count+" connections="+links+" width=2 connected=true");
    }
}
