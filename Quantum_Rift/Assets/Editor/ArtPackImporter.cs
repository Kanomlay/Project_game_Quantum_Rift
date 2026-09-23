using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// ตั้งค่า import ให้ชุดภาพที่เพื่อนส่งมา (อาวุธ 23 ชิ้น + เอฟเฟกต์สกิลฮีโร่ 8 สกิล)
//
// ตอนลากไฟล์เข้า Unity จะตั้งให้เองเป็น Multiple + Bilinear + บีบอัด ซึ่งผิดทั้งสามอย่าง:
// ภาพพิกเซลเบลอ สีเพี้ยน และแผ่นเฟรมถูกตัดอัตโนมัติเป็นเศษ ๆ (แผ่น 7 เฟรมได้มา 12–38 ชิ้น)
// สคริปต์นี้ตั้ง Point + ไม่บีบอัด แล้วตัดแผ่นเฟรมตามขนาดช่องจริงที่ README ของแต่ละชุดระบุ
//
// สั่งซ้ำได้: sprite ที่ชื่อเดิมจะได้ ID เดิม prefab/แอนิเมชันที่อ้างถึงไว้แล้วไม่หลุด
public static class ArtPackImporter
{
    const string WeaponFolder = "Assets/image/Weapon/Quantum-Rift-Weapons-23-v1";
    const string SkillFolder = "Assets/image/hero/QuantumRift-Hero-Skills-v1";

    // เท่ากับตัวฮีโร่ (Heroes-BlondeStyle-NoHands-v2 ใช้ 100) ขนาดอาวุธแต่ละชิ้นค่อยปรับที่ scale ของ prefab
    const float PixelsPerUnit = 100f;

    static readonly Vector2Int WeaponCell = new Vector2Int(192, 128); // README ชุดอาวุธ: ช่องละ 192 × 128
    static readonly Vector2Int SkillCell = new Vector2Int(256, 256);  // README ชุดสกิล: ช่องละ 256 × 256

    [MenuItem("Tools/Quantum Rift/Import Art/All Packs")]
    public static void ImportAll()
    {
        int count = Import(WeaponFolder) + Import(SkillFolder);
        Debug.Log($"ตั้งค่า import ชุดอาวุธและสกิลเรียบร้อย ({count} ไฟล์)");
    }

    [MenuItem("Tools/Quantum Rift/Import Art/Weapons 23")]
    public static void ImportWeapons() => Debug.Log($"ตั้งค่า import ชุดอาวุธเรียบร้อย ({Import(WeaponFolder)} ไฟล์)");

    [MenuItem("Tools/Quantum Rift/Import Art/Hero Skills")]
    public static void ImportSkills() => Debug.Log($"ตั้งค่า import ชุดสกิลเรียบร้อย ({Import(SkillFolder)} ไฟล์)");

    static int Import(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            throw new InvalidOperationException($"ไม่เจอโฟลเดอร์ {folder}");

        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var factory = new SpriteDataProviderFactories();
        factory.Init();

        // รวบการ reimport ไว้ทีเดียวตอนจบ ไม่งั้น Unity จะ reimport ทีละไฟล์ช้ามาก
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Import Art", paths[i], (float)i / paths.Count)) break;
                Configure(paths[i], factory);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
        return paths.Count;
    }

    static void Configure(string path, SpriteDataProviderFactories factory)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;                                // ภาพพิกเซล ห้ามเบลอ
        importer.textureCompression = TextureImporterCompression.Uncompressed; // บีบอัดแล้วสีขอบเพี้ยน
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        var cell = GridCellFor(Path.GetFileName(path));
        if (cell == null)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
            return;
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        SliceGrid(importer, factory, cell.Value, Path.GetFileNameWithoutExtension(path));
        importer.SaveAndReimport();
    }

    // แผ่นเฟรมเรียงซ้ายไปขวา (ไอคอนรวม 8 อันเป็น 4 × 2) ไฟล์อื่นเป็นภาพเดี่ยว
    static Vector2Int? GridCellFor(string fileName)
    {
        if (fileName.EndsWith("-strip.png", StringComparison.OrdinalIgnoreCase)) return WeaponCell;
        if (fileName.EndsWith("-7Frames.png", StringComparison.OrdinalIgnoreCase)) return SkillCell;
        if (fileName.Equals("Skill-Icons-8.png", StringComparison.OrdinalIgnoreCase)) return SkillCell;
        return null;
    }

    static void SliceGrid(TextureImporter importer, SpriteDataProviderFactories factory, Vector2Int cell, string baseName)
    {
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int columns = width / cell.x;
        int rows = height / cell.y;
        if (columns == 0 || rows == 0 || width % cell.x != 0 || height % cell.y != 0)
            Debug.LogWarning($"{importer.assetPath} ขนาด {width}×{height} ไม่ลงช่อง {cell.x}×{cell.y} พอดี เศษที่เหลือจะไม่ถูกตัด");

        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // ใช้ ID เดิมของ sprite ชื่อเดียวกัน อะไรที่อ้างถึง sprite นี้ไว้แล้วจะได้ไม่หลุด
        var oldIds = new Dictionary<string, GUID>();
        foreach (var old in provider.GetSpriteRects())
            oldIds[old.name] = old.spriteID;

        var rects = new List<SpriteRect>();
        int index = 0;
        // เรียงจากแถวบนลงล่าง ซ้ายไปขวา แต่พิกัด rect ของ Unity นับจากล่างขึ้นบน
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < columns; col++)
            {
                string name = $"{baseName}_{index++}";
                rects.Add(new SpriteRect
                {
                    name = name,
                    rect = new Rect(col * cell.x, row * cell.y, cell.x, cell.y),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = oldIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());

        var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        provider.Apply();
    }
}
