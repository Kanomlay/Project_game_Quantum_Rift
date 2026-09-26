using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

/// <summary>Places breakable props and themed traps in the active, randomized layout.</summary>
public sealed class MapGameplayFeatures : MonoBehaviour
{
    public enum Theme { Spaceship, LivingForest }
    public Theme theme;
    public Sprite trapSprite;
    public Sprite healthPotionSprite;
    public Sprite energyPotionSprite;
    bool initialized;
    public float minSpawnInterval = 4f;
    public float maxSpawnInterval = 7f;
    [Range(0,1)] public float aheadChance = .35f;
    public int maxActiveTraps = 3;
    readonly List<StageTrap> activeTraps = new List<StageTrap>();
    System.Random random;
    MapRoomGraph graph;
    Tilemap floor, walls;
    Transform[] gates, portals;
    PlayerStats player;
    Vector2 lastPlayerPosition, heading = Vector2.right;
    float nextSpawn;
    public int TotalSpawned { get; private set; }
    public bool LastSpawnWasAhead { get; private set; }
    public int ActiveTrapCount { get { activeTraps.RemoveAll(t=>t==null || t.CurrentPhase==StageTrap.Phase.Expired); return activeTraps.Count; } }

    public void Initialize(Vector2 playerSpawn)
    {
        if (initialized) return;
        initialized = true;
        Physics2D.SyncTransforms();
        random = new System.Random(Guid.NewGuid().GetHashCode());
        graph = new MapRoomGraph(gameObject);
        var tiles = GetComponentsInChildren<Tilemap>(false);
        floor = tiles.FirstOrDefault(t=>t.name=="Floor_Unified64");
        walls = tiles.FirstOrDefault(t=>t.name=="Bulkheads_Unified64");
        gates = GetComponentsInChildren<AnimatedRoomGate>(false).Select(g=>g.transform).ToArray();
        portals = GetComponentsInChildren<MapPortal>(false).Select(p=>p.transform).ToArray();
        PlaceBreakables(random);
        TrySpawnTrap(playerSpawn,Vector2.up,false);
        TrySpawnTrap(playerSpawn,Vector2.up,false);
        nextSpawn = Time.time + Range(minSpawnInterval,maxSpawnInterval);
    }

    void PlaceBreakables(System.Random random)
    {
        var available = new List<MapAssetRandomizer.Slot>();
        foreach (var randomizer in GetComponentsInChildren<MapAssetRandomizer>(false))
            foreach (var slot in randomizer.slots)
                if (slot != null && slot.display != null && slot.display.gameObject.activeInHierarchy &&
                    slot.display.enabled && slot.display.GetComponent<BreakableProp>() == null)
                    available.Add(slot);
        int count = Mathf.Min(6, available.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = random.Next(available.Count);
            var slot = available[pick];
            available.RemoveAt(pick);
            var prop = slot.display.gameObject.AddComponent<BreakableProp>();
            prop.Configure(slot.display, slot.blockers, healthPotionSprite, energyPotionSprite, transform);
        }
    }

    float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());

    void Update()
    {
        if(!initialized || PauseManager.isGamePaused || ShopWindow.IsOpen || Time.timeScale<=0)return;
        if(player==null)
        {
            var hero=GameObject.FindGameObjectWithTag("Player");if(hero==null)return;
            player=hero.GetComponent<PlayerStats>();lastPlayerPosition=hero.transform.position;
        }
        if(player==null || player.isDead)return;
        Vector2 position=player.transform.position;
        Vector2 moved=position-lastPlayerPosition;
        if(moved.sqrMagnitude>.0001f)heading=moved.normalized;
        else if(player.weaponController!=null)heading=player.weaponController.transform.right;
        lastPlayerPosition=position;
        if(Time.time<nextSpawn)return;
        bool ahead=random.NextDouble()<aheadChance;
        if(!TrySpawnTrap(position,heading,ahead) && ahead)TrySpawnTrap(position,heading,false);
        nextSpawn=Time.time+Range(Mathf.Max(2,minSpawnInterval),Mathf.Max(minSpawnInterval,maxSpawnInterval));
    }

    // Used by the live scheduler and the editor placement checks.
    public bool TrySpawnTrap(Vector2 playerPosition,Vector2 forward,bool ahead)
    {
        if(trapSprite==null || graph==null || floor==null || ActiveTrapCount>=Mathf.Max(1,maxActiveTraps))return false;
        var rooms=graph.nodes.Where(n=>n.room!=null && !n.room.IsSafeRoom).ToArray();
        if(rooms.Length==0)return false;
        if(forward.sqrMagnitude<.01f)forward=Vector2.right;
        forward.Normalize();
        for(int attempt=0;attempt<90;attempt++)
        {
            Vector2 spot;
            MapRoomGraph.Node room;
            if(ahead)
            {
                var side=new Vector2(-forward.y,forward.x);
                spot=playerPosition+forward*Range(3.4f,5f)+side*Range(-.65f,.65f);
                room=rooms.FirstOrDefault(n=>n.Contains(spot));
                if(room==null)continue;
            }
            else
            {
                room=rooms[random.Next(rooms.Length)];
                var area=room.areas[random.Next(room.areas.Length)];
                var b=area.bounds;
                spot=new Vector2(Range(b.min.x,b.max.x),Range(b.min.y,b.max.y));
            }
            if(!ValidSpot(spot,playerPosition,room))continue;
            if(ahead && !ClearLine(playerPosition,spot))continue;
            var trap=StageTrap.Spawn(theme==Theme.Spaceship?StageTrap.Kind.RiftPull:StageTrap.Kind.PoisonVine,
                spot,trapSprite,room.room.transform);
            activeTraps.Add(trap);TotalSpawned++;LastSpawnWasAhead=ahead;
            return true;
        }
        return false;
    }

    bool ValidSpot(Vector2 spot,Vector2 hero,MapRoomGraph.Node room)
    {
        if(!room.Contains(spot) || Vector2.Distance(spot,hero)<3.3f)return false;
        foreach(var offset in new[]{Vector2.zero,new Vector2(.6f,.6f),new Vector2(-.6f,.6f),new Vector2(.6f,-.6f),new Vector2(-.6f,-.6f)})
        {
            Vector2 p=spot+offset;
            if(!floor.HasTile(floor.WorldToCell(p)) || (walls!=null && walls.HasTile(walls.WorldToCell(p))))return false;
        }
        if(gates.Any(g=>Vector2.Distance(g.position,spot)<3.3f) || portals.Any(p=>Vector2.Distance(p.position,spot)<3.3f))return false;
        if(activeTraps.Any(t=>t!=null && Vector2.Distance(t.transform.position,spot)<radiusClearance))return false;
        foreach(var col in Physics2D.OverlapCircleAll(spot,.7f))if(LootPlacement.IsSolid(col))return false;
        return true;
    }
    const float radiusClearance=4f;
    static bool ClearLine(Vector2 from,Vector2 to)
    {
        foreach(var hit in Physics2D.LinecastAll(from,to))if(LootPlacement.IsSolid(hit.collider))return false;
        return true;
    }
}
