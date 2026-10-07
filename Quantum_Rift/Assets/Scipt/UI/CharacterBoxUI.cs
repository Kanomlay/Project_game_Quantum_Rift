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
    [Header("รายละเอียดที่อ่านได้ในหน้าเลือกตัวละคร")]
    public Image skill1Icon, skill2Icon;
    public TextMeshProUGUI skill1Info, skill2Info, skill1Description, skill2Description;
    [Header("อาวุธเริ่มต้นจาก prefab ของอาชีพจริง")]
    public Image starterWeaponIcon;
    public TextMeshProUGUI starterWeaponTitle,starterWeaponName,starterWeaponStats;
    // อ่านอาวุธช่องแรกจากตัวละครจริง ไม่กำหนดชื่อ/รูปซ้ำใน UI เพื่อให้เปลี่ยนอาวุธแล้วหน้าจอตรงกันเสมอ
    public WeaponData StarterWeapon=>currentData!=null&&currentData.characterPrefab!=null?
        currentData.characterPrefab.GetComponent<PlayerStats>()?.weapon1:null;

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
        ShowSkill(currentData.skillQ, skill1Icon, skill1Info, skill1Description, "Q");
        ShowSkill(currentData.skillE, skill2Icon, skill2Info, skill2Description, "E");
        var weapon=StarterWeapon;
        if(starterWeaponTitle!=null)starterWeaponTitle.text=LanguageSettings.IsThai?"อาวุธเริ่มต้น":"STARTING WEAPON";
        if(starterWeaponIcon!=null){starterWeaponIcon.sprite=weapon!=null?weapon.weaponIcon:null;starterWeaponIcon.enabled=starterWeaponIcon.sprite!=null;starterWeaponIcon.preserveAspect=true;}
        if(starterWeaponName!=null)starterWeaponName.text=weapon!=null?weapon.weaponName:LanguageSettings.IsThai?"ไม่มีอาวุธ":"No weapon";
        if(starterWeaponStats!=null)starterWeaponStats.text=weapon==null?"":LanguageSettings.IsThai?
            $"ดาเมจ {weapon.attackDamage:0.##}   ·   พลังงาน {weapon.energyCost}":$"Damage {weapon.attackDamage:0.##}   ·   Energy {weapon.energyCost}";
    }
    static void ShowSkill(SkillData skill, Image icon, TMP_Text info, TMP_Text description, string key)
    {
        if(icon!=null){icon.sprite=skill!=null?skill.skillIcon:null;icon.enabled=icon.sprite!=null;icon.preserveAspect=true;}
        if(info!=null)info.text=skill==null?"":LanguageSettings.IsThai
            ? $"ปุ่ม {key}  ·  พลังงาน {skill.energyCost}  ·  คูลดาวน์ {skill.cooldown:0.#} วินาที"
            : $"{key}  ·  Energy {skill.energyCost}  ·  Cooldown {skill.cooldown:0.#} s";
        if(description!=null)description.text=skill!=null?skill.Description:"";
    }
}
