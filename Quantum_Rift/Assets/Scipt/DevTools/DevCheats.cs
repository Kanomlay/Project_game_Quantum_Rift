using System;
using System.Collections;
using UnityEngine;

// สูตรที่เปิดจากคอนโซลทดสอบ (DevConsole) ระบบเกมเช็คค่าพวกนี้เอง
// ปิดคอนโซลในหน้าตั้งค่าแล้ว ResetAll ทำให้ทุกอย่างกลับเป็นปกติ
public static class DevCheats
{
    public static bool GodMode;          // เลือดไม่ต่ำกว่า 1 (ยังเห็นดาเมจที่โดนและเลือดลด)
    public static bool InfiniteEnergy;   // พลังงานเต็มตลอด
    public static bool NoSkillCooldown;  // สกิล Q/E ใช้ซ้ำได้ทันที
    public static bool FreezeMonsters;   // มอนและบอสทุกตัวยืนนิ่ง ไม่เดิน ไม่เริ่มท่าใหม่ (ยังโดนตี เซ ตายได้)
    public static float TimeScale = 1f;  // ความเร็วเกม (ดูท่าบอสช้า ๆ)
    public static float DamageScale = 1f; // ตัวคูณดาเมจของผู้เล่นทุกแบบ (RunStatBuffs.Damage) ไว้ข้ามการต่อสู้ตอนทดสอบ

    public static void SetTimeScale(float scale)
    {
        // เกมหยุดอยู่ (หน้า Pause/เลือกพร/สรุป) ไม่แตะ ปล่อยให้คอนโซลใส่ให้ตอนเกมเดินต่อ
        if (Mathf.Approximately(Time.timeScale, TimeScale)) Time.timeScale = scale;
        TimeScale = scale;
    }

    // ท่าของบอสเดินทีละจังหวะ ระหว่างหยุด AI ค้างไว้ที่จังหวะปัจจุบัน ปล่อยแล้วทำต่อจากเดิม
    // ไม่ตัดท่ากลางคัน: ท่าเปลี่ยนเฟส/คลั่งตั้งสถานะไว้ในท่า ตัดทิ้งแล้วบอสค้าง (เช่นเลือดล็อกไม่ปลด)
    public static IEnumerator Pausable(IEnumerator move, Func<bool> held)
    {
        while (move.MoveNext())
        {
            yield return move.Current;
            while (held()) yield return null;
        }
    }

    public static void ResetAll()
    {
        GodMode = InfiniteEnergy = NoSkillCooldown = FreezeMonsters = false;
        DamageScale = 1f;
        SetTimeScale(1f);
    }
}
