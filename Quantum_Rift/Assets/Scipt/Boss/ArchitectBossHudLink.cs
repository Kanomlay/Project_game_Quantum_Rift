using UnityEngine;

/// <summary>เชื่อม HUD กับเลือดและเฟสของ Architect โดยไม่เพิ่มหรือแก้ AI</summary>
[DisallowMultipleComponent]
public sealed class ArchitectBossHudLink : MonoBehaviour
{
    public ArchitectBossHealth boss;
    public LivingBossArena arena;
    public ArchitectBossHud hudPrefab;
    public ArchitectBossHud Instance { get; private set; }
    float lastCurrent = float.NaN, lastMaximum = float.NaN;

    // อ่านหลัง Update ของสนาม เพื่อไม่ค้างหลอดเมื่อผู้เล่นตายหรือยกเลิกการต่อสู้
    void LateUpdate()
    {
        if (boss == null || arena == null || hudPrefab == null || !boss.isActiveAndEnabled
            || !arena.isActiveAndEnabled || !arena.IsFighting || boss.IsDefeated || boss.CurrentHealth <= 0)
        {
            Release();
            return;
        }
        if (Instance == null) Instance = Instantiate(hudPrefab);
        if (!Instance.health.IsVisible) Instance.health.Show(boss.CurrentHealth, boss.maxHealth);
        // ส่งเฉพาะเมื่อค่าเปลี่ยน เพื่อไม่รีเซ็ตเวลาของรอยเลือดสีส้มทุกเฟรม
        else if (lastCurrent != boss.CurrentHealth || lastMaximum != boss.maxHealth)
            Instance.health.SetHealth(boss.CurrentHealth, boss.maxHealth);
        lastCurrent = boss.CurrentHealth;
        lastMaximum = boss.maxHealth;
        Instance.SetThresholds(boss.phaseTwoThreshold, boss.enragedThreshold);
        // ระหว่างชาร์จยังเป็นเฟสเดิม; เปลี่ยนพร้อมร่างและสนามหลังชาร์จสำเร็จเท่านั้น
        Instance.SetPhase(arena.CurrentPhase);
    }

    void Release()
    {
        if (Instance == null) return;
        Instance.health.Hide();
        Destroy(Instance.gameObject);
        Instance = null;
        lastCurrent = lastMaximum = float.NaN;
    }

    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }
}
