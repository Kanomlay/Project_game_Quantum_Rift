using System;
using System.Collections;
using UnityEngine;

// สลับภาพอาวุธยิงตามจังหวะ (ธนูง้าง → ปล่อย, ปืนไฟแลบ → ถีบกลับ) แล้วคืนท่าถือ
//
// ภาพแต่ละเฟรมจากชุดอาวุธวาดตำแหน่งด้ามไม่ตรงกัน ถ้าสลับ sprite เฉย ๆ อาวุธจะกระตุกหลุดมือ
// จึงเก็บระยะเลื่อนของแต่ละเฟรมไว้ด้วย ให้จุดจับ (ด้ามปืน / กลางคันธนู) อยู่ที่มือตลอด
public sealed class WeaponSpriteAnimator : MonoBehaviour
{
    [Serializable]
    public struct Frame
    {
        public Sprite sprite;
        public Vector2 offset; // ตำแหน่งของภาพเทียบกับจุดจับ (หน่วยก่อนย่อ prefab)
    }

    public SpriteRenderer target;
    public Frame idle;
    public Frame[] attackFrames;
    [Tooltip("ยิงกระสุนออกตอนเริ่มเฟรมลำดับนี้ (0 = เฟรมแรก)")]
    public int releaseFrame;
    [Min(0.01f)] public float frameTime = 0.08f;
    [Tooltip("กดค้างเพื่อง้างไว้ ปล่อยเมาส์ถึงยิง (ธนู) ถ้าไม่ติ๊ก กดแล้วยิงเลย (ปืน)")]
    public bool holdToCharge;

    public void ShowIdle() => Show(idle);

    // ท่าง้างค้าง = เฟรมสุดท้ายก่อนเฟรมปล่อย
    public void ShowCharge()
    {
        if (attackFrames != null && releaseFrame > 0 && releaseFrame <= attackFrames.Length)
            Show(attackFrames[releaseFrame - 1]);
    }

    // เล่นต่อจากท่าง้าง: เฟรมปล่อย (ยิงตรงนี้) → เฟรมที่เหลือ → กลับท่าถือ
    public IEnumerator PlayRelease(Action release)
    {
        int count = attackFrames != null ? attackFrames.Length : 0;
        bool released = false;
        for (int i = Mathf.Max(0, releaseFrame); i < count; i++)
        {
            Show(attackFrames[i]);
            if (i == releaseFrame) { release?.Invoke(); released = true; }
            yield return new WaitForSeconds(frameTime);
        }

        if (!released) release?.Invoke();
        ShowIdle();
    }

    // ไม่ยาวเกิน maxDuration เพื่อให้ยิงรัว ๆ ตาม attackSpeed ได้โดยท่าไม่ค้างทับกัน
    public IEnumerator PlayAttack(float maxDuration, Action release)
    {
        int count = attackFrames != null ? attackFrames.Length : 0;
        if (count == 0)
        {
            release?.Invoke();
            yield break;
        }

        float perFrame = Mathf.Min(frameTime, maxDuration / (count + 1));
        bool released = false;
        for (int i = 0; i < count; i++)
        {
            Show(attackFrames[i]);
            if (i == releaseFrame) { release?.Invoke(); released = true; }
            yield return new WaitForSeconds(perFrame);
        }

        if (!released) release?.Invoke();
        ShowIdle();
    }

    private void Show(Frame frame)
    {
        if (target == null || frame.sprite == null) return;
        target.sprite = frame.sprite;
        target.transform.localPosition = frame.offset;
    }
}
