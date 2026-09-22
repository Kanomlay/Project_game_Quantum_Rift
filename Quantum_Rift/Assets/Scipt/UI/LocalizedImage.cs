using UnityEngine;
using UnityEngine.UI;

// สลับภาพตามภาษาที่เลือก ใช้กับปุ่มที่มีตัวหนังสืออยู่ในภาพ (ชุด EN-*.png / TH-*.png)
// ข้อดีคือภาษาไทยในภาพเป็นพิกเซลอยู่แล้ว ไม่ต้องพึ่งฟอนต์ที่มีสระไทย
[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    public Sprite englishSprite;
    public Sprite thaiSprite;

    Image target;

    void OnEnable()
    {
        LanguageSettings.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        LanguageSettings.Changed -= Apply;
    }

    public void Apply()
    {
        if (target == null) target = GetComponent<Image>();

        Sprite wanted = LanguageSettings.IsThai ? thaiSprite : englishSprite;
        if (wanted != null) target.sprite = wanted;
    }
}
