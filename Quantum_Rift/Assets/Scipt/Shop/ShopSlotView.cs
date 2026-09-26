using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ช่องสินค้าหนึ่งช่องในหน้าร้าน: รูป ชื่อ ปุ่มราคา (คลิกซื้อ) ชี้เมาส์แล้วขึ้นคำอธิบายในกล่องล่าง
// ส่วนประกอบถูกสร้างโดย ShopUIBuilder
public sealed class ShopSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button buyButton;

    [HideInInspector] public ShopWindow window;
    [HideInInspector] public int index;

    static readonly Color Affordable = new Color(1f, 0.87f, 0.35f);
    static readonly Color TooExpensive = new Color(1f, 0.4f, 0.4f);
    static readonly Color SoldColor = new Color(0.6f, 0.6f, 0.7f);

    public void Show(ShopOffer offer, Sprite sprite, bool canAfford)
    {
        bool empty = offer == null;
        icon.enabled = !empty && sprite != null;
        icon.sprite = sprite;
        icon.color = !empty && offer.sold ? new Color(1f, 1f, 1f, 0.3f) : Color.white;

        nameText.text = empty ? "-" : offer.DisplayName;
        nameText.color = empty ? SoldColor : offer.NameColor;

        if (empty) priceText.text = "-";
        else if (offer.sold) priceText.text = "ขายแล้ว";
        else priceText.text = $"{offer.price} <size=80%>เหรียญ</size>";
        priceText.color = empty || offer.sold ? SoldColor : canAfford ? Affordable : TooExpensive;

        buyButton.interactable = !empty && !offer.sold;
    }

    public void OnPointerEnter(PointerEventData eventData) { if (window != null) window.Describe(index); }
    public void OnPointerExit(PointerEventData eventData) { if (window != null) window.HoverEnded(); }
}
