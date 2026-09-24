using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Game Data/Map Data")]
public class MapData : ScriptableObject
{
    [Header("ข้อมูลด่าน (Map Info)")]
    public string mapName; 
    public GameObject mapPrefab; 
    
    [Header("จุดเกิดของฮีโร่ (Spawn Point)")]
    public Vector2 spawnPosition; 

    [Header("เหตุการณ์พิเศษที่แมพนี้สุ่มได้ (เลือกมาใช้แค่ 1 อย่างต่อการเข้าหนึ่งครั้ง)")]
    public MapEventData[] possibleEvents; // แยกตามแมพ จะได้ใส่ร้านค้าคนละธีมกันได้

    [Header("กล่องสมบัติหลังเคลียร์ห้อง (ขอบเขต: แมพ 1 และ 2) เว้นว่าง = แมพนี้ไม่มีกล่อง")]
    public LootTable chestLoot;

    [Header("การเชื่อมโยงด่าน (Progression)")]
    public MapData nextMap; 
    public bool isBossRoom;
}