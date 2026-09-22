using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน (Basic Info)")]
    public string className;
    public string classNameThai;

    // ชื่ออาชีพตามภาษาที่เลือกอยู่ ถ้ายังไม่ได้ใส่ชื่อไทยก็ใช้ชื่ออังกฤษไปก่อน
    public string DisplayName =>
        (LanguageSettings.IsThai && !string.IsNullOrEmpty(classNameThai)) ? classNameThai : className;

    public Sprite characterSprite;
    public GameObject characterPrefab;

    [Header("ค่าสถานะ (Stats)")]
    public float maxHealth = 8f;
    public int maxEnergy;
    public float moveSpeed;

    [Header("ชื่อทักษะ (Skill Names)")]
    public string skill1Name;
    public string skill2Name;

    [Header("ข้อมูลสกิล (Skills)")]
    public SkillData skillQ; 
    public SkillData skillE;

}