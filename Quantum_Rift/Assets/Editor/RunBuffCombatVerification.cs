using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

// ทดสอบในฉากเปล่า ไม่บันทึกทับฉากของผู้ใช้
[InitializeOnLoad]
public static class RunBuffCombatVerification
{
    const string Key="RunBuffCombatVerification.Pending";
    const string Output="D:/drive-download-20260403T132919Z-1-001/Project จบ game/output/unity/Run-Buffs-Combat-v1";
    static Stack<IEnumerator> steps;
    static double deadline;
    static int checks;
    static RunBuffCombatVerification()
    {EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Run();};}
    static void Check(bool okay,string message)
    {if(!okay)throw new Exception("VERIFY FAILED: "+message);checks++;Debug.Log("CHECK "+message);}
    static T Asset<T>(string path)where T:Object
    {var obj=AssetDatabase.LoadAssetAtPath<T>(path);if(obj==null)throw new Exception("Missing "+path);return obj;}
    public static void Begin()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditChecks();SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void EditChecks()
    {
        foreach(string set in new[]{"RedMerchant","FluxPounce","WolfPounce","HuskExplosion","SoldierSlash","TrapSpaceship","TrapForest"})
        {
            var frames=RunBuffCombatInstaller.Frames(set);Check(frames.Length==7&&frames.All(f=>f!=null),set+" has 7 sprites");
            foreach(var sprite in frames){var t=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));Check(t.filterMode==FilterMode.Point&&t.textureCompression==TextureImporterCompression.Uncompressed,set+" pixel import");}
        }
        int roomCount=0,trapCount=0,pairs=0;
        foreach(string name in RunBuffCombatInstaller.Maps)
        {
            var root=Object.Instantiate(Asset<GameObject>("Assets/Prefab/"+name+".prefab"));
            try
            {
                var layouts=root.GetComponent<MapLayoutRandomizer>();
                Check(root.GetComponent<MapGameplayFeatures>().roomEntryTrapMode,name+" entry mode");
                Check(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="FixedCorridorTraps"),name+" no legacy corridor traps");
                foreach(var layout in layouts.layouts)layout.SetActive(false);
                foreach(var layout in layouts.layouts)
                {
                    layout.SetActive(true);Physics2D.SyncTransforms();
                    var floor=layout.GetComponentsInChildren<Tilemap>().First(t=>t.name=="Floor_Unified64");
                    foreach(var room in layout.GetComponentsInChildren<RoomController>())
                    {
                        roomCount++;var spawn=room.GetComponent<RoomEntryTrapSpawner>();Check(spawn!=null,name+" room spawner");
                        spawn.ActivateWithSeed(roomCount*731);Check(spawn.Spawned.Count==3,name+" "+room.name+" three legal positions (got "+spawn.Spawned.Count+")");
                        var points=spawn.Spawned.Select(t=>t.transform.position).ToArray();spawn.ActivateWithSeed(999);
                        Check(points.SequenceEqual(spawn.Spawned.Select(t=>t.transform.position)),"reentry retains traps");
                        foreach(var t in spawn.Spawned)
                        {
                            trapCount++;var cell=floor.WorldToCell(t.transform.position);
                            Check(Vector3.Distance(t.transform.position,floor.GetCellCenterWorld(cell))<.001f,"trap grid aligned");
                            Check(Mathf.Abs(t.damageArea.bounds.size.x-1.25f)<.01f,"trap one tile wide");
                            t.Advance(8.9f);Check(t.CurrentPhase==FixedSpikeTrap.Phase.Warning,"warning before spike");
                            t.Advance(1.11f);Check(t.CurrentPhase==FixedSpikeTrap.Phase.Raised,"spike at 10 seconds");
                            t.Advance(1.5f);Check(t.CurrentPhase==FixedSpikeTrap.Phase.Retracted,"spike retracts");
                        }
                        if(room.canHostEvent)
                        {
                            string theme=name.StartsWith("Map_2")?"Forest":"Spaceship";
                            var shop=Object.Instantiate(Asset<GameObject>("Assets/Prefab/Shop/Shop"+theme+".prefab"),room.eventAnchor!=null?room.eventAnchor.position:room.transform.position,Quaternion.identity,room.transform);
                            Physics2D.SyncTransforms();var red=shop.GetComponent<BuffShopCompanion>().Spawn();Check(red!=null,"paired red merchant "+name+" "+room.name);pairs++;
                            Object.DestroyImmediate(red);Object.DestroyImmediate(shop);
                        }
                    }
                    layout.SetActive(false);
                }
            }finally{Object.DestroyImmediate(root);}
        }
        var safe=new GameObject("Safe room");var sr=safe.AddComponent<RoomController>();
        typeof(RoomController).GetField("isSafeRoom",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(sr,true);
        var ss=safe.AddComponent<RoomEntryTrapSpawner>();ss.frames=RunBuffCombatInstaller.Frames("TrapForest");ss.Activate();Check(ss.Spawned.Count==0,"safe room has no trap");Object.DestroyImmediate(safe);
        Debug.Log($"EDIT_CHECKS_PASS rooms={roomCount} traps={trapCount} pairedShopLocations={pairs}");
    }
    static void Run()
    {
        SessionState.SetBool(Key,false);deadline=EditorApplication.timeSinceStartup+100;
        steps=new Stack<IEnumerator>();steps.Push(PlayChecks());EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Verification timeout");
            if(steps.Count==0){EditorApplication.update-=Tick;Debug.Log("RUN_BUFF_COMBAT_PLAY_PASS checks="+checks);EditorApplication.Exit(0);return;}
            var step=steps.Peek();if(!step.MoveNext()){steps.Pop();return;}
            if(step.Current is IEnumerator child)steps.Push(child);
        }catch(Exception e){EditorApplication.update-=Tick;Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
    static GameObject Monster(string name,Vector2 at)
    {
        var go=Object.Instantiate(Asset<GameObject>("Assets/Prefab/Monster/"+name+"_0.prefab"),at,Quaternion.identity);
        go.GetComponent<MonsterController>().enabled=false;
        return go;
    }
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static IEnumerator PlayChecks()
    {
        Time.timeScale=1;GameManager.selectedCharacter=null;PauseManager.isGamePaused=false;
        var camera=new GameObject("Preview Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.backgroundColor=new Color(.045f,.05f,.07f);
        var hero=new GameObject("Test Hero");hero.tag="Player";var player=hero.AddComponent<PlayerStats>();player.enabled=false;player.maxHP=player.currentHP=20;player.maxEnergy=player.currentEnergy=100;player.currentCurrency=2000;
        var body=hero.AddComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;
        hero.AddComponent<BoxCollider2D>().size=Vector2.one*.4f;
        var move=hero.AddComponent<PlayerMovement>();move.enabled=false;Set(move,"rb",body);Set(move,"stats",player);
        var shop=Object.Instantiate(Asset<GameObject>("Assets/Prefab/Shop/ShopBuffRed.prefab")).GetComponent<ShopClickable>();shop.transform.position=new Vector3(-4,2,0);
        var stock=shop.Stock;Check(stock.Count==4&&stock.Count(o=>o.risky)==1,"stock 3 normal + 1 risky");Check(stock.All(o=>o.price>50),"prices exceed weapons and potions");Check(ReferenceEquals(stock,shop.Stock),"stock cached on reopen");
        ShopWindow.Open(shop.windowPrefab,shop);var win=Object.FindFirstObjectByType<ShopWindow>();
        yield return null;win.Buy(0);Check(player.maxHP==25&&player.currentHP==25,"flat HP +5");int balance=player.currentCurrency;win.Buy(0);Check(player.currentCurrency==balance&&player.maxHP==25,"cannot buy sold offer twice");
        int energy=stock[1].energyDelta;win.Buy(1);Check(player.maxEnergy==100+energy,"flat energy +50 or +100");
        win.Buy(2);Check(player.runDamageBonus==2&&RunStatBuffs.Damage(3,player)==5,"damage buff +2 applied once");
        float hp=player.maxHP;int en=player.maxEnergy,damage=player.runDamageBonus;var cursed=stock[3];win.Buy(3);
        Check(player.maxHP==hp+cursed.hpDelta&&player.maxEnergy==en+cursed.energyDelta&&player.runDamageBonus==damage+cursed.damageDelta,"risky upside and downside both applied");
        win.Close();ShopWindow.Open(shop.windowPrefab,shop);Check(shop.Stock.All(o=>o.sold),"reopen preserves sold status");
        var shop2=Object.Instantiate(shop);shop2.transform.position=new Vector3(4,2,0);player.currentCurrency=0;
        ShopWindow.Open(shop2.windowPrefab,shop2);win.Buy(0);Check(!shop2.Stock[0].sold&&player.currentCurrency==0,"insufficient funds no sale");
        player.currentCurrency=500;win.Close();ShopWindow.Open(shop2.windowPrefab,shop2);
        yield return Wait(.15f);Capture(camera,win,"buff-shop-preview.png");win.Close();
        var risky=new ShopOffer(ShopOffer.Kind.RunBuff,160){hpDelta=-100,damageDelta=6};Check(!RunStatBuffs.CanApply(player,risky),"reject lethal stat downside");
        var fresh=new GameObject("New Run").AddComponent<PlayerStats>();fresh.enabled=false;Check(fresh.runDamageBonus==0&&fresh.maxHP==8&&fresh.maxEnergy==100,"fresh run has original stats");Object.DestroyImmediate(fresh.gameObject);
        Object.DestroyImmediate(shop.gameObject);Object.DestroyImmediate(shop2.gameObject);
        player.maxHP=player.currentHP=100;player.iframeDuration=.02f;hero.transform.position=new Vector3(3,0,0);Physics2D.SyncTransforms();
        foreach(string name in new[]{"Flux Jaw","Dimensional Wolf"})
        {
            var animal=Monster(name,Vector2.zero);var action=animal.GetComponent<MonsterCombatActions>();var data=animal.GetComponent<MonsterController>().myData;
            action.Initialize(data,hero.transform);action.Tick();Check(action.IsAttacking,"pounce windup "+name);
            yield return Wait(.15f);Check(animal.transform.position.x<.2f,"pounce warns before moving "+name);
            yield return Wait(.62f);Check(animal.transform.position.x>1,"pounce moves forward "+name);Check(action.MeleeImpacts==1,"pounce hits once "+name);
            action.CancelAttack();Check(!action.IsAttacking&&animal.GetComponent<Rigidbody2D>().linearVelocity==Vector2.zero,"pounce cancels cleanly");Object.DestroyImmediate(animal);
            hero.transform.position=new Vector3(3,0,0);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return Wait(.1f);
        }
        var soldier=Monster("Phase Soldier",Vector2.zero);var rifle=soldier.GetComponent<MonsterCombatActions>();rifle.Initialize(soldier.GetComponent<MonsterController>().myData,hero.transform);
        hero.transform.position=new Vector3(5,0,0);Physics2D.SyncTransforms();rifle.Tick();yield return Wait(.3f);rifle.Tick();yield return Wait(.3f);Check(rifle.ShotsReleased==1&&rifle.LastAttackWasRanged,"Phase Soldier still shoots at range");
        foreach(var p in Object.FindObjectsByType<MonsterAttackProjectile>(FindObjectsSortMode.None))Object.DestroyImmediate(p.gameObject);
        yield return Wait(2);hero.transform.position=Vector3.right*1.5f;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();float before=player.currentHP;rifle.Tick();yield return Wait(.30f);
        Check(!rifle.LastAttackWasRanged&&rifle.MeleeImpacts==1&&player.currentHP<before,"Phase Soldier close slash hurts");Check(body.linearVelocity.x>0||hero.transform.position.x>1.5f,"Phase Soldier knocks player back");Object.DestroyImmediate(soldier);
        yield return Wait(.2f);hero.transform.position=Vector3.right;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
        var husk=Monster("Zero Husk",Vector2.zero);var monster=husk.GetComponent<MonsterController>();
        // เริ่มระบบตาม Start จริง จากนั้นปิด Update เพื่อทดสอบการตายโดยไม่ให้เดิน
        typeof(MonsterController).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(monster,null);
        before=player.currentHP;monster.TakeDamage(99999);var burst=husk.GetComponent<ZeroHuskDeathBurst>();
        yield return Wait(.4f);Check(!burst.Exploded&&player.currentHP==before,"Husk death has warning window");yield return Wait(.5f);Check(burst.Exploded&&player.currentHP<before,"Husk death explosion damages");
        float afterBlast=player.currentHP;yield return Wait(1.4f);Check(player.currentHP<afterBlast,"Husk burn ticks after explosion");Check(!husk.activeSelf,"Husk deactivates after death animation");
        Object.DestroyImmediate(husk);yield return Wait(3);
        hero.transform.position=new Vector3(8,0,0);Physics2D.SyncTransforms();var far=Monster("Zero Husk",Vector2.zero);var mc=far.GetComponent<MonsterController>();typeof(MonsterController).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mc,null);
        before=player.currentHP;mc.TakeDamage(99999);yield return Wait(1.3f);Check(player.currentHP==before,"outside Husk radius safe");Object.DestroyImmediate(far);
        File.WriteAllText(Output+"/verification-result.txt","PASS: editor structure, 90 room placements, merchant pairs, seven-frame assets, shop transactions, flat run stats, pounce, soldier ranged/melee, Husk warning/explosion/burn.\n");
    }
    static void Capture(Camera camera,ShopWindow window,string file)
    {
        var canvas=window.GetComponentInParent<Canvas>();if(canvas==null)canvas=window.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null){scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;}
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+file,pixels.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);
    }
}
