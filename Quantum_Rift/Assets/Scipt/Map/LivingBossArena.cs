using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class LivingBossArena : MonoBehaviour
{
    public ArchitectBossHealth boss;
    public ArenaHazardZone[] zones;
    public AnimatedRoomGate entranceGate;
    public GameObject exitPortal;
    public Tilemap floor;
    public SpriteRenderer core;
    [Min(1)] public float warningSeconds=1.5f;
    [Min(1)] public float phaseGraceSeconds=2f;
    public int CurrentPhase { get; private set; }=1;
    public bool IsFighting { get; private set; }
    public bool IsComplete { get; private set; }
    Coroutine cycle;
    PlayerStats participant;
    Color originalFloor=new Color(.92f,.9f,.92f,1);
    void Awake()
    {
        originalFloor=floor!=null?floor.color:Color.white;
        if(exitPortal!=null)exitPortal.SetActive(false);
        Safe();
    }
    public void BeginFight(PlayerStats player)
    {
        if(IsFighting||IsComplete||player==null||player.isDead||player.currentHP<=0)return;
        participant=player;IsFighting=true;
        if(entranceGate!=null)entranceGate.SetClosed(true);
        boss.BeginFight();SetPhase(1);
    }
    public void SetPhase(int phase)
    {
        if(CurrentPhase==Mathf.Clamp(phase,1,3)&&cycle!=null)return;
        CurrentPhase=Mathf.Clamp(phase,1,3);
        if(floor!=null)floor.color=CurrentPhase==1?originalFloor:CurrentPhase==2?new Color(.92f,.75f,.79f,1):new Color(.82f,.61f,.72f,1);
        if(cycle!=null)StopCoroutine(cycle);
        Safe();
        if(IsFighting)cycle=StartCoroutine(HazardCycle());
    }
    IEnumerator HazardCycle()
    {
        yield return new WaitForSeconds(Mathf.Max(1,phaseGraceSeconds));
        int turn=0;
        while(IsFighting)
        {
            int first=turn%zones.Length;
            int second=(first+(CurrentPhase==2?2:1))%zones.Length;
            zones[first].SetState(ArenaHazardZone.State.Warning);
            if(CurrentPhase>1)zones[second].SetState(ArenaHazardZone.State.Warning);
            yield return new WaitForSeconds(Mathf.Max(1,warningSeconds));
            zones[first].SetState(ArenaHazardZone.State.Active);
            if(CurrentPhase>1)zones[second].SetState(ArenaHazardZone.State.Active);
            yield return new WaitForSeconds(CurrentPhase==1?2f:CurrentPhase==2?2.5f:3f);
            Safe();turn++;
            yield return new WaitForSeconds(CurrentPhase==1?4f:CurrentPhase==2?3f:2f);
        }
    }
    void Update()
    {
        if(IsFighting&&(participant==null||participant.isDead||participant.currentHP<=0))EndBattle(false);
        if(core!=null)
        {
            float pulse=IsFighting?.88f+.12f*Mathf.Sin(Time.time*(1.5f+CurrentPhase)):1f;
            core.color=new Color(pulse,1,pulse,1);
        }
    }
    public void EndBattle(bool victory)
    {
        IsFighting=false;IsComplete=IsComplete||victory;
        if(cycle!=null){StopCoroutine(cycle);cycle=null;}
        Safe();
        if(entranceGate!=null)entranceGate.SetClosed(false);
        if(exitPortal!=null)exitPortal.SetActive(victory);
        if(!victory&&boss!=null)boss.CancelFight();
    }
    void Safe(){if(zones!=null)foreach(var zone in zones)if(zone!=null)zone.SetState(ArenaHazardZone.State.Safe);}
    void OnDisable(){IsFighting=false;if(cycle!=null)StopCoroutine(cycle);cycle=null;Safe();}
}
