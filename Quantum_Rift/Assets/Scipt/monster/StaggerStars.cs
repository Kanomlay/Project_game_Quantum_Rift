using UnityEngine;

// มอนเซ (โดนอาวุธหนักจนค่าทนแรงกระแทกหมด): วาบฟ้าเหนือหัว หยุดภาพสั้น ๆ จอสั่นนิด ให้รู้สึกว่าตีแตก
// แล้วมีดาว 3 ดวงวนเหนือหัวตลอดที่เซ (ประกายเม็ดพิกเซลเดียวกับตอนตีโดน ไม่ต้องมีภาพใหม่)
public static class StaggerStars
{
    static readonly Color Cyan = new Color(0.45f, 0.95f, 1f);

    public static void Play(MonsterController monster, SpriteRenderer view, float time)
    {
        if (monster == null) return;
        HitStop.Freeze(0.06f);
        CameraFollow.Shake(0.15f, 0.12f);
        Vector2 head = Head(monster, view);
        EchoFx.Flash(head, Cyan, 1.4f, 0.25f);
        ImpactSparks.Spawn(head, Cyan, 10, Vector2.zero, 4f);

        float width = view != null ? Mathf.Clamp(view.bounds.extents.x * 0.7f, 0.35f, 1.2f) : 0.5f;
        float next = 0f;
        EchoFxTween.Play(monster.gameObject, time, k =>
        {
            if (monster == null || !monster.IsAlive) return;
            float t = k * time;
            if (t < next) return;
            next = t + 0.05f;
            Vector2 at = Head(monster, view);
            for (int i = 0; i < 3; i++)
            {
                float a = (t * 540f + 120f * i) * Mathf.Deg2Rad;
                var spot = at + new Vector2(Mathf.Cos(a) * width, Mathf.Sin(a) * width * 0.35f);
                ImpactSparks.Spawn(spot, i == 0 ? Color.white : Cyan, 1, Vector2.zero, 0.3f);
            }
        });
    }

    static Vector2 Head(MonsterController monster, SpriteRenderer view) =>
        (Vector2)DamageNumbers.Above(view, monster.transform.position) + Vector2.up * 0.25f;
}
