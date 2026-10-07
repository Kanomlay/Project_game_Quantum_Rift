using System.Collections;
using UnityEngine;

// อยู่กับ MapManager: เปลี่ยนแมพยังใช้ตัวเล่นเดิม แต่กลับเมนูแล้วเพลงหยุดตามฉาก
// เพลงมาจาก MapData.backgroundMusic ของแต่ละด่าน (ไม่เดาจาก isBossRoom) ห้องบอสจะมีเพลงของตัวเองหรือใช้เพลงเดิมของแมพก็ได้
[DisallowMultipleComponent]
public sealed class MapBackgroundMusic : MonoBehaviour
{
    [Range(0f, 1f)] public float musicVolume = .32f;
    [Min(0f)] public float fadeSeconds = .45f;
    AudioSource source;
    AudioClip requested;
    Coroutine transition;
    public AudioSource PlaybackSource => source;
    // ความดังเป้าหมาย = ค่าของเพลงประจำแมพ × แถบเสียงเพลงในหน้าตั้งค่า
    float TargetVolume => musicVolume * GameAudio.MusicVolume;

    void Awake()
    {
        var child = new GameObject("Map BGM");
        child.transform.SetParent(transform, false);
        source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.priority = 32;
        // ใช้ความดังรวม AudioListener.volume จาก Settings เดิม ไม่เขียนทับค่าผู้เล่น
        source.ignoreListenerVolume = false;
    }

    public void PlayMap(MapData map)
    {
        var clip = map != null ? map.backgroundMusic : null;
        if (requested == clip) return; // ด่านย่อยและบอสธีมเดียวกันไม่เริ่มเพลงใหม่
        requested = clip;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(ChangeTrack(clip));
    }

    IEnumerator ChangeTrack(AudioClip clip)
    {
        if (source.clip != null) yield return FadeTo(0f);
        source.Stop();
        source.clip = clip;
        if (clip != null)
        {
            source.Play();
            yield return FadeTo(TargetVolume);
        }
        transition = null;
    }

    IEnumerator FadeTo(float target)
    {
        float start = source.volume, elapsed = 0f;
        // เฟดต่อได้แม้เปิด Pause/Help; เพลงประกอบยังเล่นขณะอ่านเหมือนเพลงเมนู
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, fadeSeconds > 0f ? elapsed / fadeSeconds : 1f);
            yield return null;
        }
        source.volume = target;
    }

    // เลื่อนแถบเสียงเพลงระหว่างเล่น: ปรับตามทันที (ช่วงเฟดเปลี่ยนเพลงปล่อยให้เฟดจบก่อน)
    void Update()
    {
        if (transition == null && source != null && source.clip != null) source.volume = TargetVolume;
    }

    void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        requested = null;
        if (source != null) { source.Stop(); source.clip = null; source.volume = 0f; }
    }
    void OnDestroy()
    {
        if (source != null) Destroy(source.gameObject);
    }
}
