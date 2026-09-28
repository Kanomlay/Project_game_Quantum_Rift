using System.Collections;
using UnityEngine;

/// <summary>Health and visual phase bridge. Attack AI lives in ArchitectBossAI (same object).</summary>
public sealed class ArchitectBossHealth : MonoBehaviour
{
    [Min(1)] public float maxHealth=300f;
    [Range(.1f,.9f)] public float phaseTwoThreshold=.5f;
    [Range(.05f,.45f)] public float enragedThreshold=.25f;
    [Min(.1f)] public float transformationSeconds=1f;
    public GameObject phaseOne, phaseTwo;
    public LivingBossArena arena;
    public Collider2D hitbox;
    public Transform healthBarFill;
    public float CurrentHealth {get;private set;}
    public bool IsDefeated {get;private set;}
    public bool IsTransforming {get;private set;}
    public bool Invulnerable {get;set;}          // AI ตั้งระหว่างคำราม/ฉากสำคัญ ตีไม่เข้า
    public event System.Action<float> Damaged;   // AI ใช้กะพริบขาว
    public event System.Action Defeated;         // มีคนรับ = คนรับเล่นฉากตายแล้วเรียก FinishDeath เอง
    public float DamageScale {get;set;}=1f;      // AI ตั้งตอนบอสเซ (รับดาเมจแรงขึ้น)
    public System.Func<bool> InterceptDeath;     // คืน true = ยังไม่ตาย (ฉากแกนกลางถล่ม) เลือดค้าง 0 จนกว่าจะ Revive / Kill
    bool fighting,secondForm;
    void Awake(){ResetHealth();}
    public void ResetHealth()
    {
        StopAllCoroutines();CurrentHealth=Mathf.Max(1,maxHealth);IsDefeated=false;IsTransforming=false;Invulnerable=false;DamageScale=1f;secondForm=false;fighting=false;
        phaseOne.SetActive(true);phaseTwo.SetActive(false);hitbox.enabled=true;RefreshBar();
    }
    public void BeginFight(){if(!IsDefeated)fighting=true;}
    public void CancelFight(){fighting=false;StopAllCoroutines();IsTransforming=false;}
    public void TakeDamage(float amount)
    {
        if(!fighting||IsDefeated||IsTransforming||Invulnerable||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
        bool crit=Random.value<PlayerStats.CritChance;if(crit)amount*=PlayerStats.CritMultiplier; // คริติคอลของผู้เล่น
        amount*=Mathf.Max(0,DamageScale);
        // ร่างแรกเลือดล็อกที่เส้นแปลงร่าง เบิร์สต์จังหวะเดียวข้ามร่างสองไม่ได้
        float floor=secondForm?0:maxHealth*phaseTwoThreshold;
        CurrentHealth=Mathf.Max(floor,CurrentHealth-amount);RefreshBar();
        DamageNumbers.Spawn(hitbox!=null?hitbox.bounds.center+Vector3.up*hitbox.bounds.extents.y*.6f:transform.position+Vector3.up,amount,DamageNumbers.Kind.Enemy,0f,crit||DamageScale>1f);
        Damaged?.Invoke(amount);
        if(CurrentHealth<=0)
        {
            if(InterceptDeath!=null&&InterceptDeath())return;
            Die();return;
        }
        if(!secondForm&&!IsTransforming&&CurrentHealth/maxHealth<=phaseTwoThreshold)StartCoroutine(TransformPhase());
        else if(secondForm)arena.SetPhase(CurrentHealth/maxHealth<=enragedThreshold?3:2);
    }
    IEnumerator TransformPhase()
    {
        IsTransforming=true;
        var animation=phaseOne.GetComponentInChildren<ArchitectPhase1Animation>();
        if(animation!=null)animation.PlayPhase2Charge();
        yield return new WaitForSeconds(Mathf.Max(.1f,transformationSeconds));
        if(IsDefeated||!fighting)yield break;
        phaseOne.SetActive(false);phaseTwo.SetActive(true);secondForm=true;IsTransforming=false;
        arena.SetPhase(CurrentHealth/maxHealth<=enragedThreshold?3:2);
    }
    public void Kill(){if(IsDefeated)return;CurrentHealth=0;RefreshBar();Die();}
    void Die()
    {
        StopAllCoroutines();IsDefeated=true;IsTransforming=false;fighting=false;hitbox.enabled=false;
        SummaryManager.enemiesDefeatedCount++;
        if(Defeated!=null)Defeated.Invoke();else FinishDeath();
    }
    // ฟื้นจากฉากแกนกลางถล่ม (ผู้เล่นทำลายแกนไม่ทัน)
    public void Revive(float amount)
    {
        if(IsDefeated)return;
        CurrentHealth=Mathf.Clamp(amount,1,maxHealth);hitbox.enabled=true;Invulnerable=false;RefreshBar();
        if(secondForm)arena.SetPhase(CurrentHealth/maxHealth<=enragedThreshold?3:2);
    }
    // ซ่อนร่างแล้วเปิดประตูออก (ไม่มี AI เรียกทันทีตอนเลือดหมด มี AI เรียกหลังฉากสลาย)
    public void FinishDeath()
    {
        phaseOne.SetActive(false);phaseTwo.SetActive(false);
        arena.EndBattle(true);
    }
    void RefreshBar()
    {
        if(healthBarFill!=null)healthBarFill.localScale=new Vector3(Mathf.Clamp01(CurrentHealth/Mathf.Max(1,maxHealth)),1,1);
    }
    void OnDisable(){StopAllCoroutines();fighting=false;IsTransforming=false;}
}
