using UnityEngine;
using UnityEngine.UI;
using TMPro; // สำคัญมาก ต้องมีบรรทัดนี้เพื่อใช้ TextMeshPro

public class CharacterBoxUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image characterImage;
    public TextMeshProUGUI classNameText;
    public TextMeshProUGUI statsText;

    // ฟังก์ชันนี้จะถูกเรียกใช้ตอนเสกกล่องขึ้นมา
    public void SetupBox(CharacterData data)
    {
        // 1. เปลี่ยนรูปตัวละคร
        if (data.characterSprite != null)
        {
            characterImage.sprite = data.characterSprite;
        }

        // 2. เปลี่ยนชื่อคลาส
        classNameText.text = data.className;

        // 3. แสดงค่า Status (เอาตัวเลขมาต่อเป็นข้อความ)
        statsText.text = $"HP: {data.maxHealth}\nEnergy: {data.maxEnergy}\nSPD: {data.moveSpeed}";
    }
}