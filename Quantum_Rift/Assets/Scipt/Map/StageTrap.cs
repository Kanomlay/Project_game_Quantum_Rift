using System.Collections;
using UnityEngine;

/// <summary>Telegraphed map trap: a purple rift pulls, a forest vine lashes and poisons.</summary>
public sealed class StageTrap : MonoBehaviour
{
    public enum Kind { RiftPull, PoisonVine }
    public enum Phase { Warning, Active, Fading, Expired }
    public Phase CurrentPhase { get; private set; } = Phase.Warning;
    public Kind kind;
    public float radius = 2.8f;
    public float warningSeconds = 1.3f;
    public float activeSeconds = 2.2f;
    public float fadeSeconds = .5f;
    SpriteRenderer display;
    Transform visual;
    PlayerStats player;
    PlayerMovement movement;
    float cycleStart;
    bool attacked;
    LineRenderer warningRing;
    Material lineMaterial;
    public float SpawnedAt => cycleStart;

    public static StageTrap Spawn(Kind type, Vector2 position, Sprite sprite, Transform parent)
    {
        var go = new GameObject(type == Kind.RiftPull ? "RiftPullTrap" : "PoisonVineTrap");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var art = new GameObject("Visual");
        art.transform.SetParent(go.transform, false);
        var renderer = art.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "object";
        renderer.sortingOrder = -2;
        float longest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        art.transform.localScale = Vector3.one * (1.35f / Mathf.Max(.01f, longest));
        var trap = go.AddComponent<StageTrap>();
        trap.kind = type;
        trap.display = renderer;
        trap.visual = art.transform;
        trap.cycleStart = Time.time;
        trap.CreateWarningRing();
        trap.Tick(Time.time);
        return trap;
    }

    void CreateWarningRing()
    {
        lineMaterial=new Material(Shader.Find("Sprites/Default"));
        var ring=new GameObject("WarningRadius");ring.transform.SetParent(transform,false);
        warningRing=ring.AddComponent<LineRenderer>();
        warningRing.sharedMaterial=lineMaterial;warningRing.useWorldSpace=false;
        warningRing.loop=true;warningRing.positionCount=48;
        warningRing.startWidth=warningRing.endWidth=.065f;
        warningRing.sortingLayerName="object";warningRing.sortingOrder=-1;
        for(int i=0;i<48;i++)
        {
            float a=i*Mathf.PI*2/48;
            warningRing.SetPosition(i,new Vector3(Mathf.Round(Mathf.Cos(a)*radius*16)/16,
                Mathf.Round(Mathf.Sin(a)*radius*16)/16,0));
        }
    }

    void Update()
    {
        if (PauseManager.isGamePaused || ShopWindow.IsOpen)
        {
            // Preserve the warning duration even when the shop does not pause Time.time.
            cycleStart += Time.deltaTime;
            return;
        }
        if (Time.timeScale <= 0f) return;
        Tick(Time.time);
    }

    public void Tick(float now)
    {
        if(CurrentPhase==Phase.Expired)return;
        float elapsed=now-cycleStart;
        if(elapsed>=warningSeconds+activeSeconds+fadeSeconds)
        {
            CurrentPhase=Phase.Expired;
            if(Application.isPlaying)Destroy(gameObject);else gameObject.SetActive(false);
            return;
        }
        CurrentPhase=elapsed<warningSeconds?Phase.Warning:
            elapsed<warningSeconds+activeSeconds?Phase.Active:Phase.Fading;
        Color tint=kind==Kind.RiftPull?new Color(.72f,.4f,1f):new Color(.3f,.95f,.58f);
        float alpha=CurrentPhase==Phase.Warning?.5f+.35f*Mathf.Abs(Mathf.Sin(elapsed*9f)):
            CurrentPhase==Phase.Fading?1f-(elapsed-warningSeconds-activeSeconds)/fadeSeconds:1f;
        display.color=new Color(tint.r,tint.g,tint.b,alpha);
        warningRing.startColor=warningRing.endColor=CurrentPhase==Phase.Warning?
            new Color(1f,.73f,.24f,alpha):new Color(tint.r,tint.g,tint.b,alpha*.4f);
        visual.localRotation=Quaternion.Euler(0,0,Mathf.Sin(elapsed*4f)*7f);
        if(CurrentPhase!=Phase.Active)return;
        if (player == null)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return;
            player = hero.GetComponent<PlayerStats>();
            movement = hero.GetComponent<PlayerMovement>();
        }
        if (player == null || player.isDead) return;
        Vector2 delta = (Vector2)transform.position - (Vector2)player.transform.position;
        if (delta.magnitude > radius) return;
        foreach(var hit in Physics2D.LinecastAll(transform.position,player.transform.position))
            if(LootPlacement.IsSolid(hit.collider))return;
        if (kind == Kind.RiftPull)
        {
            if (movement != null) movement.SetEnvironmentalPull(delta.normalized * 3.2f, .12f);
        }
        else if (!attacked)
        {
            attacked = true;
            StartCoroutine(VineLash(player.transform.position));
            player.TakeDamage(1f);
            player.ApplyPoison(3.2f, 1f, 1.2f);
        }
    }

    IEnumerator VineLash(Vector3 target)
    {
        var go = new GameObject("VineLash");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 8;
        line.startWidth = .14f;
        line.endWidth = .04f;
        line.sharedMaterial = lineMaterial;
        line.startColor = new Color(.35f,1f,.57f);
        line.endColor = new Color(.13f,.55f,.35f);
        line.sortingLayerName = "object";
        line.sortingOrder = 12;
        Vector3 start = transform.position;
        for (float t = 0f; t < .3f; t += Time.deltaTime)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float p = i / 7f;
                Vector3 point = Vector3.Lerp(start,target,p);
                point += Vector3.Cross((target-start).normalized,Vector3.forward) *
                    Mathf.Sin(p * Mathf.PI * 3f - t * 22f) * .12f;
                line.SetPosition(i,point);
            }
            yield return null;
        }
        Destroy(go);
    }
    void OnDestroy()
    {
        if(lineMaterial==null)return;
        if(Application.isPlaying)Destroy(lineMaterial);else DestroyImmediate(lineMaterial);
    }
}
