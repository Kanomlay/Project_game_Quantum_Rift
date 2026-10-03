using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// เล่นเสียงเอฟเฟกต์: Sfx.Play(SfxId.X) จากที่ไหนก็ได้ ไม่ต้องวางอะไรในฉาก
// ตัวเล่นสร้างตัวเองตอนเริ่มเกม (อยู่ข้ามฉาก) ใช้ AudioSource หมุนเวียนชุดเดียว
//   - เสียงเดียวกันที่สั่งถี่เกิน minInterval ถูกข้าม (ตีโดนหลายตัวในเฟรมเดียวดังครั้งเดียว)
//   - แต่ละครั้งสุ่มคลิป (ไม่ซ้ำคลิปเดิมติดกัน) และสุ่มระดับเสียงเล็กน้อย เสียงจะได้ไม่ซ้ำจนน่ารำคาญ
//   - PlayAt = เสียงในฉาก: เบาลงตามระยะจากกล้อง ไกลเกินจอไปมากไม่ได้ยิน
//   - คลิกปุ่ม UI ทุกปุ่มมีเสียงเอง (เช็คจากสิ่งที่เมาส์กดโดน ไม่ต้องไปผูกทีละปุ่ม)
// ความดังรวมใช้ AudioListener.volume ของหน้าตั้งค่า
public sealed class Sfx : MonoBehaviour
{
    const int Voices = 20;
    const float FullVolumeRange = 9f;  // ใกล้กล้องกว่านี้ดังเต็ม
    const float SilentRange = 21f;     // ไกลกว่านี้ไม่เล่น

    static Sfx instance;
    static SfxLibrary library;
    static bool searched;

    readonly List<AudioSource> sources = new List<AudioSource>();
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    int next;

    public static SfxLibrary Library
    {
        get
        {
            if (library == null && !searched)
            {
                searched = true;
                library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
                if (library == null) Debug.LogWarning("ไม่เจอคลังเสียง Resources/SfxLibrary (กด Play ใน Unity ใหม่อีกครั้งให้ Editor สร้างให้)");
            }
            return library;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Sfx>();
    }

    void Awake()
    {
        for (int i = 0; i < Voices; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // เกม 2D: ไม่แพนซ้ายขวา ความดังตามระยะคิดเองใน PlayAt
            sources.Add(source);
        }
    }

    public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
    {
        if (id == SfxId.None || instance == null || Library == null) return;
        var entry = Library.Find(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;
        float now = Time.unscaledTime;
        if (now - entry.lastPlayed < entry.minInterval) return;

        int pick = Random.Range(0, entry.clips.Length);
        if (entry.clips.Length > 1 && pick == entry.lastClip) pick = (pick + 1) % entry.clips.Length;
        var clip = entry.clips[pick];
        if (clip == null) return;
        entry.lastPlayed = now;
        entry.lastClip = pick;

        var source = instance.FreeSource();
        source.clip = clip;
        source.volume = Mathf.Clamp01(entry.volume * volume);
        source.pitch = Mathf.Max(0.1f, entry.pitch * pitch * (1f + Random.Range(-entry.pitchJitter, entry.pitchJitter)));
        source.Play();
    }

    // เสียงที่เกิดในฉาก (มอน บอส กับดัก): เบาลงตามระยะจากกล้อง
    public static void PlayAt(SfxId id, Vector2 position, float volume = 1f, float pitch = 1f)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Play(id, volume, pitch);
            return;
        }
        float distance = Vector2.Distance(position, cam.transform.position);
        if (distance >= SilentRange) return;
        Play(id, volume * Mathf.InverseLerp(SilentRange, FullVolumeRange, distance), pitch);
    }

    public static void PlayAttack(WeaponData weapon)
    {
        if (Library != null) Play(Library.AttackOf(weapon));
    }

    // เสียงยิงของมอน (ส่วนใหญ่ใช้ MonShot บางตัวมีเสียงของตัวเอง)
    public static void PlayMonsterShot(MonsterData monster, Vector2 position)
    {
        if (Library != null) PlayAt(Library.ShotOf(monster), position);
    }

    // เสียงร้องของตัวมอน (คำรามตอนโจมตี ร้องเจ็บ ร้องตอนตาย) เล่นซ้อนกับเสียงเอฟเฟกต์ของท่า
    public static void PlayMonsterVoice(MonsterData monster, SfxLibrary.Voice voice, Vector2 position)
    {
        if (Library != null) PlayAt(Library.VoiceOf(monster, voice), position);
    }

    // ช่องที่ว่างอยู่ ไม่มีก็ทับช่องถัดไปตามลำดับ (เสียงเก่าสุด)
    AudioSource FreeSource()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[(next + i) % sources.Count];
            if (source.isPlaying) continue;
            next = (next + i + 1) % sources.Count;
            return source;
        }
        var oldest = sources[next];
        next = (next + 1) % sources.Count;
        return oldest;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var events = EventSystem.current;
        if (events == null) return;
        var pointer = new PointerEventData(events) { position = Input.mousePosition }; // EventSystem เปลี่ยนตามฉาก สร้างใหม่ทุกคลิก
        hits.Clear();
        events.RaycastAll(pointer, hits);
        if (hits.Count == 0) return;
        var target = hits[0].gameObject.GetComponentInParent<Selectable>();
        if (target != null && target.IsInteractable() && (target is Button || target is Toggle)) Play(SfxId.UiClick);
    }
}
