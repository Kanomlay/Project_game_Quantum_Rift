using UnityEngine;

/// <summary>กระสุน/หินแยกจากเฟรมตัวมอนสเตอร์ ตรวจเส้นทางทุกเฟรมเพื่อไม่ทะลุเป้าหมาย</summary>
public sealed class MonsterAttackProjectile : MonoBehaviour, IEnemyBullet
{
    public bool IsRock { get; private set; }
    Vector2 direction;
    float speed,damage,remaining,radius;
    Transform owner;
    bool spent;
    public void Launch(MonsterCombatActions source,Vector2 heading,float velocity,float lifetime,float power,bool rock)
    {
        owner=source.transform;direction=heading.normalized;speed=velocity;remaining=lifetime;damage=power;IsRock=rock;
        radius=rock?.18f:.18f; // กระสุนปืน Phase Soldier ขยายเป็น 1.8 ให้เห็นชัด hitbox ขยายตามให้ตรงภาพ
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
    }
    void Update()
    {
        if(spent)return;
        if((remaining-=Time.deltaTime)<=0){Expire();return;}
        float step=speed*EnemyBullets.SpeedFactor(transform.position)*Time.deltaTime; // พรสนามชะลอกระสุน
        // CircleCastAll เรียงตามระยะ จึงชนกำแพงก่อนเป้าหมายที่อยู่หลังกำแพง
        foreach(var hit in Physics2D.CircleCastAll(transform.position,radius,direction,step))
        {
            var c=hit.collider;if(c==null)continue;
            if(owner!=null&&c.transform.IsChildOf(owner))continue;
            var player=c.GetComponentInParent<PlayerStats>();
            if(player!=null)
            {
                MonsterCombatActions.DamagePlayer(player,damage,transform.position);Expire();return;
            }
            if(c.isTrigger||c.GetComponentInParent<MonsterController>()!=null)continue;
            Expire();return;
        }
        transform.position+=(Vector3)(direction*step);
        if(IsRock)transform.Rotate(0,0,210f*Time.deltaTime);
    }
    void Expire(){spent=true;Destroy(gameObject);}
    // ทะเบียนกระสุนศัตรู (พรคมสลายมิติฟันลบได้ สนามชะลอกระสุนทำให้ช้าลง)
    void OnEnable(){EnemyBullets.Register(this);}
    void OnDisable(){EnemyBullets.Unregister(this);}
}
