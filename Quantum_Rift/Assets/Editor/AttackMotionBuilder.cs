using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ท่าตีตามชนิดอาวุธ (AttackMotion) ชุดละหนึ่งไฟล์ที่ Data/Weapon/Motion แล้วใส่ให้อาวุธทุกชิ้น
// - ค่าในไฟล์ตั้งจากค่าตั้งต้นเฉพาะตอนสร้างครั้งแรก ปรับจังหวะ/แรงสั่น/หยุดภาพใน Inspector แล้วสั่งซ้ำก็ไม่ทับ
// - ใส่ให้เฉพาะอาวุธที่ยังไม่มี motion (เปลี่ยนเป็นชุดอื่นเองแล้วไม่ถูกเปลี่ยนกลับ)
public static class AttackMotionBuilder
{
    const string MotionFolder = "Assets/Data/Weapon/Motion";

    // อาวุธที่ใช้ท่าต่างจากค่าตั้งต้นของชนิด
    static AttackMotion.Preset PresetFor(WeaponData weapon)
    {
        switch (weapon.name)
        {
            case "Rusty Dagger": return AttackMotion.Preset.Dagger;
            case "Old Rifle":
            case "Rocket Launcher": return AttackMotion.Preset.HeavyGun;
            default: return AttackMotion.PresetFor(weapon.weaponType);
        }
    }

    [MenuItem("Tools/Quantum Rift/Setup Weapon/Attack Motions (ท่าตีและความรู้สึกตอนโดน)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งท่าตี");
        int assigned = AssignAll();
        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้งท่าตี {Enum.GetValues(typeof(AttackMotion.Preset)).Length} ชุดที่ {MotionFolder} ใส่ให้อาวุธเพิ่ม {assigned} ชิ้น");
    }

    // คืนจำนวนอาวุธที่เพิ่งได้ใส่ท่า (WeaponCollectionBuilder เรียกต่อท้ายการสร้างอาวุธด้วย)
    internal static int AssignAll()
    {
        StarterWeaponBuilder.EnsureFolder(MotionFolder);
        var motions = Enum.GetValues(typeof(AttackMotion.Preset)).Cast<AttackMotion.Preset>()
                          .ToDictionary(p => p, LoadOrCreate);

        int assigned = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:WeaponData"))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon == null || weapon.motion != null) continue;
            weapon.motion = motions[PresetFor(weapon)];
            EditorUtility.SetDirty(weapon);
            assigned++;
        }
        return assigned;
    }

    static AttackMotion LoadOrCreate(AttackMotion.Preset preset)
    {
        string path = $"{MotionFolder}/{preset}.asset";
        var motion = AssetDatabase.LoadAssetAtPath<AttackMotion>(path);
        if (motion != null) return motion;

        motion = ScriptableObject.CreateInstance<AttackMotion>();
        motion.ApplyPreset(preset);
        AssetDatabase.CreateAsset(motion, path);
        return motion;
    }
}
