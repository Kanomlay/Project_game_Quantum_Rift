using System;
using UnityEditor;
using UnityEngine;

public static class MapBgmInstaller
{
    public static readonly string[] MapNames = { "MapData_1_1", "MapData_1_2", "MapData_1_3", "MapData_1_bossroom", "MapData_2_1", "MapData_2_2", "MapData_2_boss", "MapData_boss" };
    // เพลงชุดที่มีสิทธิ์ใช้ (ดู Assets/Audio/CREDITS.md): ด่านย่อยของแมพเดียวกันใช้เพลงเดียวกัน ห้องบอสมีเพลงของตัวเอง
    public static readonly string[] Songs = { "Map1_SpaceDungeon.ogg", "Boss1_SpaceBossBattle.ogg", "Map2_TranceAdventure.ogg", "Boss2_HeavyBossBattle2.ogg", "FinalBoss_EpicBossBattle.wav" };
    static readonly int[] SongOfMap = { 0, 0, 0, 1, 2, 2, 3, 4 }; // ตามลำดับ MapNames
    public static string MapPath(string name) => "Assets/Data/Map/" + name + ".asset";
    public static string ClipPath(int index) => "Assets/Audio/BGM/" + Songs[index];
    public static int SongFor(int mapIndex) => SongOfMap[mapIndex];

    [MenuItem("Tools/Quantum Rift/Install Map Background Music")]
    public static void Install()
    {
        for (int i = 0; i < Songs.Length; i++)
        {
            var path = ClipPath(i);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new Exception("Missing audio: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .85f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
        for (int i = 0; i < MapNames.Length; i++)
        {
            var map = AssetDatabase.LoadAssetAtPath<MapData>(MapPath(MapNames[i]));
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(SongFor(i)));
            if (map == null || clip == null || clip.length < 30f) throw new Exception("Invalid map/song: " + MapNames[i]);
            map.backgroundMusic = clip;
            EditorUtility.SetDirty(map);
            Debug.Log("BGM_ASSIGNED " + map.name + " -> " + clip.name + " seconds=" + clip.length);
        }
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("MAP_BGM_INSTALL_COMPLETE maps=8 clips=" + Songs.Length);
    }

    public static void Verify()
    {
        var finalClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(Songs.Length - 1));
        int finalUses = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:MapData"))
        {
            var map = AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(guid));
            if (map.backgroundMusic != finalClip) continue;
            finalUses++;
            if (map.name != "MapData_boss" || !map.isBossRoom || map.nextMap != null)
                throw new Exception("Final boss song on wrong map: " + map.name);
        }
        if (finalUses != 1) throw new Exception("Expected exactly one final boss song assignment");
        for (int i = 0; i < MapNames.Length; i++)
        {
            var map = AssetDatabase.LoadAssetAtPath<MapData>(MapPath(MapNames[i]));
            if (map.backgroundMusic != AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(SongFor(i))))
                throw new Exception("Wrong song: " + MapNames[i]);
        }
        Debug.Log("MAP_BGM_ASSET_CHECKS_PASS finalBossOnly=true");
    }
}
