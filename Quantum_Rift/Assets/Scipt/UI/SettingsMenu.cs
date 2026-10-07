using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าตั้งค่า: ความดังเสียง (ทั้งหมด / เพลง / เอฟเฟกต์) กับ ภาษา
// วางไว้ได้ทั้งฉากเมนูหลักและฉากเกม เปิดจากปุ่ม Settings ของหน้านั้นๆ
public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu instance;

    [Header("ชิ้นส่วน UI")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public TMP_Text volumeValueText;
    public TMP_Text languageValueText;

    const string VolumeKey = "quantumrift.volume";

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

    void Awake()
    {
        if (instance == null) instance = this;

        // ตั้งเสียงตั้งแต่เริ่มฉาก ไม่ต้องรอให้ผู้เล่นเปิดหน้าตั้งค่าก่อน
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(AudioListener.volume); // กันไม่ให้ยิง event ตอนตั้งค่าเริ่มต้น
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        SettingsAudioRows.Attach(this);     // แถบเสียงเพลง / เสียงเอฟเฟกต์ ต่อจากแถบเสียงทั้งหมด (ต้องมาก่อนแถวที่ต่อท้ายแถวภาษา)
        DevConsoleSettingsRow.Attach(this); // แถวเปิด/ปิดคอนโซลทดสอบ ต่อท้ายแถวภาษา
        CreditsWindow.Attach(this);         // แถวเครดิต (เสียง CC BY ต้องให้เครดิตในเกม)
        RefreshLabels();
    }

    public void Open()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsMenu ยังไม่ได้ลาก Settings Panel ใส่ใน Inspector");
            return;
        }

        // หน้าตั้งค่าอยู่ก่อนหน้าหยุดเกมในลำดับของฉาก ถ้าไม่ดึงขึ้นมาจะถูกหน้าหยุดเกมวาดทับและกดอะไรไม่ได้
        settingsPanel.transform.SetAsLastSibling();
        settingsPanel.SetActive(true);
        RefreshLabels();
    }

    public void Close()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        RefreshLabels();
    }

    // ปุ่มสลับภาษา ตัวที่เปลี่ยนภาพ/ข้อความจริงคือ LocalizedImage กับ LocalizedText ที่ฟัง event อยู่
    public void ToggleLanguage()
    {
        LanguageSettings.Toggle();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        if (languageValueText != null) languageValueText.text = LanguageSettings.ShortName;
    }
}
