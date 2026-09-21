using UnityEngine;

/// <summary>จุดเชื่อม HUD ของบอส ไม่มีการสร้างเลือดจำลองหรือเพิ่มระบบโจมตี</summary>
public sealed class BossHealthHudLink : MonoBehaviour
{
    public BossHealthHud hudPrefab;
    public BossHealthHud Instance { get; private set; }
    PlayerStats participant;

    public void BeginFight(float currentHealth, float maximumHealth, PlayerStats player = null)
    {
        if (!isActiveAndEnabled || hudPrefab == null) return;
        participant = player;
        if (Instance == null) Instance = Instantiate(hudPrefab);
        Instance.Show(currentHealth, maximumHealth);
    }

    public void RefreshHealth(float currentHealth, float maximumHealth)
    {
        if (Instance != null) Instance.SetHealth(currentHealth, maximumHealth);
    }

    public void EndFight()
    {
        participant = null;
        if (Instance == null) return;
        Instance.Hide();
        Destroy(Instance.gameObject);
        Instance = null;
    }

    void Update()
    {
        if (participant != null && (participant.isDead || participant.currentHP <= 0)) EndFight();
    }
    void OnDisable() { EndFight(); }
    void OnDestroy() { EndFight(); }
}
