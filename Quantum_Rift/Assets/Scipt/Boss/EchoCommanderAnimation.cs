using UnityEngine;

/// <summary>Animation controls only; boss combat and projectile spawning are separate systems.</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class EchoCommanderAnimation : MonoBehaviour
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

    [ContextMenu("Preview/Summon (Play Mode)")]
    public void PlaySummon() => TriggerAction("Summon");

    [ContextMenu("Preview/Shoot (Play Mode)")]
    public void PlayShoot() => TriggerAction("Shoot");

    [ContextMenu("Preview/Melee (Play Mode)")]
    public void PlayMelee() => TriggerAction("Attack");

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
        Animation.ResetTrigger("Summon");
        Animation.ResetTrigger("Shoot");
        Animation.ResetTrigger("Attack");
    }
}
