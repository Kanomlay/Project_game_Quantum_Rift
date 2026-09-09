using UnityEngine;

// ควบคุมเฉพาะภาพเคลื่อนไหว เพื่อเชื่อมกับระบบเดินและเลือดของเกมภายหลัง
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class HeroNoHandsAnimation : MonoBehaviour
{
    static readonly int Walking = Animator.StringToHash("isWalking");
    static readonly int Dead = Animator.StringToHash("isDead");
    Animator animator;
    Animator Controller => animator != null ? animator : animator = GetComponent<Animator>();

    // ใช้ชื่อ isWalking ร่วมกับ PlayerMovement เดิม; เมื่อเสียชีวิตจะไม่รับคำสั่งเดินใหม่
    public void SetWalking(bool walking)
    {
        if (!Controller.GetBool(Dead)) Controller.SetBool(Walking, walking);
    }

    public void FaceLeft(bool left) => GetComponent<SpriteRenderer>().flipX = left;

    // Animator ให้ Death มีลำดับก่อน Idle/Walk และค้างเฟรมสุดท้ายจน ResetToIdle
    public void PlayDeath()
    {
        Controller.SetBool(Walking, false);
        Controller.SetBool(Dead, true);
    }

    // ต้องสั่งคืนชีพอย่างชัดเจน เพื่อไม่ให้การเดินทำให้ท่าตายหลุดเอง
    public void ResetToIdle()
    {
        Controller.SetBool(Walking, false);
        Controller.SetBool(Dead, false);
        Controller.Play("Idle", 0, 0);
    }

    [ContextMenu("Preview/Idle (Play Mode)")]
    void PreviewIdle() { if (Application.isPlaying) ResetToIdle(); }
    [ContextMenu("Preview/Walk (Play Mode)")]
    void PreviewWalk() { if (Application.isPlaying) SetWalking(true); }
    [ContextMenu("Preview/Death (Play Mode)")]
    void PreviewDeath() { if (Application.isPlaying) PlayDeath(); }
}
