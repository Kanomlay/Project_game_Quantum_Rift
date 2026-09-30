using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// คู่มือใช้ prefab เดียวกันในเมนูและเกม ไม่โหลดฉากฝึกสอนและไม่เปลี่ยนข้อมูลรอบเล่น
[DefaultExecutionOrder(-200)]
public sealed class GameHelpWindow : MonoBehaviour
{
    [Serializable] public sealed class Copy
    {
        [TextArea] public string thai;
        [TextArea] public string english;
        public string Value => LanguageSettings.IsThai ? thai : english;
    }
    [Serializable] public sealed class Section
    {
        public Copy heading;
        public Copy body;
        public Color accent = Color.white;
    }
    [Serializable] public sealed class Page
    {
        public Copy tab;
        public Copy title;
        public Copy summary;
        public Section[] sections;
    }

    public bool pauseGameplay;
    public GameObject panel;
    public Button entryButton, closeButton, previousButton, nextButton, languageButton;
    public TMP_Text entryLabel, entryHint, title, summary, footer, statusLabel, closeLabel, languageLabel;
    public TMP_Text[] sectionTitles, sectionBodies, tabLabels;
    public Image[] sectionAccents, tabBackgrounds;
    public Button[] tabs;
    public Page[] pages;
    public static GameHelpWindow Instance { get; private set; }
    static int closedFrame = -1;
    public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;
    public static bool BlocksGameplayInput => IsOpen || Time.frameCount == closedFrame;
    public int CurrentPage { get; private set; }
    bool ownsPause;
    bool previousPaused;
    float previousTimeScale;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;
    HUDManager hud;
    ShopWindow shopContext;
    Page[] normalPages;
    int normalPage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; closedFrame = -1; }

    void OnEnable()
    {
        Instance = this;
        LanguageSettings.Changed += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        LanguageSettings.Changed -= Refresh;
        if (panel != null) panel.SetActive(false);
        RestorePause();
        RestoreGuide();
        if (Instance == this) Instance = null;
    }

    public bool CanOpen()
    {
        return CanOpen(false);
    }
    bool CanOpen(bool fromShop)
    {
        if ((!fromShop && ShopWindow.IsOpen) || BlessingManager.IsChoosing) return false;
        if (SettingsMenu.instance != null && SettingsMenu.instance.IsOpen) return false;
        if (SummaryManager.instance != null && SummaryManager.instance.IsShowing) return false;
        if (pauseGameplay)
        {
            if (hud == null) hud = FindFirstObjectByType<HUDManager>();
            if (hud != null && hud.transitionCanvas != null && hud.transitionCanvas.gameObject.activeInHierarchy && hud.transitionCanvas.alpha > .01f) return false;
        }
        return true;
    }

    void Update()
    {
        if (entryButton != null) entryButton.interactable = IsOpen || CanOpen();
        if (Input.GetKeyDown(KeyCode.F1)) { if (IsOpen) Close(); else if (ShopWindow.Active != null) ShopWindow.Active.OpenHelp(); else Open(); return; }
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.PageDown)) Next();
        if (Input.GetKeyDown(KeyCode.PageUp)) Previous();
    }

    public void Open()
    {
        if (panel == null || IsOpen || !CanOpen()) return;
        OpenPanel();
    }
    public void OpenFromShop(ShopWindow source, int offerIndex = -1)
    {
        if (source == null || ShopWindow.Active != source || panel == null || IsOpen || !CanOpen(true)) return;
        normalPages = pages;
        normalPage = CurrentPage;
        shopContext = source;
        pages = ShopHelpPages.Create(source);
        CurrentPage = Mathf.Clamp(offerIndex + 1, 0, pages.Length - 1);
        OpenPanel();
    }
    public bool IsGuideFor(ShopWindow source) => IsOpen && shopContext == source;
    void OpenPanel()
    {
        Instance = this;
        if (pauseGameplay && Application.isPlaying)
        {
            previousPaused = PauseManager.isGamePaused;
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            ownsPause = true;
            PauseManager.isGamePaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        panel.SetActive(true);
        Refresh();
        if (EventSystem.current != null && tabs != null && tabs.Length > CurrentPage)
            EventSystem.current.SetSelectedGameObject(tabs[CurrentPage].gameObject);
    }

    public void Close()
    {
        if (panel == null || !panel.activeSelf) return;
        panel.SetActive(false);
        closedFrame = Time.frameCount; // ESC/คลิกที่ปิดคู่มือไม่เปิด Pause หรือโจมตีในเฟรมเดียวกัน
        RestorePause();
        RestoreGuide();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    void RestoreGuide()
    {
        if (normalPages == null) return;
        pages = normalPages; CurrentPage = normalPage;
        normalPages = null; shopContext = null;
        Refresh();
    }
    void RestorePause()
    {
        if (!ownsPause) return;
        ownsPause = false;
        Time.timeScale = previousTimeScale;
        PauseManager.isGamePaused = previousPaused;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    public void SelectPage(int index)
    {
        if (pages == null || pages.Length == 0) return;
        CurrentPage = Mathf.Clamp(index, 0, pages.Length - 1);
        Refresh();
    }
    public void Previous() => SelectPage(CurrentPage - 1);
    public void Next() => SelectPage(CurrentPage + 1);
    public void ToggleLanguage() => LanguageSettings.Toggle();
    public void Refresh()
    {
        if (pages == null || pages.Length == 0 || title == null) return;
        CurrentPage = Mathf.Clamp(CurrentPage, 0, pages.Length - 1);
        bool th = LanguageSettings.IsThai;
        var page = pages[CurrentPage];
        title.text = page.title.Value;
        summary.text = page.summary.Value;
        entryLabel.text = pauseGameplay ? "?" : th ? "?  คู่มือการเล่น" : "?  HOW TO PLAY";
        entryHint.text = th ? "F1 · คู่มือ" : "F1 · HELP";
        closeLabel.text = shopContext != null ? (th ? "กลับร้าน [Esc]" : "SHOP [Esc]") : th ? "ปิด  [Esc]" : "CLOSE  [Esc]";
        languageLabel.text = th ? "TH / EN" : "EN / TH";
        footer.text = $"{CurrentPage + 1:00} / {pages.Length:00}   ·   " + (th ? "เลือกหัวข้อด้านซ้าย หรือใช้ PgUp / PgDn" : "Choose a topic or use PgUp / PgDn");
        statusLabel.text = shopContext != null ? (th ? "ดูข้อมูลได้โดยไม่ซื้อสินค้า" : "READING DOES NOT BUY AN ITEM") : pauseGameplay ? (th ? "หยุดเกมชั่วคราวระหว่างอ่าน" : "GAME PAUSED WHILE READING") : (th ? "คู่มือผู้เดินทางข้ามมิติ" : "RIFT TRAVELER'S FIELD GUIDE");
        previousButton.interactable = CurrentPage > 0;
        nextButton.interactable = CurrentPage < pages.Length - 1;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].gameObject.SetActive(i < pages.Length);
            if (i >= pages.Length) continue;
            tabLabels[i].text = $"{i + 1:00}   {pages[i].tab.Value}";
            tabBackgrounds[i].color = i == CurrentPage ? new Color32(74,57,109,255) : new Color32(30,27,47,255);
            tabLabels[i].color = i == CurrentPage ? new Color32(140,235,244,255) : new Color32(212,206,224,255);
        }
        for (int i = 0; i < sectionTitles.Length; i++)
        {
            var section = page.sections[i];
            sectionTitles[i].text = section.heading.Value;
            sectionBodies[i].text = section.body.Value;
            sectionBodies[i].enableAutoSizing = shopContext != null;
            sectionBodies[i].fontSizeMin = 22;
            sectionBodies[i].fontSizeMax = 25;
            sectionBodies[i].fontSize = 25;
            sectionAccents[i].color = section.accent;
            sectionTitles[i].color = section.accent;
        }
    }
}
