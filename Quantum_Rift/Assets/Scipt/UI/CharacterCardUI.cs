using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class CharacterCardUI : MonoBehaviour
{
    [Header("UI Elements (ส่วนแสดงผลบนจอ)")]
    public Image characterImage;
    public TMP_Text nameText;
    public TMP_Text hpText;
    public TMP_Text energyText;

    [Header("Data (ข้อมูลตัวละคร)")]
    public CharacterData characterData;

    public CharacterDetailUI detailUI; 
    public MainMenuController menuController;

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (characterData != null)
        {
            nameText.text = characterData.className;
            hpText.text = "Health: " + characterData.maxHealth.ToString();
            energyText.text = "Energy: " + characterData.maxEnergy.ToString(); 

            if (characterData.characterSprite != null)
            {
                characterImage.sprite = characterData.characterSprite;
            }
        }
    }
    
    public void OnSelectButtonClicked()
    {
        detailUI.SetupAndShow(characterData);
        menuController.OpenCharacterDetail();
    }
}