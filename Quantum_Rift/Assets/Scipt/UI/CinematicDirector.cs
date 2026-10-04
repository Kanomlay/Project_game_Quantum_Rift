using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ภาพประกอบกับตัวหนังสือจริงของเกม เคลื่อนไหวด้วยเวลาจริงขณะหยุดการต่อสู้
public sealed class CinematicDirector : MonoBehaviour
{
    public static CinematicDirector Instance {get;private set;}
    static int closedFrame=-1;
    public static bool IsOpen=>Instance!=null&&Instance.open;
    public static bool BlocksGameplayInput=>IsOpen||closedFrame==Time.frameCount;
    public CinematicLibrary library;
    public bool IsStory {get;private set;}
    public string CurrentBossId {get;private set;}
    public int CurrentPage {get;private set;}
    public GameObject Overlay=>root;
    public Image Portrait=>portrait;
    public TMP_Text Title=>title;
    public TMP_Text Body=>body;
    bool open,ownsPause,oldPaused,oldCursorVisible,advance,skip;
    float oldScale,openedAt;
    CursorLockMode oldCursorLock;
    GameObject root,portraitPanel;
    CanvasGroup visibility;
    Image background,portrait,accentLine,progressBar;
    TMP_Text eyebrow,title,body,pageLabel,nextLabel,skipLabel;
    Button nextButton,skipButton;
    RectTransform content,bossReveal;
    Sprite[] frames;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){Instance=null;closedFrame=-1;}
    void Awake(){Instance=this;if(library==null)library=Resources.Load<CinematicLibrary>("CinematicLibrary");}
    void OnDisable(){Cancel();if(Instance==this)Instance=null;}
    void OnDestroy(){if(root!=null)Destroy(root);}
    public void Advance(){if(open&&Time.unscaledTime-openedAt>.2f)advance=true;}
    public void Skip(){if(open&&Time.unscaledTime-openedAt>.2f)skip=true;}
    void Update()
    {
        if(!open)return;
        if(Input.GetKeyDown(KeyCode.Escape))Skip();
        else if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))Advance();
    }
    public IEnumerator PlayOpening()
    {
        if(library==null||library.opening==null||library.opening.Length==0||!CinematicProgress.ShouldShowOpening)yield break;
        while(IsOpen)yield return null;
        Open(true);
        for(int i=0;i<library.opening.Length&&open&&!skip;i++)
        {
            SetStoryPage(i);advance=false;
            float age=0;int characters=body.textInfo.characterCount;
            while(open&&!skip)
            {
                age+=Time.unscaledDeltaTime;
                body.maxVisibleCharacters=Mathf.Min(characters,Mathf.FloorToInt(age*42));
                TickVisuals(age);
                if(advance)
                {
                    advance=false;
                    if(body.maxVisibleCharacters<characters){age=characters/42f;body.maxVisibleCharacters=characters;}
                    else break;
                }
                yield return null;
            }
        }
        if(open)CinematicProgress.MarkOpeningWatched();
        Close();
    }
    public IEnumerator PlayBoss(GameObject boss)
    {
        var profile=library!=null?library.Find(boss):null;
        if(profile==null)yield break;
        while(IsOpen)yield return null;
        Open(false);SetBoss(profile,0);
        float age=0;
        while(open&&!skip&&!advance&&age<profile.duration)
        {
            age+=Time.unscaledDeltaTime;TickVisuals(age);
            progressBar.fillAmount=Mathf.Clamp01(age/profile.duration);
            yield return null;
        }
        Close();
    }
    void Open(bool story)
    {
        Build();Instance=this;IsStory=story;CurrentBossId=null;advance=skip=false;open=true;openedAt=Time.unscaledTime;
        oldScale=Time.timeScale;oldPaused=PauseManager.isGamePaused;oldCursorLock=Cursor.lockState;oldCursorVisible=Cursor.visible;
        ownsPause=true;PauseManager.isGamePaused=true;Time.timeScale=0;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        var hero=FindFirstObjectByType<PlayerStats>();var rb=hero!=null?hero.GetComponent<Rigidbody2D>():null;
        if(rb!=null)rb.linearVelocity=Vector2.zero;
        root.SetActive(true);visibility.alpha=1;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }
    public void Cancel(){if(open||ownsPause){skip=true;Close();}}
    void Close()
    {
        if(open)closedFrame=Time.frameCount;
        open=false;
        if(root!=null)root.SetActive(false);
        if(ownsPause)
        {
            ownsPause=false;Time.timeScale=oldScale;PauseManager.isGamePaused=oldPaused;
            Cursor.lockState=oldCursorLock;Cursor.visible=oldCursorVisible;
        }
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }
    public void SetStoryPage(int index)
    {
        Build();IsStory=true;CurrentPage=index;CurrentBossId=null;
        var page=library.opening[index];frames=page.backgrounds;
        title.transform.SetParent(content,false);body.transform.SetParent(content,false);
        bossReveal.gameObject.SetActive(false);
        portraitPanel.SetActive(false);progressBar.gameObject.SetActive(false);
        title.rectTransform.anchoredPosition=new Vector2(0,70);title.rectTransform.sizeDelta=new Vector2(1270,90);
        body.rectTransform.anchoredPosition=new Vector2(0,-70);body.rectTransform.sizeDelta=new Vector2(1270,190);
        body.alignment=TextAlignmentOptions.TopLeft;body.fontSize=30;
        eyebrow.text=LanguageSettings.IsThai?"QUANTUM RIFT · บันทึกการเดินทาง":"QUANTUM RIFT · THE JOURNEY";
        title.text=LanguageSettings.IsThai?page.titleThai:page.titleEnglish;
        body.text=LanguageSettings.IsThai?page.bodyThai:page.bodyEnglish;
        body.ForceMeshUpdate();body.maxVisibleCharacters=0;
        pageLabel.text=$"{index+1:00} / {library.opening.Length:00}";
        nextLabel.text=LanguageSettings.IsThai?(index==library.opening.Length-1?"เริ่มเดินทาง  [Space]":"ถัดไป  [Space]"):(index==library.opening.Length-1?"BEGIN  [Space]":"NEXT  [Space]");
        skipLabel.text=LanguageSettings.IsThai?"ข้ามเนื้อเรื่อง  [Esc]":"SKIP STORY  [Esc]";
        accentLine.color=page.accent;RefreshBackground(0);
    }
    public void SetBoss(CinematicLibrary.BossProfile profile,float revealAge=.9f)
    {
        Build();IsStory=false;CurrentBossId=profile.id;frames=profile.backgrounds;
        bossReveal.gameObject.SetActive(true);
        title.transform.SetParent(bossReveal,false);body.transform.SetParent(bossReveal,false);
        content.anchoredPosition=Vector2.zero;MoveBossReveal(revealAge);
        portraitPanel.SetActive(true);portrait.sprite=profile.portrait;portrait.preserveAspect=true;
        title.rectTransform.anchoredPosition=new Vector2(210,30);title.rectTransform.sizeDelta=new Vector2(850,160);
        body.rectTransform.anchoredPosition=new Vector2(210,-100);body.rectTransform.sizeDelta=new Vector2(850,110);
        body.alignment=TextAlignmentOptions.MidlineLeft;body.fontSize=32;
        eyebrow.text=LanguageSettings.IsThai?"ตรวจพบศัตรูระดับบอส":"BOSS ENCOUNTER";
        title.text=profile.displayName;title.color=Color.white;
        body.text=LanguageSettings.IsThai?profile.titleThai:profile.titleEnglish;body.maxVisibleCharacters=int.MaxValue;
        pageLabel.text=LanguageSettings.IsThai?"เตรียมพร้อมก่อนเริ่มการต่อสู้":"PREPARE FOR BATTLE";
        nextLabel.text=LanguageSettings.IsThai?"เริ่มสู้  [Space]":"FIGHT  [Space]";
        skipLabel.text=LanguageSettings.IsThai?"ข้าม  [Esc]":"SKIP  [Esc]";
        accentLine.color=profile.accent;progressBar.color=profile.accent;
        progressBar.gameObject.SetActive(true);progressBar.fillAmount=0;RefreshBackground(0);
    }
    void TickVisuals(float age)
    {
        visibility.alpha=Mathf.Clamp01(age/.3f);
        if(IsStory)content.anchoredPosition=new Vector2(0,Mathf.Lerp(-12,0,Mathf.Clamp01(age/.4f)));
        else MoveBossReveal(age);
        RefreshBackground(age);
    }
    // เลื่อนภาพและชื่อจากใต้จอพร้อมกัน ใช้เวลาจริงแม้ timeScale เป็นศูนย์
    void MoveBossReveal(float age)
    {
        float t=Mathf.Clamp01(age/.9f);
        float eased=1-Mathf.Pow(1-t,3);
        float distance=Mathf.Max(760,((RectTransform)root.transform).rect.height*.5f+260);
        bossReveal.anchoredPosition=new Vector2(0,Mathf.Lerp(-distance,0,eased));
    }
    void RefreshBackground(float age)
    {
        if(frames==null||frames.Length==0){background.enabled=false;return;}
        background.enabled=true;background.sprite=frames[Mathf.FloorToInt(age*6)%frames.Length];
        var sprite=background.sprite;if(sprite==null)return;
        var canvasRect=(RectTransform)background.transform.parent;
        float w=canvasRect.rect.width,h=canvasRect.rect.height;
        float scale=Mathf.Max(w/sprite.rect.width,h/sprite.rect.height)*1.04f;
        background.rectTransform.sizeDelta=sprite.rect.size*scale;
        background.rectTransform.anchoredPosition=new Vector2(Mathf.Sin(age*.12f)*8,0);
    }
    void Build()
    {
        if(root!=null)return;
        root=new GameObject("CinematicOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
        root.layer=5;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=31000;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        visibility=root.GetComponent<CanvasGroup>();
        var blocker=Box(root.transform,"Backdrop",Vector2.zero,new Vector2(1600,900),new Color32(6,9,18,255));
        blocker.rectTransform.anchorMin=Vector2.zero;blocker.rectTransform.anchorMax=Vector2.one;blocker.rectTransform.sizeDelta=Vector2.zero;blocker.raycastTarget=true;
        background=Box(root.transform,"SceneArt",Vector2.zero,new Vector2(1600,900),Color.white);
        var tint=Box(root.transform,"ArtTint",Vector2.zero,new Vector2(1600,900),new Color32(9,12,24,75));tint.rectTransform.anchorMin=Vector2.zero;tint.rectTransform.anchorMax=Vector2.one;tint.rectTransform.sizeDelta=Vector2.zero;
        Box(root.transform,"TopLetterbox",new Vector2(0,385),new Vector2(3000,130),new Color32(5,8,15,255));
        Box(root.transform,"BottomLetterbox",new Vector2(0,-385),new Vector2(3000,130),new Color32(5,8,15,255));
        content=Rect(root.transform,"Content",Vector2.zero,new Vector2(1420,630));
        var matte=Box(content,"ReadableMatte",Vector2.zero,new Vector2(1420,580),new Color32(12,17,30,235));QuantumUiSkin.Frame(matte.transform,24);
        var revealViewport=Rect(content,"BossRevealViewport",Vector2.zero,new Vector2(1270,400));
        revealViewport.gameObject.AddComponent<RectMask2D>();
        bossReveal=Rect(revealViewport,"BossReveal",Vector2.zero,new Vector2(1420,630));
        eyebrow=Label(content,"Eyebrow",new Vector2(0,218),new Vector2(1270,60),24,new Color32(121,229,240,255));
        title=Label(content,"Title",new Vector2(0,20),new Vector2(1270,90),54,Color.white);
        body=Label(content,"Body",new Vector2(0,-80),new Vector2(1270,140),32,new Color32(228,230,242,255));
        title.enableAutoSizing=true;title.fontSizeMin=38;title.fontSizeMax=54;body.lineSpacing=10;
        accentLine=Box(content,"Accent",new Vector2(0,172),new Vector2(1270,3),Color.cyan);
        portraitPanel=Rect(bossReveal,"BossPortrait",new Vector2(-440,-15),new Vector2(360,360)).gameObject;
        var plate=Box(portraitPanel.transform,"Plate",Vector2.zero,new Vector2(360,360),new Color32(19,25,41,255));QuantumUiSkin.Frame(plate.transform,20);
        portrait=Box(portraitPanel.transform,"Portrait",Vector2.zero,new Vector2(330,330),Color.white);
        pageLabel=Label(content,"Page",new Vector2(-425,-238),new Vector2(480,48),22,new Color32(169,175,195,255));
        nextButton=MakeButton(content,"Next",new Vector2(480,-238),new Vector2(300,56),out nextLabel);nextButton.onClick.AddListener(Advance);
        skipButton=MakeButton(root.transform,"Skip",new Vector2(500,385),new Vector2(330,58),out skipLabel);skipButton.onClick.AddListener(Skip);
        progressBar=Box(content,"BossCountdown",new Vector2(0,-190),new Vector2(1270,4),Color.cyan);progressBar.sprite=Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");progressBar.type=Image.Type.Filled;progressBar.fillMethod=Image.FillMethod.Horizontal;
        root.SetActive(false);
    }
    static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;
    }
    static Image Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var image=Rect(parent,name,pos,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static TMP_Text Label(Transform parent,string name,Vector2 pos,Vector2 size,float fontSize,Color color)
    {
        var text=Rect(parent,name,pos,size).gameObject.AddComponent<TextMeshProUGUI>();text.fontSize=fontSize;text.color=color;text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;text.extraPadding=true;return text;
    }
    static Button MakeButton(Transform parent,string name,Vector2 pos,Vector2 size,out TMP_Text label)
    {
        var image=Box(parent,name,pos,size,QuantumUiSkin.Surface);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        label=Label(image.transform,"Label",Vector2.zero,size-new Vector2(16,8),22,Color.white);label.alignment=TextAlignmentOptions.Center;
        QuantumUiSkin.Button(button,10);return button;
    }
}
