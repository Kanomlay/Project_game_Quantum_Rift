using System.Collections;
using UnityEngine;

// เล่นภาพเอฟเฟกต์สกิลทีละเฟรม (ชุด QuantumRift-Hero-Skills-v1 สกิลละ 7 เฟรม) แล้วลบตัวเองเมื่อจบ
//
// ท่าที่ต้องค้างนาน (เกราะ, ออร่า) สั่ง Loop ให้วนช่วงกลางจนหมดเวลา แล้วค่อยเล่นเฟรมท้ายที่จางหาย
// Follow ให้ตามตัวผู้เล่นโดยไม่เป็นลูก (ตัวละครถูกย่อไว้ ถ้าเป็นลูกขนาดเอฟเฟกต์จะเพี้ยนตาม)
public sealed class SkillVfx : MonoBehaviour
{
    const string SortingLayerName = "Effect";

    private Sprite[] frames;
    private float frameTime;
    private int loopStart = -1;
    private int loopEnd = -1;
    private float loopUntil;
    private bool stopRequested;
    private Transform follow;
    private Vector3 followOffset;
    private int pulseFrame = -1;
    private float pulseUntil;
    private SpriteRenderer view;

    // contentOffset = SkillData.effectOffset: ภาพอยู่ในลูกที่เลื่อนกลับ ให้กึ่งกลางเอฟเฟกต์ตรง position
    public static SkillVfx Spawn(Sprite[] frames, Vector3 position, float scale, float angle = 0f, float fps = 14f,
                                 Vector2 contentOffset = default)
    {
        var go = new GameObject("SkillVfx");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = -(Vector3)contentOffset;

        var vfx = go.AddComponent<SkillVfx>();
        vfx.view = sprite.AddComponent<SpriteRenderer>();
        vfx.view.sortingLayerName = SortingLayerName;
        vfx.view.sortingOrder = 5;
        vfx.frames = frames;
        vfx.frameTime = 1f / Mathf.Max(1f, fps);
        return vfx;
    }

    // วนเฟรม start..end ไปเรื่อย ๆ จนครบ duration วินาที (หรือโดนสั่ง Stop) แล้วเล่นเฟรมที่เหลือต่อจนจบ
    public SkillVfx Loop(int start, int end, float duration)
    {
        loopStart = start;
        loopEnd = end;
        loopUntil = Time.time + duration;
        return this;
    }

    // ตามเป้าโดยรักษาระยะห่างตอนเกิดไว้ (เกิดที่กลางลำตัว แต่ตัวละครมีจุดอ้างอิงอยู่ที่เท้า)
    public SkillVfx Follow(Transform target)
    {
        follow = target;
        if (target != null) followOffset = transform.position - target.position;
        return this;
    }

    public void Stop() => stopRequested = true;

    // แทรกเฟรมเดียวชั่วครู่ เช่นเกราะสะท้อนแสงวาบตอนกันดาเมจได้
    public void Pulse(int frame, float seconds)
    {
        pulseFrame = frame;
        pulseUntil = Time.time + seconds;
        Show(frame);
    }

    private IEnumerator Start()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        int i = 0;
        while (i < frames.Length)
        {
            Show(Time.time < pulseUntil ? pulseFrame : i);
            yield return new WaitForSeconds(frameTime);

            bool keepLooping = loopStart >= 0 && i == loopEnd && !stopRequested && Time.time < loopUntil;
            i = keepLooping ? loopStart : i + 1;
        }

        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (follow == null) return;
        if (!follow.gameObject.activeInHierarchy) { Stop(); return; }
        transform.position = follow.position + followOffset;
    }

    private void Show(int index)
    {
        if (view != null && index >= 0 && index < frames.Length) view.sprite = frames[index];
    }
}
