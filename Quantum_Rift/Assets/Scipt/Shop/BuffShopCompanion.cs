using System.Linq;
using UnityEngine;

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
        foreach(var offset in new[]{new Vector2(3.2f,0),new Vector2(-3.2f,0),new Vector2(0,-3.2f),new Vector2(0,3.2f),new Vector2(3.2f,-2.5f),new Vector2(-3.2f,-2.5f)})
        {
            Vector2 p=(Vector2)transform.position+offset;
            if(room!=null && !room.GetComponents<Collider2D>().Any(c=>c.isTrigger&&c.OverlapPoint(p)))continue;
            if(Physics2D.OverlapBoxAll(p+Vector2.up*.8f,new Vector2(2.6f,1.8f),0).Any(c=>LootPlacement.IsSolid(c)))continue;
            Spawned=Instantiate(buffShopPrefab,p,Quaternion.identity,transform.parent);
            var original=GetComponent<ShopClickable>();var shop=Spawned.GetComponent<ShopClickable>();
            if(original!=null){shop.itemPool=original.itemPool;shop.legendaryPrice=Mathf.Max(original.legendaryPrice,Mathf.Max(original.commonPrice,Mathf.Max(original.rarePrice,original.potionPrice)));}
            return Spawned;
        }
        Debug.LogWarning("ไม่มีพื้นที่ว่างข้างร้านสำหรับร้านบัพ: "+name);return null;
    }
}
