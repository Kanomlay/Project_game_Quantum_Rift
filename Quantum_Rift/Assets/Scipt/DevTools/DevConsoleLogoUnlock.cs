using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ทางลัดลับบนโลโก้เดิม ไม่เปลี่ยนภาพ ขนาด ตำแหน่ง หรือเพิ่มปุ่มที่เห็นได้
public sealed class DevConsoleLogoUnlock : MonoBehaviour, IPointerClickHandler
{
    public const int RequiredClicks = 5;
    MainMenuController menu;
    int clicks;

    public static void Attach(MainMenuController owner)
    {
        if (owner == null || owner.mainMenuUI == null) return;
        foreach (var image in owner.mainMenuUI.GetComponentsInChildren<Image>(true))
        {
            if (image.name != "Logo") continue;
            var target = image.GetComponent<DevConsoleLogoUnlock>();
            if (target == null) target = image.gameObject.AddComponent<DevConsoleLogoUnlock>();
            target.menu = owner;
            image.raycastTarget = true;
            return;
        }
        Debug.LogWarning("ไม่พบ Logo ในเมนูหลัก จึงยังผูกการปลดล็อกคอนโซลทดสอบไม่ได้");
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (data == null || data.button != PointerEventData.InputButton.Left || DevConsole.Unlocked) return;
        if (menu == null || menu.mainMenuUI == null || !menu.mainMenuUI.activeInHierarchy) return;
        if (SettingsMenu.instance != null && SettingsMenu.instance.IsOpen) return;
        if (GameHelpWindow.IsOpen || MonsterCollectionWindow.IsOpen || CreditsWindow.BlocksEscape) return;
        clicks++;
        if (clicks >= RequiredClicks) DevConsole.UnlockFromLogo();
    }
}
