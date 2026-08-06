using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("ระบบหลอดเลือด (Rect Mask 2D)")]
    public RectMask2D hpMask;
    public TMP_Text hpText; 
    private float maxHpPaddingRight = 410f; 

    [Header("ระบบพลังงาน (Rect Mask 2D)")]
    public RectMask2D energyMask;
    public TMP_Text energyText;
    private float maxEnergyPaddingRight = 410f; 

    [Header("ระบบอาวุธ")]
    public Image activeWeaponIcon; 

    [Header("ระบบสกิล")]
    public Image skillQIcon;
    public TMP_Text skillQCooldownText; 
    
    public Image skillEIcon;
    public TMP_Text skillECooldownText; 

    [Header("🌟 ระบบเปลี่ยนด่าน (Map Transition)")]
    public CanvasGroup transitionCanvas; 
    public TMP_Text transitionMapNameText; 

    [Header("ระบบเงิน/คริสตัล (Currency)")]
    public TMP_Text currencyText;

    public void UpdateHP(float currentHP, float maxHP)
    {
        float hpPercent = currentHP / maxHP;
        float currentPadding = maxHpPaddingRight * (1f - hpPercent);

        Vector4 currentPaddingVector = hpMask.padding;
        currentPaddingVector.z = currentPadding;
        hpMask.padding = currentPaddingVector;

        if (hpText != null)
        {
            hpText.text = "HP " + currentHP.ToString("F1") + "/" + maxHP.ToString("F1");
        }
    }
    public void UpdateEnergy(int currentEnergy, int maxEnergy)
    {
        float energyPercent = (float)currentEnergy / maxEnergy;
        float currentPadding = maxEnergyPaddingRight * (1f - energyPercent);

        Vector4 currentPaddingVector = energyMask.padding;
        currentPaddingVector.z = currentPadding;
        energyMask.padding = currentPaddingVector;

        if (energyText != null)
        {
            energyText.text = "Energy " + currentEnergy + "/" + maxEnergy;
        }
    }
    public void UpdateWeaponIcon(Sprite weaponSprite)
    {
        if (weaponSprite != null)
        {
            activeWeaponIcon.sprite = weaponSprite;
            activeWeaponIcon.color = Color.white; 
        }
    }

    public void SetupSkillIcons(Sprite qIcon, Sprite eIcon)
    {
        if (qIcon != null) { skillQIcon.sprite = qIcon; skillQIcon.color = Color.white; }
        if (eIcon != null) { skillEIcon.sprite = eIcon; skillEIcon.color = Color.white; }
    }

    public void UpdateSkillCooldown(string skillKey, float currentCooldown)
    {
        if (skillKey == "Q")
        {
            skillQCooldownText.text = currentCooldown > 0 ? Mathf.Ceil(currentCooldown).ToString() : "";
        }
        else if (skillKey == "E")
        {
            skillECooldownText.text = currentCooldown > 0 ? Mathf.Ceil(currentCooldown).ToString() : "";
        }
    }

    public IEnumerator FadeInBlack(string mapName)
    {
        transitionCanvas.gameObject.SetActive(true); 
        if(transitionMapNameText != null) transitionMapNameText.text = "- " + mapName + " -"; 

        float t = transitionCanvas.alpha; 
        while (t < 1f)
        {
            t += Time.deltaTime * 2f; 
            transitionCanvas.alpha = t; 
            yield return null;
        }
    }

    public IEnumerator FadeOutClear()
    {
        float t = transitionCanvas.alpha; 
        while (t > 0f)
        {
            t -= Time.deltaTime * 2f;
            transitionCanvas.alpha = t;
            yield return null;
        }
        transitionCanvas.gameObject.SetActive(false); 
    }

    public void UpdateCurrency(int currentAmount)
    {
        if (currencyText != null)
        {
            currencyText.text = currentAmount.ToString();
        }
    }
}