using UnityEngine;
using UnityEngine.SceneManagement;

// เพลงหน้าเมนูหลัก: สร้างตัวเองตอนเริ่มเกม ไม่ต้องวางอะไรในฉาก เล่นเฉพาะในฉากเมนูหลัก
// เข้าฉากอื่น (เล่นเกม) ค่อย ๆ เบาลงจนหยุด แล้วเพลงประจำแมพ (MapBackgroundMusic) รับช่วงต่อ กลับมาเมนูเริ่มเล่นใหม่ตั้งแต่ต้น
// ไฟล์เพลงอยู่ใน Resources/Music โหลดด้วยชื่อ ความดังรวมใช้ AudioListener.volume ของหน้าตั้งค่าเหมือนเสียงอื่น
public sealed class MenuMusic : MonoBehaviour
{
    const string ClipPath = "Music/Menu_CosmicJourney";
    const string MenuScene = "MainMenu";
    const float Volume = 0.32f;      // เท่าเพลงประจำแมพ
    const float FadeSeconds = 0.6f;

    static MenuMusic instance;
    AudioSource source;
    float target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null) return; // ยังไม่ได้ใส่เพลงเมนู
        var go = new GameObject("Menu Music");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MenuMusic>();
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.priority = 32;
        instance.source = source;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        instance.Apply(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null && mode == LoadSceneMode.Single) instance.Apply(scene.name);
    }

    void Apply(string sceneName)
    {
        bool menu = sceneName == MenuScene;
        target = menu ? Volume : 0f;
        if (menu && !source.isPlaying) source.Play();
    }

    void Update()
    {
        if (Mathf.Approximately(source.volume, target))
        {
            if (target <= 0f && source.isPlaying) source.Stop(); // หยุดจริง กลับมาเมนูจะได้เริ่มเพลงใหม่
            return;
        }
        // เฟดด้วยเวลาจริง: ตอนเปลี่ยนฉากเกมอาจหยุดเวลาอยู่ (หน้าสรุปผล / หยุดเกม)
        source.volume = Mathf.MoveTowards(source.volume, target, Volume / FadeSeconds * Time.unscaledDeltaTime);
    }
}
