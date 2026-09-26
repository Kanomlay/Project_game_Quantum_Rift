using UnityEngine;

/// <summary>A random map prop that can be destroyed by melee weapons or projectiles.</summary>
public sealed class BreakableProp : MonoBehaviour
{
    SpriteRenderer display;
    Collider2D[] blockers;
    Sprite hpPotion, energyPotion;
    Transform dropParent;
    float health;
    bool broken;

    public void Configure(SpriteRenderer renderer, Collider2D[] solidColliders,
        Sprite healthDrop, Sprite energyDrop, Transform mapRoot, float hitPoints = 12f)
    {
        display = renderer;
        blockers = solidColliders;
        hpPotion = healthDrop;
        energyPotion = energyDrop;
        dropParent = mapRoot;
        health = hitPoints;
        var hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.isTrigger = true;
        float width = renderer != null && renderer.sprite != null ?
            renderer.sprite.bounds.size.x * Mathf.Abs(renderer.transform.lossyScale.x) : 1f;
        hitbox.radius = Mathf.Clamp(width * .42f, .35f, .85f) /
            Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
    }

    public void TakeDamage(float amount)
    {
        if (broken || amount <= 0f || display == null || !display.enabled) return;
        health -= amount;
        if (health > 0f)
        {
            ImpactSparks.Spawn(transform.position, new Color(.7f,.85f,.9f), 3, Vector2.up);
            return;
        }
        broken = true;
        ImpactSparks.Spawn(transform.position, new Color(.75f,.9f,1f), 7, Vector2.up);
        display.enabled = false;
        if (blockers != null)
            foreach (var blocker in blockers) if (blocker != null) blocker.enabled = false;
        foreach (var hitbox in GetComponents<CircleCollider2D>()) hitbox.enabled = false;

        // 40% to drop a useful potion: half health, half energy.
        if (Random.value >= .4f) return;
        bool healthPotion = Random.value < .5f;
        Sprite sprite = healthPotion ? hpPotion : energyPotion;
        if (sprite == null) return;
        var kind = healthPotion ? LootPickup.Kind.HpPotion : LootPickup.Kind.EnergyPotion;
        var item = LootPickup.Create(kind, healthPotion ? 2f : 15f, sprite, .8f,
            dropParent != null ? dropParent : transform.parent, transform.position);
        item.Toss(transform.position, (Vector2)transform.position + Random.insideUnitCircle * .75f);
    }
}
