using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐานสกิล")]
    public string skillName;
    public Sprite skillIcon;
    public float cooldown;
    public int energyCost;

    public abstract void ActivateSkill(GameObject player); 
}