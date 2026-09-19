using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เปลี่ยน prefab ตัวละครชุด Heroes-BlondeStyle-NoHands-v2 (ที่มีแค่ภาพกับ Animator) ให้เป็นตัวที่เล่นได้จริง:
// ใส่ Rigidbody2D, Collider, PlayerMovement, PlayerStats, WeaponHolder + WeaponController และ Tag Player
// แล้วผูกเข้ากับ CharacterData ของอาชีพนั้นพร้อมตั้งค่าสถานะตามตาราง 1.1 ในเอกสารขอบเขต
//
// ค่าที่ไม่ได้อยู่ในเอกสาร (ความเร็วท่าฟัน, layer ศัตรู, sorting layer ฯลฯ) ลอกมาจาก Warrior_0 ที่เล่นได้อยู่แล้ว
// สั่งซ้ำได้: collider, ตำแหน่ง/ขนาด WeaponHolder และอาวุธเริ่มต้น ตั้งให้เฉพาะครั้งแรก ปรับเองแล้วไม่โดนทับ
public static class HeroSetupBuilder
{
    const string ReferenceHeroPath = "Assets/Prefab/Hero/Warrior_0.prefab";
    const string HeroFolder = "Assets/Prefab/Hero/Heroes-BlondeStyle-NoHands-v2";
    const string CharacterFolder = "Assets/Data/Character/Hero";
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string CharacterBoxPrefabPath = "Assets/Prefab/Template/CharacterBox_Template.prefab";

    // วัดจากภาพจริง: ลำตัวกว้าง ~1.6 สูง ~2.0 หน่วย (ก่อนย่อ) และ pivot อยู่ที่เท้า ทั้งสี่ตัวใช้โครงร่างเดียวกัน
    // collider เอาแค่ช่วงลำตัวถึงเท้า ไม่รวมผม/ผ้าคลุม/ปีก หัวจะได้ซ้อนกำแพงด้านบนได้แบบเกม top-down และเดินผ่านประตูแคบได้
    static readonly Vector2 BodyColliderSize = new Vector2(1.1f, 1.5f);
    static readonly Vector2 BodyColliderOffset = new Vector2(0f, 0.75f);
    static readonly Vector3 WeaponHolderPosition = new Vector3(0f, 0.7f, 0f); // ระดับเอว

    class HeroSpec
    {
        public string Prefab, Character, ClassName, Skill1, Skill2;
        public float MaxHealth, MoveSpeed;
        public int MaxEnergy;
    }

    // ตาราง 1.1 อาชีพ เรียงตามลำดับในเอกสาร ซึ่งเป็นลำดับที่จะขึ้นในหน้าเลือกตัวละครด้วย
    // จับคู่ภาพตามหน้าตา: Knight = นักรบ, Blonde (หูเอลฟ์) = นักธนู, BlueHair (ชุดช่าง) = นักประดิษฐ์, WhiteHair (ปีกค้างคาว) = มนุษย์กลายพันธุ์
    static readonly HeroSpec[] Heroes =
    {
        new HeroSpec { Prefab = "Hero01_Knight", Character = "นักรบ", ClassName = "Warrior",
            MaxHealth = 8f, MaxEnergy = 150, MoveSpeed = 100f, Skill1 = "พุ่งชนกระแทก", Skill2 = "เกราะสะท้อนกลับ" },
        new HeroSpec { Prefab = "Hero02_Blonde", Character = "นักธนู", ClassName = "Archer",
            MaxHealth = 6f, MaxEnergy = 170, MoveSpeed = 100f, Skill1 = "ลูกศรทะลวงมิติ", Skill2 = "ย่างก้าวเงา" },
        new HeroSpec { Prefab = "Hero03_BlueHair", Character = "นักประดิษฐ์", ClassName = "Engineer",
            MaxHealth = 7f, MaxEnergy = 100, MoveSpeed = 100f, Skill1 = "โดรนจู่โจม", Skill2 = "กับดักแม่เหล็กไฟฟ้า" },
        new HeroSpec { Prefab = "Hero04_WhiteHair", Character = "มนุษย์กลายพันธุ์", ClassName = "Mutant",
            MaxHealth = 9f, MaxEnergy = 100, MoveSpeed = 75f, Skill1 = "คมเขี้ยวดูดกลืน", Skill2 = "กระตุ้นเซลล์" },
    };

    // ติดตั้งครบทั้ง 4 อาชีพ แล้วใส่ทุกตัวลงหน้าเลือกตัวละครในฉาก MainMenu
    [MenuItem("Tools/Quantum Rift/Setup Hero/All 4 Classes")]
    public static void SetupAll()
    {
        GuardEditorState(needsMainMenu: true);

        var characters = Heroes.Select(SetupHero).ToList();
        WireCharacterBox();
        AssetDatabase.SaveAssets();

        RegisterInCharacterSelect(characters);
    }

    [MenuItem("Tools/Quantum Rift/Setup Hero/Hero01 Knight (นักรบ)")]
    public static void SetupKnight() => SetupSingle(0);

    [MenuItem("Tools/Quantum Rift/Setup Hero/Hero02 Blonde (นักธนู)")]
    public static void SetupBlonde() => SetupSingle(1);

    [MenuItem("Tools/Quantum Rift/Setup Hero/Hero03 BlueHair (นักประดิษฐ์)")]
    public static void SetupBlueHair() => SetupSingle(2);

    [MenuItem("Tools/Quantum Rift/Setup Hero/Hero04 WhiteHair (มนุษย์กลายพันธุ์)")]
    public static void SetupWhiteHair() => SetupSingle(3);

    // กล่องตัวละครมีช่องสกิลสองช่องที่ยังโชว์คำว่า Button อยู่ ต่อให้ไปแสดงชื่อทักษะประจำอาชีพแทน
    [MenuItem("Tools/Quantum Rift/Setup Hero/Wire Character Box")]
    public static void WireCharacterBox()
    {
        var root = PrefabUtility.LoadPrefabContents(CharacterBoxPrefabPath);
        try
        {
            var box = root.GetComponent<CharacterBoxUI>();
            if (box == null)
            {
                Debug.LogWarning($"{CharacterBoxPrefabPath} ไม่มี CharacterBoxUI ข้ามไป");
                return;
            }

            box.skill1Text = FindLabel(root.transform, "Skill/SkillQ");
            box.skill2Text = FindLabel(root.transform, "Skill/SkillE");

            PrefabUtility.SaveAsPrefabAsset(root, CharacterBoxPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static TextMeshProUGUI FindLabel(Transform root, string path)
    {
        var slot = root.Find(path);
        if (slot == null)
        {
            Debug.LogWarning($"ไม่เจอช่องสกิล {path} ในกล่องตัวละคร");
            return null;
        }
        return slot.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    static void SetupSingle(int index)
    {
        GuardEditorState(needsMainMenu: false);
        SetupHero(Heroes[index]);
        AssetDatabase.SaveAssets();
    }

    static void GuardEditorState(bool needsMainMenu)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งตัวละคร");

        // เช็คตั้งแต่ต้น จะได้ไม่ติดตั้งไปครึ่งทางแล้วค่อยมาเจอว่าเปิดฉากเมนูไม่ได้
        var scene = SceneManager.GetActiveScene();
        if (needsMainMenu && scene.path != MainMenuScenePath && scene.isDirty)
            throw new InvalidOperationException("เซฟฉากที่เปิดอยู่ก่อน แล้วค่อยสั่งติดตั้งตัวละคร");
    }

    static CharacterData SetupHero(HeroSpec spec)
    {
        string heroPath = $"{HeroFolder}/{spec.Prefab}.prefab";

        var reference = Load<GameObject>(ReferenceHeroPath);
        var refRenderer = reference.GetComponent<SpriteRenderer>();
        var refBody = reference.GetComponent<Rigidbody2D>();
        var refMovement = reference.GetComponent<PlayerMovement>();
        var refStats = reference.GetComponent<PlayerStats>();
        var refWeapon = reference.GetComponentInChildren<WeaponController>(true);
        if (refRenderer == null || refBody == null || refMovement == null || refStats == null || refWeapon == null)
            throw new InvalidOperationException($"{ReferenceHeroPath} ไม่ครบ ต้องมี SpriteRenderer, Rigidbody2D, PlayerMovement, PlayerStats และ WeaponController");

        Load<GameObject>(heroPath); // เช็คว่ามีไฟล์จริงก่อนเปิดแก้

        var root = PrefabUtility.LoadPrefabContents(heroPath);
        Sprite heroSprite;
        try
        {
            root.tag = "Player"; // MapPortal, CameraFollow และมอนสเตอร์หาตัวผู้เล่นจาก Tag นี้

            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null) throw new InvalidOperationException($"{heroPath} ไม่มี SpriteRenderer ที่ตัวหลัก");
            // prefab ชุดนี้มาเป็น sorting layer Default จะถูกพื้นแมพทับจนมองไม่เห็น
            renderer.sortingLayerID = refRenderer.sortingLayerID;
            heroSprite = renderer.sprite;

            var body = Ensure<Rigidbody2D>(root, out _);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f; // top-down ไม่มีแรงโน้มถ่วง
            body.constraints = refBody.constraints;
            body.collisionDetectionMode = refBody.collisionDetectionMode;
            body.interpolation = refBody.interpolation;
            body.mass = refBody.mass;

            var box = Ensure<BoxCollider2D>(root, out bool newCollider);
            box.isTrigger = false;
            if (newCollider)
            {
                box.size = BodyColliderSize;
                box.offset = BodyColliderOffset;
            }

            var movement = Ensure<PlayerMovement>(root, out _);
            movement.baseSpeed = refMovement.baseSpeed;
            movement.runMultiplier = refMovement.runMultiplier;

            var weapon = SetupWeaponHolder(root, reference, refWeapon);

            var stats = Ensure<PlayerStats>(root, out _);
            stats.maxHP = spec.MaxHealth;
            stats.maxEnergy = spec.MaxEnergy;
            stats.iframeDuration = refStats.iframeDuration;
            stats.weaponController = weapon;
            // อาวุธประจำอาชีพจริงยังไม่มี (กรงเล็บ = A3, ปืน/ธนู = B6) ใส่ดาบสนิมไว้ชั่วคราวให้ทุกตัวตีได้ก่อน
            // ใส่ให้เฉพาะตอนยังว่าง พอเปลี่ยนเป็นอาวุธจริงแล้วสั่งซ้ำจะได้ไม่ถูกเปลี่ยนกลับเป็นดาบ
            if (stats.weapon1 == null) stats.weapon1 = refStats.weapon1;

            PrefabUtility.SaveAsPrefabAsset(root, heroPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // หน้าเลือกตัวละครกับ MapManager อ่านจาก CharacterData ต้องชี้มาที่ prefab ใหม่ถึงจะได้ตัวนี้ในเกม
        var data = LoadOrCreateCharacter(spec.Character);
        data.className = spec.ClassName;
        data.classNameThai = spec.Character;
        data.characterPrefab = Load<GameObject>(heroPath);
        data.characterSprite = heroSprite;
        data.maxHealth = spec.MaxHealth;
        data.maxEnergy = spec.MaxEnergy;
        data.moveSpeed = spec.MoveSpeed;
        data.skill1Name = spec.Skill1;
        data.skill2Name = spec.Skill2;
        EditorUtility.SetDirty(data);

        Debug.Log($"ติดตั้ง {spec.Prefab} เป็น {spec.Character} ({spec.ClassName}) เรียบร้อย " +
                  $"HP {spec.MaxHealth}, Energy {spec.MaxEnergy}, Speed {spec.MoveSpeed}");
        return data;
    }

    static WeaponController SetupWeaponHolder(GameObject root, GameObject reference, WeaponController refWeapon)
    {
        var holder = root.transform.Find("WeaponHolder");
        if (holder == null)
        {
            holder = new GameObject("WeaponHolder").transform;
            holder.SetParent(root.transform, false);
            holder.localPosition = WeaponHolderPosition;

            // ตัวละครชุดนี้ถูกย่อไว้ 0.5 ส่วน Warrior_0 ขยาย 1.2 ถ้าไม่ชดเชยดาบจะเล็กลงเหลือไม่ถึงครึ่ง
            // จึงคิด scale ให้ดาบขนาดบนจอเท่ากับของ Warrior_0 พอดี
            float refWorldScale = reference.transform.localScale.x * refWeapon.transform.localScale.x;
            float scale = refWorldScale / Mathf.Max(0.0001f, root.transform.localScale.x);
            holder.localScale = new Vector3(scale, scale, 1f);
        }

        var weapon = Ensure<WeaponController>(holder.gameObject, out _);
        weapon.enemyLayers = refWeapon.enemyLayers;
        weapon.swingDuration = refWeapon.swingDuration;
        weapon.restReturnDuration = refWeapon.restReturnDuration;
        weapon.slashEffectDistance = refWeapon.slashEffectDistance;
        weapon.slashEffectScale = refWeapon.slashEffectScale;
        return weapon;
    }

    // นักประดิษฐ์กับมนุษย์กลายพันธุ์ยังไม่เคยมี CharacterData เลย ต้องสร้างใหม่
    static CharacterData LoadOrCreateCharacter(string name)
    {
        string path = $"{CharacterFolder}/{name}.asset";
        var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<CharacterData>();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    // หน้าเลือกตัวละครสร้างกล่องตามลิสต์ allCharacters ถ้าไม่ใส่ตัวใหม่ลงไปจะเลือกไม่ได้
    static void RegisterInCharacterSelect(List<CharacterData> characters)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MainMenuScenePath)
            scene = EditorSceneManager.OpenScene(MainMenuScenePath);

        var selection = UnityEngine.Object.FindObjectOfType<CharacterSelectionManager>(true);
        if (selection == null)
        {
            Debug.LogWarning($"ไม่เจอ CharacterSelectionManager ใน {MainMenuScenePath} ต้องลากตัวละครใส่ All Characters เอง");
            return;
        }

        Undo.RecordObject(selection, "Register Heroes");
        selection.allCharacters = characters;
        EditorUtility.SetDirty(selection);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"ใส่ตัวละคร {characters.Count} อาชีพลงหน้าเลือกตัวละครแล้ว กด Ctrl+S เพื่อบันทึกฉาก MainMenu");
    }

    static T Ensure<T>(GameObject go, out bool added) where T : Component
    {
        var component = go.GetComponent<T>();
        added = component == null;
        return added ? go.AddComponent<T>() : component;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
