using TMPro;
using UnityEngine;

/// <summary>กรอบ Architect จากภาพที่อนุมัติแล้ว; แสดงเฟสสนามจริง ไม่เปลี่ยนการต่อสู้</summary>
public sealed class ArchitectBossHud : MonoBehaviour
{
    public BossHealthHud health;
    public TMP_Text phaseLabel;
    public Sprite phaseOneFill, phaseTwoFill, enragedFill;
    public RectTransform phaseTwoMarker, enragedMarker;
    public int DisplayedPhase { get; private set; }

    public void SetPhase(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);
        if (DisplayedPhase == phase) return;
        DisplayedPhase = phase;
        phaseLabel.text = phase == 1 ? "PHASE 1" : phase == 2 ? "PHASE 2" : "ENRAGED";
        phaseLabel.color = phase == 1 ? new Color(.48f,1,.04f) : phase == 2 ? new Color(.85f,.15f,1) : new Color(1,.12f,.22f);
        health.healthFill.sprite = phase == 1 ? phaseOneFill : phase == 2 ? phaseTwoFill : enragedFill;
    }

    // ตำแหน่งขีดอิงค่าเลือดจริงใน Inspector ไม่ยึดตำแหน่งที่คลาดเคลื่อนในภาพตัวอย่าง
    public void SetThresholds(float secondPhase, float enraged)
    {
        PlaceMarker(phaseTwoMarker, secondPhase);
        PlaceMarker(enragedMarker, enraged);
    }

    static void PlaceMarker(RectTransform marker, float ratio)
    {
        if (marker == null) return;
        ratio = Mathf.Clamp01(ratio);
        marker.anchorMin = new Vector2(ratio, 0);
        marker.anchorMax = new Vector2(ratio, 1);
        marker.anchoredPosition = Vector2.zero;
    }
}
