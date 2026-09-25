using System.Collections.Generic;
using UnityEngine;

// ท่าตีของอาวุธแต่ละชนิด (ทำด้วยการขยับ/หมุนภาพอาวุธ ไม่ต้องมี sprite เพิ่ม) และความรู้สึกตอนตีโดน
// ท่าแบ่ง 3 ช่วง: ง้าง (ช้า) → ฟาด (เร็วมาก ตามกราฟ strikeCurve) → ค้าง/ตามแรง แล้วคืนท่า
// ถ้าเวลาท่ารวมเกินช่วงห่างระหว่างการโจมตี (1 / attackSpeed) จะถูกย่อลงตามสัดส่วนเอง
// ตั้งไว้ที่ WeaponData.motion ชนิดละหนึ่งชุด ไม่ใส่ก็ใช้ค่าตั้งต้นตามชนิดอาวุธ (Default)
[CreateAssetMenu(fileName = "AttackMotion", menuName = "Game Data/Attack Motion")]
public class AttackMotion : ScriptableObject
{
    public enum Style
    {
        Swing,  // ดาบ มีดสั้น กระบอง: ง้างถอย → กวาดโค้ง (สลับบน/ล่างตามคอมโบ) → เลยปลายวงนิดแล้วค่อยกลับ
        Smash,  // ค้อน: ยกขึ้นเหนือหัว → ทุบลงเร่งแรง → ค้างที่พื้น → คืนท่า
        Thrust, // หอก: ดึงกลับ → แทงพุ่ง → ค้าง → ดึงกลับ
        Strike, // กรงเล็บ/มีดคู่: ตะปบสลับมือ (ท่าอยู่ใน DualClawWeapon)
        Shoot   // ปืน/ธนู: แรงถีบอาวุธถอยกลับ
    }

    public enum Preset { Sword, Dagger, Hammer, Spear, Claw, Gun, HeavyGun, Bow }

    public Style style;

    [Header("จังหวะท่า (วินาที)")]
    public float windupTime = 0.07f;
    public float strikeTime = 0.09f;
    public float holdTime = 0.06f;
    public float recoverTime = 0.12f; // Smash / Thrust คืนท่าเอง (Swing ค้างรอคอมโบต่อ)
    [Tooltip("ความคืบหน้าท่าฟาดตามเวลา แกน X = เวลา 0–1, แกน Y = ระยะ 0–1 (โค้งชันตอนต้น = พุ่งออกแรง)")]
    public AnimationCurve strikeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("ท่าฟัน / ทุบ (องศา)")]
    public float windupAngle = 25f;    // Swing: ง้างเลยจุดเริ่มวงไปอีกเท่านี้ / Smash: ยกค้อนขึ้นถึงมุมนี้
    public float followThrough = 12f;  // Swing: ฟาดเลยปลายวงไปนิดแล้วค่อยกลับ
    public float smashEndAngle = -25f; // Smash: ทุบลงถึงมุมนี้
    [Range(0f, 1f)] public float hitFrom = 0f; // เริ่มตรวจโดนเมื่อท่าฟาดไปได้สัดส่วนนี้ (ค้อนโดนตอนใกล้ถึงพื้น)

    [Header("ท่าแทง (สัดส่วนความยาวอาวุธ)")]
    public float pullBack = 0.18f;
    public float thrustReach = 0.55f;

    [Header("ท่ายิง")]
    public float recoilDistance = 0.12f; // อาวุธถอยกลับในมือ (หน่วยของ WeaponHolder)
    public float recoilTime = 0.1f;

    [Header("ตัวละครขยับตาม (ติดลบ = ถอยหลังจากแรงถีบ)")]
    public float stepDistance = 0.1f;

    [Header("ยืด/หดภาพอาวุธตอนฟาด")]
    public float stretch = 0.15f;

    [Header("เงาภาพค้างตามทางฟัน (0 = ไม่มี)")]
    public float trailInterval = 0.015f;
    public float trailLife = 0.12f;
    [Range(0f, 1f)] public float trailAlpha = 0.45f;

    [Header("ตอนตีโดน")]
    public float hitStop = 0.045f;    // เกมค้างชั่วขณะ (วินาทีจริง)
    public float hitShake = 0.07f;    // กล้องสั่น
    public float impactShake;         // สั่นทุกครั้งแม้ไม่โดนใคร (ค้อนทุบพื้น, ปืนยิง)
    public int sparkCount = 6;
    public Color sparkColor = new Color(1f, 0.93f, 0.7f);

    // ---------- ค่าตั้งต้น ----------

    public void ApplyPreset(Preset preset)
    {
        switch (preset)
        {
            case Preset.Sword:
                Set(Style.Swing, 0.07f, 0.09f, 0.06f, 0.12f);
                windupAngle = 25f; followThrough = 12f; stepDistance = 0.12f; stretch = 0.18f;
                trailInterval = 0.015f; trailLife = 0.12f; hitStop = 0.045f; hitShake = 0.07f; sparkCount = 6;
                break;
            case Preset.Dagger:
                Set(Style.Swing, 0.03f, 0.06f, 0.03f, 0.08f);
                windupAngle = 12f; followThrough = 8f; stepDistance = 0.08f; stretch = 0.12f;
                trailInterval = 0.015f; trailLife = 0.08f; hitStop = 0.03f; hitShake = 0.04f; sparkCount = 4;
                break;
            case Preset.Hammer:
                Set(Style.Smash, 0.16f, 0.08f, 0.1f, 0.18f);
                strikeCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2.5f, 2.5f)); // เร่งแรงช่วงท้าย
                windupAngle = 110f; smashEndAngle = -25f; hitFrom = 0.45f; stepDistance = 0.1f; stretch = 0.25f;
                trailInterval = 0.015f; trailLife = 0.14f; hitStop = 0.09f; hitShake = 0.16f; impactShake = 0.12f;
                sparkCount = 10; sparkColor = new Color(0.86f, 0.8f, 0.7f);
                break;
            case Preset.Spear:
                Set(Style.Thrust, 0.07f, 0.07f, 0.05f, 0.12f);
                pullBack = 0.18f; thrustReach = 0.55f; stepDistance = 0.22f; stretch = 0.2f;
                trailInterval = 0.015f; trailLife = 0.1f; hitStop = 0.05f; hitShake = 0.07f; sparkCount = 5;
                break;
            case Preset.Claw:
                Set(Style.Strike, 0f, 0.07f, 0f, 0.12f);
                stepDistance = 0.06f; stretch = 0f; trailInterval = 0f;
                hitStop = 0.035f; hitShake = 0.05f; sparkCount = 4;
                break;
            case Preset.Gun:
                Set(Style.Shoot, 0f, 0f, 0f, 0f);
                recoilDistance = 0.12f; recoilTime = 0.1f; stepDistance = -0.03f; stretch = 0f; trailInterval = 0f;
                hitStop = 0f; hitShake = 0f; impactShake = 0.03f; sparkCount = 4;
                break;
            case Preset.HeavyGun:
                Set(Style.Shoot, 0f, 0f, 0f, 0f);
                recoilDistance = 0.25f; recoilTime = 0.16f; stepDistance = -0.1f; stretch = 0f; trailInterval = 0f;
                hitStop = 0f; hitShake = 0f; impactShake = 0.09f; sparkCount = 6;
                break;
            case Preset.Bow:
                Set(Style.Shoot, 0f, 0f, 0f, 0f);
                recoilDistance = 0.06f; recoilTime = 0.08f; stepDistance = 0f; stretch = 0f; trailInterval = 0f;
                hitStop = 0f; hitShake = 0f; impactShake = 0.02f; sparkCount = 4;
                break;
        }
    }

    void Set(Style s, float windup, float strike, float hold, float recover)
    {
        style = s;
        windupTime = windup;
        strikeTime = strike;
        holdTime = hold;
        recoverTime = recover;
        // ค่าเริ่มของกราฟ: พุ่งออกแรงตอนต้นแล้วชะลอ (ease-out)
        strikeCurve = new AnimationCurve(new Keyframe(0f, 0f, 2.5f, 2.5f), new Keyframe(1f, 1f, 0f, 0f));
    }

    public static Preset PresetFor(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Hammer: return Preset.Hammer;
            case WeaponType.Spear: return Preset.Spear;
            case WeaponType.Claw: return Preset.Claw;
            case WeaponType.Gun: return Preset.Gun;
            case WeaponType.Bow: return Preset.Bow;
            default: return Preset.Sword;
        }
    }

    static readonly Dictionary<Preset, AttackMotion> defaults = new Dictionary<Preset, AttackMotion>();

    // ใช้ตอน WeaponData ยังไม่ได้ใส่ motion
    public static AttackMotion Default(WeaponType type)
    {
        var preset = PresetFor(type);
        if (!defaults.TryGetValue(preset, out var motion) || motion == null)
        {
            motion = CreateInstance<AttackMotion>();
            motion.name = "Default " + preset;
            motion.hideFlags = HideFlags.DontSave;
            motion.ApplyPreset(preset);
            defaults[preset] = motion;
        }
        return motion;
    }
}
