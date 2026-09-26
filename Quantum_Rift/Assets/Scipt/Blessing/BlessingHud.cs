using UnityEngine;

// แถบพรที่มีบน HUD (ใต้หลอดเลือด/พลังงาน) ช่องละหนึ่งพรตามลำดับที่ได้ ช่องที่ยังว่างซ่อนไว้
// สถานะรายช่องมาจาก BlessingManager.Describe: ตัวเลขมุมไอคอน / มืด (คูลดาวน์ ใช้ไปแล้วในห้องนี้) / เรืองแสง (กำลังทำงาน)
public sealed class BlessingHud : MonoBehaviour
{
    public BlessingSlotView[] slots;

    public void Refresh(BlessingManager manager)
    {
        if (slots == null) return;
        var owned = manager != null ? manager.Owned : null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var blessing = owned != null && i < owned.Count ? owned[i] : null;
            slots[i].Show(blessing);
        }
        UpdateStates(manager);
    }

    public void UpdateStates(BlessingManager manager)
    {
        if (slots == null || manager == null) return;
        var owned = manager.Owned;
        for (int i = 0; i < slots.Length && i < owned.Count; i++)
        {
            if (slots[i] == null) continue;
            manager.Describe(owned[i], out string badge, out float dim, out bool glow);
            slots[i].SetState(badge, dim, glow);
        }
    }

    // ไอคอนเด้งให้เห็นว่าพรนี้เพิ่งทำงาน
    public void Ping(int index)
    {
        if (slots != null && index >= 0 && index < slots.Length && slots[index] != null) slots[index].Ping();
    }
}
