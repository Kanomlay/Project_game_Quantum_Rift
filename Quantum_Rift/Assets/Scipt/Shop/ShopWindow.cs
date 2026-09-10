using UnityEngine;
using UnityEngine.EventSystems;

// หน้าร้านเปล่าร่วมกันทั้งสองธีม; สร้างเพียงครั้งเดียวแล้วเปิด/ปิดซ้ำ
public sealed class ShopWindow : MonoBehaviour
{
    public GameObject content;
    static ShopWindow current;
    public static bool IsOpen => current != null && current.content != null && current.content.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { current = null; }

    public static void Open(ShopWindow prefab)
    {
        if (prefab == null) return;
        if (current == null) current = Instantiate(prefab);
        // รองรับการลาก prefab ร้านไปฉากอื่นที่ยังไม่มี EventSystem
        if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null)
            new GameObject("Shop EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        current.content.SetActive(true);
    }

    public void Close()
    {
        content.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (content.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }
}
