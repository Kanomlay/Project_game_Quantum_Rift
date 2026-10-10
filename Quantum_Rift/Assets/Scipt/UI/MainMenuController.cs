using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuUI;
    public GameObject characterSelectUI;
    public GameObject characterDetailUI;
    // สนามฝึกจบแล้วกด "เลือกอาชีพ เริ่มเกมจริง": กลับมาฉากเมนูแล้วเปิดหน้าเลือกอาชีพทันที (ใช้ครั้งเดียว)
    public static bool openCharacterSelectOnLoad;
    // ปุ่มสนามฝึกวางอยู่ใต้ Canvas ตรง ๆ (ไม่ได้อยู่ในหน้าเมนูหลัก) ให้เห็นเฉพาะตอนหน้าเมนูหลักเปิดอยู่
    GameObject tutorialButton;

    void Start()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (characterSelectUI != null) characterSelectUI.SetActive(false);
        if (characterDetailUI != null) characterDetailUI.SetActive(false);
        var button = mainMenuUI != null && mainMenuUI.transform.parent != null ? mainMenuUI.transform.parent.Find("TutorialButton") : null;
        if (button != null) tutorialButton = button.gameObject;
        ContinueMenu.Attach(this); // มีเซฟค้าง: ปุ่ม "เล่นต่อ" และถามยืนยันก่อนเริ่มเกมใหม่ทับเซฟ
        DevConsoleLogoUnlock.Attach(this); // โลโก้เดิมคลิก 5 ครั้งจึงเห็นตัวเลือกคอนโซลทดสอบ
        if (openCharacterSelectOnLoad)
        {
            openCharacterSelectOnLoad = false;
            if (mainMenuUI != null && characterSelectUI != null) OnNewGameClicked();
        }
    }

    void LateUpdate()
    {
        if (tutorialButton != null && mainMenuUI != null && tutorialButton.activeSelf != mainMenuUI.activeSelf)
            tutorialButton.SetActive(mainMenuUI.activeSelf);
    }

    public void OnNewGameClicked()
    {
        mainMenuUI.SetActive(false);
        characterSelectUI.SetActive(true);
    }

    public void OnTutorialClicked()
    {
        MapManager.startOverride=null;
        Time.timeScale=1f;PauseManager.isGamePaused=false;
        SceneManager.LoadScene("TutorialScene");
    }

    public void OnSettingClicked()
    {
        if (SettingsMenu.instance != null) SettingsMenu.instance.Open();
        else Debug.LogWarning("ยังไม่มีหน้าตั้งค่าในฉาก สั่ง Tools > Quantum Rift > Build Settings Screen ก่อน");
    }

    public void OpenCharacterDetail()
    {
        characterSelectUI.SetActive(false);
        characterDetailUI.SetActive(true);
    }

    public void BackToSelectCharacter()
    {
        characterDetailUI.SetActive(false);
        characterSelectUI.SetActive(true);
    }

    public void OnExitClicked()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }

    public void OnBackClicked()
    {
        characterSelectUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }

}
