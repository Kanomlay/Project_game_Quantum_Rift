using UnityEngine;

// ผู้เล่นโดนตี: กะพริบขาวทั้งตัว + ยุบตัวแวบหนึ่ง (shader เดียวกับมอนสเตอร์ QuantumRift/SpriteFX)
// PlayerStats ใส่ให้เองตอนเริ่ม ไม่มี material = ไม่ทำอะไร (ยังกะพริบหายตอนอมตะเหมือนเดิม)
[DisallowMultipleComponent]
public sealed class PlayerHitFx : MonoBehaviour
{
    const float FlashTime = 0.15f;
    static readonly Vector2 HitSquash = new Vector2(1.14f, 0.86f);
    static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    static readonly int SquashId = Shader.PropertyToID("_Squash");

    SpriteRenderer view;
    MaterialPropertyBlock block;
    float flash;
    Vector2 kick = Vector2.one;

    public bool Active { get; private set; }
    public bool Flashing => flash > 0f;

    public static PlayerHitFx Attach(GameObject owner, SpriteRenderer view)
    {
        var fx = owner.GetComponent<PlayerHitFx>();
        if (fx == null) fx = owner.AddComponent<PlayerHitFx>();
        fx.view = view;
        fx.block = new MaterialPropertyBlock();
        if (view != null && MonsterFx.SharedMaterial != null)
        {
            view.sharedMaterial = MonsterFx.SharedMaterial;
            fx.Active = true;
        }
        return fx;
    }

    public void Hit()
    {
        flash = 1f;
        kick = HitSquash;
    }

    void LateUpdate()
    {
        if (!Active || view == null) return;
        float dt = Time.deltaTime;
        flash = Mathf.Max(0f, flash - dt / FlashTime);
        kick = Vector2.Lerp(kick, Vector2.one, 1f - Mathf.Exp(-14f * dt));

        view.GetPropertyBlock(block);
        block.SetColor(FlashColorId, Color.white);
        block.SetFloat(FlashAmountId, Mathf.Clamp01(flash * 1.5f)); // ขาวเต็มช่วงแรกแล้วค่อยจาง
        block.SetVector(SquashId, new Vector4(kick.x, kick.y, 0f, 0f));
        view.SetPropertyBlock(block);
    }
}
