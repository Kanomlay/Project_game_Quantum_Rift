using TMPro;
using UnityEngine;
using UnityEngine.UI;

// เปลี่ยนเฉพาะผิว UI: ไม่ย้าย/ย่อ RectTransform เดิม ไม่เปลี่ยนข้อความหรือ callback
public static class QuantumUiSkin
{
    public static readonly Color Ink = new Color32(17,18,34,255);
    public static readonly Color Surface = new Color32(28,26,48,255);
    public static readonly Color Cyan = new Color32(113,230,248,255);
    static Sprite frame;
    public static Sprite FrameSprite => frame != null ? frame : frame = Resources.Load<Sprite>("QuantumUI/QuantumFrame");

    static Image Layer(Transform parent, string name)
    {
        // MiniMap สั่ง Destroy หลังจบเฟรม ต้องไม่หยิบชิ้นเก่าที่กำลังถูกลบกลับมาใช้
        for (int i=parent.childCount-1;i>=0;i--)
        {
            var child=parent.GetChild(i);
            if(child.name==name && child.gameObject.activeSelf) return child.GetComponent<Image>();
        }
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;
        go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.raycastTarget=false;
        return image;
    }
    public static Graphic Frame(Transform parent,float edge=8f)
    {
        if(parent==null)return null;
        // ปิดกรอบภาพเก่าที่ย่อแล้วลายตา คง object ไว้เพื่อไม่เสียการอ้างอิงเดิม
        var old=parent.Find("__QuantumFrame");if(old!=null)old.gameObject.SetActive(false);
        Transform child=null;
        for(int i=parent.childCount-1;i>=0;i--)
            if(parent.GetChild(i).name=="__CleanFrame"&&parent.GetChild(i).gameObject.activeSelf){child=parent.GetChild(i);break;}
        if(child==null)
        {
            child=new GameObject("__CleanFrame",typeof(RectTransform),typeof(QuantumUiFrame)).transform;
            child.gameObject.layer=5;child.SetParent(parent,false);
        }
        if(child.GetComponent<CanvasRenderer>()==null)child.gameObject.AddComponent<CanvasRenderer>();
        var image=child.GetComponent<QuantumUiFrame>();var rect=image.rectTransform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        image.Configure(Mathf.Clamp(edge*.38f,1,10));
        image.raycastTarget=false;image.color=Color.white;rect.SetAsLastSibling();
        return image;
    }
    public static void Button(Button button,float edge=6f)
    {
        if(button==null)return;
        var surface=button.GetComponent<Image>();if(surface!=null)surface.color=Surface;
        var border=Frame(button.transform,edge);
        var state=button.GetComponent<QuantumUiButtonState>()??button.gameObject.AddComponent<QuantumUiButtonState>();
        state.border=border;state.surface=surface;state.button=button;
        button.transition=Selectable.Transition.None;
        state.Refresh();
    }
    static void Panel(Transform panel,float edge=12)
    {
        if(panel==null)return;
        var im=panel.GetComponent<Image>();if(im!=null)im.color=Ink;
        Frame(panel,edge);
    }
    static void Plate(TMP_Text text,Color color,float edge=3)
    {
        if(text==null)return;
        var src=text.rectTransform;
        var back=Layer(src.parent,"__Plate_"+src.name);var r=back.rectTransform;
        r.anchorMin=src.anchorMin;r.anchorMax=src.anchorMax;r.pivot=src.pivot;
        r.anchoredPosition=src.anchoredPosition;r.sizeDelta=src.sizeDelta+new Vector2(12,6);
        r.localRotation=src.localRotation;r.localScale=src.localScale;
        back.color=color;Frame(r,edge);
        // เมื่อทาซ้ำ แผ่นรองอาจอยู่ก่อนข้อความอยู่แล้ว ต้องไม่ขยับไปบังข้อความ
        int textIndex=src.GetSiblingIndex();
        r.SetSiblingIndex(r.GetSiblingIndex()<textIndex?textIndex-1:textIndex);
    }
    public static void Help(GameHelpWindow help)
    {
        if(help==null || help.panel==null)return;
        var window=help.panel.transform.Find("GuideWindow");if(window==null)return;
        Panel(window,32);
        var inner=window.Find("Inner");if(inner!=null)inner.GetComponent<Image>().color=Ink;
        Panel(window.Find("Sidebar"),7);
        Button(help.entryButton,12);Button(help.closeButton,8);Button(help.languageButton,8);
        Button(help.previousButton,8);Button(help.nextButton,8);
        foreach(var tab in help.tabs)Button(tab,5);
        for(int i=0;i<3;i++)
        {
            var section=window.Find("Section"+i);
            if(section==null)continue;
            section.GetComponent<Image>().color=Surface;Frame(section,8);
        }
        HelpSelection(help);
    }
    public static void HelpSelection(GameHelpWindow help)
    {
        if(help.tabs==null)return;
        for(int i=0;i<help.tabs.Length;i++)
        {
            var state=help.tabs[i].GetComponent<QuantumUiButtonState>();
            if(state!=null){state.chosen=i==help.CurrentPage;state.Refresh();}
        }
    }
    public static void Blessings(BlessingWindow window)
    {
        if(window==null || window.panel==null)return;
        var panel=window.panel.transform.Find("BlessingWindow");
        Panel(panel,32);
        if(window.cards!=null)foreach(var card in window.cards)
        {
            if(card==null)continue;
            var inner=card.transform.Find("Inner");if(inner!=null)inner.GetComponent<Image>().color=Surface;
            var border=Frame(card.transform,18);
            var state=card.GetComponent<QuantumUiButtonState>()??card.gameObject.AddComponent<QuantumUiButtonState>();
            state.border=border;state.button=card.button;
            state.surface=null;state.Refresh(); // สีหมวดพรเดิมยังดูแลโดย BlessingCardView
            Plate(card.categoryText,new Color32(49,29,75,255),3);
            Plate(card.keyText,new Color32(43,30,68,255),3);
            Plate(card.limitText,new Color32(18,18,32,255),4);
            if(card.limitText!=null){card.limitText.enableAutoSizing=true;card.limitText.fontSizeMin=18;card.limitText.fontSizeMax=23;card.limitText.lineSpacing=-3;}
        }
        if(window.ownedIcons!=null)foreach(var icon in window.ownedIcons)if(icon!=null)Frame(icon.transform,5);
    }
    static Transform Ancestor(Transform item,string name)
    {
        while(item!=null){if(item.name==name)return item;item=item.parent;}return null;
    }
    public static void Hud(HUDManager hud)
    {
        if(hud==null)return;
        if(hud.currencyText!=null)Panel(Ancestor(hud.currencyText.transform,"CurrencyHUD"),16);
        if(hud.activeWeaponIcon!=null)
        {
            var slot=Ancestor(hud.activeWeaponIcon.transform,"WeaponSlot");Panel(slot,16);
            if(slot!=null){var background=slot.Find("Background");if(background!=null && background.GetComponent<Image>()!=null)background.GetComponent<Image>().color=Ink;}
        }
        foreach(var icon in new[]{hud.skillQIcon,hud.skillEIcon})
        {
            if(icon==null)continue;
            var slot=Ancestor(icon.transform,icon==hud.skillQIcon?"SkillQ":"SkillE");Panel(slot,14);
            if(slot!=null){var inner=slot.Find("Inner");if(inner!=null && inner.GetComponent<Image>()!=null)inner.GetComponent<Image>().color=Ink;}
        }
        Plate(hud.weaponEnergyCostText,new Color32(20,38,55,255),3);
        // ป้ายปุ่มอยู่พิกัดเดิม เพิ่มเฉพาะแผ่นรองโดยไม่เลื่อนอาวุธ/สกิล
        var combat=hud.activeWeaponIcon!=null?Ancestor(hud.activeWeaponIcon.transform,"CombatHUD"):null;
        if(combat!=null)foreach(var label in combat.GetComponentsInChildren<TMP_Text>(true))
            if(label.name=="Key")Plate(label,new Color32(33,28,48,255),3);
    }
    public static void MiniMap(Transform panel)
    {
        if(panel==null)return;
        Panel(panel,16);
        for(int i=0;i<panel.childCount;i++)
        {
            var child=panel.GetChild(i);
            if(child.gameObject.activeSelf && child.name.StartsWith("Room_"))Frame(child,2);
        }
        var inset=panel.Find("Inset");
        if(inset!=null)foreach(Transform child in inset)
            if(child.name.StartsWith("__Grid"))child.gameObject.SetActive(false);
    }
}
