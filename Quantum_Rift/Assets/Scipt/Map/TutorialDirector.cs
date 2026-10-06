using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// สนามฝึกทีละขั้น ตรวจผลจริง: เดิน ตีหุ่น ใช้สกิล ทุบกล่อง เก็บของ เคลียร์ห้อง เปิดหีบ และไปประตู
public sealed class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance {get;private set;}
    public static bool IsCompleting=>Instance!=null&&Instance.CurrentLesson==Lesson.Complete;
    public enum Lesson { Move,Attack,Skills,Crate,Pickups,Combat,Chest,Exit,Complete }
    public Transform dummyPoint,cratePoint,pickupPoint,exitPoint;
    public MonsterData dummyData;
    public GameObject cratePrefab,portalVisual;
    public RoomController combatRoom;
    public LootTable loot;
    public Lesson CurrentLesson {get;private set;}
    public MonsterController Dummy {get;private set;}
    public BreakableProp Crate {get;private set;}
    public bool UsedQ {get;private set;}
    public bool UsedE {get;private set;}
    public bool Ready {get;private set;}
    public bool CollectedHealth=>hpItem==null;
    public bool CollectedEnergy=>energyItem==null;
    PlayerStats hero;
    Vector2 movementOrigin;
    float distanceWalked;
    Vector2 lastPosition;
    int currencyBefore;
    LootPickup coinItem,hpItem,energyItem;
    TreasureChest reward;
    TMP_Text heading,body,counter,targetLabel;
    GameObject hud,completion;
    Image progress;
    Sprite progressSprite;
    float lessonStarted;
    string previousLanguage;

    IEnumerator Start()
    {
        Instance=this;
        while(MapManager.instance==null||MapManager.instance.IsLoading)yield return null;
        hero=FindFirstObjectByType<PlayerStats>();if(hero==null)yield break;
        hero.SkillUsed+=OnSkill;
        // ฝึกได้โดยไม่ตาย ไม่ใช้สูตร DevConsole หรือแก้ค่าสูงสุดจริง
        hero.GrantInvincibility(86400);
        BuildHud();Ready=true;BeginLesson(Lesson.Move);
    }
    void OnDestroy()
    {
        if(hero!=null)hero.SkillUsed-=OnSkill;
        if(hud!=null)Destroy(hud);
        if(progressSprite!=null)Destroy(progressSprite);
        if(Instance==this){if(IsCompleting){Time.timeScale=1;PauseManager.isGamePaused=false;}Instance=null;}
    }
    void OnSkill(string key)
    {
        if(CurrentLesson!=Lesson.Skills)return;
        if(key=="Q")UsedQ=true;if(key=="E")UsedE=true;
        RefreshText();
    }
    void Update()
    {
        if(!Ready||hero==null||hero.isDead||PauseManager.isGamePaused||GameHelpWindow.BlocksGameplayInput||ShopWindow.IsOpen)return;
        if(previousLanguage!=(LanguageSettings.IsThai?"TH":"EN"))RefreshText();
        var at=(Vector2)hero.transform.position;
        switch(CurrentLesson)
        {
            case Lesson.Move:
                distanceWalked+=Vector2.Distance(at,lastPosition);lastPosition=at;
                if(distanceWalked>=4)BeginLesson(Lesson.Attack);break;
            case Lesson.Attack:
                if(Dummy!=null&&Dummy.HealthFraction<.999f)BeginLesson(Lesson.Skills);break;
            case Lesson.Skills:
                if(UsedQ&&UsedE)BeginLesson(Lesson.Crate);break;
            case Lesson.Crate:
                if(Crate==null||Crate.IsBroken)BeginLesson(Lesson.Pickups);break;
            case Lesson.Pickups:
                if(coinItem==null&&hpItem==null&&energyItem==null&&hero.currentCurrency>currencyBefore)BeginLesson(Lesson.Combat);break;
            case Lesson.Combat:
                if(combatRoom.IsCleared)
                {
                    reward=combatRoom.GetComponentInChildren<TreasureChest>();
                    BeginLesson(Lesson.Chest);
                }
                break;
            case Lesson.Chest:
                if(reward!=null&&reward.IsOpen)BeginLesson(Lesson.Exit);break;
            case Lesson.Exit:
                if(Input.GetKeyDown(KeyCode.F))TryUseExit();break;
        }
        var target=CurrentTarget;
        if(targetLabel!=null)
        {
            targetLabel.gameObject.SetActive(target!=null&&CurrentLesson!=Lesson.Complete);
            if(target!=null)targetLabel.transform.position=target.position+Vector3.up*2.3f;
        }
    }
    public Transform CurrentTarget=>CurrentLesson switch
    {
        Lesson.Attack=>dummyPoint,Lesson.Skills=>dummyPoint,Lesson.Crate=>cratePoint,
        Lesson.Pickups=>pickupPoint,Lesson.Combat=>combatRoom.transform,
        Lesson.Chest=>reward!=null?reward.transform:combatRoom.transform,Lesson.Exit=>exitPoint,_=>null
    };
    public bool TryUseExit()
    {
        if(!Ready||hero==null||CurrentLesson!=Lesson.Exit||PauseManager.isGamePaused||GameHelpWindow.BlocksGameplayInput||WeaponPickup.AnyInReach||Vector2.Distance(hero.transform.position,exitPoint.position)>2.5f)return false;
        BeginLesson(Lesson.Complete);return true;
    }
    void BeginLesson(Lesson lesson)
    {
        CurrentLesson=lesson;lessonStarted=Time.time;
        if(lesson==Lesson.Move){movementOrigin=hero.transform.position;lastPosition=movementOrigin;distanceWalked=0;}
        if(lesson==Lesson.Attack)
        {
            var go=Instantiate(dummyData.monsterPrefab,dummyPoint.position,Quaternion.identity,transform);
            Dummy=go.GetComponent<MonsterController>();Dummy.myData=dummyData;Dummy.Frozen=true;
        }
        if(lesson==Lesson.Skills){UsedQ=UsedE=false;hero.RestoreEnergy(hero.maxEnergy);hero.ResetSkillCooldowns();}
        if(lesson==Lesson.Crate)
        {
            if(Dummy!=null)Destroy(Dummy.gameObject);
            Crate=Instantiate(cratePrefab,cratePoint.position,Quaternion.identity,transform).GetComponentInChildren<BreakableProp>();
        }
        if(lesson==Lesson.Pickups)
        {
            currencyBefore=hero.currentCurrency;
            // ลดเพื่อให้ได้ลองเก็บจริง ขวดเต็มหลอดจะไม่ถูกกิน เป็นกติกาเดียวกับเกมจริง
            hero.SetHealthForTesting(Mathf.Max(1,hero.maxHP-3));hero.UseEnergy(hero.currentEnergy);
            var p=(Vector2)pickupPoint.position;
            coinItem=LootPickup.Create(LootPickup.Kind.Coin,5,loot.coinSprite,loot.coinSize,transform,p+Vector2.left*2);
            hpItem=LootPickup.Create(LootPickup.Kind.HpPotion,3,loot.hpPotionSprite,loot.potionSize,transform,p);
            energyItem=LootPickup.Create(LootPickup.Kind.EnergyPotion,100,loot.energyPotionSprite,loot.potionSize,transform,p+Vector2.right*2);
        }
        if(lesson==Lesson.Combat)
        {
            hero.Heal(hero.maxHP);hero.RestoreEnergy(hero.maxEnergy);hero.ResetSkillCooldowns();
            foreach(var collider in combatRoom.GetComponents<Collider2D>())collider.enabled=true;
            combatRoom.enabled=true;
        }
        if(lesson==Lesson.Exit&&portalVisual!=null)portalVisual.SetActive(true);
        if(lesson==Lesson.Complete)ShowCompletion();
        RefreshText();
    }
    void RefreshText()
    {
        if(heading==null)return;
        bool th=LanguageSettings.IsThai;previousLanguage=th?"TH":"EN";
        string[] titles=th?new[]{"เดินและสำรวจ","เล็งและโจมตี","ใช้สกิลประจำอาชีพ","ทำลายกล่องเสบียง","เก็บเงินและขวดยา","เข้าห้องและเคลียร์ศัตรู","เปิดกล่องรางวัล","ใช้ประตูมิติ","ฝึกครบแล้ว!"}:
            new[]{"MOVE AND EXPLORE","AIM AND ATTACK","USE YOUR CLASS SKILLS","BREAK A SUPPLY CRATE","COLLECT COINS AND POTIONS","CLEAR THE COMBAT ROOM","OPEN THE REWARD CHEST","USE THE RIFT EXIT","TRAINING COMPLETE"};
        string[] instructions=th?new[]{
            "กด W A S D เพื่อเดิน ลองขยับให้ครบ 4 หน่วย · กด ? หรือ F1 เพื่อเปิดคู่มือได้ทุกเวลา",
            "เดินไปหาหุ่นฝึก เล็งด้วยเมาส์ แล้วคลิกซ้ายให้โจมตีโดนหุ่น · อาวุธบางชนิดใช้พลังงาน",
            $"ลองใช้ Q และ E ให้ครบทั้งสองสกิล · Q {(UsedQ?"✓":"ยังไม่ได้ใช้")}  /  E {(UsedE?"✓":"ยังไม่ได้ใช้")} · ต้องรอคูลดาวน์ก่อนใช้ซ้ำ",
            "เดินไปกล่องที่มีป้ายเป้าหมายแล้วโจมตีจนแตก · เม็ดพลังงานจากกล่องจะลอยมาฟื้นพลังงาน",
            "เดินทับเหรียญ ขวดเลือด และขวดพลังงานให้ครบ · ขวดฟื้นได้ไม่เกินค่าสูงสุด และไม่ถูกเก็บเมื่อหลอดเต็ม",
            "เดินตามป้ายเป้าหมายเข้าห้องฝึกต่อสู้ · ประตูปิดเมื่อเริ่ม และเปิดเมื่อศัตรูตายครบทุกตัว",
            "เดินเข้าใกล้กล่องรางวัลที่เกิดหลังเคลียร์ห้อง · ได้เงิน ยา และอาวุธสุ่ม; อาวุธบนพื้นกด F เพื่อเก็บ",
            "ไปยืนใกล้ประตูมิติ แล้วกด F เพื่อจบการฝึก · ในเกมจริงห้องทางออกต้องเคลียร์ก่อนประตูปรากฏ",
            "พร้อมออกเดินทางแล้ว! เริ่มเกมจริงจะสร้างรอบใหม่ เงิน อาวุธและบัพจากสนามฝึกไม่ติดไปด้วย"
        }:new[]{
            "Move with W A S D and travel 4 units. Open the guide with ? or F1 at any time.",
            "Approach the training dummy, aim with the mouse, and land a left-click attack. Some weapons cost energy.",
            $"Successfully use both Q and E. Q: {(UsedQ?"DONE":"pending")} / E: {(UsedE?"DONE":"pending")}. Skills have cooldowns.",
            "Attack the marked supply crate until it breaks. Its blue energy motes restore your energy.",
            "Walk over the coin, health potion and energy potion. Restoration is capped; full bars do not consume potions.",
            "Follow the marker into the combat room. Doors close during combat and open after every enemy is defeated.",
            "Approach the reward chest. It contains coins, potions and a random weapon. Press F to pick up a weapon.",
            "Stand near the rift exit and press F to finish training. Real stage exits appear after their room is cleared.",
            "You are ready! Starting the real game creates a fresh run; training coins, weapons and buffs do not carry over."
        };
        heading.text=titles[(int)CurrentLesson];body.text=instructions[(int)CurrentLesson];
        counter.text=$"{Mathf.Min(8,(int)CurrentLesson+1):00} / 08";progress.fillAmount=Mathf.Min(1,((int)CurrentLesson+1)/8f);
        if(targetLabel!=null)targetLabel.text=th?"▼ เป้าหมายการฝึก":"▼ TRAINING TARGET";
    }
    void BuildHud()
    {
        hud=new GameObject("TutorialHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));hud.layer=5;
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10000;
        var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var panel=Box(hud.transform,"Lesson",new Vector2(0,110),new Vector2(1140,180),new Color32(15,19,34,245));
        panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=new Vector2(.44f,0);QuantumUiSkin.Frame(panel.transform,12);
        heading=Label(panel.transform,"Heading",new Vector2(0,48),new Vector2(780,48),32);
        body=Label(panel.transform,"Instruction",new Vector2(0,-12),new Vector2(1040,78),26);body.enableAutoSizing=true;body.fontSizeMin=22;body.fontSizeMax=26;
        counter=Label(panel.transform,"Count",new Vector2(-475,51),new Vector2(130,40),23);counter.color=QuantumUiSkin.Cyan;
        progress=Box(panel.transform,"Progress",new Vector2(0,-78),new Vector2(1030,4),QuantumUiSkin.Cyan);
        // ไม่ต้องใช้ builtin sprite ที่อาจไม่มีในเครื่องอื่น ใช้ white texture ทำ filled Image
        progressSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),Vector2.one*.5f);progress.sprite=progressSprite;
        progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;
        Button(panel.transform,"ExitTraining",new Vector2(450,51),new Vector2(158,43),LanguageSettings.IsThai?"กลับเมนู":"MENU",ReturnToMenu);
        var marker=new GameObject("TutorialTarget");marker.transform.SetParent(transform,false);targetLabel=marker.AddComponent<TextMeshPro>();targetLabel.fontSize=2.5f;targetLabel.alignment=TextAlignmentOptions.Center;targetLabel.color=QuantumUiSkin.Cyan;targetLabel.outlineWidth=.2f;targetLabel.rectTransform.sizeDelta=new Vector2(12,2);targetLabel.GetComponent<MeshRenderer>().sortingLayerName="Effect";
    }
    void ShowCompletion()
    {
        Time.timeScale=0;PauseManager.isGamePaused=true;
        completion=Box(hud.transform,"Complete",Vector2.zero,new Vector2(920,400),QuantumUiSkin.Ink).gameObject;QuantumUiSkin.Frame(completion.transform,16);
        Label(completion.transform,"Title",new Vector2(0,110),new Vector2(820,65),40).text=LanguageSettings.IsThai?"ฝึกครบแล้ว พร้อมออกเดินทาง!":"READY FOR YOUR JOURNEY!";
        Label(completion.transform,"Note",new Vector2(0,20),new Vector2(820,90),27).text=LanguageSettings.IsThai?"เริ่มเกมจริงด้วยรอบใหม่\nของและเงินจากการฝึกจะไม่ติดไปด้วย":"Start a fresh run.\nTraining items and coins do not carry over.";
        Button(completion.transform,"StartGame",new Vector2(-215,-105),new Vector2(360,65),LanguageSettings.IsThai?"เริ่มเกมจริง":"START GAME",StartRealGame);
        Button(completion.transform,"Menu",new Vector2(215,-105),new Vector2(360,65),LanguageSettings.IsThai?"กลับเมนู":"MAIN MENU",ReturnToMenu);
    }
    public void ReturnToMenu(){MapManager.startOverride=null;Time.timeScale=1;PauseManager.isGamePaused=false;SceneManager.LoadScene("MainMenu");}
    public void StartRealGame(){MapManager.startOverride=null;Time.timeScale=1;PauseManager.isGamePaused=false;SceneManager.LoadScene("GameScene");}
    static RectTransform Rect(Transform parent,string name,Vector2 at,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=at;r.sizeDelta=size;return r;
    }
    static Image Box(Transform p,string n,Vector2 at,Vector2 size,Color color){var i=Rect(p,n,at,size).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static TMP_Text Label(Transform p,string n,Vector2 at,Vector2 size,float font){var t=Rect(p,n,at,size).gameObject.AddComponent<TextMeshProUGUI>();t.fontSize=font;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static void Button(Transform p,string n,Vector2 at,Vector2 size,string text,UnityEngine.Events.UnityAction action){var i=Box(p,n,at,size,QuantumUiSkin.Surface);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);Label(i.transform,"Label",Vector2.zero,size-new Vector2(10,4),23).text=text;QuantumUiSkin.Button(b,8);}
}
