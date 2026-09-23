using UnityEngine;

// กรงเล็บคู่ (มือบน/มือล่าง ขนาบแนวเล็ง) ตะปบสลับมือทีละข้างทุกครั้งที่กดโจมตี
//
// สคริปต์นี้ดูแลแค่ท่าทาง: กรงเล็บข้างที่ตีจะพุ่งไปข้างหน้า เฉียงเข้าหาแนวกลาง แล้วหดกลับ
// การตีโดน/คลื่นฟันอยู่ใน WeaponController เหมือนดาบ (อ่านตำแหน่งปลายเล็บจาก TipOf)
// ตำแหน่งทั้งหมดเป็นหน่วยของ prefab ก่อนย่อ ถูกพลิกซ้าย/ขวาตาม WeaponHolder ให้เอง
public sealed class DualClawWeapon : MonoBehaviour
{
    public Transform upperClaw;
    public Transform lowerClaw;
    public Transform upperTip;  // จุดตีโดนของกรงเล็บมือบน
    public Transform lowerTip;

    [Header("ระยะตีโดน")]
    [Tooltip("รัศมีตีโดนรอบปลายเล็บ หน่วยก่อนย่อ prefab ย่อ/ขยาย prefab แล้ว hitbox ย่อ/ขยายตามภาพเอง (ใช้แทน Attack Range ของ WeaponData)")]
    public float hitRadius = 0.8f;

    // รัศมีจริงในฉาก รวมการย่อของ prefab, WeaponHolder และตัวละครแล้ว
    public float WorldHitRadius => hitRadius * Mathf.Abs(transform.lossyScale.x);

    [Header("ท่าตะปบ")]
    public float lungeDistance = 0.45f;  // พุ่งไปข้างหน้า
    public float inwardDistance = 0.12f; // เบี่ยงเข้าหาแนวเล็งตอนพุ่ง
    public float swipeAngle = 25f;       // ปลายเล็บกวาดเข้าหาแนวกลาง ให้ดูเป็นการตะปบ ไม่ใช่แค่แทง
    public float strikeTime = 0.07f;
    public float recoverTime = 0.12f;

    private Vector3 upperRest;
    private Vector3 lowerRest;
    private bool restSaved;
    private bool upperNext = true;

    void Awake() => SaveRest();

    // มือที่จะตีครั้งนี้ แล้วสลับไว้ให้ครั้งหน้าเป็นอีกข้าง
    public bool NextHand()
    {
        bool upper = upperNext;
        upperNext = !upperNext;
        return upper;
    }

    public Transform TipOf(bool upper) => upper ? upperTip : lowerTip;

    public void ResetPose()
    {
        Pose(true, 0f);
        Pose(false, 0f);
    }

    // amount 0 = ท่าพัก, 1 = พุ่งสุด
    public void Pose(bool upper, float amount)
    {
        SaveRest();
        Transform claw = upper ? upperClaw : lowerClaw;
        if (claw == null) return;

        float t = Mathf.Clamp01(amount);
        float eased = 1f - (1f - t) * (1f - t); // พุ่งออกเร็วแล้วค่อยช้าลงตอนใกล้สุด
        float side = upper ? 1f : -1f;          // มือบนเบี่ยงลง มือล่างเบี่ยงขึ้น

        claw.localPosition = (upper ? upperRest : lowerRest)
                           + new Vector3(lungeDistance * eased, -side * inwardDistance * eased, 0f);
        claw.localRotation = Quaternion.Euler(0f, 0f, -side * swipeAngle * eased);
    }

    private void SaveRest()
    {
        if (restSaved || upperClaw == null || lowerClaw == null) return;
        upperRest = upperClaw.localPosition;
        lowerRest = lowerClaw.localPosition;
        restSaved = true;
    }
}
