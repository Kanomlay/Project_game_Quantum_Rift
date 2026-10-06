using UnityEngine;
using System.Collections;

public class MapManager : MonoBehaviour
{
    public static MapManager instance; 

    [Header("ด่านเริ่มต้น")]
    public MapData firstMap; // ใส่ข้อมูลด่าน 1-1 ไว้ตรงนี้
    // คอนโซลทดสอบเปลี่ยนอาชีพ: โหลดฉากใหม่แล้วเริ่มที่แมพนี้แทนด่านแรก (ใช้ครั้งเดียว)
    public static MapData startOverride;
    public bool IsLoading => loading;
    
    private MapData currentMap; // จำว่าตอนนี้อยู่ด่านไหน
    public MapData CurrentMap => currentMap;
    private GameObject currentMapInstance; // ตัวแผนที่จริงๆ ที่กำลังโชว์อยู่ฉาก
    // ของที่เสกลงแมพระหว่างเล่น (กับดัก ฯลฯ) ผูกกับตัวนี้ จะได้หายไปพร้อมแมพตอนเปลี่ยนด่าน
    public Transform CurrentMapRoot => currentMapInstance != null ? currentMapInstance.transform : null;
    private GameObject player; // ตัวฮีโร่ของเรา
    private bool loading;      // กำลังเปลี่ยนด่าน (กันเดินชนพอร์ทัลซ้ำระหว่างจอมืด แล้วโหลดด่านซ้อนกัน)
    private MapBackgroundMusic backgroundMusic;
    private CinematicDirector cinematics;
    private bool openingConsidered;

    void Awake()
    {
        if (instance == null) instance = this;
        backgroundMusic = GetComponent<MapBackgroundMusic>();
        if (backgroundMusic == null) backgroundMusic = gameObject.AddComponent<MapBackgroundMusic>();
        cinematics=GetComponent<CinematicDirector>();
        if(cinematics==null)cinematics=gameObject.AddComponent<CinematicDirector>();
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

        MapData start = startOverride != null ? startOverride : firstMap;
        startOverride = null;
        if (start != null)
        {
            // ด่านแรกยังไม่มีอะไรให้ค่อยๆ มืด ถ้าปล่อยให้เฟดตามปกติจะเห็นฉากเปล่าแว็บนึงก่อนแมพโหลด
            // จึงบังคับให้ดำสนิทตั้งแต่เฟรมแรก แล้วปล่อยให้ LoadMapRoutine เฟดออกตอนแมพพร้อมแล้ว
            HUDManager hud = FindObjectOfType<HUDManager>();
            if (hud != null && hud.transitionCanvas != null)
            {
                hud.transitionCanvas.gameObject.SetActive(true);
                hud.transitionCanvas.alpha = 1f;
            }

            LoadMap(start);
        }
    }

    public void LoadMap(MapData mapToLoad)
    {
        StartCoroutine(LoadMapRoutine(mapToLoad));
    }

    // จบด่าน: ยังสะสมพรไม่ครบก็ให้เลือกพรก่อน (เกมหยุดระหว่างเลือก) แล้วค่อยโหลดด่านถัดไป
    private void ProceedTo(MapData next)
    {
        if (loading || BlessingManager.IsChoosing) return;
        if (currentMap != null && (currentMap.isTestLab||currentMap.isTutorial)) { LoadMap(next); return; }
        if (BlessingManager.Instance != null && BlessingManager.Instance.OfferChoice(() => LoadMap(next))) return;
        LoadMap(next);
    }

    private IEnumerator LoadMapRoutine(MapData mapToLoad)
    {
        loading = true;
        HUDManager hud = FindObjectOfType<HUDManager>();
        if (hud != null && hud.transitionCanvas != null) 
        {
            yield return StartCoroutine(hud.FadeInBlack(mapToLoad.mapName));
        }
        if (currentMapInstance != null) Destroy(currentMapInstance);
        
        currentMap = mapToLoad;
        backgroundMusic.PlayMap(currentMap);
        
        if (currentMap.mapPrefab != null)
        {
            currentMapInstance = Instantiate(currentMap.mapPrefab, Vector3.zero, Quaternion.identity);
            // ห้องทดสอบยืมผังห้องบอส ถอดมอนของห้องออก เดินเข้าไปแล้วห้องเคลียร์ทันที (ประตูออกโผล่)
            if (currentMap.isTestLab)
                foreach (var room in currentMapInstance.GetComponentsInChildren<RoomController>(true)) room.roomData = null;
            // สุ่มเหตุการณ์พิเศษของแมพนี้ (ร้านค้า ฯลฯ) หนึ่งอย่างต่อการเข้าหนึ่งครั้ง
            if(!currentMap.isTutorial)MapEventDirector.PlaceEvent(currentMapInstance, currentMap);
        }
        
        if (player != null)
        {
            player.transform.position = new Vector3(currentMap.spawnPosition.x, currentMap.spawnPosition.y, 0f);
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        if (currentMapInstance != null)
        {
            var features = currentMapInstance.GetComponent<MapGameplayFeatures>();
            if (features != null && !currentMap.isBossRoom) features.Initialize(currentMap.spawnPosition);
            // กำแพงในห้องกับกล่องทำลายได้สุ่มใหม่ทุกครั้งที่เข้าแมพ กำแพงก่อน กล่องจะได้หลบกำแพง
            // (ร้านค้าถูกวางไปแล้วข้างบน ห้องร้านจึงถูกข้าม) กับดักสุ่มทีหลังตอนเดินเข้าห้องครั้งแรก
            var walls = currentMapInstance.GetComponent<RandomRoomWalls>();
            if (walls != null && !currentMap.isBossRoom) walls.Spawn(currentMap.spawnPosition);
            var crates = currentMapInstance.GetComponent<RandomCrateClusters>();
            if (crates != null && !currentMap.isBossRoom) crates.Spawn(currentMap.spawnPosition);
            MiniMapHUD.Show(hud, currentMapInstance, player != null ? player.transform : null);
        }
        yield return new WaitForSeconds(1.5f);
        if (hud != null && hud.transitionCanvas != null) 
        {
            yield return StartCoroutine(hud.FadeOutClear());
        }
        loading = false;
        if(!openingConsidered)
        {
            openingConsidered=true;
            if(currentMap==firstMap&&!currentMap.isTestLab&&!currentMap.isTutorial)yield return cinematics.PlayOpening();
        }
    }
    public void GoToNextMap()
    {
        if (currentMap.isBossRoom && SummaryManager.instance != null)
        {
            string nextMapName = (currentMap.nextMap != null) ? currentMap.nextMap.mapName : "จบเกม!";
            SummaryManager.instance.ShowSummary(true, nextMapName);
        }
        else if (currentMap.nextMap != null)
        {
            ProceedTo(currentMap.nextMap); 
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
            ProceedTo(currentMap.nextMap);
        }
        else
        {
            Debug.Log("ไม่มีด่านต่อไปให้โหลดแล้วครับ");
        }
    }

}
