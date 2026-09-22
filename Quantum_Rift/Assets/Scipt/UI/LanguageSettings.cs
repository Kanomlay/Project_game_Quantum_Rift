using System;
using UnityEngine;

public enum GameLanguage
{
    English,
    Thai
}

// ตัวเก็บภาษาที่เลือกไว้ ใช้ร่วมกันทุกฉาก และจำค่าข้ามรอบเล่นด้วย PlayerPrefs
// ของที่ต้องเปลี่ยนตามภาษา (ภาพปุ่ม/ข้อความ) ไม่ต้องไปตามหาเอง แค่ subscribe Changed ไว้
public static class LanguageSettings
{
    const string PrefsKey = "quantumrift.language";

    public static event Action Changed;

    static GameLanguage current;
    static bool loaded;

    public static GameLanguage Current
    {
        get
        {
            if (!loaded)
            {
                current = (GameLanguage)PlayerPrefs.GetInt(PrefsKey, (int)GameLanguage.English);
                loaded = true;
            }
            return current;
        }
        set
        {
            if (loaded && current == value) return;

            current = value;
            loaded = true;
            PlayerPrefs.SetInt(PrefsKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static bool IsThai => Current == GameLanguage.Thai;

    public static void Toggle()
    {
        Current = IsThai ? GameLanguage.English : GameLanguage.Thai;
    }

    // ตัวย่อที่เอาไปโชว์บนปุ่มสลับภาษาได้เลย
    public static string ShortName => IsThai ? "TH" : "EN";
}
