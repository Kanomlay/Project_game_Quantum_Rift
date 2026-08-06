using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomEncounter", menuName = "Game Data/Room Encounter")]
public class RoomEncounterData : ScriptableObject
{
    [Header("กองทัพมอนสเตอร์ในห้องนี้")]
    public MonsterData[] monstersToSpawn; // ใส่ข้อมูลมอนสเตอร์กี่ตัวก็ได้

    [Header("ของรางวัลเมื่อเคลียร์ห้อง (ตู้สมบัติ)")]
    public WeaponData[] possibleWeaponDrops; // รายชื่ออาวุธที่มีโอกาสดรอป
    // (อนาคตสามารถเพิ่ม public ItemData[] possiblePotions; ได้ตรงนี้)
}