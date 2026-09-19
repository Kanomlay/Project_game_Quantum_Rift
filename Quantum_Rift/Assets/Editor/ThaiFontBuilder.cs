using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// สร้าง TMP Font Asset จากฟอนต์ไทย Kanit แล้วเปลี่ยนให้ทั้งโปรเจกต์ใช้ฟอนต์นี้
// ฟอนต์เดิม LiberationSans ไม่มีตัวอักษรไทย ข้อความไทยจะขึ้นเป็นกล่องสี่เหลี่ยม
//
// ใช้ atlas แบบ Dynamic คือค่อยๆ สร้างตัวอักษรตอนใช้งานจริง
// เหมาะกับภาษาไทยที่มีสระ/วรรณยุกต์ผสมกันได้หลายพันแบบ ไม่ต้องอบตัวอักษรทั้งชุดไว้ล่วงหน้า
public static class ThaiFontBuilder
{
    const string TtfPath = "Assets/Fonts/Kanit/Kanit-Regular.ttf";
    const string FontAssetPath = "Assets/Fonts/Kanit/Kanit SDF.asset";

    [MenuItem("Tools/Quantum Rift/Thai Font/1. Create Font Asset")]
    public static void CreateFontAsset()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้างฟอนต์");

        var fontAsset = LoadOrCreateFontAsset();
        SetAsDefaultFont(fontAsset);
        ApplyToPrefabs(fontAsset);

        AssetDatabase.SaveAssets();
        Debug.Log($"สร้างฟอนต์ {FontAssetPath} และตั้งเป็นฟอนต์หลักของ TextMeshPro แล้ว " +
                  "ต่อไปเปิดแต่ละฉากแล้วสั่ง Thai Font > 2. Apply To Open Scene");
    }

    [MenuItem("Tools/Quantum Rift/Thai Font/2. Apply To Open Scene")]
    public static void ApplyToOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งเปลี่ยนฟอนต์");

        var fontAsset = Load<TMP_FontAsset>(FontAssetPath);
        var scene = SceneManager.GetActiveScene();

        int changed = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                if (SwapFont(text, fontAsset)) changed++;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"เปลี่ยนฟอนต์ในฉาก {scene.name} แล้ว {changed} ชิ้น กด Ctrl+S เพื่อบันทึก");
    }

    static TMP_FontAsset LoadOrCreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var font = Load<Font>(TtfPath);

        // 90 = ขนาดที่ใช้อบตัวอักษร (ยิ่งใหญ่ยิ่งคม), padding 9 กันขอบ SDF ชนกัน, atlas เริ่มที่ 1024x1024
        var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
        if (fontAsset == null) throw new InvalidOperationException($"สร้าง font asset จาก {TtfPath} ไม่สำเร็จ");

        fontAsset.name = "Kanit SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // texture กับ material ต้องฝังเป็นของลูกในไฟล์เดียวกัน ไม่งั้นเปิดโปรเจกต์ใหม่แล้วฟอนต์จะพัง
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            fontAsset.atlasTextures[0].name = "Kanit SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        }
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Kanit SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    // ตั้งเป็นฟอนต์ตั้งต้นของ TextMeshPro ข้อความที่สร้างใหม่หลังจากนี้จะได้ฟอนต์ไทยเลย
    static void SetAsDefaultFont(TMP_FontAsset fontAsset)
    {
        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null)
        {
            Debug.LogWarning("ไม่เจอไฟล์ TMP Settings ต้องไปตั้ง Default Font Asset เองที่ Project Settings");
            return;
        }

        var serialized = new SerializedObject(settings);
        serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
    }

    static void ApplyToPrefabs(TMP_FontAsset fontAsset)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab" });
        int changedPrefabs = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || !asset.GetComponentsInChildren<TMP_Text>(true).Any()) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    changed |= SwapFont(text, fontAsset);

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedPrefabs++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"เปลี่ยนฟอนต์ใน prefab แล้ว {changedPrefabs} ไฟล์");
    }

    static bool SwapFont(TMP_Text text, TMP_FontAsset fontAsset)
    {
        if (text.font == fontAsset) return false;

        text.font = fontAsset;
        EditorUtility.SetDirty(text);
        return true;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
