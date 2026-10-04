using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// สีสถานะแยกชัดโดยไม่ขยับหรือเปลี่ยนขนาดปุ่ม และใช้เวลาจริงได้ขณะเกมหยุด
[DisallowMultipleComponent]
public sealed class QuantumUiButtonState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public Graphic border;
    public Image surface;
    public Button button;
    public bool chosen;
    bool hover,pressed,keyboard;
    bool lastInteractable;
    public void Refresh()
    {
        bool enabledButton=button==null || button.IsInteractable();lastInteractable=enabledButton;
        if(border!=null)border.color=!enabledButton?new Color(.4f,.4f,.45f,.7f):pressed?new Color(.4f,.8f,.88f):hover||keyboard||chosen?new Color(.75f,1f,1f):Color.white;
        if(surface!=null)surface.color=!enabledButton?new Color32(25,24,33,255):pressed?new Color32(10,48,65,255):hover||keyboard||chosen?new Color32(41,49,82,255):QuantumUiSkin.Surface;
    }
    void OnEnable(){hover=pressed=keyboard=false;Refresh();}
    void OnDisable(){hover=pressed=keyboard=false;}
    void Update(){if(button!=null && button.IsInteractable()!=lastInteractable)Refresh();}
    public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
    public void OnPointerExit(PointerEventData e){hover=pressed=false;Refresh();}
    public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;Refresh();}
    public void OnPointerUp(PointerEventData e){pressed=false;Refresh();}
    public void OnSelect(BaseEventData e){keyboard=true;Refresh();}
    public void OnDeselect(BaseEventData e){keyboard=false;Refresh();}
}
