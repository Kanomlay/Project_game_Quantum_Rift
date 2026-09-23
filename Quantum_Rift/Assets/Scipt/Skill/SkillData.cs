using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐานสกิล")]
    public string skillName;
    public Sprite skillIcon;
    public float cooldown;
    public int energyCost;
    public string Description;

    [Header("ภาพเอฟเฟกต์ (7 เฟรมจาก QuantumRift-Hero-Skills-v1)")]
    public Sprite[] effectFrames;
    public float effectScale = 1f;
    // จุดกึ่งกลางของภาพเอฟเฟกต์เทียบกับกลางช่อง 256 px (หน่วยก่อนย่อ) ภาพบางท่าวาดไว้ต่ำ/สูงกว่ากลางช่อง
    // SkillVfx เลื่อนภาพกลับให้กึ่งกลางเอฟเฟกต์ตรงจุดที่สั่งเกิด และหมุนรอบจุดนั้นพอดี
    public Vector2 effectOffset;

    // asset สกิลใช้ร่วมกันทุกเกม ห้ามเก็บสถานะระหว่างเล่นไว้ในนี้ ของที่ต้องจำให้สร้างเป็น GameObject/coroutine แทน
    public abstract void ActivateSkill(GameObject player);
}
