using System.Collections;
using UnityEngine;

// กำแพงในห้องหนึ่งช่อง (สร้างโดย RoomBreakableWalls): collider ช่องละชิ้น ภาพคือ tile ในแผนที่กำแพง
// ผู้เล่นตีผ่าน IBreakable (อาวุธ/กระสุน) มอนสเตอร์ของห้องเดียวกันทุบผ่าน Smash
// โดนตี: สั่นนิดหนึ่ง สีหมองลงตามเลือดที่เหลือ   แตก: tile หาย ภาพแตกเป็นชิ้น ๆ collider ปิด เดินผ่านได้
public sealed class BreakableWallCell : MonoBehaviour, IBreakable
{
    RoomBreakableWalls walls;
    Vector3Int cell;
    Collider2D box;
    float health;
    bool broken;

    public bool IsBroken => broken;
    public RoomController Room => walls != null ? walls.Room : null;
    public Vector2 Center => transform.position;

    public void Setup(RoomBreakableWalls owner, Vector3Int at)
    {
        walls = owner;
        cell = at;
        box = GetComponent<Collider2D>();
        health = owner.cellHealth;
    }

    public void TakeDamage(float amount) => Hit(amount, true);  // ผู้เล่น
    public void Smash(float amount) => Hit(amount, false);      // มอนสเตอร์ทุบเปิดทาง

    void Hit(float amount, bool byPlayer)
    {
        if (broken || walls == null || amount <= 0f) return;
        health -= amount;
        if (health <= 0f)
        {
            Break(byPlayer);
            return;
        }
        walls.Wear(cell, health / walls.cellHealth);
        ImpactSparks.Spawn(Center, walls.debrisColor, 4, Vector2.up, 3.5f);
        StopAllCoroutines();
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            walls.Nudge(cell, Random.insideUnitCircle * 0.05f);
            yield return null;
        }
        walls.Nudge(cell, Vector2.zero);
    }

    void Break(bool byPlayer)
    {
        broken = true;
        StopAllCoroutines();
        if (box != null) box.enabled = false;
        walls.Crumble(cell);
        ImpactSparks.Spawn(Center, walls.debrisColor, 14, Vector2.zero, 4.5f);
        CameraFollow.Shake(byPlayer ? 0.08f : 0.05f, 0.12f);
        if (byPlayer)
        {
            var mana = walls.manaPerCell;
            ManaMotes.Spawn(Center, Random.Range(Mathf.Min(mana.x, mana.y), Mathf.Max(mana.x, mana.y) + 1));
        }
        Destroy(gameObject);
    }
}
