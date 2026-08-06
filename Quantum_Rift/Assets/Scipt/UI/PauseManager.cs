using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI หน้าต่าง Pause")]
    public GameObject pauseMenuPanel;

    public static bool isGamePaused = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void OpenSettings()
    {
        // TODO: สั่งเปิดหน้า UI Settings
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f; 
        isGamePaused = false;
        SceneManager.LoadScene("SampleScene"); 
    }
}