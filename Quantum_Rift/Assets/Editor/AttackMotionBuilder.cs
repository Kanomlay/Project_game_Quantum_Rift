using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ท่าตีตามชนิดอาวุธ (AttackMotion) ชุดละหนึ่งไฟล์ที่ Data/Weapon/Motion แล้วใส่ให้อาวุธทุกชิ้น
// - ค่าในไฟล์ตั้งจากค่าตั้งต้นเฉพาะตอนสร้างครั้งแรก ปรับจังหวะ/แรงสั่น/หยุดภาพใน Inspector แล้วสั่งซ้ำก็ไม่ทับ
// - ใส่ให้อาวุธที่ยังไม่มี motion หรือได้ชุดมาตรฐานไม่ตรงชนิด (ถ้าอยากให้อาวุธชิ้นไหนใช้ท่าต่างจากชนิดของมัน
//   ให้ทำไฟล์ท่าใหม่แยกไว้นอก 8 ชุดนี้ จะไม่ถูกเปลี่ยนกลับ)
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
            if (weapon == null) continue;
            var expected = motions[PresetFor(weapon)];
            // ใส่ให้ถ้ายังว่าง หรือถ้าเป็นชุดมาตรฐานคนละชนิดกับอาวุธ (เช่นค้อนที่เคยเป็นชนิดดาบ ได้ท่าดาบไปก่อน)
            // ท่าที่สร้างเองนอกชุดมาตรฐานไม่ถูกเปลี่ยน
            bool autoAssigned = weapon.motion != null && motions.ContainsValue(weapon.motion);
            if (weapon.motion == expected || (weapon.motion != null && !autoAssigned)) continue;
            weapon.motion = expected;
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
