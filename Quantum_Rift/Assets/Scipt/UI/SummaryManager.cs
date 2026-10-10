using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SummaryManager : MonoBehaviour
{
    public static SummaryManager instance; 

    [Header("ชิ้นส่วน UI ที่ต้องเปิด/ปิด")]
    public GameObject summaryPanel;   
    public GameObject nextSectorBox;  
    public GameObject continueButton;  

    [Header("ตัวหนังสือที่ต้องอัปเดต")]
    public TMP_Text titleText;        
    public TMP_Text rewardText;        
    public TMP_Text enemiesDefeatedText; 
    public TMP_Text timeText;         
    public TMP_Text nextSectorText;    

    [Header("ฉากที่จะกลับไปเมื่อจบเกม")]
    public string mainMenuScene = "MainMenu";

    public static int enemiesDefeatedCount = 0;
    private float startTime;
    bool finalResultHandled; // หน้าสรุปอาจถูกเรียกซ้ำ ไม่เพิ่มประวัติรอบเดียวกันสองครั้ง
    float finalElapsed;
    int finalKills, finalCoins;

    // PauseManager ใช้เช็คว่ากำลังโชว์หน้าสรุปอยู่ไหม จะได้ไม่ให้กด ESC หนีหน้าสรุปไปเล่นต่อ
    public bool IsShowing => summaryPanel != null && summaryPanel.activeSelf;
    public float ElapsedSeconds => finalResultHandled ? finalElapsed : Time.time - startTime; // ไม่รวมเวลารอท่าตายหลังจบรอบ

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
        startTime = Time.time;
        enemiesDefeatedCount = 0;
        // เล่นต่อจากเซฟ: เวลาเล่นและจำนวนมอนนับต่อจากที่บันทึกไว้
        if (RunSave.ResumeStats(out float elapsed, out int kills))
        {
            startTime -= elapsed;
            enemiesDefeatedCount = kills;
        }
    }

    // ตอนตายเรียกก่อนลบเซฟและเล่นท่าตาย ตอนชนะเรียกจากหน้าสรุป ใช้จุดเดียวป้องกันผลซ้ำ
    public void RecordFinalResult(bool isWin, MapData map, PlayerStats player = null)
    {
        if (map != null && !map.isTestLab && !map.isTutorial && (!isWin || map.nextMap == null) && !finalResultHandled)
        {
            finalElapsed = ElapsedSeconds;
            finalKills = enemiesDefeatedCount;
            if (player == null) player = FindFirstObjectByType<PlayerStats>();
            finalCoins = player != null ? player.currentCurrency : 0;
            finalResultHandled = true;
            RunHistory.RecordFinished(map, isWin, GameManager.selectedCharacter, finalElapsed, finalKills, finalCoins);
            CinematicProgress.MarkRunFinished();
            RunSave.Delete(); // ลบเซฟเล่นต่อหลังบันทึกประวัติแล้ว ประวัติอยู่คนละไฟล์
        }
    }

    public void ShowSummary(bool isWin, string nextMapName)
    {
        if(CinematicDirector.Instance!=null)CinematicDirector.Instance.Cancel();
        var map=MapManager.instance!=null?MapManager.instance.CurrentMap:null;
        RecordFinalResult(isWin, map);
        if (summaryPanel == null)
        {
            Debug.LogWarning("SummaryManager ยังไม่ได้ลาก Summary Panel ใส่ใน Inspector เลยไม่มีหน้าสรุปให้แสดง");
            return;
        }

        summaryPanel.SetActive(true);
        Time.timeScale = 0f; 
        PauseManager.isGamePaused = true; 

        float timePlayed = ElapsedSeconds;
        int minutes = Mathf.FloorToInt(timePlayed / 60F);
        int seconds = Mathf.FloorToInt(timePlayed - minutes * 60);
        SetText(timeText, string.Format("{0:00}:{1:00}", minutes, seconds));

        SetText(enemiesDefeatedText, (finalResultHandled ? finalKills : enemiesDefeatedCount).ToString());

        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (finalResultHandled) SetText(rewardText, finalCoins.ToString());
        else if (player != null) SetText(rewardText, player.currentCurrency.ToString());

        // หัวเรื่องเปลี่ยนตามผลที่ได้ จึงแปลตรงนี้เอง ใช้ LocalizedText ไม่ได้เพราะข้อความไม่ตายตัว
        // ชนะแล้วถึงจะมีด่านต่อไปให้ไป ตายแล้วเหลือแค่ปุ่มกลับเมนู
        // ชนะด่านสุดท้าย = เคลียร์เกม ไม่มีด่านถัดไปให้บอก
        bool cleared = isWin && map != null && map.nextMap == null;
        if (LanguageSettings.IsThai) SetText(titleText, cleared ? "เคลียร์เกมแล้ว!" : isWin ? "ผ่านด่านแล้ว!" : "เกมโอเวอร์");
        else SetText(titleText, cleared ? "Game Cleared!" : isWin ? "Stage Cleared!" : "Game Over!");
        if (nextSectorBox != null) nextSectorBox.SetActive(isWin && !cleared);
        if (continueButton != null) continueButton.SetActive(isWin&&map!=null&&map.nextMap!=null);
        if (isWin) SetText(nextSectorText, nextMapName);
    }

    private void SetText(TMP_Text field, string value)
    {
        if (field != null) field.text = value;
    }

    public void OnClickEnd()
    {
        // ต้องคืน timeScale ก่อนเปลี่ยนฉาก ไม่งั้นเมนูหลักจะค้างเพราะเวลายังเป็น 0
        Time.timeScale = 1f;
        PauseManager.isGamePaused = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnClickContinue()
    {
        Time.timeScale = 1f;
        PauseManager.isGamePaused = false;
        summaryPanel.SetActive(false);

        MapManager.instance.LoadNextMapFromSummary(); 
    }
}
