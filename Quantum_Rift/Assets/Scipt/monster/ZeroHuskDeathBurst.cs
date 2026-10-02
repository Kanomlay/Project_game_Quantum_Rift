using System.Collections;
using UnityEngine;

public sealed class ZeroHuskDeathBurst:MonoBehaviour
{
    public Sprite[] frames;
    public float radius=2.2f,damage=2f,burnSeconds=3.8f;
    public bool Exploded {get;private set;}
    public IEnumerator Play(Transform player)
    {
        var display=GetComponent<SpriteRenderer>();Exploded=false;
        if(frames==null || frames.Length!=7)yield break;
        for(int i=0;i<frames.Length;i++)
        {
            display.sprite=frames[i];display.color=Color.white;
            // สามเฟรมแรกเตือน 0.75 วินาที ก่อนเกิดแรงระเบิดจริง
            if(i==3)
            {
                Exploded=true;
                Sfx.PlayAt(SfxId.MonExplode,transform.position);
                if(player!=null && Vector2.Distance(player.position,transform.position)<=radius && Clear(player.position))
                {
                    var stats=player.GetComponent<PlayerStats>();
                    if(stats!=null && stats.CanTakeHit)
                    {float before=stats.currentHP;stats.TakeDamage(damage);if(stats.currentHP<before)stats.ApplyBurn(burnSeconds);}
                }
            }
            yield return new WaitForSeconds(i<3?.25f:.1f);
        }
    }
    bool Clear(Vector2 target)
    {
        foreach(var hit in Physics2D.LinecastAll(transform.position,target))
            if(hit.collider!=null && LootPlacement.IsSolid(hit.collider) && hit.collider.GetComponentInParent<PlayerStats>()==null && hit.collider.GetComponentInParent<MonsterController>()==null)return false;
        return true;
    }
}
