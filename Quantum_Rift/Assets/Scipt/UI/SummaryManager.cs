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

    // PauseManager ใช้เช็คว่ากำลังโชว์หน้าสรุปอยู่ไหม จะได้ไม่ให้กด ESC หนีหน้าสรุปไปเล่นต่อ
    public bool IsShowing => summaryPanel != null && summaryPanel.activeSelf;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
        startTime = Time.time;
        enemiesDefeatedCount = 0;
    }

    public void ShowSummary(bool isWin, string nextMapName)
    {
        if (summaryPanel == null)
        {
            Debug.LogWarning("SummaryManager ยังไม่ได้ลาก Summary Panel ใส่ใน Inspector เลยไม่มีหน้าสรุปให้แสดง");
            return;
        }

        summaryPanel.SetActive(true);
        Time.timeScale = 0f; 
        PauseManager.isGamePaused = true; 

        float timePlayed = Time.time - startTime;
        int minutes = Mathf.FloorToInt(timePlayed / 60F);
        int seconds = Mathf.FloorToInt(timePlayed - minutes * 60);
        SetText(timeText, string.Format("{0:00}:{1:00}", minutes, seconds));

        SetText(enemiesDefeatedText, enemiesDefeatedCount.ToString());

        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (player != null) SetText(rewardText, player.currentCurrency.ToString());

        // หัวเรื่องเปลี่ยนตามผลที่ได้ จึงแปลตรงนี้เอง ใช้ LocalizedText ไม่ได้เพราะข้อความไม่ตายตัว
        // ชนะแล้วถึงจะมีด่านต่อไปให้ไป ตายแล้วเหลือแค่ปุ่มกลับเมนู
        if (LanguageSettings.IsThai) SetText(titleText, isWin ? "ผ่านด่านแล้ว!" : "เกมโอเวอร์");
        else SetText(titleText, isWin ? "Stage Cleared!" : "Game Over!");
        if (nextSectorBox != null) nextSectorBox.SetActive(isWin);
        if (continueButton != null) continueButton.SetActive(isWin);
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