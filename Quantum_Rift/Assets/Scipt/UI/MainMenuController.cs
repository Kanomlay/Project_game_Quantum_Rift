using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuUI;
    public GameObject characterSelectUI;
    public GameObject characterDetailUI;

    void Start()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (characterSelectUI != null) characterSelectUI.SetActive(false);
        if (characterDetailUI != null) characterDetailUI.SetActive(false);
    }

    public void OnNewGameClicked()
    {
        mainMenuUI.SetActive(false);
        characterSelectUI.SetActive(true);
    }

    public void OnTutorialClicked()
    {
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