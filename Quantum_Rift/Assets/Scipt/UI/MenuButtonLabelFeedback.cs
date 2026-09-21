using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ให้ข้อความตอบสนองตาม SpriteSwap โดยไม่เปลี่ยนปุ่มหรือคำสั่ง OnClick เดิม</summary>
[DisallowMultipleComponent]
public sealed class MenuButtonLabelFeedback : MonoBehaviour
{
    public Button button;
    public TMP_Text label;
    public Vector2 restingPosition;
    public float pressedOffset = 2f;
    public Color normalColor = new Color32(245,245,255,255);
    public Color pressedColor = new Color32(31,16,42,255);
    public Color disabledColor = new Color32(150,144,165,255);

    void OnEnable() { Refresh(); }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if (button == null || label == null) return;
        bool available = button.IsActive() && button.IsInteractable();
        var image = button.targetGraphic as Image;
        bool pressed = available && image != null && button.transition == Selectable.Transition.SpriteSwap
            && button.spriteState.pressedSprite != null && image.overrideSprite == button.spriteState.pressedSprite;
        label.color = !available ? disabledColor : pressed ? pressedColor : normalColor;
        label.rectTransform.anchoredPosition = restingPosition + (pressed ? Vector2.down * pressedOffset : Vector2.zero);
    }
    void OnDisable()
    {
        if (label == null) return;
        label.color = normalColor;
        label.rectTransform.anchoredPosition = restingPosition;
    }
}
