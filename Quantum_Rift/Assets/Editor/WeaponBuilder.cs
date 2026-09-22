using System;
using UnityEditor;
using UnityEngine;

// สร้างอาวุธประจำอาชีพให้ครบชุด: ตั้งค่าภาพ, สร้าง prefab (มี AttackPoint), สร้าง WeaponData ตามตาราง 1.2
// แล้วใส่เป็นอาวุธเริ่มต้นของตัวละครอาชีพนั้น
// ลอกค่าที่เอกสารไม่ได้ระบุ (sorting layer, คลื่นดาบ) มาจากดาบสนิมที่ใช้งานได้อยู่แล้ว
// สั่งซ้ำได้: ขนาด prefab, ตำแหน่ง AttackPoint, ระยะ/มุมโจมตี ตั้งให้เฉพาะครั้งแรก ปรับเองแล้วไม่โดนทับ
public static class WeaponBuilder
{
    const string ReferenceWeaponPath = "Assets/Data/Weapon/Rusty Sword.asset";

    // ภาพกรงเล็บมาเป็นชื่อไฟล์สุ่ม เปลี่ยนชื่อให้หาเจอง่าย (MoveAsset เก็บ GUID เดิมไว้ ของที่อ้างถึงไม่หลุด)
    const string ClawImageOldPath = "Assets/image/Weapon/exec-25bc9c79-cd5b-41ea-8112-f7ba3fa3032f.png";
    const string ClawImagePath = "Assets/image/Weapon/Rusty Claw.png";
    const string ClawPrefabPath = "Assets/Prefab/Weapon/Rusty Claw.prefab";
    const string ClawDataPath = "Assets/Data/Weapon/Rusty Claw.asset";
    const string MutantPrefabPath = "Assets/Prefab/Hero/Heroes-BlondeStyle-NoHands-v2/Hero04_WhiteHair.prefab";

    // วัดจากภาพ 1672x941: กำปั้นทองแดงอยู่ราว x=237, y=440 (นับจากบนซ้าย) ใช้เป็นจุดหมุนเหมือนด้ามดาบ
    static readonly Vector2 ClawPivot = new Vector2(237f / 1672f, 1f - 440f / 941f);
    const float PixelsPerUnit = 64f; // เท่ากับดาบสนิม ขนาดภาพจะได้เทียบกันได้ตรงๆ
    // ความยาวกรงเล็บราว 65% ของดาบ ดูเป็นอาวุธติดกำปั้นมากกว่าอาวุธยาว
    const float ClawScale = 0.06f;
    // ปลายเล็บอยู่ราว x=1590 px ถอยเข้ามานิดนึงเหมือน AttackPoint ของดาบ (หน่วยเป็นหน่วยของภาพก่อนย่อ)
    static readonly Vector3 ClawAttackPoint = new Vector3((1480f - 237f) / PixelsPerUnit, 0f, 0f);

    [MenuItem("Tools/Quantum Rift/Setup Weapon/Rusty Claw (มนุษย์กลายพันธุ์)")]
    public static void SetupRustyClaw()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้างอาวุธ");

        var reference = Load<WeaponData>(ReferenceWeaponPath);
        var refRenderer = reference.weaponPrefab != null ? reference.weaponPrefab.GetComponent<SpriteRenderer>() : null;
        if (refRenderer == null)
            throw new InvalidOperationException($"prefab ของ {ReferenceWeaponPath} ไม่มี SpriteRenderer ให้ลอก sorting layer");

        RenameClawImage();
        var sprite = ImportWeaponSprite(ClawImagePath, ClawPivot);
        var prefab = BuildWeaponPrefab(ClawPrefabPath, "Rusty Claw", sprite, refRenderer, ClawScale, ClawAttackPoint);

        // ตาราง 1.2: มนุษย์กลายพันธุ์ กรงเล็บขึ้นสนิม ดาเมจ 5, 1.8 ครั้ง/วินาที, ใช้พลังงาน 0
        var data = LoadOrCreate<WeaponData>(ClawDataPath, out bool isNew);
        data.weaponName = "Rusty Claw";
        data.weaponIcon = sprite;
        data.weaponPrefab = prefab;
        data.weaponType = WeaponType.Claw;
        data.rarity = WeaponRarity.Starter;
        data.attackDamage = 5f;
        data.attackSpeed = 1.8f;
        data.energyCost = 0;
        if (isNew)
        {
            // เอกสารไม่ได้กำหนดระยะ กรงเล็บสั้นกว่าดาบจึงให้ระยะตีโดนแคบลงนิดหน่อย
            data.attackRange = reference.attackRange * 0.8f;
            data.attackAngle = reference.attackAngle;
        }
        if (data.slashEffectPrefab == null) data.slashEffectPrefab = reference.slashEffectPrefab;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        EquipAsStarter(MutantPrefabPath, data);

        Debug.Log("สร้างกรงเล็บสนิม (Rusty Claw) แล้วใส่ให้มนุษย์กลายพันธุ์เรียบร้อย");
    }

    static void RenameClawImage()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(ClawImagePath) != null) return;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(ClawImageOldPath) == null)
            throw new InvalidOperationException($"ไม่เจอภาพกรงเล็บทั้งที่ {ClawImagePath} และ {ClawImageOldPath}");

        string error = AssetDatabase.MoveAsset(ClawImageOldPath, ClawImagePath);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException($"เปลี่ยนชื่อภาพกรงเล็บไม่ได้: {error}");
    }

    // ภาพมาแบบ Multiple + Bilinear + 100 PPU ใช้เป็นอาวุธชิ้นเดียวไม่ได้ และขอบจะเบลอ
    static Sprite ImportWeaponSprite(string path, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        // ตั้งผ่าน settings ชุดเดียว ไม่งั้น SetTextureSettings จะเขียนทับโหมด Single กลับเป็นค่าเดิมที่อ่านมา
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spritePixelsPerUnit = PixelsPerUnit;
        settings.filterMode = FilterMode.Point;
        settings.mipmapEnabled = false;
        importer.SetTextureSettings(settings);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return Load<Sprite>(path);
    }

    static GameObject BuildWeaponPrefab(string path, string name, Sprite sprite, SpriteRenderer refRenderer, float scale, Vector3 attackPoint)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        try
        {
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // ต้องอยู่ layer เดียวกับดาบ ไม่งั้นอาวุธจะจมอยู่หลังตัวละครหรือพื้นแมพ
            renderer.sortingLayerID = refRenderer.sortingLayerID;
            renderer.sortingOrder = refRenderer.sortingOrder;
            renderer.sharedMaterial = refRenderer.sharedMaterial;

            if (!exists) root.transform.localScale = new Vector3(scale, scale, 1f);

            // WeaponController หาจุดตีโดนจากลูกชื่อ AttackPoint ถ้าไม่มีจะตีอะไรไม่โดนเลย
            if (root.transform.Find("AttackPoint") == null)
            {
                var point = new GameObject("AttackPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = attackPoint;
            }

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // อาวุธประจำอาชีพคืออาวุธที่ตัวละครถือมาตั้งแต่เริ่ม ต้องไปแทนดาบที่ใส่ไว้ชั่วคราวตอน A2
    static void EquipAsStarter(string heroPath, WeaponData weapon)
    {
        var root = PrefabUtility.LoadPrefabContents(heroPath);
        try
        {
            var stats = root.GetComponent<PlayerStats>();
            if (stats == null)
            {
                Debug.LogWarning($"{heroPath} ยังไม่มี PlayerStats สั่ง Setup Hero ก่อน แล้วค่อยสร้างอาวุธใหม่");
                return;
            }

            stats.weapon1 = weapon;
            PrefabUtility.SaveAsPrefabAsset(root, heroPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static T LoadOrCreate<T>(string path, out bool isNew) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        isNew = asset == null;
        if (!isNew) return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
