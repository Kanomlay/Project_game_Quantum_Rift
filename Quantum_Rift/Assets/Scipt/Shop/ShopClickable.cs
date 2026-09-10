using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ShopClickable : MonoBehaviour
{
    public ShopWindow windowPrefab;

    // OnMouseDown ใช้ Collider2D และกล้องของฉาก; ไม่ต้องเพิ่ม PhysicsRaycaster
    void OnMouseDown()
    {
        if (ShopWindow.IsOpen) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        ShopWindow.Open(windowPrefab);
    }
}
