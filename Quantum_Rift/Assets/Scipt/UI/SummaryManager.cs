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

    public static int enemiesDefeatedCount = 0; 
    private float startTime;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        summaryPanel.SetActive(false); 
        startTime = Time.time;         
        enemiesDefeatedCount = 0;      
    }

    public void ShowSummary(bool isWin, string nextMapName)
    {
        summaryPanel.SetActive(true);
        Time.timeScale = 0f; 
        PauseManager.isGamePaused = true; 

        float timePlayed = Time.time - startTime;
        int minutes = Mathf.FloorToInt(timePlayed / 60F);
        int seconds = Mathf.FloorToInt(timePlayed - minutes * 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        enemiesDefeatedText.text = enemiesDefeatedCount.ToString();

        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (player != null) rewardText.text = player.currentCurrency.ToString();


        if (isWin)
        {
            titleText.text = "Stage Cleared!";
            nextSectorBox.SetActive(true);  
            continueButton.SetActive(true); 
            nextSectorText.text = nextMapName;
        }
        else
        {
            titleText.text = "Game Over!";
            nextSectorBox.SetActive(false);  
            continueButton.SetActive(false); 
        }
    }

    public void OnClickEnd()
    {
        Time.timeScale = 1f;
        PauseManager.isGamePaused = false;
        SceneManager.LoadScene("SampleScene");
    }

    public void OnClickContinue()
    {
        Time.timeScale = 1f;
        PauseManager.isGamePaused = false;
        summaryPanel.SetActive(false);

        MapManager.instance.LoadNextMapFromSummary(); 
    }
}