using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน (Basic Info)")]
    public string className;
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