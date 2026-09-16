using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer),typeof(CircleCollider2D))]
public sealed class ArenaHazardZone : MonoBehaviour
{
    public enum State { Safe, Warning, Active }
    public Sprite warningSprite, activeSprite;
    [Min(0)] public float damage = 1f;
    [Min(.1f)] public float hitInterval = 1.1f;
    public State CurrentState { get; private set; }
    readonly Dictionary<PlayerStats,float> nextHit = new Dictionary<PlayerStats,float>();
    SpriteRenderer display;
    CircleCollider2D area;
    void Awake() { Cache(); SetState(State.Safe); }
    void Cache() { if(display==null)display=GetComponent<SpriteRenderer>();if(area==null)area=GetComponent<CircleCollider2D>(); }
    public void SetState(State state)
    {
        Cache();CurrentState=state;area.isTrigger=true;area.enabled=state==State.Active;
        display.enabled=state!=State.Safe;display.sprite=state==State.Warning?warningSprite:activeSprite;
        display.color=Color.white;
        if(state==State.Safe)nextHit.Clear();
    }
    void Update()
    {
        if(CurrentState==State.Warning)display.color=new Color(1,1,1,.7f+.3f*Mathf.Sin(Time.time*9f));
    }
    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnTriggerStay2D(Collider2D other) { TryHit(other); }
    void TryHit(Collider2D other)
    {
        if(CurrentState!=State.Active||Time.timeScale<=0)return;
        var player=other.GetComponentInParent<PlayerStats>();
        if(player==null||player.isDead||player.currentHP<=0)return;
        if(nextHit.TryGetValue(player,out float ready)&&Time.time<ready)return;
        nextHit[player]=Time.time+Mathf.Max(.1f,hitInterval);
        player.TakeDamage(Mathf.Max(0,damage));
    }
    void OnDisable() { Cache();CurrentState=State.Safe;area.enabled=false;display.enabled=false;nextHit.Clear(); }
}
