using System.Collections.Generic;
using UnityEngine;

// ค่าซื้อเพิ่มเป็นหน่วย อยู่บนผู้เล่นของรอบนี้ ไม่เขียนลง CharacterData หรือ PlayerPrefs
public static class RunStatBuffs
{
    public static List<ShopOffer> Stock(int ceiling)
    {
        int floor=Mathf.Max(50,ceiling);bool large=Random.value<.5f;
        var result=new List<ShopOffer>{Offer("ยาเสริมชีวิต",5,0,0,floor+30),
            Offer("ยาแกนพลังงาน",0,large?100:50,0,floor+(large?90:40)),Offer("ยาเสริมพลังโจมตี",0,0,2,floor+70)};
        int roll=Random.Range(0,3);
        result.Add(roll==0?Offer("สัญญาเขี้ยวแดง",-3,0,6,floor+110,true):
            roll==1?Offer("สัญญาเลือดอสูร",12,-30,0,floor+110,true):Offer("สัญญาแกนร้าว",-3,150,0,floor+110,true));return result;
    }
    static ShopOffer Offer(string name,int hp,int energy,int damage,int price,bool risky=false)
        =>new ShopOffer(ShopOffer.Kind.RunBuff,price){buffName=name,hpDelta=hp,energyDelta=energy,damageDelta=damage,risky=risky};
    public static bool CanApply(PlayerStats player,ShopOffer offer)=>player!=null && !player.isDead &&
        player.maxHP+offer.hpDelta>=1 && player.maxEnergy+offer.energyDelta>=10;
    public static float Damage(float value,PlayerStats owner=null)
    {
        if(owner==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)owner=go.GetComponent<PlayerStats>();}
        return Mathf.Max(0,(value+(owner!=null?owner.runDamageBonus:0))*BlessingManager.DamageScale); // พรก้าวพ้นรอยแยกระดับ 3
    }
}
