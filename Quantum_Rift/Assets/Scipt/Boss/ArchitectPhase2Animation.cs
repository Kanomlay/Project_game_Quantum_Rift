using UnityEngine;

/// <summary>ควบคุมภาพ Phase 2 โดยไม่เคลื่อน transform หรือสร้างดาเมจ</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class ArchitectPhase2Animation : MonoBehaviour
{
    private Animator cachedAnimator;
    private Animator Animation => cachedAnimator != null ? cachedAnimator : cachedAnimator = GetComponent<Animator>();
    private static readonly string[] Actions = { "AuraDash", "ReturnSlash", "LaserPose", "ScytheSlash" };
    public void SetFloating(bool floating) => Animation.SetBool("isFloating", floating);
    public void FaceLeft(bool left) => GetComponent<SpriteRenderer>().flipX = left;

    [ContextMenu("Preview/Idle (Play Mode)")]
    public void PlayIdle()
    {
        if (!Application.isPlaying) return;
        ClearActions();
        SetFloating(false);
        Animation.Play("Idle", 0, 0);
    }
    [ContextMenu("Preview/Float (Play Mode)")]
    public void PlayFloat() { if (Application.isPlaying) SetFloating(true); }
    [ContextMenu("Preview/Aura Dash + Return Slash (Play Mode)")]
    public void PlayAuraDash() => Trigger("AuraDash");
    [ContextMenu("Preview/Return Slash (Play Mode)")]
    public void PlayReturnSlash() => Trigger("ReturnSlash");
    [ContextMenu("Preview/Laser Pose (Play Mode)")]
    public void PlayLaserPose() => Trigger("LaserPose");
    [ContextMenu("Preview/Scythe Slash (Play Mode)")]
    public void PlayScytheSlash() => Trigger("ScytheSlash");

    // Animator ต่อ AuraDash ไป ReturnSlash อัตโนมัติ ก่อนกลับ Idle/Float
    private void Trigger(string action)
    {
        if (!Application.isPlaying) return;
        ClearActions();
        Animation.SetTrigger(action);
    }
    private void ClearActions() { foreach (var action in Actions) Animation.ResetTrigger(action); }
}
