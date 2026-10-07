using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniMapHUD : MonoBehaviour
{
    readonly List<MapRoomIcon> markers=new List<MapRoomIcon>();
    readonly List<TMP_Text> legendLabels=new List<TMP_Text>();
    Transform player;GameObject mapRoot;MapRoomGraph graph;
    RectTransform panel,playerDot;float[] xs,ys;float stepX,stepY,nextRefresh,nextTypes;
    public MapRoomGraph Graph=>graph;
    public IReadOnlyList<MapRoomIcon> Markers=>markers;
    void OnEnable(){LanguageSettings.Changed+=RefreshLegend;}
    void OnDisable(){LanguageSettings.Changed-=RefreshLegend;}
    void RefreshLegend(){var labels=LanguageSettings.IsThai?new[]{"มอน","ร้าน","วาร์ป"}:new[]{"FIGHT","SHOP","EXIT"};for(int i=0;i<legendLabels.Count;i++)if(legendLabels[i]!=null)legendLabels[i].text=labels[i];}
    public static void Show(HUDManager hud,GameObject map,Transform hero)
    {
        if(hud==null||map==null)return;var canvas=hud.GetComponentInParent<Canvas>();
        if(canvas==null){var ui=GameObject.Find("UI");if(ui!=null)canvas=ui.GetComponent<Canvas>();}if(canvas==null)return;
        var overlay=canvas.transform.Find("GameplayHUD");if(overlay==null)return;var old=overlay.Find("MiniMapHUD");
        var go=old!=null?old.gameObject:new GameObject("MiniMapHUD",typeof(RectTransform));if(old==null)go.transform.SetParent(overlay,false);go.layer=5;
        (go.GetComponent<MiniMapHUD>()??go.AddComponent<MiniMapHUD>()).Build(hud,map,hero);
    }
    void Build(HUDManager hud,GameObject map,Transform hero)
    {
        player=hero;Physics2D.SyncTransforms();if(mapRoot!=map||graph==null)graph=new MapRoomGraph(map);mapRoot=map;
        panel=(RectTransform)transform;panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;panel.anchoredPosition=new Vector2(-32,-132);panel.sizeDelta=new Vector2(320,232);
        var border=GetComponent<Image>()??gameObject.AddComponent<Image>();border.color=QuantumUiSkin.Ink;border.raycastTarget=false;
        for(int i=transform.childCount-1;i>=0;i--){var c=transform.GetChild(i).gameObject;c.SetActive(false);if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);}
        markers.Clear();legendLabels.Clear();nextTypes=0;Box("Inset",Vector2.zero,new Vector2(312,224),new Color32(12,17,31,245));gameObject.SetActive(graph.nodes.Count>0);if(graph.nodes.Count==0)return;
        xs=graph.nodes.Select(n=>n.center.x).Distinct().OrderBy(x=>x).ToArray();ys=graph.nodes.Select(n=>n.center.y).Distinct().OrderBy(y=>y).ToArray();
        stepX=Mathf.Min(76,258f/Mathf.Max(1,xs.Length-1));stepY=Mathf.Min(51,145f/Mathf.Max(1,ys.Length-1));
        foreach(var edge in graph.edges)
        {
            Vector2 a=Project(graph.nodes[edge.x].center),b=Project(graph.nodes[edge.y].center);var bridge=Box("Connection",(a+b)*.5f,new Vector2(Vector2.Distance(a,b),4),new Color32(91,77,131,255));
            bridge.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
        }
        float size=Mathf.Clamp(Mathf.Min(stepX-5,stepY-5),20,38);
        foreach(var node in graph.nodes)
        {
            var go=new GameObject("Room_"+node.id,typeof(RectTransform),typeof(MapRoomIcon));go.layer=5;go.transform.SetParent(panel,false);var icon=go.GetComponent<MapRoomIcon>();icon.rectTransform.anchoredPosition=Project(node.center);icon.rectTransform.sizeDelta=Vector2.one*size;icon.raycastTarget=false;markers.Add(icon);
        }
        // ตัวผู้เล่นเป็นจุดเล็กแยกจากรูปประเภทห้อง ไม่บังรูปหัวกะโหลกหรือร้าน
        var dot=new GameObject("Player",typeof(RectTransform),typeof(Image));dot.layer=5;dot.transform.SetParent(panel,false);playerDot=(RectTransform)dot.transform;playerDot.sizeDelta=Vector2.one*7;playerDot.localRotation=Quaternion.Euler(0,0,45);dot.GetComponent<Image>().raycastTarget=false;
        Legend(hud.currencyText!=null?hud.currencyText.font:TMP_Settings.defaultFontAsset);QuantumUiSkin.MiniMap(transform);Refresh();
    }
    void Legend(TMP_FontAsset font)
    {
        var types=new[]{MapRoomGraph.RoomKind.Combat,MapRoomGraph.RoomKind.Shop,MapRoomGraph.RoomKind.Exit};var labels=LanguageSettings.IsThai?new[]{"มอน","ร้าน","วาร์ป"}:new[]{"FIGHT","SHOP","EXIT"};
        for(int i=0;i<types.Length;i++)
        {
            var go=new GameObject("LegendIcon",typeof(RectTransform),typeof(MapRoomIcon));go.layer=5;go.transform.SetParent(panel,false);var icon=go.GetComponent<MapRoomIcon>();icon.rectTransform.anchoredPosition=new Vector2(-127+i*98,-99);icon.rectTransform.sizeDelta=Vector2.one*20;icon.raycastTarget=false;icon.Configure(types[i],false,false,false,false);
            var label=new GameObject("LegendLabel",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();label.gameObject.layer=5;label.transform.SetParent(panel,false);label.rectTransform.anchoredPosition=new Vector2(-87+i*98,-99);label.rectTransform.sizeDelta=new Vector2(55,30);label.font=font;label.fontSize=15;label.text=labels[i];label.alignment=TextAlignmentOptions.Left;label.raycastTarget=false;label.color=new Color32(203,210,228,255);legendLabels.Add(label);
        }
    }
    Image Box(string name,Vector2 pos,Vector2 size,Color c)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;go.transform.SetParent(panel,false);var r=(RectTransform)go.transform;r.anchoredPosition=pos;r.sizeDelta=size;var im=go.GetComponent<Image>();im.color=c;im.raycastTarget=false;return im;
    }
    static float Rank(float value,float[] values)
    {
        if(values.Length<=1)return 0;for(int i=0;i<values.Length-1;i++)if(value<=values[i+1])return i+Mathf.InverseLerp(values[i],values[i+1],value)-(values.Length-1)*.5f;return (values.Length-1)*.5f;
    }
    // ย่อระยะด้วยลำดับพิกัดเพื่อให้ผังพอดีกรอบ แต่เส้นเชื่อมยังอิงประตูจริงจาก MapRoomGraph
    Vector2 Project(Vector2 world)=>new Vector2(Rank(world.x,xs)*stepX,Rank(world.y,ys)*stepY+8);
    void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;Refresh();}
    public void Refresh()
    {
        if(player==null||graph==null||graph.nodes.Count==0)return;int current=graph.Observe(player.position);
        if(Time.unscaledTime>=nextTypes){nextTypes=Time.unscaledTime+1;foreach(var node in graph.nodes)node.RefreshKind();}
        for(int i=0;i<markers.Count;i++)
        {
            var node=graph.nodes[i];if(node.room!=null&&node.room.HasBeenVisited)node.visited=true;
            // เปิดเผยประเภททุกห้องตั้งแต่โหลดแมพ; วงขอบและเครื่องหมายถูกบอกว่าเคยเข้า/เคลียร์แล้ว
            markers[i].Configure(node.kind,node.visited,i==current,node.room!=null&&node.room.IsCleared,node.hasExitPortal);
        }
        playerDot.anchoredPosition=Project(player.position)+new Vector2(13,-13);
    }
}
