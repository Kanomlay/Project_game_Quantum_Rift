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
    public TMP_Text weaponEnergyCostText;
    private int weaponEnergyCost;
    private int availableEnergy = int.MaxValue;

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
        availableEnergy = currentEnergy;
        RefreshWeaponCostColor();
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
        if (activeWeaponIcon != null)
        {
            activeWeaponIcon.sprite = weaponSprite;
            activeWeaponIcon.color = Color.white;
            activeWeaponIcon.preserveAspect = true;
            activeWeaponIcon.enabled = weaponSprite != null;
            FitWeaponIcon(weaponSprite);
        }
    }

    private void FitWeaponIcon(Sprite sprite)
    {
        var clip = activeWeaponIcon.rectTransform.parent as RectTransform;
        if (sprite == null || clip == null || clip.GetComponent<RectMask2D>() == null) return;
        Canvas.ForceUpdateCanvases();
        // ใช้ขอบรูปอาวุธจริงจาก tight mesh เพื่อลดผลของพื้นที่โปร่งใสในไฟล์ภาพ
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        var vertices = sprite.vertices;
        foreach (Vector2 vertex in vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); }
        if (vertices.Length == 0) return;
        Vector2 content = (max - min) * sprite.pixelsPerUnit;
        if (content.x <= 0 || content.y <= 0) return;
        Vector2 available = clip.rect.size - Vector2.one * 8f;
        float scale = Mathf.Min(available.x / content.x, available.y / content.y);
        if (scale <= 0) return;
        Vector2 centerOffset = (min + max) * .5f * sprite.pixelsPerUnit - (sprite.rect.size * .5f - sprite.pivot);
        var rect = activeWeaponIcon.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = sprite.rect.size * scale;
        rect.anchoredPosition = -centerOffset * scale;
    }

    // ใช้ข้อมูลอาวุธชิ้นเดียวกับระบบต่อสู้ เลขมุมกรอบคือพลังงานต่อการโจมตีหนึ่งครั้ง
    public void UpdateWeapon(WeaponData weapon)
    {
        UpdateWeaponIcon(weapon != null ? weapon.weaponIcon : null);
        weaponEnergyCost = weapon != null ? Mathf.Max(0, weapon.energyCost) : 0;
        if (weaponEnergyCostText != null)
            weaponEnergyCostText.text = weapon != null ? weaponEnergyCost.ToString() : "-";
        RefreshWeaponCostColor();
    }

    private void RefreshWeaponCostColor()
    {
        if (weaponEnergyCostText != null)
            weaponEnergyCostText.color = availableEnergy < weaponEnergyCost
                ? new Color(1f, .36f, .36f) : new Color(.72f, .96f, 1f);
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
