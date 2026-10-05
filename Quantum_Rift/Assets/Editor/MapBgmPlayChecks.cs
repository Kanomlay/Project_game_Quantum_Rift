using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ตรวจแมพจริงทั้งแปดแบบไม่บันทึกฉากหรือค่าตั้งเสียงของผู้เล่น
[InitializeOnLoad]
public static class MapBgmPlayChecks
{
    const string Key = "QuantumRift.BgmChecks";
    static int step, mapIndex, checks;
    static double deadline, waitUntil;
    static MapManager manager;
    static MapBackgroundMusic music;
    static AudioSource previousSource;
    static float previousTime;
    static float savedVolume;
    static MapBgmPlayChecks() { EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        MapBgmInstaller.Verify();
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false) || state != PlayModeStateChange.EnteredPlayMode) return;
        step = 0; mapIndex = 0; checks = 0; deadline = EditorApplication.timeSinceStartup + 150;
        waitUntil = EditorApplication.timeSinceStartup + 4;
        savedVolume = AudioListener.volume;
        AudioListener.volume = 0f; // ไม่เล่นเสียงออกลำโพงระหว่างชุดทดสอบ
        EditorApplication.update += Tick;
    }
    static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("BGM_CHECK_FAILED " + label);
        checks++; Debug.Log("BGM_CHECK_PASS " + label);
    }
    static void Tick()
    {
        try
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("BGM checks timeout step=" + step);
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            switch (step)
            {
                case 0:
                    manager = Object.FindFirstObjectByType<MapManager>();
                    if (manager == null || manager.IsLoading) return;
                    music = manager.GetComponent<MapBackgroundMusic>();
                    Check(music != null, "MapManager creates music controller");
                    step = 1; break;
                case 1:
                    if (manager.IsLoading) return;
                    var map = AssetDatabase.LoadAssetAtPath<MapData>(MapBgmInstaller.MapPath(MapBgmInstaller.MapNames[mapIndex]));
                    previousSource = music.PlaybackSource;
                    previousTime = previousSource.time;
                    manager.LoadMap(map);
                    step = 2; waitUntil = EditorApplication.timeSinceStartup + 4; break;
                case 2:
                    if (manager.IsLoading) return;
                    var source = music.PlaybackSource;
                    var expected = AssetDatabase.LoadAssetAtPath<AudioClip>(MapBgmInstaller.ClipPath(MapBgmInstaller.SongFor(mapIndex)));
                    Check(source.clip == expected && manager.CurrentMap.backgroundMusic == expected, "actual map uses correct clip " + manager.CurrentMap.name);
                    Check(source == previousSource && Object.FindObjectsByType<MapBackgroundMusic>(FindObjectsSortMode.None).Length == 1, "one music player across maps " + mapIndex);
                    Check(source.loop && source.spatialBlend == 0f && !source.playOnAwake && !source.ignoreListenerVolume, "loop 2D and master-volume settings " + mapIndex);
                    Check(source.isPlaying && Mathf.Abs(source.volume - music.musicVolume) < .01f, "track playing after fade " + mapIndex);
                    if (mapIndex > 0 && MapBgmInstaller.SongFor(mapIndex) == MapBgmInstaller.SongFor(mapIndex - 1))
                        Check(source.time > previousTime, "same-theme transition does not restart track " + mapIndex);
                    mapIndex++;
                    if (mapIndex < MapBgmInstaller.MapNames.Length) { step = 1; break; }
                    Time.timeScale = 0; music.PlayMap(AssetDatabase.LoadAssetAtPath<MapData>(MapBgmInstaller.MapPath("MapData_1_1")));
                    step = 3; waitUntil = EditorApplication.timeSinceStartup + 1.4; break;
                case 3:
                    Check(music.PlaybackSource.clip.name == "Map1_SpaceDungeon" && Mathf.Abs(music.PlaybackSource.volume - music.musicVolume) < .01f, "fade completes while paused");
                    Time.timeScale = 1;
                    music.PlayMap(null); step = 4; waitUntil = EditorApplication.timeSinceStartup + .8; break;
                case 4:
                    Check(music.PlaybackSource.clip == null && !music.PlaybackSource.isPlaying, "unassigned map stops old music");
                    music.PlayMap(manager.CurrentMap); music.PlayMap(AssetDatabase.LoadAssetAtPath<MapData>(MapBgmInstaller.MapPath("MapData_2_1")));
                    step = 5; waitUntil = EditorApplication.timeSinceStartup + 1.4; break;
                case 5:
                    Check(music.PlaybackSource.clip.name == "Map2_TranceAdventure", "rapid requests end on newest theme");
                    SceneManager.LoadScene("MainMenu"); step = 6; waitUntil = EditorApplication.timeSinceStartup + 1; break;
                case 6:
                    Check(Object.FindFirstObjectByType<MapBackgroundMusic>() == null, "returning to menu removes gameplay music");
                    Debug.Log("MAP_BGM_PLAY_CHECKS_COMPLETE checks=" + checks); Finish(0); break;
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Key, false);
        Time.timeScale = 1; AudioListener.volume = savedVolume;
        EditorApplication.Exit(code);
    }
}
