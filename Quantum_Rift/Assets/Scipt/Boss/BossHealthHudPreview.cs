using UnityEngine;

/// <summary>ค่าจำลองเฉพาะฉากตรวจ UI; ไม่ใช่ระบบเลือดบอสในเกม</summary>
public sealed class BossHealthHudPreview : MonoBehaviour
{
    public BossHealthHud echo;
    public BossHealthHud entborn;
    BossHealthHud selected;
    float previewHealth = 100;
    void Start() { Select(0); }
    public void Select(int index)
    {
        echo.Hide(); entborn.Hide();
        selected = index == 0 ? echo : entborn;
        previewHealth = 100;
        selected.Show(100, 100);
    }
    public void Damage()
    {
        previewHealth = Mathf.Max(0, previewHealth - 20);
        selected.SetHealth(previewHealth, 100);
    }
    public void ResetHealth() { previewHealth = 100; selected.Show(100, 100); }
}
