using System.Collections;
using UnityEngine;

/// <summary>Health and visual phase bridge only. Does not implement boss attack AI.</summary>
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
    bool fighting,secondForm;
    void Awake(){ResetHealth();}
    public void ResetHealth()
    {
        StopAllCoroutines();CurrentHealth=Mathf.Max(1,maxHealth);IsDefeated=false;IsTransforming=false;secondForm=false;fighting=false;
        phaseOne.SetActive(true);phaseTwo.SetActive(false);hitbox.enabled=true;RefreshBar();
    }
    public void BeginFight(){if(!IsDefeated)fighting=true;}
    public void CancelFight(){fighting=false;StopAllCoroutines();IsTransforming=false;}
    public void TakeDamage(float amount)
    {
        if(!fighting||IsDefeated||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
        CurrentHealth=Mathf.Max(0,CurrentHealth-amount);RefreshBar();
        if(CurrentHealth<=0)
        {
            StopAllCoroutines();IsDefeated=true;IsTransforming=false;fighting=false;hitbox.enabled=false;
            phaseOne.SetActive(false);phaseTwo.SetActive(false);
            SummaryManager.enemiesDefeatedCount++;arena.EndBattle(true);return;
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
    void RefreshBar()
    {
        if(healthBarFill!=null)healthBarFill.localScale=new Vector3(Mathf.Clamp01(CurrentHealth/Mathf.Max(1,maxHealth)),1,1);
    }
    void OnDisable(){StopAllCoroutines();fighting=false;IsTransforming=false;}
}
