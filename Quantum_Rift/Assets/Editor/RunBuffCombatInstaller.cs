using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RunBuffCombatInstaller
{
    public const string Art="Assets/image/Gameplay/Run-Buffs-Combat-v1";
    const string Anim="Assets/Animation/Run-Buffs-Combat-v1";
    public static readonly string[] Maps={"map_1","map_1_2","map_1_3","Map_2","Map_2_2"};
    static readonly string[] Sets={"RedMerchant","FluxPounce","WolfPounce","HuskExplosion","SoldierSlash","TrapSpaceship","TrapForest"};
    static readonly float[] Ppu={100,182.19177f,183.11688f,146.61653f,153.38983f,51.2f,51.2f};
    static readonly float[] Pivots={.12f,.288259844f,.29785156f,.35058594f,.3354161f,.5f,.5f};
    public static Sprite[] Frames(string set)=>Enumerable.Range(1,7).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{set}/Frame-{i:00}.png")).ToArray();
    static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    [MenuItem("Tools/Quantum Rift/Install Run Buff Shops And Combat Revision")]
    public static void Install()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Folder(Anim);
        for(int s=0;s<Sets.Length;s++)foreach(string path in Directory.GetFiles(Art+"/"+Sets[s],"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=Ppu[s];
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.maxTextureSize=2048;
            var setting=new TextureImporterSettings();importer.ReadTextureSettings(setting);setting.spriteAlignment=(int)SpriteAlignment.Custom;setting.spritePivot=new Vector2(.5f,Pivots[s]);importer.SetTextureSettings(setting);importer.SaveAndReimport();
        }
        BuildMerchant();
        foreach(string theme in new[]{"Spaceship","Forest"})
        {
            string path="Assets/Prefab/Shop/Shop"+theme+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{var link=root.GetComponent<BuffShopCompanion>();if(link==null)link=root.AddComponent<BuffShopCompanion>();link.buffShopPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopBuffRed.prefab");PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            BuildSquareTrap(theme);
        }
        BuildMonster("Flux Jaw","FluxPounce");BuildMonster("Dimensional Wolf","WolfPounce");BuildMonster("Phase Soldier","SoldierSlash");BuildMonster("Zero Husk","HuskExplosion");
        int rooms=0;
        foreach(string name in Maps)
        {
            string path="Assets/Prefab/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<MapGameplayFeatures>().roomEntryTrapMode=true;
                foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="FixedCorridorTraps").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach(var room in root.GetComponentsInChildren<RoomController>(true))
                {var spawn=room.GetComponent<RoomEntryTrapSpawner>();if(spawn==null)spawn=room.gameObject.AddComponent<RoomEntryTrapSpawner>();spawn.frames=Frames(name.StartsWith("Map_2")?"TrapForest":"TrapSpaceship");spawn.count=3;rooms++;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("RUN_BUFF_COMBAT_INSTALLED rooms="+rooms+" frames=49 shops=2 companion=sharedRed buffs=4");
    }
    static AnimationClip Clip(string name,bool loop,float fps=10)
    {
        string path=Anim+"/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.frameRate=fps;
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),Frames(name).Select((s,i)=>new ObjectReferenceKeyframe{time=i/fps,value=s}).ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.stopTime=7/fps;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
    }
    static void BuildMerchant()
    {
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/ShopSpaceship.prefab");
        var go=UnityEngine.Object.Instantiate(original);go.name="ShopBuffRed";
        try
        {
            var link=go.GetComponent<BuffShopCompanion>();if(link!=null)UnityEngine.Object.DestroyImmediate(link);
            go.transform.localScale=Vector3.one;go.transform.position=Vector3.zero;
            var shop=go.GetComponent<ShopClickable>();shop.isBuffShop=true;
            var col=go.GetComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(2.3f,1.9f);col.offset=new Vector2(0,.9f);
            var display=go.GetComponent<SpriteRenderer>();display.sprite=Frames("RedMerchant")[0];
            string path=Anim+"/RedMerchant.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            if(controller.layers.Length==0)controller.AddLayer("Base Layer");
            var machine=controller.layers[0].stateMachine;var state=machine.states.Length>0?machine.states[0].state:machine.AddState("Idle");state.motion=Clip("RedMerchant",true,7);machine.defaultState=state;
            EditorUtility.SetDirty(controller);
            go.GetComponent<Animator>().runtimeAnimatorController=controller;
            PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefab/Shop/ShopBuffRed.prefab");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void BuildSquareTrap(string theme)
    {
        string path="Assets/Prefab/MapObjects/"+theme+"/FloorSpikes.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            root.transform.localScale=Vector3.one;
            var display=root.GetComponent<SpriteRenderer>();if(display==null)display=root.AddComponent<SpriteRenderer>();display.sprite=Frames("Trap"+theme)[0];display.sortingLayerName="object";display.sortingOrder=-2;
            var trap=root.GetComponent<FixedSpikeTrap>();trap.animationFrames=Frames("Trap"+theme);trap.animatedDisplay=display;trap.spikes=null;trap.warningDisplay=null;trap.room=null;
            trap.damageArea=root.GetComponent<BoxCollider2D>();trap.damageArea.isTrigger=true;trap.damageArea.offset=Vector2.zero;trap.damageArea.size=Vector2.one*1.25f;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildMonster(string name,string set)
    {
        string path="Assets/Prefab/Monster/"+name+"_0.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            if(set=="HuskExplosion")
            {var burst=root.GetComponent<ZeroHuskDeathBurst>();if(burst==null)burst=root.AddComponent<ZeroHuskDeathBurst>();burst.frames=Frames(set);}
            else
            {
                var action=root.GetComponent<MonsterCombatActions>();if(action==null)action=root.AddComponent<MonsterCombatActions>();
                var controller=(AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
                var machine=controller.layers[0].stateMachine;
                if(set=="SoldierSlash")
                {
                    action.closeRangeSlash=true;action.meleeDistance=2.5f;action.keepAwayDistance=2.2f;
                    if(!controller.parameters.Any(p=>p.name=="Melee"))controller.AddParameter("Melee",AnimatorControllerParameterType.Trigger);
                    var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="PhaseMelee")??machine.AddState("PhaseMelee");state.motion=Clip(set,false,12);state.writeDefaultValues=false;
                    if(!machine.anyStateTransitions.Any(t=>t.destinationState==state))
                    {var tr=machine.AddAnyStateTransition(state);tr.hasExitTime=false;tr.duration=0;tr.canTransitionToSelf=false;tr.AddCondition(AnimatorConditionMode.If,0,"Melee");tr.AddCondition(AnimatorConditionMode.IfNot,0,"isDead");}
                    if(state.transitions.Length==0){var tr=state.AddTransition(machine.states.Select(s=>s.state).First(s=>s.name.EndsWith("_idel")));tr.hasExitTime=true;tr.exitTime=1;tr.duration=0;}
                }
                else
                {
                    action.style=MonsterCombatActions.Style.Pounce;action.lungeTriggerDistance=4.5f;action.lungeSpeed=10;action.lungeStopDistance=.8f;action.meleeDistance=1.35f;
                    var state=machine.states.Select(s=>s.state).First(s=>s.name.EndsWith("_attack"));state.motion=Clip(set,false);
                    root.GetComponent<Rigidbody2D>().collisionDetectionMode=CollisionDetectionMode2D.Continuous;
                }
                EditorUtility.SetDirty(controller);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
