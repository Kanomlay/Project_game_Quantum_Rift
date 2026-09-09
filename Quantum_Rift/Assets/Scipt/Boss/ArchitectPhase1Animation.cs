using UnityEngine;

/// <summary>ควบคุมภาพ Phase 1; การยิงจริงและการสลับเฟสเป็นหน้าที่ระบบบอส</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class ArchitectPhase1Animation : MonoBehaviour
{
    private Animator cachedAnimator;
    private Animator Animation => cachedAnimator != null ? cachedAnimator : cachedAnimator = GetComponent<Animator>();
    private static readonly string[] Actions = { "BulletPose", "LaserPose", "MixedPose", "Phase2Charge" };

    [ContextMenu("Preview/Idle (Play Mode)")]
    public void PlayIdle()
    {
        if (!Application.isPlaying) return;
        ClearActions();
        Animation.Play("Idle", 0, 0);
    }
    [ContextMenu("Preview/Bullet Pose (Play Mode)")]
    public void PlayBulletPose() => Trigger("BulletPose");
    [ContextMenu("Preview/Laser Pose (Play Mode)")]
    public void PlayLaserPose() => Trigger("LaserPose");
    [ContextMenu("Preview/Mixed Pose (Play Mode)")]
    public void PlayMixedPose() => Trigger("MixedPose");
    [ContextMenu("Preview/Phase 2 Charge (Play Mode)")]
    public void PlayPhase2Charge() => Trigger("Phase2Charge");

    // Charge ค้างเฟรมสุดท้ายรอระบบเกมสลับ prefab; PlayIdle ใช้เริ่มทดลองใหม่
    private void Trigger(string action)
    {
        if (!Application.isPlaying) return;
        ClearActions();
        Animation.SetTrigger(action);
    }
    private void ClearActions() { foreach (var action in Actions) Animation.ResetTrigger(action); }
}
