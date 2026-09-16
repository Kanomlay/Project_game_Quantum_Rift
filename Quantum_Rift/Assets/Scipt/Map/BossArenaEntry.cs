using UnityEngine;
public sealed class BossArenaEntry : MonoBehaviour
{
    public LivingBossArena arena;
    void OnTriggerEnter2D(Collider2D other){Enter(other);}
    void OnTriggerStay2D(Collider2D other){Enter(other);}
    void Enter(Collider2D other)
    {
        var player=other.GetComponentInParent<PlayerStats>();
        if(player!=null)arena.BeginFight(player);
    }
}
