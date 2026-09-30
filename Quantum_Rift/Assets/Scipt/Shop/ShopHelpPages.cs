using System.Collections.Generic;
using UnityEngine;

// คู่มือร้านสร้างจาก Stock/WeaponData/LootTable ชุดที่ผู้เล่นกำลังดู ไม่สุ่มใหม่และไม่หักเงิน
public static class ShopHelpPages
{
    static readonly Color Blue=new Color32(130,231,242,255),Gold=new Color32(255,209,111,255),Red=new Color32(255,148,158,255),Green=new Color32(150,226,179,255);
    static GameHelpWindow.Copy T(string th,string en)=>new GameHelpWindow.Copy{thai=th,english=en};
    static GameHelpWindow.Section S(string th,string en,string a,string b,Color c)=>new GameHelpWindow.Section{heading=T(th,en),body=T(a,b),accent=c};
    static GameHelpWindow.Page P(string tabTh,string tabEn,string th,string en,string subTh,string subEn,params GameHelpWindow.Section[] sections)=>new GameHelpWindow.Page{tab=T(tabTh,tabEn),title=T(th,en),summary=T(subTh,subEn),sections=sections};
    public static GameHelpWindow.Page[] Create(ShopWindow window)
    {
        var result=new List<GameHelpWindow.Page>();
        bool buffs=window.IsBuffShop;var pool=window.HelpPool;
        float hp=pool!=null?pool.hpRestore:3;int energy=pool!=null?pool.energyRestore:100;
        result.Add(buffs?
            P("วิธีซื้อบัพ","Buff guide","บัพประจำรอบและข้อเสีย","Run buffs and trade-offs","เลือกสินค้าในแถบซ้ายเพื่อดูตัวเลขของข้อเสนอนั้น","Choose an offer on the left to see its actual stats",
                S("เพิ่มค่าสูงสุด / เพิ่มดาเมจ","Raise maximum stats / damage","บัพเลือดและพลังงานเพิ่มค่าสูงสุด บัพโจมตีเพิ่มดาเมจเป็นหน่วย\nไม่ใช่ขวดยาฟื้นฟู และมีผลจนจบรอบการเล่น","HP and energy buffs raise their maximums. Damage buffs add flat damage.\nThese are run-long bonuses, not ordinary recovery potions.",Blue),
                S("ข้อเสนอพิเศษมีข้อแลกเปลี่ยน","A stronger offer has a drawback","หนึ่งในสี่ข้อเสนอให้ผลดีมาก แต่ลดสแตตบางอย่างทันที\nอ่านทั้งค่าบวกและค่าลบก่อนตัดสินใจซื้อ","One of four offers has a strong benefit and an immediate stat penalty.\nRead both positive and negative values before buying.",Red),
                S("เริ่มเกมใหม่ บัพจะรีเซ็ต","Buffs reset on a new run","บัพอยู่ต่อเมื่อข้ามด่าน แต่ไม่ติดไปในรอบใหม่\nการเปิดรายละเอียดไม่ซื้อสินค้า ปิดแล้วกลับไปร้านเดิมได้","Buffs carry across stages, but not into a new run.\nViewing details never buys an item. Close this guide to return to the shop.",Gold)):
            P("วิธีซื้อของ","Shop guide","ระดับอาวุธ ยา และพลังงาน","Rarity, potions and energy","เลือกสินค้าในแถบซ้ายเพื่อดูค่าจริงก่อนซื้อ","Choose an item on the left to inspect its actual values",
                S("สีและระดับของอาวุธ","Weapon colors and rarity","สีขาว = ทั่วไป  ·  สีฟ้า = หายาก  ·  สีทอง = ตำนาน\nแต่ละชิ้นมีดาเมจ ความเร็ว และความสามารถต่างกัน","White = Common  ·  Blue = Rare  ·  Gold = Legendary\nWeapons differ in damage, attack speed and special effects.",Gold),
                S("พลังงานต่อการโจมตี","Energy cost per attack","อาวุธแต่ละชิ้นใช้พลังงานไม่เท่ากัน ถ้าไม่พอจะโจมตีไม่ได้\nเลขพลังงานในรายละเอียดเป็นค่าของอาวุธ ไม่ใช่ราคาซื้อ","Energy cost varies by weapon. You cannot attack without enough energy.\nThe energy stat is the weapon's cost, not its purchase price.",Blue),
                S("ขวดยาฟื้นฟู ไม่ใช่บัพเพิ่มค่าสูงสุด","Recovery potions do not raise your maximum",
                    $"ร้านนี้: ขวดยาเลือด +{hp:0.#}  ·  ขวดยาพลังงาน +{energy}\nฟื้นฟูไม่เกินค่าสูงสุด ถ้าเต็มแล้วขวดจะวางรอที่พื้น",
                    $"This shop: HP potion +{hp:0.#}  ·  Energy potion +{energy}\nRestores up to your maximum. At full capacity, the potion waits on the ground.",Green)));
        if(window.HelpStock!=null)for(int i=0;i<window.HelpStock.Count;i++)
        {var offer=window.HelpStock[i];if(offer!=null)result.Add(Offer(offer,pool,i));}
        return result.ToArray();
    }
    public static GameHelpWindow.Page Offer(ShopOffer o,LootTable pool,int index)
    {
        string stateTh=o.sold?"ขายแล้ว":$"ราคา {o.price} เหรียญ";
        string stateEn=o.sold?"SOLD":$"Price: {o.price} coins";
        string subTh=stateTh+"  ·  หน้านี้เป็นข้อมูล ไม่มีการซื้อสินค้า";
        string subEn=stateEn+"  ·  Information only; no purchase is made";
        if(o.kind==ShopOffer.Kind.RunBuff)
        {
            string positiveTh=Deltas(o,true,true),positiveEn=Deltas(o,true,false),negativeTh=Deltas(o,false,true),negativeEn=Deltas(o,false,false);
            return P("บัพ "+(index+1),"Buff "+(index+1),o.DisplayName,o.DisplayName,subTh,subEn,
                S("ผลที่ได้รับ","Benefits",positiveTh,positiveEn,Green),
                S(o.risky?"ข้อเสียมีผลทันที":"ไม่มีข้อเสียในข้อเสนอนี้",o.risky?"Immediate drawback":"No drawback on this offer",negativeTh,negativeEn,Red),
                S("ระยะเวลาและเงื่อนไข","Duration and conditions","มีผลจนจบรอบ ข้ามด่านแล้วยังอยู่ เริ่มเกมใหม่จะรีเซ็ต\nซื้อไม่ได้หากข้อเสียทำให้เลือดหรือพลังงานสูงสุดต่ำเกินไป","Lasts for this run, including later stages. Resets on a new game.\nAn offer is blocked if its penalty would lower maximum stats too far.",Gold));
        }
        if(o.kind==ShopOffer.Kind.HpPotion||o.kind==ShopOffer.Kind.EnergyPotion)
        {
            bool isHp=o.kind==ShopOffer.Kind.HpPotion;
            float amount=isHp?(pool!=null?pool.hpRestore:3):(pool!=null?pool.energyRestore:100);
            string th=isHp?"ขวดยาเลือด":"ขวดยาพลังงาน",en=isHp?"Health potion":"Energy potion";
            return P(isHp?"ยาเลือด":"ยาพลังงาน",isHp?"HP potion":"Energy potion",th,en,subTh,subEn,
                S("ค่าฟื้นฟูของขวดนี้","This potion restores",$"{(isHp?"เลือด":"พลังงาน")} +{amount:0.#} หน่วย เมื่อเก็บขวด\nเป็นการฟื้นค่าที่เสียไป ไม่เพิ่มค่าสูงสุด",$"{(isHp?"HP":"Energy")} +{amount:0.#} on pickup.\nRestores a depleted resource; does not increase its maximum.",isHp?Red:Blue),
                S("ถ้าหลอดเต็มจะเป็นอย่างไร?","What if the bar is full?","ฟื้นฟูได้ไม่เกินค่าสูงสุด\nหากเต็มอยู่ จะยังไม่เก็บขวดจนกว่าค่านั้นจะลดลง","Restoration is capped at your maximum.\nAt full capacity, the potion remains available until the resource drops.",Green),
                S("ซื้อแล้วรับขวดตรงไหน?","Where does the purchased potion go?","ขวดจะวางใกล้เท้าผู้เล่น และเก็บเมื่อเข้าเงื่อนไข\nปิดคู่มือกลับไปร้าน แล้วกดปุ่มราคาเพื่อซื้อ","The potion drops near your feet and is collected when eligible.\nReturn to the shop and click its price to buy.",Gold));
        }
        var w=o.weapon;
        if(w==null)return P("สินค้า","Item","ไม่มีข้อมูลอาวุธ","No weapon data",subTh,subEn,S("ข้อมูล","Info","ยังไม่มีอาวุธผูกกับสินค้านี้","This offer has no weapon assigned.",Blue),S("ราคา","Price",stateTh,stateEn,Gold),S("กลับร้าน","Return","ปิดคู่มือเพื่อกลับร้าน","Close the guide to return.",Green));
        string rarityTh=w.rarity==WeaponRarity.Legendary?"ตำนาน (สีทอง)":w.rarity==WeaponRarity.Rare?"หายาก (สีฟ้า)":w.rarity==WeaponRarity.Starter?"เริ่มต้น (สีขาว)":"ทั่วไป (สีขาว)";
        string rarityEn=w.rarity==WeaponRarity.Legendary?"Legendary (gold)":w.rarity==WeaponRarity.Rare?"Rare (blue)":w.rarity==WeaponRarity.Starter?"Starter (white)":"Common (white)";
        var rarityColor=WeaponPickup.RarityColor(w.rarity);
        string abilityTh=string.IsNullOrWhiteSpace(w.abilityDescription)?"ไม่มีคำอธิบายความสามารถพิเศษเพิ่มเติม":w.abilityDescription;
        string abilityEn=string.IsNullOrWhiteSpace(w.abilityDescription)?"No additional special ability description.":w.abilityDescription;
        return P("อาวุธ "+Mathf.Max(1,index-1),"Weapon "+Mathf.Max(1,index-1),w.weaponName,w.weaponName,subTh,subEn,
            S("ระดับ: "+rarityTh,"Rarity: "+rarityEn,$"ดาเมจพื้นฐาน {w.attackDamage:0.#}  ·  ความเร็ว {w.attackSpeed:0.##} ครั้ง/วินาที\nค่าพื้นฐานนี้ยังไม่รวมบัพหรือพรของผู้เล่น",$"Base damage {w.attackDamage:0.#}  ·  Attack speed {w.attackSpeed:0.##}/s\nBase stats shown here exclude the player's buffs and blessings.",rarityColor),
            S("พลังงานที่ใช้","Energy requirement",$"ใช้พลังงาน {w.energyCost} หน่วยต่อการโจมตี\n{(w.energyCost==0?"อาวุธนี้โจมตีได้โดยไม่เสียพลังงาน":"ต้องมีพลังงานเพียงพอก่อนเริ่มโจมตี")}",$"Energy cost: {w.energyCost} per attack.\n{(w.energyCost==0?"This weapon can attack without spending energy.":"You need enough energy before starting an attack.")}",Blue),
            S("ความสามารถของอาวุธ","Weapon ability",abilityTh,abilityEn,Gold));
    }
    static string Deltas(ShopOffer o,bool positive,bool thai)
    {
        var lines=new List<string>();
        Add(lines,o.hpDelta,positive,thai?"เลือดสูงสุด":"Maximum HP");Add(lines,o.energyDelta,positive,thai?"พลังงานสูงสุด":"Maximum energy");Add(lines,o.damageDelta,positive,thai?"ดาเมจ":"Damage");
        return lines.Count>0?string.Join("\n",lines):(positive?(thai?"ไม่มีค่าสเตตัสเพิ่ม":"No stat increase."):(thai?"ไม่ลดค่าสเตตัสอื่น":"No other stats are reduced."));
    }
    static void Add(List<string> lines,int value,bool positive,string label){if(positive?value>0:value<0)lines.Add(label+" "+value.ToString("+0;-0"));}
}
