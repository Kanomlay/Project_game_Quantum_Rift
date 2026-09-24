using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;

/// <summary>พื้นหลังเต็ม viewport และ HUD ยึดมุมจอสำหรับทุกธีม</summary>
public static class GameplayPresentationBuilder
{
    const string GameScene = "Assets/Scenes/GameScene.unity";
    const string Art = "Assets/image/Map/FlowBackgrounds-v1";
    const string UIArt = "Assets/image/UI_Image/QuantumRift-UI-Objects-v1/runtime";
    static readonly string[] Themes = { "Map1-Spaceship", "Map2-LivingForest", "Map3-OrganicHive" };
    static readonly string[][] Maps = {
        new[] { "map_1", "map_1_2", "map_1_3", "map_1_bossroom" },
        new[] { "Map_2", "Map_2_2", "Map_2_boss" }, new[] { "Map_boss" }
    };
    static readonly Color Surface = new Color32(12,17,31,245);
    static readonly Color Rim = new Color32(105,81,151,255);
    static readonly Color Cyan = new Color32(98,212,228,255);
    static string Output {
        get {
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-qrOutput");
            return i>=0 ? args[i+1] : Path.GetFullPath("../PresentationReport");
        }
    }
    static T Need<T>(string path) where T : UnityEngine.Object
    {
        var value=AssetDatabase.LoadAssetAtPath<T>(path);
        if(value==null) throw new Exception("Missing asset: "+path);
        return value;
    }

    [MenuItem("Tools/Quantum Rift/Presentation/Full Background + Corner HUD")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first");
        Directory.CreateDirectory(Output);
        var material=Need<Material>(Art+"/FlowBackground-Unlit.mat");
        int count=0;
        for(int theme=0;theme<3;theme++)
        {
            var frames=Enumerable.Range(1,7).Select(i=>Need<Sprite>(Art+"/"+Themes[theme]+"/Background-"+i.ToString("00")+".png")).ToArray();
            foreach(var name in Maps[theme])
            {
                string path="Assets/Prefab/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var child=root.transform.Find("WorldFlowCoverage");
                    if(child==null) { child=new GameObject("WorldFlowCoverage").transform; child.SetParent(root.transform,false); }
                    child.localPosition=new Vector3(0,0,5);
                    child.localScale=new Vector3(.64f,.64f,1);
                    var renderer=Ensure<SpriteRenderer>(child.gameObject);
                    renderer.sprite=frames[0]; renderer.sharedMaterial=material;
                    renderer.sortingLayerName="Default"; renderer.sortingOrder=-200;
                    renderer.color=new Color(.6f,.6f,.6f,1);
                    renderer.drawMode=SpriteDrawMode.Tiled; renderer.tileMode=SpriteTileMode.Continuous;
                    renderer.size=new Vector2(400,400);
                    var flow=Ensure<WorldFlowBackdrop>(child.gameObject);
                    flow.frames=frames; flow.framesPerSecond=5;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                    count++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        var scene=EditorSceneManager.OpenScene(GameScene);
        var hud=UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        var canvas=GameObject.Find("UI").GetComponent<Canvas>();
        ConfigureHUD(hud,canvas);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        VerifyAndCapture();
        File.WriteAllText(Path.Combine(Output,"result.txt"),"Installed in 8 map prefabs and GameScene. 7-frame viewport coverage, HUD anchors, weapon cost/containment and multi-aspect renders verified.\n");
        Debug.Log("PRESENTATION_INSTALL_OK prefabs="+count);
    }

    public static void ConfigureHUD(HUDManager hud,Canvas canvas)
    {
        if(hud==null || canvas==null) throw new Exception("HUD/Canvas missing");
        var scaler=Ensure<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight=.5f;
        var overlay=Rect(canvas.transform,"GameplayHUD"); Stretch(overlay,0);
        overlay.SetAsFirstSibling();
        var vitals=Rect(overlay,"VitalsHUD");
        Place(vitals,Vector2.up,Vector2.up,new Vector2(32,-28),new Vector2(430,220));
        Bar(vitals,"HealthBar",hud.hpFillImage,hud.hpText,UIArt+"/HP-Frame.png",0);
        Bar(vitals,"EnergyBar",hud.energyFillImage,hud.energyText,UIArt+"/Energy-Frame.png",-108);
        var legacy=canvas.transform.Find("MenuHP-ER"); if(legacy!=null) legacy.gameObject.SetActive(false);

        var combat=Rect(overlay,"CombatHUD");
        Place(combat,Vector2.right,Vector2.right,new Vector2(-32,28),new Vector2(300,282));
        var weapon=Rect(combat,"WeaponSlot");
        Place(weapon,new Vector2(.5f,0),Vector2.one*.5f,new Vector2(0,210),new Vector2(128,128));
        Frame(weapon);
        var clip=Rect(weapon,"IconClip"); Stretch(clip,15); Ensure<RectMask2D>(clip.gameObject);
        hud.activeWeaponIcon.transform.SetParent(clip,false);
        Stretch(hud.activeWeaponIcon.rectTransform,3);
        hud.activeWeaponIcon.type=Image.Type.Simple; hud.activeWeaponIcon.preserveAspect=true;
        hud.activeWeaponIcon.raycastTarget=false; hud.activeWeaponIcon.color=Color.white;
        var badge=Rect(weapon,"EnergyCostBadge");
        Place(badge,Vector2.one,Vector2.one,new Vector2(-6,-6),new Vector2(42,34));
        Ensure<Image>(badge.gameObject).color=Surface;
        var number=Text(badge,"Cost",hud.hpText.font,26);
        Stretch(number.rectTransform,0); number.text="0"; number.color=new Color(.72f,.96f,1f);
        hud.weaponEnergyCostText=number;
        badge.SetAsLastSibling();
        var use=Text(weapon,"InputHint",hud.hpText.font,17);
        Place(use.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,5),new Vector2(100,22));
        use.text="LMB"; use.color=new Color(.68f,.75f,.84f);

        Skill(combat,"SkillQ",hud.skillQIcon,hud.skillQCooldownText,-74,"Q",hud.hpText.font);
        Skill(combat,"SkillE",hud.skillEIcon,hud.skillECooldownText,74,"E",hud.hpText.font);
        // เก็บ container เดิมไว้ให้ย้อนกลับได้ แต่ภาพและ reference ย้ายมาอยู่ในโครงใหม่แล้ว
        foreach(string name in new[]{"WeaponSlot","SkillSlot"})
        { var old=canvas.transform.Find(name); if(old!=null) old.gameObject.SetActive(false); }
        ConfigureCurrency(hud,canvas);
        EditorUtility.SetDirty(hud);
    }
    static void ConfigureCurrency(HUDManager hud,Canvas canvas)
    {
        if(hud.currencyText==null) throw new Exception("Currency text reference missing");
        var overlay=Rect(canvas.transform,"GameplayHUD");
        var panel=Rect(overlay,"CurrencyHUD");
        Place(panel,Vector2.one,Vector2.one,new Vector2(-32,-28),new Vector2(320,88));
        Frame(panel);
        // ย้ายเหรียญเดิมออกจากตัวเลข เพื่อให้ขนาดและระยะห่างไม่ขึ้นกับจำนวนเงิน
        var iconTransform=hud.currencyText.transform.Find("CurrencyIcon");
        if(iconTransform!=null) iconTransform.SetParent(panel,false);
        var icon=Ensure<Image>(Rect(panel,"CurrencyIcon").gameObject);
        Place(icon.rectTransform,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(14,0),new Vector2(60,60));
        icon.sprite=Need<Sprite>(UIArt+"/Coin-Single.png");
        icon.color=Color.white; icon.type=Image.Type.Simple; icon.preserveAspect=true; icon.raycastTarget=false;
        var label=hud.currencyText;
        label.transform.SetParent(panel,false); label.gameObject.layer=5;
        Place(label.rectTransform,Vector2.right*.5f+Vector2.up*.5f,Vector2.one*.5f,new Vector2(35,0),new Vector2(218,64));
        StyleText(label,38); label.fontStyle=FontStyles.Bold;
        label.alignment=TextAlignmentOptions.MidlineRight;
        label.color=new Color32(255,234,163,255);
        label.enableAutoSizing=true; label.fontSizeMin=24; label.fontSizeMax=38;
        var shadow=Ensure<Shadow>(label.gameObject); shadow.effectColor=Color.black; shadow.effectDistance=new Vector2(2,-2);
        label.transform.SetAsLastSibling();
        EditorUtility.SetDirty(hud);
    }

    public static void UpdateCurrencyAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first");
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.OpenScene(GameScene);
        ConfigureCurrency(UnityEngine.Object.FindFirstObjectByType<HUDManager>(),GameObject.Find("UI").GetComponent<Canvas>());
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        VerifyAndCapture();
    }
    static void Bar(Transform parent,string name,Image fill,TMP_Text label,string art,float y)
    {
        var bar=Rect(parent,name);
        Place(bar,Vector2.up,Vector2.up,new Vector2(0,y),new Vector2(430,110));
        var frame=Ensure<Image>(bar.gameObject); frame.sprite=Need<Sprite>(art);
        frame.color=Color.white; frame.raycastTarget=false;
        fill.transform.SetParent(bar,false); fill.transform.SetAsFirstSibling();
        fill.rectTransform.localScale=Vector3.one;
        fill.rectTransform.anchorMin=new Vector2(.25664893f,.29166666f);
        fill.rectTransform.anchorMax=new Vector2(.92021275f,.6197917f);
        fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillOrigin=0;
        fill.raycastTarget=false;
        label.transform.SetParent(bar,false);
        label.rectTransform.anchorMin=fill.rectTransform.anchorMin;
        label.rectTransform.anchorMax=fill.rectTransform.anchorMax;
        label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        StyleText(label,30); label.fontStyle=FontStyles.Bold;
        label.color=Color.white;
        var shadow=Ensure<Shadow>(label.gameObject);
        shadow.effectColor=new Color(0,0,0,.95f); shadow.effectDistance=new Vector2(2,-2);
        label.transform.SetAsLastSibling();
    }
    static void Skill(Transform parent,string name,Image icon,TMP_Text cooldown,float x,string key,TMP_FontAsset font)
    {
        var slot=Rect(parent,name);
        Place(slot,new Vector2(.5f,0),Vector2.one*.5f,new Vector2(x,65),new Vector2(112,112));
        Frame(slot);
        icon.transform.SetParent(slot,false); Stretch(icon.rectTransform,9);
        icon.type=Image.Type.Simple; icon.preserveAspect=true; icon.raycastTarget=false; icon.color=Color.white;
        cooldown.transform.SetParent(slot,false); Stretch(cooldown.rectTransform,0);
        StyleText(cooldown,38); cooldown.fontStyle=FontStyles.Bold;
        var shadow=Ensure<Shadow>(cooldown.gameObject); shadow.effectColor=Color.black; shadow.effectDistance=new Vector2(2,-2);
        var hint=Text(slot,"Key",font,22);
        Place(hint.rectTransform,Vector2.zero,Vector2.zero,new Vector2(5,3),new Vector2(30,28)); hint.text=key;
        cooldown.transform.SetAsLastSibling();
    }
    static void Frame(RectTransform slot)
    {
        var border=Ensure<Image>(slot.gameObject); border.color=Rim; border.raycastTarget=false;
        var inner=Rect(slot,"Inset"); Stretch(inner,4);
        var surface=Ensure<Image>(inner.gameObject); surface.color=Surface; surface.raycastTarget=false;
        inner.SetAsFirstSibling();
        foreach(var side in new[]{"TopAccent","BottomAccent"})
        {
            var line=Rect(slot,side);
            var top=side=="TopAccent";
            Place(line,new Vector2(.5f,top?1:0),new Vector2(.5f,top?1:0),new Vector2(0,top?-2:2),new Vector2(36,3));
            var image=Ensure<Image>(line.gameObject); image.color=Cyan; image.raycastTarget=false;
        }
    }
    static RectTransform Rect(Transform parent,string name)
    {
        var child=parent.Find(name);
        if(child==null) { child=new GameObject(name,typeof(RectTransform)).transform; child.SetParent(parent,false); }
        child.gameObject.layer=5; child.gameObject.SetActive(true); child.localScale=Vector3.one;
        return (RectTransform)child;
    }
    static T Ensure<T>(GameObject go) where T:Component
    { var result=go.GetComponent<T>(); return result!=null?result:go.AddComponent<T>(); }
    static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,float size)
    {
        var text=Ensure<TextMeshProUGUI>(Rect(parent,name).gameObject);
        text.font=font; StyleText(text,size); return text;
    }
    static void StyleText(TMP_Text text,float size)
    {
        text.fontSize=size; text.enableAutoSizing=false; text.alignment=TextAlignmentOptions.Center;
        text.color=Color.white; text.raycastTarget=false; text.textWrappingMode=TextWrappingModes.NoWrap;
        text.margin=Vector4.zero;
        text.overflowMode=TextOverflowModes.Overflow;
    }
    static void Place(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    { r.localScale=Vector3.one; r.anchorMin=r.anchorMax=anchor; r.pivot=pivot; r.anchoredPosition=position; r.sizeDelta=size; }
    static void Stretch(RectTransform r,float inset)
    { r.localScale=Vector3.one; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.pivot=Vector2.one*.5f; r.offsetMin=Vector2.one*inset; r.offsetMax=-Vector2.one*inset; }

    public static void UpdateHUDAndVerify()
    {
        var scene=EditorSceneManager.OpenScene(GameScene);
        ConfigureHUD(UnityEngine.Object.FindFirstObjectByType<HUDManager>(),GameObject.Find("UI").GetComponent<Canvas>());
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        VerifyAndCapture();
    }

    public static void VerifyAndCapture()
    {
        var scene=EditorSceneManager.OpenScene(GameScene);
        var canvas=GameObject.Find("UI").GetComponent<Canvas>();
        var hud=UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        var camera=Camera.main;
        if(camera==null) throw new Exception("Missing main camera");
        var weapons=AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/Data/Weapon"})
            .Select(g=>Need<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        foreach(var weapon in weapons)
        {
            hud.UpdateWeapon(weapon);
            if(hud.weaponEnergyCostText.text!=Mathf.Max(0,weapon.energyCost).ToString()) throw new Exception("Wrong weapon energy cost");
            if(hud.activeWeaponIcon.sprite!=weapon.weaponIcon || !hud.activeWeaponIcon.preserveAspect ||
               hud.activeWeaponIcon.GetComponentInParent<RectMask2D>()==null) throw new Exception("Weapon containment missing");
        }
        var character=Need<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        hud.SetupSkillIcons(character.skillQ.skillIcon,character.skillE.skillIcon);
        hud.UpdateHP(6,8); hud.UpdateEnergy(42,50); hud.UpdateWeapon(Need<WeaponData>("Assets/Data/Weapon/Rusty Pistol.asset"));
        foreach(var amount in new[]{0,1250,999999,int.MaxValue})
        {
            hud.UpdateCurrency(amount); Canvas.ForceUpdateCanvases(); hud.currencyText.ForceMeshUpdate();
            if(hud.currencyText.text!=amount.ToString() || hud.currencyText.isTextOverflowing)
                throw new Exception("Currency display overflow: "+amount);
        }
        hud.UpdateCurrency(1250);
        if(hud.transitionCanvas!=null) hud.transitionCanvas.gameObject.SetActive(false);
        // ภาพตรวจใน Editor ไม่มี Start() ของหน้าพัก/สรุปมาปิดหน้าต่างเหมือนตอนเล่นจริง
        foreach(Transform child in canvas.transform)
            if(child.name!="GameplayHUD" && !child.name.Contains("Currency") && !child.name.Contains("Coin")) child.gameObject.SetActive(false);
        var scaler=canvas.GetComponent<CanvasScaler>(); scaler.enabled=false;
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
        canvas.sortingLayerID=SortingLayer.layers[SortingLayer.layers.Length-1].id;
        canvas.sortingOrder=32760; canvas.overrideSorting=true;
        int checks=0;
        for(int theme=0;theme<3;theme++)
        {
            var prefab=Need<GameObject>("Assets/Prefab/"+Maps[theme][0]+".prefab");
            var map=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var random=map.GetComponentInChildren<MapLayoutRandomizer>(true);
            if(random!=null) for(int i=0;i<random.layouts.Length;i++) random.layouts[i].SetActive(i==0);
            var floor=map.GetComponentsInChildren<Tilemap>().OrderByDescending(t=>t.cellBounds.size.x*t.cellBounds.size.y).First();
            var flow=map.GetComponentInChildren<WorldFlowBackdrop>();
            var rooms=map.GetComponentsInChildren<RoomController>();
            var center=rooms.Length>0 ? rooms.OrderBy(r=>r.transform.position.sqrMagnitude).First().transform.position : floor.localBounds.center;
            var positions=new[]{center,center+new Vector3(0,-18,0),center+new Vector3(-22,0,0),new Vector3(-200,-200,0)};
            foreach(var aspect in new[]{16f/9f,4f/3f,21f/9f})
            foreach(var pos in positions)
            {
                camera.aspect=aspect; camera.orthographicSize=7;
                camera.transform.position=new Vector3(pos.x,pos.y,-10);
                flow.RefreshForCamera(camera,.6f);
                var b=flow.GetComponent<SpriteRenderer>().bounds;
                if(!b.Contains(new Vector3(pos.x-7*aspect,pos.y-7,b.center.z)) ||
                   !b.Contains(new Vector3(pos.x+7*aspect,pos.y+7,b.center.z))) throw new Exception("Background does not cover viewport");
                checks++;
            }
            // ทางเดินและขอบห้องคือจุดที่เคยเห็นสีฟ้า ใช้กล้องใกล้ระหว่างเดินจริง
            camera.transform.position=new Vector3(center.x,center.y-18,-10);
            camera.orthographicSize=7; camera.aspect=16f/9f;
            flow.RefreshForCamera(camera,.6f);
            Render(camera,canvas,1280,720,Themes[theme]+"-Corridor-HUD.png");
            camera.aspect=4f/3f; flow.RefreshForCamera(camera,.6f);
            Render(camera,canvas,1024,768,Themes[theme]+"-4x3-HUD.png");
            UnityEngine.Object.DestroyImmediate(map);
        }
        Debug.Log("PRESENTATION_VERIFIED coverage="+checks+" weaponDefinitions="+weapons.Length);
    }
    static void Render(Camera camera,Canvas canvas,int width,int height,string name)
    {
        var rt=new RenderTexture(width,height,24); camera.targetTexture=rt; camera.aspect=(float)width/height;
        canvas.scaleFactor=Mathf.Sqrt((width/1920f)*(height/1080f));
        Canvas.ForceUpdateCanvases();
        foreach(var t in canvas.GetComponentsInChildren<TMP_Text>()) t.ForceMeshUpdate();
        foreach(string path in new[]{"GameplayHUD/VitalsHUD","GameplayHUD/CombatHUD","GameplayHUD/CurrencyHUD"})
        {
            var corners=new Vector3[4]; ((RectTransform)canvas.transform.Find(path)).GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var viewport=camera.WorldToViewportPoint(corner);
                if(viewport.x<0 || viewport.x>1 || viewport.y<0 || viewport.y>1) throw new Exception("HUD outside viewport: "+path);
            }
        }
        camera.Render();
        var old=RenderTexture.active; RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
        File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());
        RenderTexture.active=old; camera.targetTexture=null; rt.Release();
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
    }
}
