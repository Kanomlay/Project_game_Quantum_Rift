using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ตัวบอกคูลดาวน์การโจมตีของอาวุธในมือ แสดงทับไอคอนอาวุธบน HUD (ช่อง LMB): ม่านมืดกวาดออกเป็นวง + เลขวินาทีที่เหลือ
// ช่วงห่างการโจมตี = 1 / ความเร็วในการโจมตีของอาวุธ (ขอบเขต 1.3.4 ตาราง 1.2–1.5) เช่น 1.0 ครั้ง/วินาที = 1.00 วิ, 1.8 = 0.56 วิ
// WeaponController สร้างให้เองตอนเริ่ม แล้วเกาะเข้ากับช่องไอคอนของ HUDManager ตอนเล่น ไม่ต้องแก้ฉากหรือ prefab ของ HUD
public sealed class WeaponCooldownDisplay : MonoBehaviour
{
    static readonly Color Shade = new Color(0.02f, 0.04f, 0.1f, 0.72f);
    static readonly Color TextColor = new Color(0.6f, 0.96f, 1f);

    WeaponController weapon;
    GameObject visuals;
    Image shade;
    TMP_Text label;
    Sprite pixel;
    float nextSearch;
    int shownHundredths = -1;

    public static WeaponCooldownDisplay Create(WeaponController weapon)
    {
        var display = new GameObject("Weapon Cooldown").AddComponent<WeaponCooldownDisplay>();
        display.weapon = weapon;
        return display;
    }

    // HUD อาจยังไม่พร้อมตอนตัวละครเกิด ลองหาใหม่เป็นระยะจนเจอ
    bool Attach()
    {
        if (visuals != null) return true;
        if (Time.unscaledTime < nextSearch) return false;
        nextSearch = Time.unscaledTime + 0.5f;
        var hud = FindFirstObjectByType<HUDManager>();
        if (hud == null || hud.activeWeaponIcon == null) return false;
        var slot = hud.activeWeaponIcon.rectTransform.parent as RectTransform; // กรอบที่ครอบภาพอาวุธ
        if (slot == null) return false;

        visuals = new GameObject("WeaponCooldown", typeof(RectTransform));
        visuals.layer = slot.gameObject.layer;
        var root = (RectTransform)visuals.transform;
        root.SetParent(slot, false);
        Stretch(root);

        pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f);
        var shadeObject = new GameObject("Shade", typeof(RectTransform));
        shadeObject.layer = visuals.layer;
        shadeObject.transform.SetParent(root, false);
        Stretch((RectTransform)shadeObject.transform);
        shade = shadeObject.AddComponent<Image>();
        shade.sprite = pixel;
        shade.color = Shade;
        shade.raycastTarget = false;
        shade.type = Image.Type.Filled;
        shade.fillMethod = Image.FillMethod.Radial360;
        shade.fillOrigin = (int)Image.Origin360.Top;
        shade.fillClockwise = false; // ม่านหดตามเข็มนาฬิกาจนหมดตอนพร้อมตี

        var textObject = new GameObject("Seconds", typeof(RectTransform));
        textObject.layer = visuals.layer;
        textObject.transform.SetParent(root, false);
        Stretch((RectTransform)textObject.transform);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        // ตัวอักษรชุดเดียวกับเลขคูลดาวน์สกิล Q/E
        var like = hud.skillQCooldownText != null ? hud.skillQCooldownText : hud.weaponEnergyCostText;
        if (like != null) { text.font = like.font; text.fontSharedMaterial = like.fontSharedMaterial; }
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = like != null ? Mathf.Max(18f, like.fontSize) : 28f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = TextColor;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(8, 12, 26, 255);
        text.raycastTarget = false;
        label = text;

        visuals.SetActive(false);
        return true;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void LateUpdate()
    {
        if (weapon == null) { Destroy(gameObject); return; }
        if (!Attach()) return;
        float left = weapon.CooldownRemaining;
        bool show = left > 0f && weapon.isActiveAndEnabled;
        if (visuals.activeSelf != show) visuals.SetActive(show);
        if (!show) return;

        visuals.transform.SetAsLastSibling(); // ทับภาพอาวุธเสมอ แม้ HUD เปลี่ยนภาพในช่อง
        shade.fillAmount = Mathf.Clamp01(left / Mathf.Max(0.01f, weapon.CooldownLength));
        int hundredths = Mathf.CeilToInt(left * 100f);
        if (hundredths == shownHundredths) return; // สร้างข้อความใหม่เฉพาะตอนเลขเปลี่ยน
        shownHundredths = hundredths;
        label.text = (hundredths / 100f).ToString("0.00");
    }

    void OnDestroy()
    {
        if (visuals != null) Destroy(visuals);
        if (pixel != null) Destroy(pixel);
    }
}
