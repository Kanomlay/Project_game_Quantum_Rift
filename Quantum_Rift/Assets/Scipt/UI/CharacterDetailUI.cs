using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterDetailUI : MonoBehaviour
{
    [Header("ช่องแสดงผล UI")]
    public Image characterImage;
    public TMP_Text nameText;
    public TMP_Text hpText;
    public TMP_Text energyText;
    public TMP_Text skill1Text;
    public TMP_Text skill2Text;
    private CharacterData currentCharacter;

    // ฟังก์ชันนี้จะถูกเรียกตอนที่เรากดปุ่ม Select ที่การ์ด
    public void SetupAndShow(CharacterData data)
    {
        if (data != null)
        {
            currentCharacter = data;

            nameText.text = data.className;
            hpText.text = data.maxHealth.ToString();
            energyText.text = data.maxEnergy.ToString();
            skill1Text.text = data.skill1Name;
            skill2Text.text = data.skill2Name;

            if (data.characterSprite != null)
            {
                characterImage.sprite = data.characterSprite;
            }
        }
    }
    public void OnStartGameClicked()
    {
        GameManager.selectedCharacter = currentCharacter; 
        SceneManager.LoadScene("GameScene"); 
    }
}