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
        damageLabel.text=(th?"ดาเมจพื้นฐาน":"BASE DAMAGE")+$"\n<color=#FFB5A3>{entry.Damage:0.##}</color>";
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
