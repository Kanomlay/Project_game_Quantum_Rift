using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// คอลเลกชันอ่านข้อมูลอย่างเดียว ไม่เสกมอนสเตอร์ ไม่ปลดล็อกสูตร และไม่แก้ความคืบหน้ารอบเล่น
public sealed class MonsterCollectionWindow : MonoBehaviour
{
    public MonsterCollectionLibrary library;
    public bool pauseGameplay;
    public GameObject panel;
    public Button entryButton,closeButton,languageButton;
    public Button[] rows;
    public TMP_Text[] rowNames;
    public TMP_Text title,subtitle,entryLabel,closeLabel,nameLabel,stageLabel,healthLabel,damageLabel,speedLabel,cooldownLabel,abilitiesTitle,abilities,footer,scrollHint;
    public Image portrait;
    public ScrollRect list,abilityScroll;
    public static MonsterCollectionWindow Instance {get;private set;}
    public static bool IsOpen=>Instance!=null&&Instance.panel!=null&&Instance.panel.activeSelf;
    static int closedFrame=-1;
    // คลิกหรือ Esc ที่ปิดหน้าต่างต้องไม่กลายเป็นการโจมตี/เปิด Pause ในเฟรมเดียวกัน
    public static bool BlocksGameplayInput=>IsOpen||closedFrame==Time.frameCount;
    public int SelectedIndex {get;private set;}
    bool ownsPause,previousPause,previousCursorVisible;
    float previousScale;
    CursorLockMode previousCursor;
    GameObject previousSelection;
    HUDManager hud;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Instance=null;closedFrame=-1;}
    void OnEnable(){Instance=this;LanguageSettings.Changed+=Refresh;Refresh();}
    void OnDisable(){LanguageSettings.Changed-=Refresh;if(panel!=null)panel.SetActive(false);RestorePause();if(Instance==this)Instance=null;}
    public bool CanOpen()
    {
        if(GameHelpWindow.IsOpen||CinematicDirector.BlocksGameplayInput||ShopWindow.IsOpen||BlessingManager.IsChoosing||TutorialDirector.IsCompleting)return false;
        if(SettingsMenu.instance!=null&&SettingsMenu.instance.IsOpen)return false;
        if(SummaryManager.instance!=null&&SummaryManager.instance.IsShowing)return false;
        if(!pauseGameplay)return true;
        if(hud==null)hud=FindFirstObjectByType<HUDManager>();
        return hud==null||hud.transitionCanvas==null||!hud.transitionCanvas.gameObject.activeInHierarchy||hud.transitionCanvas.alpha<=.01f;
    }
    // ปุ่มเปิดคอลเลกชัน จัดตอนเล่น (ไม่แก้ฉาก): แยกชั้นวาดของปุ่มลงไปอยู่ใต้หน้าคู่มือ (30000) หน้าต่างคอลเลกชันเองยังอยู่ชั้นเดิม
    // สูงเท่าและวางชิดขวาปุ่มคู่มือ ในเกม/สนามฝึกเหลือแค่ไอคอนกะโหลก เมนูหลักยังมีข้อความ จัดใหม่เมื่อขนาดจอเปลี่ยน
    const int EntrySortingOrder=29990;
    Vector2 arrangedFor;
    void LateUpdate()
    {
        var screen=new Vector2(Screen.width,Screen.height);
        // หน้าเมนูที่รวมกับคู่มือซ่อนปุ่มเดี่ยวไว้ ไม่จัดตำแหน่งปุ่มที่ถูกซ่อนซ้ำ
        if(entryButton==null||!entryButton.gameObject.activeInHierarchy||arrangedFor==screen)return;
        var own=GetComponent<Canvas>();var help=GameHelpWindow.Instance!=null?GameHelpWindow.Instance:FindFirstObjectByType<GameHelpWindow>();
        if(own==null||own.scaleFactor<=0)return;
        arrangedFor=screen;
        var layer=entryButton.GetComponent<Canvas>();
        if(layer==null){layer=entryButton.gameObject.AddComponent<Canvas>();entryButton.gameObject.AddComponent<GraphicRaycaster>();}
        layer.overrideSorting=true;layer.sortingOrder=EntrySortingOrder;
        var button=(RectTransform)entryButton.transform;var icon=button.Find("SkullIcon") as RectTransform;
        if(help!=null&&help.entryButton!=null)
        {
            // สองหน้านี้ใช้ฐานจอคนละขนาด (1600 กับ 1920) จึงวัดปุ่มคู่มือเป็นพิกเซลจริงแล้วแปลงเป็นหน่วยของ Canvas นี้
            var corners=new Vector3[4];((RectTransform)help.entryButton.transform).GetWorldCorners(corners);
            float scale=own.scaleFactor,height=(corners[2].y-corners[0].y)/scale;
            button.anchorMin=button.anchorMax=button.pivot=Vector2.zero;
            button.anchoredPosition=new Vector2(corners[2].x/scale+14,corners[0].y/scale);
            button.sizeDelta=new Vector2(pauseGameplay?height:button.sizeDelta.x,height);
        }
        if(entryLabel!=null)entryLabel.gameObject.SetActive(!pauseGameplay);
        if(icon!=null)
        {
            // ไอคอนกะโหลกวาดขนาดตายตัว (ราว 32) จึงขยายด้วย scale
            icon.anchoredPosition=pauseGameplay?Vector2.zero:new Vector2(-button.sizeDelta.x*.5f+34,0);
            icon.localScale=Vector3.one*(pauseGameplay?button.sizeDelta.y*.58f/32f:1.15f);
        }
    }
    void Update()
    {
        if(entryButton!=null)entryButton.interactable=IsOpen||CanOpen();
        if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))Close();
    }
    public void Open()
    {
        if(panel==null||IsOpen||!CanOpen())return;Instance=this;
        previousSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        if(pauseGameplay&&Application.isPlaying)
        {
            previousPause=PauseManager.isGamePaused;previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;ownsPause=true;
            PauseManager.isGamePaused=true;Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        panel.SetActive(true);Refresh();
        if(EventSystem.current!=null&&rows!=null&&rows.Length>SelectedIndex)EventSystem.current.SetSelectedGameObject(rows[SelectedIndex].gameObject);
    }
    public void Close()
    {
        if(panel==null||!panel.activeSelf)return;panel.SetActive(false);closedFrame=Time.frameCount;RestorePause();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection!=null&&previousSelection.activeInHierarchy?previousSelection:null);
    }
    // คืนสถานะเดิมแทนการบังคับเวลาเป็น 1: ถ้าพักเกมไว้ก่อนเปิด ต้องยังพักอยู่เมื่ออ่านจบ
    void RestorePause(){if(!ownsPause)return;ownsPause=false;Time.timeScale=previousScale;PauseManager.isGamePaused=previousPause;Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;}
    public void Select(int index){if(library==null||library.entries==null||library.entries.Length==0)return;SelectedIndex=Mathf.Clamp(index,0,library.entries.Length-1);Refresh();}
    public void ToggleLanguage()=>LanguageSettings.Toggle();
    public void Refresh()
    {
        if(library==null||library.entries==null||library.entries.Length==0||title==null)return;
        bool th=LanguageSettings.IsThai;SelectedIndex=Mathf.Clamp(SelectedIndex,0,library.entries.Length-1);var entry=library.entries[SelectedIndex];
        title.text=th?"คอลเลกชันมอนสเตอร์":"MONSTER COLLECTION";
        subtitle.text=th?$"ข้อมูลศัตรูและบอสทั้ง {library.entries.Length} ตัว · เลือกชื่อด้านซ้าย":$"{library.entries.Length} enemies and bosses · select an entry on the left";
        if(scrollHint!=null)scrollHint.text=th?"เลื่อนรายการเพื่อดูตัวอื่น":"SCROLL FOR MORE ENTRIES";
        entryLabel.text=th?"มอนสเตอร์":"BESTIARY";closeLabel.text=th?"ปิด [Esc]":"CLOSE [Esc]";
        nameLabel.text=entry.name;stageLabel.text=(entry.boss?(th?"บอส  ·  ":"BOSS  ·  "):(th?"มอนสเตอร์  ·  ":"ENEMY  ·  "))+(th?entry.stageThai:entry.stageEnglish);
        healthLabel.text=(th?"เลือดสูงสุด":"MAX HP")+$"\n<color=#A3EDF4>{entry.Health:0.##}</color>";
        damageLabel.text=(th?"ดาเมจพื้นฐาน":"BASE DAMAGE")+$"\n<color=#FFB5A3>{entry.DamageText}</color>";
        speedLabel.text=(th?"ความเร็วเดิน":"MOVE SPEED")+$"\n<color=#DBC0FF>{entry.Speed:0.##}</color>";
        cooldownLabel.text=(th?"คูลดาวน์พื้นฐาน":"COOLDOWN")+$"\n<color=#FFDA8C>{entry.Cooldown:0.##} {(th?"วิ":"s")}</color>";
        abilities.text=entry.Abilities(th);
        // ข้อมูลยาวเลื่อนได้ ไม่ย่อฟอนต์จนอ่านยากหรือตัดความสามารถทิ้ง
        float height=Mathf.Max(208,abilities.GetPreferredValues(abilities.text,928,float.PositiveInfinity).y+8);
        abilities.rectTransform.sizeDelta=new Vector2(928,height);
        abilitiesTitle.text=(th?"รูปแบบโจมตีและความสามารถ":"ATTACKS & ABILITIES")+(height>208?(th?" · เลื่อนอ่านเพิ่ม":" · SCROLL FOR MORE"):"");
        if(abilityScroll!=null)abilityScroll.verticalNormalizedPosition=1;
        portrait.sprite=entry.portrait;portrait.enabled=portrait.sprite!=null;portrait.preserveAspect=true;
        footer.text=th?"ค่าพื้นฐานจากเกมจริง · ท่าพิเศษอาจมีดาเมจต่างกัน · มอนผิดเพี้ยนและเฟสบอสมีตัวคูณเพิ่มเติม":"Live base stats · special attacks may differ · anomalies and boss phases use extra multipliers";
        for(int i=0;i<rows.Length;i++)
        {
            rowNames[i].text=library.entries[i].name;var state=rows[i].GetComponent<QuantumUiButtonState>();if(state!=null){state.chosen=i==SelectedIndex;state.Refresh();}
        }
    }
}
