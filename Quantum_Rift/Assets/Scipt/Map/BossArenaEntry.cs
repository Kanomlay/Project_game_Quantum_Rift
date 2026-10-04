using UnityEngine;
using System.Collections;
public sealed class BossArenaEntry : MonoBehaviour
{
    public LivingBossArena arena;
    bool introducing,introduced;
    void OnTriggerEnter2D(Collider2D other){Enter(other);}
    void OnTriggerStay2D(Collider2D other){Enter(other);}
    void Enter(Collider2D other)
    {
        var player=other.GetComponentInParent<PlayerStats>();
        TryEnter(player);
    }
    public void TryEnter(PlayerStats player)
    {
        if(arena==null||player==null||player.isDead||arena.IsFighting||arena.IsComplete||introducing)return;
        if((MapManager.instance!=null&&MapManager.instance.IsLoading)||CinematicDirector.IsOpen)return;
        if(introduced){arena.BeginFight(player);return;}
        introducing=true;StartCoroutine(EnterAfterIntro(player));
    }
    IEnumerator EnterAfterIntro(PlayerStats player)
    {
        if(CinematicDirector.Instance!=null&&arena.boss!=null)
            yield return CinematicDirector.Instance.PlayBoss(arena.boss.gameObject);
        introducing=false;introduced=true;
        if(player!=null&&!player.isDead&&player.currentHP>0)arena.BeginFight(player);
    }
}
