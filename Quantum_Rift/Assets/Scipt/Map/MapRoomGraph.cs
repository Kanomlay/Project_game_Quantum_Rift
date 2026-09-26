using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Read authored door connections; never invent a link between nearby rooms.
public sealed class MapRoomGraph
{
    public sealed class Node
    {
        public int id;
        public RoomController room;
        public Vector2 center;
        public Collider2D[] areas;
        public bool visited;
        public bool Contains(Vector2 point)
        {
            if (room == null) return Mathf.Abs(point.x-center.x)<5f && Mathf.Abs(point.y-center.y)<5f;
            return areas.Any(c=>c!=null && c.enabled && c.isTrigger && c.OverlapPoint(point));
        }
    }
    public readonly List<Node> nodes = new List<Node>();
    public readonly List<Vector2Int> edges = new List<Vector2Int>();
    public MapRoomGraph(GameObject map)
    {
        var links = new HashSet<Vector2Int>();
        int fallback=1;
        foreach(var room in map.GetComponentsInChildren<RoomController>(false))
        {
            int id=-1;
            if(room.doors!=null) foreach(var door in room.doors)
            {
                if(door==null) continue;
                var parts=door.name.Split('_');
                if(parts.Length!=4 || parts[0]!="Gate" || parts[2]!="To" ||
                    !int.TryParse(parts[1],out int from) || !int.TryParse(parts[3],out int to)) continue;
                id=from;
                links.Add(new Vector2Int(Mathf.Min(from,to),Mathf.Max(from,to)));
            }
            nodes.Add(new Node { id=id>=0?id:fallback,room=room,center=room.transform.position,
                areas=room.GetComponents<Collider2D>(),visited=room.HasBeenVisited });
            fallback++;
        }
        if(links.Any(e=>e.x==0 || e.y==0))
        {
            var spawn=map.GetComponentsInChildren<Transform>(false).FirstOrDefault(t=>t.name=="PlayerSpawn");
            nodes.Add(new Node {id=0,center=spawn!=null?(Vector2)spawn.position:(Vector2)map.transform.position});
        }
        nodes.Sort((a,b)=>a.id.CompareTo(b.id));
        foreach(var link in links.OrderBy(e=>e.x).ThenBy(e=>e.y))
        {
            int a=nodes.FindIndex(n=>n.id==link.x), b=nodes.FindIndex(n=>n.id==link.y);
            if(a>=0 && b>=0) edges.Add(new Vector2Int(a,b));
        }
    }
    public int Observe(Vector2 position)
    {
        for(int i=0;i<nodes.Count;i++) if(nodes[i].Contains(position))
        {
            nodes[i].visited=true;
            if(nodes[i].room!=null) nodes[i].room.MarkVisited();
            return i;
        }
        return -1;
    }
}
