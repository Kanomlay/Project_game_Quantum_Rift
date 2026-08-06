using UnityEngine;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("สถานะปัจจุบัน")]
    public float maxHP = 8f; 
    public float currentHP;  
    public int maxEnergy = 100;
    public int currentEnergy;

    [Header("ระบบอาวุธ")]
    public WeaponData weapon1; 
    public WeaponData weapon2; 
    private int currentWeaponIndex = 1;

    private SkillData activeSkillQ;
    private SkillData activeSkillE;

    [Header("ระบบคูลดาวน์สกิล")]
    private float maxCooldownQ;
    private float maxCooldownE;
    private float currentCooldownQ = 0f;
    private float currentCooldownE = 0f;

    [Header("ระบบอมตะ (I-Frames)")]
    public float iframeDuration = 1.0f;
    private bool isInvincible = false;
    private SpriteRenderer sr; 

    [Header("ระบบเงิน (Currency)")]
    public int currentCurrency = 0;

    private HUDManager hud;
    public WeaponController weaponController;

    public bool isDead = false;

    void Start()
    {
        Time.timeScale = 1f;
        sr = GetComponent<SpriteRenderer>();
        hud = FindObjectOfType<HUDManager>();

        if (hud != null) hud.UpdateCurrency(currentCurrency);
        
        if (GameManager.selectedCharacter != null)
        {
            maxHP = GameManager.selectedCharacter.maxHealth;
            maxEnergy = GameManager.selectedCharacter.maxEnergy; 

            activeSkillQ = GameManager.selectedCharacter.skillQ;
            activeSkillE = GameManager.selectedCharacter.skillE;

            if (activeSkillQ != null) maxCooldownQ = activeSkillQ.cooldown;
            if (activeSkillE != null) maxCooldownE = activeSkillE.cooldown;

            if(hud != null) hud.SetupSkillIcons(
                activeSkillQ != null ? activeSkillQ.skillIcon : null, 
                activeSkillE != null ? activeSkillE.skillIcon : null
            );

            
            if (hud != null && weapon1 != null) 
            {
                hud.UpdateWeaponIcon(weapon1.weaponIcon);
                if (weaponController != null) weaponController.EquipWeapon(weapon1);
                currentWeaponIndex = 1;
            }
        }

        currentHP = maxHP;
        currentEnergy = maxEnergy;
        UpdateAllHUD(); 
    }

    void Update()
    {
        if (PauseManager.isGamePaused) return;

        if (currentCooldownQ > 0)
        {
            currentCooldownQ -= Time.deltaTime;
            hud.UpdateSkillCooldown("Q", currentCooldownQ);
        }
        if (currentCooldownE > 0)
        {
            currentCooldownE -= Time.deltaTime;
            hud.UpdateSkillCooldown("E", currentCooldownE);
        }

        
        if (Input.GetKeyDown(KeyCode.R)) SwapWeapon();

        if (Input.GetKeyDown(KeyCode.Q) && currentCooldownQ <= 0) UseSkillQ();
        if (Input.GetKeyDown(KeyCode.E) && currentCooldownE <= 0) UseSkillE();
    }

    public void TakeDamage(float damage)
    {

        if (isInvincible) return; 

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
        
        if (hud != null) hud.UpdateHP(currentHP, maxHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(IframeRoutine());
        }
    }
    private IEnumerator IframeRoutine()
    {
        isInvincible = true; 

        float blinkInterval = 0.1f; 
        float timePassed = 0f;
        bool isVisible = true;

        while (timePassed < iframeDuration)
        {
            isVisible = !isVisible;
            if (sr != null) sr.enabled = isVisible; 

            yield return new WaitForSeconds(blinkInterval);
            timePassed += blinkInterval;
        }
        if (sr != null) sr.enabled = true; 
        isInvincible = false; 
    }

    private void Die()
    {
        SummaryManager.instance.ShowSummary(false, "");
    }


    public void UseEnergy(int cost)
    {
        currentEnergy -= cost;
        if (currentEnergy < 0) currentEnergy = 0;
        if (hud != null) hud.UpdateEnergy(currentEnergy, maxEnergy);
    }

    private void UpdateAllHUD()
    {
        if (hud != null)
        {
            hud.UpdateHP(currentHP, maxHP);
            hud.UpdateEnergy(currentEnergy, maxEnergy);
        }
    }

    private void SwapWeapon()
    {
        currentWeaponIndex = (currentWeaponIndex == 1) ? 2 : 1; 
        WeaponData activeWeapon = (currentWeaponIndex == 1) ? weapon1 : weapon2;

        if (activeWeapon != null)
        {
            if (hud != null) hud.UpdateWeaponIcon(activeWeapon.weaponIcon);
            if (weaponController != null) weaponController.EquipWeapon(activeWeapon);
            Debug.Log("สลับไปใช้อาวุธ: " + activeWeapon.weaponName);
        }
    }

    private void UseSkillQ()
    {
        if (activeSkillQ != null && currentEnergy >= activeSkillQ.energyCost)
        {
            UseEnergy(activeSkillQ.energyCost);
            activeSkillQ.ActivateSkill(this.gameObject); 
            currentCooldownQ = maxCooldownQ;
        }
    }

    private void UseSkillE()
    {
        if (activeSkillE != null && currentEnergy >= activeSkillE.energyCost)
        {
            UseEnergy(activeSkillE.energyCost);
            activeSkillE.ActivateSkill(this.gameObject);
            currentCooldownE = maxCooldownE;
        }
    }

    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        
        if (hud != null) hud.UpdateCurrency(currentCurrency);
        
        Debug.Log("เก็บเงินได้ " + amount + " คริสตัล! รวมเป็น: " + currentCurrency);
    }

}