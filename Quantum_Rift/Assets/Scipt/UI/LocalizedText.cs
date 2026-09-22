using TMPro;
using UnityEngine;

// สลับข้อความตามภาษาที่เลือก ใช้กับป้ายที่เป็นตัวหนังสือ TMP ไม่ใช่ภาพ
// ภาษาไทยจะอ่านออกก็ต่อเมื่อฟอนต์ที่ใช้มีตัวอักษรไทย ถ้ายังเป็น LiberationSans จะขึ้นเป็นกล่องสี่เหลี่ยม
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [TextArea] public string englishText;
    [TextArea] public string thaiText;

    TMP_Text target;

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
        if (target == null) target = GetComponent<TMP_Text>();

        string wanted = LanguageSettings.IsThai ? thaiText : englishText;
        if (!string.IsNullOrEmpty(wanted)) target.text = wanted;
    }
}
