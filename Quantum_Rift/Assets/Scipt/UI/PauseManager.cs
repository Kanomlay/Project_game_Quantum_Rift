using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI หน้าต่าง Pause")]
    public GameObject pauseMenuPanel;

    [Header("ฉากที่จะกลับไปเมื่อออกจากเกม")]
    public string mainMenuScene = "MainMenu";

    public static bool isGamePaused = false;

    void Start()
    {
        // ฉากอื่นอาจกดพักค้างไว้ก่อนเปลี่ยนฉาก ต้องรีเซ็ตให้เกมเดินปกติเสมอตอนเริ่ม
        SetPaused(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(!isGamePaused);
        }
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void PauseGame()
    {
        SetPaused(true);
    }

    // รวมการเปิด/ปิดหน้าต่างกับการหยุดเวลาไว้ที่เดียว จะได้ไม่มีทางที่จอค้างแต่เวลายังเดิน
    private void SetPaused(bool paused)
    {
        // ยังไม่ได้ลากหน้าต่างใส่ก็ไม่ควรทำเกมพัง แค่เตือนแล้วปล่อยให้เล่นต่อ
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(paused);
        else Debug.LogWarning("PauseManager ยังไม่ได้ลาก Pause Menu Panel ใส่ใน Inspector");

        Time.timeScale = paused ? 0f : 1f;
        isGamePaused = paused;
    }

    public void OpenSettings()
    {
        // TODO: สั่งเปิดหน้า UI Settings
        Debug.Log("ยังไม่มีหน้า Settings เดี๋ยวค่อยทำ");
    }

    public void ExitToMainMenu()
    {
        // ต้องคืน timeScale ก่อนเปลี่ยนฉาก ไม่งั้นเมนูหลักจะค้างเพราะเวลายังเป็น 0
        Time.timeScale = 1f;
        isGamePaused = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
