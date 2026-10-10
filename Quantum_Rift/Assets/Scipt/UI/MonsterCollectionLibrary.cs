using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Quantum Rift/Monster Collection")]
public sealed class MonsterCollectionLibrary : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public MonsterData data;
        public GameObject prefab;
        public Sprite portrait;
        public string name,stageThai,stageEnglish;
        public bool boss;
        public float Health=>data!=null?data.maxHealth:prefab.GetComponent<ArchitectBossHealth>().maxHealth;
        public float Damage=>data!=null?data.attackDamage:prefab.GetComponent<ArchitectBossAI>().damage;
        // บอสสุดท้ายมีสองค่าจริง แสดงทั้งสองร่างแทนเลขเดียวที่ทำให้เข้าใจผิด
        public string DamageText=>prefab!=null&&prefab.GetComponent<ArchitectBossAI>() is ArchitectBossAI ai?$"{ai.damage:0.##} / {ai.phaseTwoDamage:0.##}":$"{Damage:0.##}";
        public float Speed=>data!=null?data.moveSpeed:prefab.GetComponent<ArchitectBossAI>().speedOne;
        public float Cooldown=>data!=null?data.attackCooldown:prefab.GetComponent<ArchitectBossAI>().spiralCooldown;
        // ค่าจริงอ่านจาก Data และคอมโพเนนต์ของ prefab ทุกครั้ง ไม่คัดลอกตัวเลขมาค้างไว้ในคู่มือ
        public string Abilities(bool th)
        {
            var lines=new List<string>();
            var echo=prefab.GetComponent<EchoCommanderBoss>();var ent=prefab.GetComponent<AncientEntbornBoss>();var architect=prefab.GetComponent<ArchitectBossAI>();
            if(echo!=null)
            {
                lines.Add(th?$"ฟันเป็นพัดและผลักถอยหลัง; ยิงกระสุน {echo.projectilesPerShot} นัดต่อชุด (ดาเมจนัดละ {echo.projectileDamage:0.##})":$"Cone slash with knockback; fires {echo.projectilesPerShot} shots per fan ({echo.projectileDamage:0.##} damage each).");
                lines.Add(th?"เรียกลูกน้องผ่านประตูมิติ และใช้ร่างเสียงสะท้อนเมื่อคลั่ง":"Summons minions through rifts; uses echo clones when enraged.");
            }
            else if(ent!=null)
            {
                lines.Add(th?$"ยิงเลเซอร์ไม้ มีสัญญาณเตือน {ent.laserWarning:0.##} วินาที; กระทืบและเรียกรากแทงจากพื้น":$"Wood laser with {ent.laserWarning:0.##} s warning; stomps and ground roots.");
                lines.Add(th?"ช่วงท้ายกระโดดทับ สร้างกรงราก และใช้รากดูดพลัง":"Later attacks include leaps, root cages and draining roots.");
            }
            else if(architect!=null)
            {
                var health=prefab.GetComponent<ArchitectBossHealth>();
                lines.Add(th?$"ดาเมจร่างแรก {architect.damage:0.##} / ร่างสอง {architect.phaseTwoDamage:0.##} ต่อครั้ง":$"Damage per hit: first form {architect.damage:0.##} / second form {architect.phaseTwoDamage:0.##}.");
                lines.Add(th?$"เปลี่ยนร่างเมื่อเลือดเหลือ {health.phaseTwoThreshold*100:0}% และคลั่งที่ {health.enragedThreshold*100:0}%":$"Transforms at {health.phaseTwoThreshold*100:0}% HP; enrages at {health.enragedThreshold*100:0}%.");
                lines.Add(th?"ร่างแรกยิงกระสุนหมุนวนและเลเซอร์ ร่างสองวาร์ปพุ่งชนและส่งคลื่นเคียว":"First form: spiral shots and lasers. Second form: warp dashes and scythe waves.");
                lines.Add(th?"ช่วงจบต้องทำลายแกนกลาง มิฉะนั้นบอสจะคืนร่าง":"Destroy the exposed core at the end or the boss reforms.");
            }
            else
            {
                var actions=prefab.GetComponent<MonsterCombatActions>();
                if(actions!=null)
                {
                    switch(actions.style)
                    {
                        case MonsterCombatActions.Style.Rifle:
                            lines.Add(th?$"ยิงกระสุนจากระยะไกล ระยะยิง {actions.rangedDistance:0.#}":$"Ranged shots; firing distance {actions.rangedDistance:0.#}.");
                            if(actions.closeRangeSlash)lines.Add(th?$"เข้าใกล้กว่า {actions.keepAwayDistance:0.#} จะฟันและผลักผู้เล่นถอยหลัง":$"Slashes and knocks you back within {actions.keepAwayDistance:0.#}.");break;
                        case MonsterCombatActions.Style.Wrench:lines.Add(th?"เดินเข้าประชิดและฟาดด้วยประแจ":"Closes in and swings a wrench.");break;
                        case MonsterCombatActions.Style.RockAndSlam:lines.Add(th?"ขุดหินแล้วขว้างใส่ผู้เล่น เปลี่ยนเป็นทุบพื้นเมื่ออยู่ใกล้":"Throws rocks at range; switches to a ground slam up close.");break;
                        case MonsterCombatActions.Style.Lunge:lines.Add(th?$"พบผู้เล่นในระยะ {actions.lungeTriggerDistance:0.#} แล้วพุ่งเข้ากัด":"Lunges to bite when you enter range {actions.lungeTriggerDistance:0.#}.");break;
                        case MonsterCombatActions.Style.Pounce:lines.Add(th?$"กระโจนเข้าหาผู้เล่นจากระยะ {actions.lungeTriggerDistance:0.#}":$"Pounces toward you within range {actions.lungeTriggerDistance:0.#}.");break;
                        case MonsterCombatActions.Style.Root:lines.Add(th?$"เรียกรากแทงใต้เท้า มีวงเตือน {actions.rootWarning:0.#} วินาที และมุดดินเมื่อผู้เล่นอยู่ไกล":$"Roots erupt under you after a {actions.rootWarning:0.#} s warning; burrows when you move away.");break;
                        case MonsterCombatActions.Style.Vine:lines.Add(th?$"ฟาดเถาวัลย์เป็นเส้น มีแถบเตือน {actions.vineWarning:0.#} วินาที":$"Line-shaped vine lash with a {actions.vineWarning:0.#} s warning.");break;
                    }
                    if(actions.poisonSeconds>0)lines.Add(th?$"ติดพิษ {actions.poisonSeconds:0.#} วินาที (ดาเมจรวม {actions.poisonDamage:0.##})":$"Poison lasts {actions.poisonSeconds:0.#} s ({actions.poisonDamage:0.##} total damage).");
                    if(actions.selfDestructAt>0)lines.Add(th?$"เลือดเหลือ {actions.selfDestructAt*100:0}% จะวิ่งเข้าหาระเบิดตัวเอง":$"Starts a suicide rush at {actions.selfDestructAt*100:0}% HP.");
                }
                else lines.Add(th?"ไล่ตามและโจมตีระยะประชิด":"Chases you and attacks in melee.");
                var burst=prefab.GetComponent<ZeroHuskDeathBurst>();
                if(burst!=null)lines.Add(th?$"ตายแล้วระเบิด: ดาเมจ {burst.damage:0.##}, รัศมี {burst.radius:0.#}, เผาไหม้ {burst.burnSeconds:0.#} วินาที":$"Death explosion: {burst.damage:0.##} damage, radius {burst.radius:0.#}; burns for {burst.burnSeconds:0.#} s.");
                if(data!=null&&data.packMax>1)lines.Add(th?$"เกิดเป็นฝูง {data.packMin}–{data.packMax} ตัว":$"Spawns in packs of {data.packMin}–{data.packMax}.");
            }
            return string.Join("\n\n",lines);
        }
    }
    public Entry[] entries;
}
