using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Game Data/Map Data")]
public class MapData : ScriptableObject
{
    [Header("ข้อมูลด่าน (Map Info)")]
    public string mapName; 
    public GameObject mapPrefab; 
    
    [Header("จุดเกิดของฮีโร่ (Spawn Point)")]
    public Vector2 spawnPosition; 

    [Header("การเชื่อมโยงด่าน (Progression)")]
    public MapData nextMap; 
    public bool isBossRoom;
}