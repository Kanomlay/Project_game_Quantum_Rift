using UnityEngine;

// เหตุการณ์พิเศษที่สุ่มโผล่ในแมพ เช่น ร้านค้า จุดฟื้นฟู หรือหีบลึกลับ
// แต่ละแมพจะได้เหตุการณ์เดียวต่อการเข้าหนึ่งครั้ง (ดู MapEventDirector)
[CreateAssetMenu(fileName = "NewMapEvent", menuName = "Game Data/Map Event")]
public class MapEventData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน")]
    public string eventName;
    public GameObject eventPrefab; // ของที่จะเสกลงห้อง เช่น prefab ร้านค้าของธีมนั้น

    [Header("โอกาสถูกสุ่ม")]
    [Min(0f)] public float weight = 1f; // ยิ่งมากยิ่งออกบ่อย เทียบกับเหตุการณ์อื่นในแมพเดียวกัน

    [Header("จังหวะที่จะโผล่")]
    public bool spawnAfterRoomCleared = true; // รอให้เคลียร์มอนสเตอร์ในห้องนั้นก่อน จะได้ไม่ต้องซื้อของกลางวงต่อสู้
}
