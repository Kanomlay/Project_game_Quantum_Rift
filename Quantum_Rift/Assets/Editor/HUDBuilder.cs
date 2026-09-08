using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// สร้างโครง HUD (ไอคอนอาวุธ, สกิล Q/E, เงิน, จอดำเปลี่ยนด่าน) แล้วลาก reference ใส่ HUDManager ให้อัตโนมัติ
// สั่งซ้ำได้ ของเดิมที่ชื่อตรงกันจะถูกใช้ซ้ำ ไม่สร้างซ้อน
public static class HUDBuilder
{
    const string ScenePath = "Assets/Scenes/GameScene.unity";
    const string CanvasName = "UI";

    [MenuItem("Tools/Quantum Rift/Build HUD")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งสร้าง HUD");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
                throw new InvalidOperationException("เซฟฉากที่เปิดอยู่ก่อน แล้วค่อยสั่งสร้าง HUD");
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        var canvas = GameObject.Find(CanvasName);
        if (canvas == null || canvas.GetComponent<Canvas>() == null)
            throw new InvalidOperationException($"ไม่เจอ Canvas ชื่อ \"{CanvasName}\" ใน {ScenePath}");

        var hud = UnityEngine.Object.FindObjectOfType<HUDManager>();
        if (hud == null)
            throw new InvalidOperationException("ไม่เจอ HUDManager ในฉาก");

        // 1) ไอคอนอาวุธ มุมซ้ายบน ใต้แถบ HP/Energy
        var weaponSlot = EnsureRect(canvas.transform, "WeaponSlot");
        Place(weaponSlot, new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(64f, 64f));
        var weaponIcon = EnsureImage(weaponSlot, "WeaponIcon");
        Place(weaponIcon.rectTransform, Center, Vector2.zero, new Vector2(64f, 64f));

        // 2) ช่องสกิล Q/E กลางล่างของจอ
        var skillSlot = EnsureRect(canvas.transform, "SkillSlot");
        Place(skillSlot, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(160f, 64f));
        var q = EnsureSkillSlot(skillSlot, "SkillQ", -45f);
        var e = EnsureSkillSlot(skillSlot, "SkillE", 45f);

        // 3) ตัวเลขเงิน/คริสตัล มุมขวาบน
        var currency = EnsureText(canvas.transform, "Currency_Text", "0", 28f, TextAlignmentOptions.Right);
        Place(currency.rectTransform, new Vector2(1f, 1f), new Vector2(-120f, -30f), new Vector2(200f, 40f));

        // 4) จอดำเปลี่ยนด่าน ต้องเป็นลูกตัวสุดท้ายจะได้วาดทับ HUD ตัวอื่น
        var transition = EnsureImage(canvas.transform, "TransitionScreen");
        Stretch(transition.rectTransform);
        transition.color = Color.black;
        transition.raycastTarget = false;

        var group = EnsureComponent<CanvasGroup>(transition.gameObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var mapName = EnsureText(transition.transform, "Transition_MapName", "", 48f, TextAlignmentOptions.Center);
        Place(mapName.rectTransform, Center, Vector2.zero, new Vector2(800f, 100f));

        transition.transform.SetAsLastSibling();
        transition.gameObject.SetActive(false); // MapManager เป็นคนเปิดเองตอนเปลี่ยนด่าน

        // ลาก reference ใส่ HUDManager ให้เสร็จในตัว
        hud.activeWeaponIcon = weaponIcon;
        hud.skillQIcon = q.icon;
        hud.skillQCooldownText = q.cooldown;
        hud.skillEIcon = e.icon;
        hud.skillECooldownText = e.cooldown;
        hud.currencyText = currency;
        hud.transitionCanvas = group;
        hud.transitionMapNameText = mapName;
        EditorUtility.SetDirty(hud);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("สร้างโครง HUD เสร็จแล้ว ตรวจดูใน Hierarchy แล้วกด Ctrl+S เพื่อบันทึกฉาก");
    }

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static (Image icon, TextMeshProUGUI cooldown) EnsureSkillSlot(Transform parent, string name, float offsetX)
    {
        var slot = EnsureRect(parent, name);
        Place(slot, Center, new Vector2(offsetX, 0f), new Vector2(64f, 64f));

        var icon = EnsureImage(slot, name + "_Icon");
        Place(icon.rectTransform, Center, Vector2.zero, new Vector2(64f, 64f));

        // ตัวเลขคูลดาวน์วางทับกลางไอคอน ปล่อยข้อความว่างไว้ HUDManager จะเติมเลขให้เอง
        var cooldown = EnsureText(slot, name + "_Cooldown", "", 32f, TextAlignmentOptions.Center);
        Place(cooldown.rectTransform, Center, Vector2.zero, new Vector2(64f, 64f));

        return (icon, cooldown);
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build HUD");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // เผื่อกรณีที่เคยสร้างมือด้วย Create Empty ซึ่งได้ Transform ธรรมดามา ไม่ใช่ RectTransform
        return existing as RectTransform ?? Undo.AddComponent<RectTransform>(existing.gameObject);
    }

    static Image EnsureImage(Transform parent, string name)
    {
        return EnsureComponent<Image>(EnsureRect(parent, name).gameObject);
    }

    static TextMeshProUGUI EnsureText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions align)
    {
        var text = EnsureComponent<TextMeshProUGUI>(EnsureRect(parent, name).gameObject);
        if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = align;
        text.raycastTarget = false;
        return text;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
