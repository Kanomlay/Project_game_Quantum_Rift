using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// อยู่กับร้านอาวุธทุกครั้งที่เหตุการณ์ร้านถูกเสก ไม่สร้างร้านบัพเพิ่มในร้านบัพเอง
public sealed class BuffShopCompanion:MonoBehaviour
{
    public GameObject buffShopPrefab;
    public GameObject Spawned {get;private set;}
    void Start(){Spawn();}
    public GameObject Spawn()
    {
        if(Spawned!=null || buffShopPrefab==null)return Spawned;
        var room=GetComponentInParent<RoomController>();
        var grid=room!=null?room.GetComponentInParent<Grid>():null;
        var floor=grid!=null?grid.GetComponentsInChildren<Tilemap>().FirstOrDefault(t=>t.name.StartsWith("Floor")):null;
        var walls=grid!=null?grid.GetComponentsInChildren<Tilemap>().FirstOrDefault(t=>t.name.StartsWith("Bulkheads")):null;
        var offsets=new[]{new Vector2(3.2f,0),new Vector2(-3.2f,0),new Vector2(0,-3.2f),new Vector2(0,3.2f),new Vector2(3.2f,-2.5f),new Vector2(-3.2f,-2.5f)}
            .Concat(Enumerable.Range(0,16).Select(i=>new Vector2(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8))*4.5f));
        foreach(var offset in offsets)
        {
            Vector2 p=(Vector2)transform.position+offset;
            // ผังบางแบบใช้ trigger เฉพาะทางเข้า ไม่ใช่พื้นที่ห้องทั้งหมด จึงตรวจพื้นจริงรอบตัวร้านแทน
            if(room!=null&&Vector2.Distance(p,room.transform.position)>7.5f)continue;
            if(floor!=null)
            {
                bool clear=true;
                foreach(var sample in new[]{Vector2.zero,new Vector2(-1.2f,0),new Vector2(1.2f,0),new Vector2(-1.2f,1.6f),new Vector2(1.2f,1.6f)})
                    if(!floor.HasTile(floor.WorldToCell(p+sample))||(walls!=null&&walls.HasTile(walls.WorldToCell(p+sample)))){clear=false;break;}
                if(!clear)continue;
                if(room.doors!=null&&room.doors.Any(d=>d!=null&&Vector2.Distance(p,d.transform.position)<2.8f))continue;
            }
            else if(room!=null&&!room.GetComponents<Collider2D>().Any(c=>c.isTrigger&&c.OverlapPoint(p)))continue;
            if(Physics2D.OverlapBoxAll(p+Vector2.up*.8f,new Vector2(2.6f,1.8f),0).Any(c=>LootPlacement.IsSolid(c)))continue;
            Spawned=Instantiate(buffShopPrefab,p,Quaternion.identity,transform.parent);
            var original=GetComponent<ShopClickable>();var shop=Spawned.GetComponent<ShopClickable>();
            if(original!=null){shop.itemPool=original.itemPool;shop.legendaryPrice=Mathf.Max(original.legendaryPrice,Mathf.Max(original.commonPrice,Mathf.Max(original.rarePrice,original.potionPrice)));}
            return Spawned;
        }
        Debug.LogWarning("ไม่มีพื้นที่ว่างข้างร้านสำหรับร้านบัพ: "+name);return null;
    }
}
