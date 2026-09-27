using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FixedObjectsPlayCheck
{
    const string Key="QuantumRift.FixedObjectsPlayCheck";
    static FixedSpikeTrap spike;
    static PlayerStats hero;
    static float readyAt;
    static int step;
    static FixedObjectsPlayCheck()
    {
        EditorApplication.playModeStateChanged+=OnPlayMode;
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Key,true);
        EditorApplication.EnterPlaymode();
    }
    static void OnPlayMode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            step=0;readyAt=Time.time+.1f;EditorApplication.update+=Tick;
        }
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying || Time.time<readyAt)return;
        try
        {
            if(step==0)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/map_1.prefab");
                var sourceSpike=prefab.GetComponentInChildren<FixedSpikeTrap>(true);
                spike=UnityEngine.Object.Instantiate(sourceSpike);spike.enabled=false;spike.room=null;
                spike.transform.position=Vector3.zero;
                var player=new GameObject("PlayerTest",typeof(SpriteRenderer));player.tag="Player";
                hero=player.AddComponent<PlayerStats>();hero.enabled=false;hero.maxHP=hero.currentHP=8;
                spike.Advance(8.9f);
                Check(!spike.TryHit(hero) && hero.currentHP==8,"Warning damaged player");
                spike.Advance(1.2f);
                Check(spike.TryHit(hero) && hero.currentHP==7,"Raised spikes did not damage player");
                Check(!spike.TryHit(hero) && hero.currentHP==7,"Multiple hits in one spike cycle");
                spike.Advance(2f);
                Check(!spike.TryHit(hero),"Retracted spikes damaged player");
                var sourceBox=prefab.GetComponentInChildren<BreakableProp>(true);
                var box=UnityEngine.Object.Instantiate(sourceBox);box.transform.position=new Vector2(5,0);
                var features=prefab.GetComponent<MapGameplayFeatures>();
                box.Configure(box.GetComponent<SpriteRenderer>(),new Collider2D[]{box.GetComponent<BoxCollider2D>()},
                    features.healthPotionSprite,features.energyPotionSprite,null);
                box.TakeDamage(6);Check(!box.IsBroken,"Crate broke before health exhausted");
                box.TakeDamage(6);Check(box.IsBroken,"Crate did not break");
                Check(box.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Broken crate left a visible part");
                Check(box.GetComponents<Collider2D>().All(c=>!c.enabled),"Broken crate still blocks player");
                step=1;readyAt=Time.time+1.3f;
            }
            else
            {
                spike.Advance(8f);
                Check(spike.TryHit(hero) && hero.currentHP==6,"Next spike cycle cannot damage player");
                Debug.Log("FIXED_PLAY_VERIFIED warningSafe=true singleHitPerCycle=true secondCycleDamage=true crateBreak=true collidersRemoved=true");
                Finish(0);
            }
        }
        catch(Exception ex){Debug.LogException(ex);Finish(1);}
    }
    static void Finish(int code)
    {
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
        EditorApplication.Exit(code);
    }
}
