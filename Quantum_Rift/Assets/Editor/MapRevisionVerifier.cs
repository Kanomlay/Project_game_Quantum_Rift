using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MapRevisionVerifier
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static GameObject OpenMap(string name,int variant)
    {
        var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/"+name+".prefab"));
        var layout=map.GetComponent<MapLayoutRandomizer>();
        for(int i=0;i<layout.layouts.Length;i++)layout.layouts[i].SetActive(i==variant);
        Physics2D.SyncTransforms();return map;
    }
    static void Expire(GameObject map)
    {
        foreach(var trap in map.GetComponentsInChildren<StageTrap>(false))
        {
            trap.Tick(trap.SpawnedAt+trap.warningSeconds*.5f);
            Check(trap.CurrentPhase==StageTrap.Phase.Warning,"Trap activated before warning finished");
            trap.Tick(trap.SpawnedAt+trap.warningSeconds+trap.activeSeconds+trap.fadeSeconds+.1f);
            Check(trap.CurrentPhase==StageTrap.Phase.Expired,"Trap never expires");
        }
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        int layouts=0,frontSpawns=0,repeats=0;
        foreach(string name in new[]{"map_1","map_1_2","map_1_3","Map_2","Map_2_2"})
        for(int variant=0;variant<3;variant++)
        {
            var map=OpenMap(name,variant);
            try
            {
                var graph=new MapRoomGraph(map);
                Check(graph.nodes.Count==7,name+" omitted a room");
                var reached=new HashSet<int>{0};bool changed=true;
                while(changed)
                {
                    changed=false;
                    foreach(var edge in graph.edges)
                    {
                        if(reached.Contains(edge.x))changed|=reached.Add(edge.y);
                        if(reached.Contains(edge.y))changed|=reached.Add(edge.x);
                    }
                }
                Check(reached.Count==7,name+" disconnected minimap "+variant);
                Check(graph.nodes.All(n=>!n.visited),"Unvisited room already revealed");
                graph.Observe(graph.nodes[0].center);graph.Observe(graph.nodes[1].center);
                Check(graph.nodes.Count(n=>n.visited)==2,"Visits not tracked by actual room geometry");
                graph.Observe(new Vector2(9999,9999));
                Check(graph.nodes.Count(n=>n.visited)==2,"Outside position reveals nearest room");
                var feature=map.GetComponent<MapGameplayFeatures>();
                feature.Initialize(graph.nodes[0].center);
                Check(feature.ActiveTrapCount==2,name+" initial trap placement failed "+variant);
                Expire(map);
                Check(feature.ActiveTrapCount==0,"Expired traps occupy spawn cap");
                var origin=graph.nodes[1].center;
                for(int wave=0;wave<4;wave++)
                {
                    Check(feature.TrySpawnTrap(origin,Vector2.up,false),name+" recurring spawn failed");
                    repeats++;Expire(map);
                }
                bool ahead=false;
                foreach(var node in graph.nodes.Where(n=>n.room!=null))
                {
                    foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                    {
                        if(!feature.TrySpawnTrap(node.center,direction,true))continue;
                        var trap=map.GetComponentsInChildren<StageTrap>(false).Single();
                        Check(Vector2.Dot((Vector2)trap.transform.position-node.center,direction)>3.3f,"Trap not ahead of player");
                        Check(Vector2.Distance(trap.transform.position,node.center)>=3.3f,"Trap spawned on player");
                        ahead=true;frontSpawns++;Expire(map);break;
                    }
                    if(ahead)break;
                }
                Check(ahead,name+" no valid forward spawns "+variant);
                for(int i=0;i<8;i++)feature.TrySpawnTrap(origin,Vector2.up,false);
                Check(feature.ActiveTrapCount<=feature.maxActiveTraps,"Spawn cap exceeded");
                Debug.Log("REVISION_LAYOUT_OK "+name+"/"+variant+" nodes="+graph.nodes.Count+" links="+graph.edges.Count);
                layouts++;
            }
            finally{UnityEngine.Object.DestroyImmediate(map);}
        }
        Debug.Log("REVISION_VERIFIED layouts="+layouts+" repeatSpawns="+repeats+" forwardSpawns="+frontSpawns);
        Preview();
    }
    static void Preview()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-qrOutput");
        string output=i>=0?args[i+1]:Path.GetFullPath("../MinimapPreview");Directory.CreateDirectory(output);
        var canvas=GameObject.Find("UI").GetComponent<Canvas>();
        var hud=UnityEngine.Object.FindFirstObjectByType<HUDManager>();var camera=Camera.main;
        foreach(Transform child in canvas.transform)if(child.name!="GameplayHUD")child.gameObject.SetActive(false);
        canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;
        canvas.worldCamera=camera;canvas.planeDistance=1;canvas.overrideSorting=true;
        canvas.sortingLayerID=SortingLayer.layers.Last().id;canvas.sortingOrder=32760;
        hud.UpdateHP(6,8);hud.UpdateEnergy(42,50);hud.UpdateCurrency(1250);
        var character=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        hud.SetupSkillIcons(character.skillQ.skillIcon,character.skillE.skillIcon);
        hud.UpdateWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapon/Quantum Hammer.asset"));
        foreach(string name in new[]{"map_1","Map_2"})
        {
            var map=OpenMap(name,name=="map_1"?0:2);
            var hero=new GameObject("PreviewHero");
            MiniMapHUD.Show(hud,map,hero.transform);
            var mini=canvas.transform.Find("GameplayHUD/MiniMapHUD").GetComponent<MiniMapHUD>();
            hero.transform.position=mini.Graph.nodes[0].center;mini.Refresh();
            hero.transform.position=mini.Graph.nodes[1].center;mini.Refresh();
            hero.transform.position=mini.Graph.nodes[2].center;mini.Refresh();
            int questions=mini.GetComponentsInChildren<TMPro.TMP_Text>(false).Count(t=>t.text=="?");
            Check(questions==4,"Visited question marks not hidden");
            var feature=map.GetComponent<MapGameplayFeatures>();feature.Initialize(mini.Graph.nodes[0].center);
            Expire(map);
            foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.right,Vector2.left})
                if(feature.TrySpawnTrap(hero.transform.position,direction,true))break;
            camera.orthographicSize=7;camera.aspect=16f/9f;
            camera.transform.position=hero.transform.position+Vector3.back*10;
            map.GetComponentInChildren<WorldFlowBackdrop>().RefreshForCamera(camera,.6f);
            MapGameplayInstaller.Capture(camera,canvas,Path.Combine(output,name+"-Connected-Minimap.png"));
            UnityEngine.Object.DestroyImmediate(map);UnityEngine.Object.DestroyImmediate(hero);
        }
    }
}
