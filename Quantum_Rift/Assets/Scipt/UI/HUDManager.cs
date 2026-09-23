using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("ระบบหลอดเลือด")]
    public Image hpFillImage;
    public TMP_Text hpText;

    [Header("ระบบพลังงาน")]
    public Image energyFillImage;
    public TMP_Text energyText;

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
        if (hpFillImage != null) hpFillImage.fillAmount = currentHP / maxHP;

        if (hpText != null)
        {
            hpText.text = currentHP.ToString("F0") + "/" + maxHP.ToString("F0");
        }
    }
    public void UpdateEnergy(int currentEnergy, int maxEnergy)
    {
        if (energyFillImage != null) energyFillImage.fillAmount = (float)currentEnergy / maxEnergy;

        if (energyText != null)
        {
            energyText.text = currentEnergy + "/" + maxEnergy;
        }
    }
    // พลังงานไม่พอยิง: กะพริบตัวเลขพลังงานเป็นสีแดงแวบหนึ่ง ผู้เล่นจะได้รู้ว่าทำไมกดแล้วไม่ยิง
    private Coroutine energyFlash;
    private Color energyTextColor;
    private bool energyTextColorSaved;

    public void FlashEnergyEmpty()
    {
        if (energyText == null) return;

        // จำสีเดิมไว้ครั้งเดียว ถ้ากะพริบซ้อนกันจะได้ไม่จำสีแดงไปเป็นสีเดิม
        if (!energyTextColorSaved)
        {
            energyTextColor = energyText.color;
            energyTextColorSaved = true;
        }

        if (energyFlash != null) StopCoroutine(energyFlash);
        energyFlash = StartCoroutine(FlashEnergyRoutine());
    }

    private IEnumerator FlashEnergyRoutine()
    {
        energyText.color = new Color(1f, 0.3f, 0.3f, energyTextColor.a);
        yield return new WaitForSeconds(0.25f);
        energyText.color = energyTextColor;
        energyFlash = null;
    }

    public void UpdateWeaponIcon(Sprite weaponSprite)
    {
        if (weaponSprite != null && activeWeaponIcon != null)
        {
            activeWeaponIcon.sprite = weaponSprite;
            activeWeaponIcon.color = Color.white;
        }
    }

    public void SetupSkillIcons(Sprite qIcon, Sprite eIcon)
    {
        if (qIcon != null && skillQIcon != null) { skillQIcon.sprite = qIcon; skillQIcon.color = Color.white; }
        if (eIcon != null && skillEIcon != null) { skillEIcon.sprite = eIcon; skillEIcon.color = Color.white; }
    }

    public void UpdateSkillCooldown(string skillKey, float currentCooldown)
    {
        if (skillKey == "Q" && skillQCooldownText != null)
        {
            skillQCooldownText.text = currentCooldown > 0 ? Mathf.Ceil(currentCooldown).ToString() : "";
        }
        else if (skillKey == "E" && skillECooldownText != null)
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