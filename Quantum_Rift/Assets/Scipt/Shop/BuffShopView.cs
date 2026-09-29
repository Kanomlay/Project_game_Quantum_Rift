using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuffShopView:MonoBehaviour
{
    ShopWindow window;
    TMP_FontAsset font;
    TMP_Text money,message;
    readonly TMP_Text[] titles=new TMP_Text[4],details=new TMP_Text[4],prices=new TMP_Text[4];
    readonly Button[] buttons=new Button[4];
    readonly Image[] icons=new Image[4];
    static readonly Color Red=new Color(.55f,.12f,.15f),Ink=new Color(.10f,.045f,.07f);
    public static BuffShopView Create(ShopWindow window,Transform parent)
    {
        var go=new GameObject("BuffShopRedPanel",typeof(RectTransform));go.transform.SetParent(parent,false);
        var view=go.AddComponent<BuffShopView>();view.window=window;view.font=window.messageText!=null?window.messageText.font:TMP_Settings.defaultFontAsset;
        var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=new Vector2(1120,660);
        view.Build();return view;
    }
    RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size,Color? color=null)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;
        if(color.HasValue)go.AddComponent<Image>().color=color.Value;return r;
    }
    TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string text,float height,Color color)
    {
        var t=Rect(name,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=Mathf.Max(22,height*1.12f);
        // เว้นที่สระและวรรณยุกต์ของฟอนต์ไทย รวมถึงรายละเอียดบัพสามบรรทัด
        t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(size.y,name=="Description"?124:t.fontSize*1.75f));
        t.alignment=TextAlignmentOptions.Center;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    Button Button(string name,Transform parent,Vector2 pos,Vector2 size,System.Action action)
    {
        var r=Rect(name,parent,pos,size,Red);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.7f,.5f);colors.pressedColor=new Color(.6f,.3f,.3f);b.colors=colors;
        b.onClick.AddListener(()=>action());return b;
    }
    void Build()
    {
        gameObject.AddComponent<Image>().color=Red;
        Rect("Inner",transform,Vector2.zero,new Vector2(1108,648),Ink);
        Label("Title",transform,new Vector2(0,268),new Vector2(880,55),"ร้านบัพเลือดสนธยา",34,new Color(1,.65f,.5f));
        money=Label("Money",transform,new Vector2(0,219),new Vector2(900,36),"",22,Color.yellow);
        var close=Button("Close",transform,new Vector2(508,281),new Vector2(62,52),()=>window.Close());
        Label("X",close.transform,Vector2.zero,new Vector2(50,45),"X",25,Color.white);
        for(int i=0;i<4;i++)
        {
            int index=i;var card=Rect("Offer"+i,transform,new Vector2(-405+270*i,5),new Vector2(250,368),i==3?new Color(.24f,.075f,.12f):new Color(.16f,.08f,.1f));
            Label("Kind",card,new Vector2(0,151),new Vector2(230,36),i==3?"ข้อเสนอมีข้อเสีย":"บัพประจำรอบ",18,i==3?new Color(1,.4f,.3f):Color.gray);
            icons[i]=Rect("Icon",card,new Vector2(0,92),new Vector2(70,70)).gameObject.AddComponent<Image>();icons[i].preserveAspect=true;icons[i].raycastTarget=false;
            titles[i]=Label("Name",card,new Vector2(0,33),new Vector2(234,46),"",22,Color.white);
            details[i]=Label("Description",card,new Vector2(0,-40),new Vector2(228,108),"",18,Color.white);
            buttons[i]=Button("Buy",card,new Vector2(0,-137),new Vector2(222,48),()=>window.Buy(index));
            prices[i]=Label("Price",buttons[i].transform,Vector2.zero,new Vector2(216,46),"",20,Color.yellow);
        }
        message=Label("Message",transform,new Vector2(0,-252),new Vector2(1040,112),"",20,Color.white);
    }
    public void Refresh(ShopClickable shop,PlayerStats player)
    {
        money.text=$"เหรียญ { (player!=null?player.currentCurrency:0) }   |   ซื้อได้รายการละ 1 ครั้ง";
        for(int i=0;i<4;i++)
        {
            var offer=shop.Stock[i];titles[i].text=offer.DisplayName;details[i].text=offer.Describe(shop.itemPool).Replace(" หน่วย","");
            icons[i].sprite=offer.Icon(shop.itemPool);icons[i].color=offer.risky?new Color(1,.3f,.3f):Color.white;
            prices[i].text=offer.sold?"ขายแล้ว":offer.price+" เหรียญ";
            buttons[i].interactable=!offer.sold && player!=null && !player.isDead;
            details[i].color=offer.sold?Color.gray:Color.white;
        }
    }
    public void Say(string text,Color color){message.text=text;message.color=color;}
}
