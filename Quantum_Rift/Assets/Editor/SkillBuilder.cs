using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// สกิลทั้ง 8 ท่าตามตาราง 1.1 (อาชีพละ 2 ท่า: Q = ทักษะที่ 1, E = ทักษะที่ 2)
// สร้าง asset สกิลใน Data/Character/Hero/Skill ใส่ภาพเอฟเฟกต์ 7 เฟรม + ไอคอนจากชุด QuantumRift-Hero-Skills-v1
// แล้วใส่ให้ CharacterData ของแต่ละอาชีพ
//
// เอกสารระบุแค่ชื่อสกิล ตัวเลข (ดาเมจ/คูลดาวน์/ระยะ) จึงเป็นค่าตั้งต้นที่ตั้งเอง ตั้งให้เฉพาะตอนสร้างครั้งแรก
// ปรับสมดุลใน Inspector แล้วสั่งซ้ำก็ไม่โดนทับ ส่วนชื่อ/ภาพ/ไอคอนตั้งใหม่ทุกครั้ง
// ทุกท่าใช้พลังงาน 0: เอกสารให้พลังงานไว้ใช้กับอาวุธ และยังไม่มีทางฟื้นพลังงานนอกจากขวดยา ใช้คูลดาวน์คุมแทน
public static class SkillBuilder
{
    const string ArtFolder = "Assets/image/hero/QuantumRift-Hero-Skills-v1/final";
    const string SkillFolder = "Assets/Data/Character/Hero/Skill";
    const string CharacterFolder = "Assets/Data/Character/Hero";
    const string DroneBulletPrefab = "Assets/Prefab/Weapon/Projectile/Rusty Bullet.prefab";

    sealed class Spec
    {
        public string Character;   // ชื่อไฟล์ CharacterData
        public bool IsQ;
        public string ClassFolder;
        public string Id;          // ชื่อโฟลเดอร์ภาพ/ไฟล์ asset
        public Type Type;
        public string Thai;
        public string Description;
        public float Cooldown;
        public float Scale;        // ขนาดเอฟเฟกต์ในฉาก (ภาพ 256 px ที่ 100 PPU)
        public Vector2 Offset;     // กึ่งกลางเอฟเฟกต์เทียบกลางช่อง วัดจากเฟรมหลักของแต่ละท่า (หน่วยก่อนย่อ)
        public Action<SkillData> Configure; // ตั้งค่าเฉพาะท่าตอนสร้างครั้งแรก
    }

    static readonly Spec[] Specs =
    {
        new Spec { Character = "นักรบ", IsQ = true, ClassFolder = "Warrior", Id = "ImpactDash", Type = typeof(ImpactDashSkill),
                   Thai = "พุ่งชนกระแทก", Description = "พุ่งไปทางเมาส์ อมตะระหว่างพุ่ง ศัตรูที่ขวางทางโดนดาเมจและกระเด็น",
                   Cooldown = 5f, Scale = 1.8f, Offset = new Vector2(0.02f, -0.335f),
                   Configure = skill => ((ImpactDashSkill)skill).hitRadius = 1.2f },
        new Spec { Character = "นักรบ", IsQ = false, ClassFolder = "Warrior", Id = "ReflectiveArmor", Type = typeof(ReflectiveArmorSkill),
                   Thai = "เกราะสะท้อนกลับ", Description = "กางเกราะกันดาเมจทั้งหมด 4 วินาที กันได้เมื่อไหร่สะท้อนแรงใส่ศัตรูรอบตัว",
                   Cooldown = 12f, Scale = 1.8f, Offset = new Vector2(-0.02f, 0.125f) },
        new Spec { Character = "นักธนู", IsQ = true, ClassFolder = "Archer", Id = "DimensionalArrow", Type = typeof(DimensionalArrowSkill),
                   Thai = "ลูกศรทะลวงมิติ", Description = "ยิงลูกศรพลังงานทะลุศัตรูทุกตัวและทะลุกำแพง",
                   Cooldown = 4f, Scale = 1f, Offset = new Vector2(0.01f, -0.1f) },
        new Spec { Character = "นักธนู", IsQ = false, ClassFolder = "Archer", Id = "ShadowStep", Type = typeof(ShadowStepSkill),
                   Thai = "ย่างก้าวเงา", Description = "หายตัวไปโผล่ทางเมาส์ ข้ามกำแพงในแมพได้แต่ไม่ออกนอกแมพ อมตะชั่วครู่",
                   // จุดอ้างอิงคือวงรีเงาบนพื้น (ล่างสุดของภาพ) เกิดที่เท้า เกลียวเงาจะลอยขึ้นคลุมตัว
                   Cooldown = 6f, Scale = 1.2f, Offset = new Vector2(-0.03f, -0.22f) },
        new Spec { Character = "นักประดิษฐ์", IsQ = true, ClassFolder = "Inventor", Id = "AssaultDrone", Type = typeof(AssaultDroneSkill),
                   Thai = "โดรนจู่โจม", Description = "ปล่อยโดรนลอยตามตัว ยิงศัตรูใกล้สุดให้อัตโนมัติ 10 วินาที",
                   Cooldown = 14f, Scale = 0.7f,
                   Configure = skill => ((AssaultDroneSkill)skill).bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DroneBulletPrefab) },
        new Spec { Character = "นักประดิษฐ์", IsQ = false, ClassFolder = "Inventor", Id = "ElectromagneticTrap", Type = typeof(ElectromagneticTrapSkill),
                   Thai = "กับดักแม่เหล็กไฟฟ้า", Description = "วางกับดักตรงเมาส์ ศัตรูเข้าใกล้แล้วโดนไฟฟ้าช็อตและติดสตัน",
                   Cooldown = 10f, Scale = 1.2f,
                   // วงไฟฟ้าเต็มในภาพกว้างราว 1.9 หน่วยที่ขนาด 1.2 ให้รัศมีโดนใกล้เคียงภาพ
                   Configure = skill => { var trap = (ElectromagneticTrapSkill)skill; trap.triggerRadius = 1f; trap.blastRadius = 1.4f; } },
        new Spec { Character = "มนุษย์กลายพันธุ์", IsQ = true, ClassFolder = "Mutant", Id = "AbsorbingFangs", Type = typeof(AbsorbingFangsSkill),
                   Thai = "คมเขี้ยวดูดกลืน", Description = "งับตรงหน้า ทำดาเมจแล้วดูดพลังชีวิตกลับตามจำนวนศัตรูที่โดน",
                   Cooldown = 6f, Scale = 1.6f, Offset = new Vector2(-0.045f, -0.115f) },
        new Spec { Character = "มนุษย์กลายพันธุ์", IsQ = false, ClassFolder = "Mutant", Id = "CellStimulation", Type = typeof(CellStimulationSkill),
                   Thai = "กระตุ้นเซลล์", Description = "ฟื้นพลังชีวิต 3 หน่วยใน 3 วินาที และวิ่งเร็วขึ้นชั่วคราว",
                   Cooldown = 15f, Scale = 1.6f, Offset = new Vector2(-0.03f, 0.085f) },
    };

    [MenuItem("Tools/Quantum Rift/Setup Skills (8 ท่า ตาราง 1.1)")]
    public static void SetupSkills()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งสกิล");

        foreach (var spec in Specs)
        {
            var skill = BuildSkill(spec);

            string characterPath = $"{CharacterFolder}/{spec.Character}.asset";
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(characterPath);
            if (character == null)
            {
                Debug.LogWarning($"ไม่เจอ {characterPath} ข้ามการใส่สกิล {spec.Thai}");
                continue;
            }

            if (spec.IsQ) { character.skillQ = skill; character.skill1Name = spec.Thai; }
            else { character.skillE = skill; character.skill2Name = spec.Thai; }
            EditorUtility.SetDirty(character);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งสกิลครบ 8 ท่า ใส่ให้ 4 อาชีพแล้ว (Q = ทักษะที่ 1, E = ทักษะที่ 2)");
    }

    static SkillData BuildSkill(Spec spec)
    {
        string path = $"{SkillFolder}/{spec.Id}.asset";
        var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        bool isNew = skill == null;
        if (skill != null && skill.GetType() != spec.Type)
            throw new InvalidOperationException($"{path} เป็นสกิลคนละชนิด ({skill.GetType().Name}) ลบไฟล์นั้นก่อนแล้วสั่งใหม่");

        if (isNew)
        {
            skill = (SkillData)ScriptableObject.CreateInstance(spec.Type);
            AssetDatabase.CreateAsset(skill, path);
            skill.cooldown = spec.Cooldown;
            skill.energyCost = 0;
            skill.effectScale = spec.Scale;
            spec.Configure?.Invoke(skill);
        }

        skill.skillName = spec.Thai;
        skill.effectOffset = spec.Offset; // ได้จากภาพ ตั้งใหม่ทุกครั้ง
        skill.Description = spec.Description;
        skill.skillIcon = Load<Sprite>($"{ArtFolder}/Icons/{spec.Id}-Icon.png");
        skill.effectFrames = Enumerable.Range(1, 7)
            .Select(i => Load<Sprite>($"{ArtFolder}/{spec.ClassFolder}/{spec.Id}/{spec.Id}_{i:00}.png"))
            .ToArray();

        EditorUtility.SetDirty(skill);
        return skill;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
