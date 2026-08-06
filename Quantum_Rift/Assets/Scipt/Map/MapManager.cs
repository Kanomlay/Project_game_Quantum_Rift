using UnityEngine;
using System.Collections;

public class MapManager : MonoBehaviour
{
    public static MapManager instance; 

    [Header("ด่านเริ่มต้น")]
    public MapData firstMap; // ใส่ข้อมูลด่าน 1-1 ไว้ตรงนี้
    
    private MapData currentMap; // จำว่าตอนนี้อยู่ด่านไหน
    private GameObject currentMapInstance; // ตัวแผนที่จริงๆ ที่กำลังโชว์อยู่ฉาก
    private GameObject player; // ตัวฮีโร่ของเรา

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (GameManager.selectedCharacter != null && GameManager.selectedCharacter.characterPrefab != null)
        {
            player = Instantiate(GameManager.selectedCharacter.characterPrefab, Vector3.zero, Quaternion.identity);
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.target = player.transform;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player"); 
        }

        if (firstMap != null)
        {
            LoadMap(firstMap);
        }
    }

    public void LoadMap(MapData mapToLoad)
    {
        StartCoroutine(LoadMapRoutine(mapToLoad));
    }

    private IEnumerator LoadMapRoutine(MapData mapToLoad)
    {
        HUDManager hud = FindObjectOfType<HUDManager>();
        if (hud != null && hud.transitionCanvas != null) 
        {
            yield return StartCoroutine(hud.FadeInBlack(mapToLoad.mapName));
        }
        if (currentMapInstance != null) Destroy(currentMapInstance);
        
        currentMap = mapToLoad;
        
        if (currentMap.mapPrefab != null) currentMapInstance = Instantiate(currentMap.mapPrefab, Vector3.zero, Quaternion.identity);
        
        if (player != null)
        {
            player.transform.position = new Vector3(currentMap.spawnPosition.x, currentMap.spawnPosition.y, 0f);
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        yield return new WaitForSeconds(1.5f);
        if (hud != null && hud.transitionCanvas != null) 
        {
            yield return StartCoroutine(hud.FadeOutClear());
        }
    }
    public void GoToNextMap()
    {
        if (currentMap.isBossRoom)
        {
            string nextMapName = (currentMap.nextMap != null) ? currentMap.nextMap.mapName : "จบเกม!";
            SummaryManager.instance.ShowSummary(true, nextMapName); 
        }
        else if (currentMap.nextMap != null)
        {
            LoadMap(currentMap.nextMap); 
        }
        else
        {
            Debug.Log("จบเกมอย่างสมบูรณ์! ไม่มีด่านต่อไปแล้ว");
        }
    }
    public void LoadNextMapFromSummary()
    {
        if (currentMap.nextMap != null)
        {
            LoadMap(currentMap.nextMap);
        }
        else
        {
            Debug.Log("ไม่มีด่านต่อไปให้โหลดแล้วครับ");
        }
    }

}