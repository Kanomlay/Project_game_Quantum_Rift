using UnityEngine;

public class MapPortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้าคนที่มาชนคือฮีโร่
        if (collision.CompareTag("Player"))
        {
            // สั่งให้ผู้จัดการเปลี่ยนด่าน!
            MapManager.instance.GoToNextMap();
        }
    }
}