using UnityEngine;

// เอฟเฟกต์คลื่นดาบ: ไล่เฟรมจาก sprite sheet แล้วลบตัวเองทิ้งเมื่อเล่นจบ
public class SlashEffect : MonoBehaviour
{
    [Header("เฟรมของคลื่นดาบ (เรียงตามลำดับ)")]
    public Sprite[] frames;
    public float frameDuration = 0.04f; // เวลาที่ค้างต่อ 1 เฟรม

    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float timer = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
        }
    }

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        if (timer < frameDuration) return;

        timer -= frameDuration;
        currentFrame++;

        if (currentFrame >= frames.Length)
        {
            Destroy(gameObject); // เล่นครบทุกเฟรมแล้ว เก็บกวาดตัวเองทิ้ง
            return;
        }

        sr.sprite = frames[currentFrame];
    }
}
