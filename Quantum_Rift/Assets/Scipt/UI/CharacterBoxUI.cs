using UnityEngine;
using UnityEngine.UI;
using TMPro; // สำคัญมาก ต้องมีบรรทัดนี้เพื่อใช้ TextMeshPro

public class CharacterBoxUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image characterImage;
    public TextMeshProUGUI classNameText;
    public TextMeshProUGUI statsText;

    [Header("ชื่อทักษะประจำอาชีพ")]
    public TextMeshProUGUI skill1Text;
    public TextMeshProUGUI skill2Text;

    private CharacterData currentData;

    void OnEnable()
    {
        LanguageSettings.Changed += Refresh;
    }

    void OnDisable()
    {
        LanguageSettings.Changed -= Refresh;
    }

    // ฟังก์ชันนี้จะถูกเรียกใช้ตอนเสกกล่องขึ้นมา
    public void SetupBox(CharacterData data)
    {
        currentData = data;
        Refresh();
    }

    // แยกออกมาเพื่อให้เรียกซ้ำได้ตอนผู้เล่นสลับภาษาในหน้าตั้งค่า
    private void Refresh()
    {
        if (currentData == null) return;

        if (currentData.characterSprite != null && characterImage != null)
        {
            characterImage.sprite = currentData.characterSprite;
        }

        if (classNameText != null) classNameText.text = currentData.DisplayName;

        if (statsText != null)
        {
            statsText.text = LanguageSettings.IsThai
                ? $"พลังชีวิต: {currentData.maxHealth}\nพลังงาน: {currentData.maxEnergy}\nความเร็ว: {currentData.moveSpeed}"
                : $"HP: {currentData.maxHealth}\nEnergy: {currentData.maxEnergy}\nSPD: {currentData.moveSpeed}";
        }

        // ชื่อทักษะมีเฉพาะภาษาไทยตามเอกสาร ทั้งสองภาษาจึงแสดงข้อความเดียวกันไปก่อน
        if (skill1Text != null) skill1Text.text = currentData.skill1Name;
        if (skill2Text != null) skill2Text.text = currentData.skill2Name;
    }
}