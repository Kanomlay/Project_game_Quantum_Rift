using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// สุ่มครั้งเดียวตอนเข้าห้องครั้งแรก จุดเดิมคงอยู่เมื่อย้อนกลับมา ไม่สุ่มไล่ตามผู้เล่น
public sealed class RoomEntryTrapSpawner:MonoBehaviour
{
    public Sprite[] frames;
    public int count=3;
    public bool HasRolled {get;private set;}
    public readonly List<FixedSpikeTrap> Spawned=new List<FixedSpikeTrap>();
    public void Activate(){ActivateWithSeed(Guid.NewGuid().GetHashCode());}
    public void ActivateWithSeed(int seed)
    {
        if(HasRolled)return;HasRolled=true;
        var room=GetComponent<RoomController>();if(room==null || room.IsSafeRoom || frames==null || frames.Length!=7)return;
        var map=GetComponentInParent<MapLayoutRandomizer>();if(map==null)return;
        var tiles=map.GetComponentsInChildren<Tilemap>();var floor=tiles.FirstOrDefault(t=>t.name=="Floor_Unified64");
        var walls=tiles.FirstOrDefault(t=>t.name=="Bulkheads_Unified64");if(floor==null)return;
        var areas=room.GetComponents<Collider2D>();var rng=new System.Random(seed);
        var hero=GameObject.FindGameObjectWithTag("Player");
        // ประตูวาร์ปยังซ่อนก่อนเคลียร์ก็ต้องกันพื้นที่ไว้ รวมทุกผังได้เพราะพิกัดปลอดภัยสำคัญกว่าเพิ่มจุดสุ่ม
        var portals=map.GetComponentsInChildren<MapPortal>(true);
        var candidates=new List<Vector3Int>();
        foreach(var cell in floor.cellBounds.allPositionsWithin)
        {
            if(!floor.HasTile(cell) || walls!=null&&walls.HasTile(cell))continue;
            Vector2 p=floor.GetCellCenterWorld(cell);
            if(!areas.Any(a=>a.enabled && a.isTrigger && a.OverlapPoint(p)))continue;
            if(hero!=null && Vector2.Distance(p,hero.transform.position)<3)continue;
            if(room.doors.Any(g=>g!=null && Vector2.Distance(p,g.transform.position)<3))continue;
            if(portals.Any(g=>Vector2.Distance(p,g.transform.position)<3))continue;
            if(Vector2.Distance(p,room.transform.position)<2.3f)continue;
            if(Physics2D.OverlapBoxAll(p,Vector2.one*1.2f,0).Any(LootPlacement.IsSolid))continue;
            candidates.Add(cell);
        }
        for(int i=candidates.Count-1;i>0;i--){int j=rng.Next(i+1);var t=candidates[i];candidates[i]=candidates[j];candidates[j]=t;}
        var group=new GameObject("RoomEntrySquareTraps");group.transform.SetParent(transform,false);
        foreach(var cell in candidates)
        {
            if(Spawned.Count>=count)break;
            Vector2 p=floor.GetCellCenterWorld(cell);
            if(Spawned.Any(t=>Vector2.Distance(t.transform.position,p)<2.5f))continue;
            var go=new GameObject("SquareTrap_"+cell.x+"_"+cell.y);go.transform.SetParent(group.transform,false);go.transform.position=p;
            var display=go.AddComponent<SpriteRenderer>();display.sprite=frames[0];display.sortingLayerName="object";display.sortingOrder=-2;
            float size=Mathf.Abs(floor.layoutGrid.cellSize.x*floor.transform.lossyScale.x);
            go.transform.localScale=Vector3.one*(size/frames[0].bounds.size.x);
            var area=go.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=frames[0].bounds.size;
            var trap=go.AddComponent<FixedSpikeTrap>();trap.room=room;trap.animatedDisplay=display;trap.animationFrames=frames;trap.damageArea=area;
            trap.cycleSeconds=10;trap.warningSeconds=1.2f;trap.Advance(0);Spawned.Add(trap);
        }
    }
}
