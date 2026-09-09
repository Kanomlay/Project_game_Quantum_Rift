using UnityEngine;

/// <summary>Animation controls only; combat, laser sweeping and root spawning are separate systems.</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class AncientEntbornAnimation : MonoBehaviour
{
    private Animator cachedAnimator;
    private Animator Animation => cachedAnimator != null ? cachedAnimator : cachedAnimator = GetComponent<Animator>();

    // isWalking กำหนดสถานะพื้นฐานที่จะกลับไปหลังท่าโจมตีจบ
    public void SetWalking(bool walking) => Animation.SetBool("isWalking", walking);
    public void FaceLeft(bool left) => GetComponent<SpriteRenderer>().flipX = left;

    [ContextMenu("Preview/Idle (Play Mode)")]
    public void PlayIdle()
    {
        if (!Application.isPlaying) return;
        ClearActions();
        Animation.SetBool("isWalking", false);
        Animation.Play("Idle", 0, 0f);
    }

    [ContextMenu("Preview/Walk (Play Mode)")]
    public void PlayWalk() { if (Application.isPlaying) SetWalking(true); }

    [ContextMenu("Preview/Laser Cast (Play Mode)")]
    public void PlayLaserCast() => TriggerAction("LaserCast");

    [ContextMenu("Preview/Stomp (Play Mode)")]
    public void PlayStomp() => TriggerAction("Stomp");

    // ทดลองคำสั่งเฉพาะ Play Mode; Edit Mode ใช้หน้าต่าง Animation พรีวิว
    private void TriggerAction(string trigger)
    {
        if (!Application.isPlaying) return;
        ClearActions();
        Animation.SetTrigger(trigger);
    }

    // ล้าง trigger ที่ค้าง เพื่อไม่ให้หลายคำสั่งรอเล่นต่อกันโดยไม่ตั้งใจ
    private void ClearActions()
    {
        Animation.ResetTrigger("LaserCast");
        Animation.ResetTrigger("Stomp");
    }
}
