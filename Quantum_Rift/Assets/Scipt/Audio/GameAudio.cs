using UnityEngine;

// ความดังแยกประเภทจากหน้าตั้งค่า: เพลงประกอบ กับ เสียงเอฟเฟกต์ (0–1 จำไว้ใน PlayerPrefs)
// ความดังรวมยังเป็น AudioListener.volume เหมือนเดิม เสียงที่ได้ยิน = ความดังรวม × ความดังของประเภทนั้น
// - เพลง: MapBackgroundMusic (เพลงประจำแมพ) และ MenuMusic (เพลงเมนูหลัก) คูณค่านี้ทุกเฟรม เลื่อนแถบแล้วได้ยินทันที
// - เอฟเฟกต์: Sfx คูณตอนเริ่มเล่นแต่ละเสียง (รวมเสียงร้องของมอนและบอส)
public static class GameAudio
{
    const string MusicKey = "quantumrift.volume.music";
    const string SfxKey = "quantumrift.volume.sfx";
    static float music = -1f, sfx = -1f; // -1 = ยังไม่ได้อ่านค่าที่จำไว้

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { music = sfx = -1f; }

    public static float MusicVolume
    {
        get { if (music < 0f) music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f)); return music; }
        set { music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get { if (sfx < 0f) sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f)); return sfx; }
        set { sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx); PlayerPrefs.Save(); }
    }
}
